using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate init [path] [--write] [--modernize]: puts the severities of the built-in .NET rules the preset relies
/// on into the repository's root .editorconfig. They can't live in the preset: 'dotnet format' ignores rule severities in a
/// package's global config (the build doesn't), so only .editorconfig makes it fix them. Written between the same markers
/// as 'stylebro-migrate --write', which replaces the block with settings matched to a StyleCop setup. --modernize adds a
/// second block with the SDK's modernization rules (<see cref="Modernize"/>). It looks at the repository first: the
/// conventions its code clearly follows are kept (<see cref="Detect"/>), EF Core migrations are marked as generated, and a
/// repository with a StyleCop setup is sent to 'stylebro-migrate --write' instead (owner's decisions 2026-10-09,
/// docs/decisions.md).
/// </summary>
internal static class InitCommand
{
    /// <summary>
    /// What multi-targeted repositories need to know: plain 'dotnet format' crashes on IDE0055's fix there (Roslyn's
    /// linked-file merge, measured on Serilog and Newtonsoft.Json without StyleBro); 'stylebro-migrate format' doesn't.
    /// </summary>
    public const string MultiTargetedHint =
        "Run 'stylebro-migrate format' instead of 'dotnet format' here: with IDE0055 on, 'dotnet format' crashes on multi-targeted\n"
        + "projects (Roslyn can't merge the target frameworks' copies of a file). The command runs it once per target framework.";

    /// <summary>The start marker of the --modernize block (its own block, so it survives 'stylebro-migrate --write').</summary>
    public const string ModernizeBegin = "# BEGIN stylebro-modernize";

    /// <summary>The end marker of the --modernize block.</summary>
    public const string ModernizeEnd = "# END stylebro-modernize";

    /// <summary>The first line of the detected conventions in the output (the preview shows that part).</summary>
    public const string DetectedHeader = "Conventions in the code";

    public static int Run(string[] args)
    {
        var (profile, rest) = Program.TakeOption(args, Program.SonarProfileOption);
        args = rest;
        var write = args.Contains("--write");
        var root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        var path = Path.Combine(root, ".editorconfig");
        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        var migrated = existing?.Contains(Migration.BeginMarker) == true && !existing.Contains("stylebro-migrate init");

        // A StyleCop repository gets StyleBro's rules only where StyleCop enforced them: that's the migration, not init.
        if (!migrated && StyleCopSetup.Evidence(root) is { Count: > 0 } evidence)
        {
            Console.WriteLine("This repository has a StyleCop setup:");
            evidence.ForEach(e => Console.WriteLine("  " + e));
            Console.WriteLine("Run 'stylebro-migrate --write' instead (a dry run without --write first): it turns StyleBro's rules on only where StyleCop");
            Console.WriteLine("enforced them, so code StyleCop was happy with stays as it is. 'init' would apply StyleBro's preset to all of it.");
            return 1;
        }

        if (!Program.ReadSonar(root, profile, out var sonar))
        {
            return 1;
        }

        var multiTargeted = MultiTargetedProjects(root).ToList();
        var ownSettings = Migration.OwnSettings(root);
        var own = new HashSet<string>(ownSettings.Keys, StringComparer.OrdinalIgnoreCase);

        // A Sonar setup turns on what fixes its rules, like in the migration; StyleBro's own rules are on in the preset.
        var sonarLines = new List<string>();
        if (sonar is not null && !migrated)
        {
            sonar.Report(sonar.Apply(sonarLines, _ => true, own)).ForEach(Console.WriteLine);

            // Code where the team suppressed a Sonar rule stays exempt from the rule that now fixes it.
            var (added, files) = Program.RewriteSuppressions(root, Suppressions.WithSonar(new Dictionary<string, SortedSet<string>>()), write);
            if (added > 0)
            {
                Console.WriteLine($"Suppressions: {added} {(write ? "added" : "to add")} for the rules that fix Sonar's in {files} files (the Sonar ones stay).");
            }
        }

        // The conventions the code follows: what the code already does stays (Ocelot: '=>' at the start of a line, '_field').
        var detected = new List<string>();
        if (!migrated)
        {
            var (lines, report) = Detect(root, ownSettings, sonarLines);
            detected = lines;
            if (report.Count > 0)
            {
                Console.WriteLine($"{DetectedHeader} (written when one form has {Conventions.Share * 100:0}% of at least {Conventions.MinimumSample} places):");
                report.ForEach(Console.WriteLine);
                Console.WriteLine();
            }
        }

        var generated = migrated ? new List<string>() : Migration.MigrationFolders(root);
        if (generated.Count > 0)
        {
            Console.WriteLine($"EF Core migrations in {string.Join(", ", generated)}: marked generated_code = true, so formatting and StyleBro leave them alone.");
        }

        var block = Block(detected, plainHeader: own.Contains("file_header_template"), sonarLines, generated);
        var modernize = args.Contains("--modernize") ? Modernize(root, multiTargeted) : (Block: null, Notes: new List<string>());
        if (multiTargeted.Count > 0)
        {
            Console.WriteLine($"{multiTargeted.Count} project(s) target several frameworks ({List(multiTargeted)}).");
            Console.WriteLine(MultiTargetedHint);
        }

        modernize.Notes.ForEach(Console.WriteLine);
        if (!write)
        {
            Console.WriteLine("== .editorconfig (run with --write to add it)");
            Console.Write(block);
            Console.Write(modernize.Block);
            return 0;
        }

        var text = existing ?? "root = true\n";
        if (migrated)
        {
            Console.WriteLine(".editorconfig already has settings from 'stylebro-migrate --write'; they include the built-in rules.");
        }
        else
        {
            text = Migration.Apply(text, block);
            Console.WriteLine("Wrote the built-in rule severities to .editorconfig.");
        }

        if (modernize.Block is not null)
        {
            text = Migration.Apply(text, modernize.Block, ModernizeBegin, ModernizeEnd);
            Console.WriteLine("Wrote the modernization rules to .editorconfig (see https://bisforboman.github.io/stylebro/modernizing/).");
        }

        if (text != existing)
        {
            File.WriteAllText(path, text);
            Console.WriteLine("To report them on build too: <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> in Directory.Build.props.");
        }

        Console.WriteLine(Program.FormatHint);
        Program.PrintWorkspaceHint(root);
        return 0;
    }

    /// <summary>
    /// The block written into .editorconfig, between the stylebro-migrate markers. <paramref name="detected"/>: the settings
    /// for the conventions the code follows (<see cref="Detect"/>); <paramref name="plainHeader"/>: the repository has
    /// IDE0073's file_header_template, so BRO1615's XML header is off (each would add its header above the other's);
    /// <paramref name="sonar"/>: the lines a SonarQube setup adds (<see cref="SonarSetup.Apply"/>); <paramref name="generated"/>:
    /// folders marked as generated code (EF Core migrations, <see cref="Migration.MigrationFolders"/>).
    /// </summary>
    public static string Block(IReadOnlyList<string>? detected = null, bool plainHeader = false, IReadOnlyList<string>? sonar = null, IReadOnlyList<string>? generated = null)
    {
        var template = Template().Replace("\r\n", "\n").TrimEnd('\n');
        if (sonar is { Count: > 0 })
        {
            template += "\n" + string.Join("\n", sonar);
        }

        if (detected is { Count: > 0 })
        {
            template += "\n\n# Conventions this repository's code follows (stylebro-migrate init counted them)\n" + string.Join("\n", detected);
        }

        if (plainHeader)
        {
            template += $"\n# The file header is IDE0073's (file_header_template); BRO1615's XML header would add a second one\ndotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.FileHeader}.severity = none";
        }

        foreach (var folder in generated ?? Array.Empty<string>())
        {
            template += $"\n\n# EF Core migrations: generated by 'dotnet ef', so formatting and StyleBro leave them alone\n[{folder}/**]\ngenerated_code = true";
        }

        return Migration.BeginMarker + " (stylebro-migrate init: built-in .NET rules for StyleBro's preset; edits inside are replaced)\n"
            + template + "\n" + Migration.EndMarker + "\n";
    }

    /// <summary>
    /// The conventions the repository's code follows (<see cref="Conventions"/>): the lines to write, and the report.
    /// Settings the root .editorconfig or the Sonar setup (<paramref name="sonarLines"/>) set win, and so does an SDK naming
    /// rule for private fields (BRO1303 follows it).
    /// </summary>
    public static (List<string> Lines, List<string> Report) Detect(string root, IReadOnlyDictionary<string, string> own, IReadOnlyList<string> sonarLines)
    {
        var decided = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var convention in Conventions.All)
        {
            if (new[] { convention.Key }.Concat(convention.AlsoKeys).FirstOrDefault(own.ContainsKey) is { } key)
            {
                decided[convention.Key] = $".editorconfig sets {key} = {own[key]}";
            }
            else if (sonarLines.Any(l => l.StartsWith(convention.Key + " ", StringComparison.Ordinal)))
            {
                decided[convention.Key] = "the Sonar setup sets " + sonarLines.First(l => l.StartsWith(convention.Key + " ", StringComparison.Ordinal));
            }
        }

        if (!decided.ContainsKey(StyleBro.Analyzers.Naming.FieldNames.StyleKey) && FieldNamingRule(own) is { } rule)
        {
            decided[StyleBro.Analyzers.Naming.FieldNames.StyleKey] = $"the naming rule dotnet_naming_rule.{rule} decides";
        }

        return Conventions.Decide(Conventions.Count(root), decided);
    }

    /// <summary>
    /// The SDK naming rule BRO1303 follows (like the analyzer): one whose symbols are fields (or '*'), private (or '*') with
    /// no required modifiers, not turned off, with a camel case style without a prefix or with '_'. Null without one: rules
    /// for interfaces, constants or static fields don't decide the private field style.
    /// </summary>
    public static string? FieldNamingRule(IReadOnlyDictionary<string, string> settings)
    {
        string? Get(string key) => settings.TryGetValue(key, out var v) ? v.Split(':')[0].Trim() : null;
        static bool Has(string? list, string item) => list is null || list.Split(',').Select(p => p.Trim()).Any(p => p == "*" || p.Equals(item, StringComparison.OrdinalIgnoreCase));

        foreach (var key in settings.Keys.Where(k => k.StartsWith("dotnet_naming_rule.", StringComparison.OrdinalIgnoreCase) && k.EndsWith(".symbols", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal))
        {
            var rule = key.Substring("dotnet_naming_rule.".Length, key.Length - "dotnet_naming_rule.".Length - ".symbols".Length);
            var symbols = Get(key);
            var style = Get($"dotnet_naming_rule.{rule}.style");
            if (symbols is not null && style is not null && Get($"dotnet_naming_rule.{rule}.severity") is not ("none" or "silent")
                && Has(Get($"dotnet_naming_symbols.{symbols}.applicable_kinds"), "field")
                && Has(Get($"dotnet_naming_symbols.{symbols}.applicable_accessibilities"), "private")
                && string.IsNullOrWhiteSpace(Get($"dotnet_naming_symbols.{symbols}.required_modifiers"))
                && Get($"dotnet_naming_style.{style}.capitalization") == "camel_case"
                && string.IsNullOrEmpty(Get($"dotnet_naming_style.{style}.required_suffix"))
                && string.IsNullOrEmpty(Get($"dotnet_naming_style.{style}.word_separator"))
                && (Get($"dotnet_naming_style.{style}.required_prefix") ?? string.Empty) is "" or "_")
            {
                return rule;
            }
        }

        return null;
    }

    /// <summary>
    /// The --modernize block: the SDK's rules that rewrite code into newer C# (tier B) and newer APIs (tier C), at warning
    /// where that's safe, else at suggestion with a note why. In a multi-targeted project the rules fire per target
    /// framework and 'dotnet format' writes the newer framework's edit into the shared file: the language rules break the
    /// older framework's build unless LangVersion is set (else each framework gets its own C# version). The API rules would
    /// too, but StyleBro's MultiTargetSuppressor hides them there, so they are always warnings. Keys the root .editorconfig
    /// sets itself are left out.
    /// </summary>
    public static (string? Block, List<string> Notes) Modernize(string root, IReadOnlyList<string> multiTargeted)
    {
        var withoutLangVersion = multiTargeted.Where(p => !SetsLangVersion(root, p)).ToList();
        var notes = new List<string>();
        if (withoutLangVersion.Count > 0)
        {
            notes.Add($"Modernize: the newer-C# rules (tier B) are suggestions: {withoutLangVersion.Count} multi-targeted project(s) don't set LangVersion ({List(withoutLangVersion)}).\n"
                + "  Each target framework then gets its own C# version (7.3 for .NET Framework and netstandard2.0); fixes made for the newer one break the older one.\n"
                + "  To turn them on: set <LangVersion>latest</LangVersion> (or a fixed version) in those projects or Directory.Build.props, then run 'stylebro-migrate init --modernize --write' again.");
        }

        if (multiTargeted.Count > 0)
        {
            notes.Add("Modernize: the newer-API rules (tier C) are warnings; in multi-targeted projects StyleBro's multi-target guard hides them where a target framework lacks the API\n"
                + "  (e.g. ArgumentNullException.ThrowIfNull in a net48;net8.0 project). The guard comes with the StyleBro.Analyzers package: projects without it would get fixes that break their older framework.");
        }

        var own = Migration.OwnKeys(root);
        var lines = ModernizeTemplate().Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
            .Select(l => l.Replace("{lang}", withoutLangVersion.Count == 0 ? "warning" : "suggestion"))
            .Where(l => l.StartsWith('#') || !l.Contains('=') || !own.Contains(l.Substring(0, l.IndexOf('=')).Trim()));
        var block = ModernizeBegin + " (stylebro-migrate init --modernize: built-in .NET modernization rules; edits inside are replaced)\n"
            + string.Join("\n", lines) + "\n" + ModernizeEnd + "\n";
        return (block, notes);
    }

    /// <summary>The template (sdk-rules.editorconfig, embedded).</summary>
    public static string Template() => Read("SdkRules.editorconfig");

    /// <summary>The modernization template (modernize-rules.editorconfig, embedded), with {lang}/{api} severities.</summary>
    public static string ModernizeTemplate() => Read("ModernizeRules.editorconfig");

    /// <summary>Project files under the root that set several target frameworks (relative paths).</summary>
    public static IEnumerable<string> MultiTargetedProjects(string root)
    {
        var several = new Regex("<TargetFrameworks>[^<]*;[^<]*</TargetFrameworks>", RegexOptions.IgnoreCase);
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || f.EndsWith(".props", StringComparison.OrdinalIgnoreCase)))
        {
            if (several.IsMatch(File.ReadAllText(file)))
            {
                yield return Path.GetRelativePath(root, file);
            }
        }
    }

    /// <summary>
    /// Whether a project (or .props) file under the root sets LangVersion, itself or in the Directory.Build.props it
    /// imports (the nearest one above it, and further up while each mentions its parent's).
    /// </summary>
    public static bool SetsLangVersion(string root, string relativePath)
    {
        // ponytail: text match, ignores conditions and custom imports; a wrong "no" only means suggestions instead of warnings.
        var langVersion = new Regex(@"<LangVersion>\s*[^<\s]", RegexOptions.IgnoreCase);
        string? file = Path.Combine(root, relativePath);
        var directory = Path.GetDirectoryName(file);
        while (file is not null)
        {
            var text = File.ReadAllText(file);
            if (langVersion.IsMatch(text))
            {
                return true;
            }

            if (Path.GetFileName(file).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
            {
                if (!text.Contains("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                directory = Path.GetDirectoryName(directory);
            }

            file = null;
            for (; directory is not null && directory.Length >= root.TrimEnd('\\', '/').Length; directory = Path.GetDirectoryName(directory))
            {
                var candidate = Path.Combine(directory, "Directory.Build.props");
                if (File.Exists(candidate))
                {
                    file = candidate;
                    break;
                }
            }
        }

        return false;
    }

    private static string List(IReadOnlyList<string> items) => string.Join(", ", items.Take(3)) + (items.Count > 3 ? ", ..." : string.Empty);

    private static string Read(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"{resource} is missing from the tool.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
