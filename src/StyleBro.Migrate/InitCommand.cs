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

    /// <summary>The comment above the detected conventions in the block.</summary>
    public const string ConventionsHeader = "# Conventions in this repository's code (stylebro-migrate init counted them; delete a line to have it counted again)";

    /// <summary>The first line of the detected conventions in the output (the preview shows that part).</summary>
    public const string DetectedHeader = "Conventions in the code";

    /// <summary>What init says, with or without --write, when 'stylebro-migrate --write' already wrote the settings.</summary>
    public const string MigratedLine = ".editorconfig already has settings from 'stylebro-migrate --write'; they include the built-in rules.";

    /// <summary>What init says, with or without --write, when the .editorconfig already has the settings it would write.</summary>
    public const string PresentLine = ".editorconfig already has these settings: nothing to add.";

    /// <summary>The IDE rule that enforces a convention's SDK key (its severity tells whether the key is enforced).</summary>
    private static readonly Dictionary<string, string> IdeRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp_prefer_braces"] = "IDE0011",
        ["csharp_using_directive_placement"] = "IDE0065",
        ["dotnet_style_parentheses_in_arithmetic_binary_operators"] = "IDE0048",
        ["csharp_prefer_simple_default_expression"] = "IDE0034",
    };

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

        JsonReport.Set("path", root);
        JsonReport.Set("write", write);

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
            var (added, files) = Program.RewriteSuppressions(root, Suppressions.WithSonar(new Dictionary<string, SortedSet<string>>(), sonar), write);
            if (added > 0)
            {
                Console.WriteLine($"Suppressions: {added} {(write ? "added" : "to add")} for the rules that fix Sonar's in {files} files (the Sonar ones stay).");
            }
        }

        // The conventions the code follows: what the code already does stays (Ocelot: '=>' at the start of a line, '_field').
        var detected = new List<string>();
        if (!migrated)
        {
            var (lines, report) = Detect(root, ownSettings, sonarLines, PreviousConventions(existing));
            detected = lines;
            if (report.Count > 0)
            {
                Console.WriteLine($"{DetectedHeader} (judged from {Conventions.MinimumAgreeing} places: all must agree below {Conventions.MinimumSample}, {Conventions.Share * 100:0}% from {Conventions.MinimumSample} on).");
                Console.WriteLine("  off: mixed, or not what your setting says, so the rule that enforces it is turned off; both: mixed, both allowed;");
                Console.WriteLine("  mixed: the default stays, nothing is turned off (no StyleBro rule enforces it, or your .editorconfig sets the rule):");
                report.ForEach(Console.WriteLine);
                Console.WriteLine();
            }
        }

        var generated = migrated ? new List<string>() : Migration.MigrationFolders(root);
        if (generated.Count > 0)
        {
            Console.WriteLine($"EF Core migrations in {string.Join(", ", generated)}: marked generated_code = true, so formatting and StyleBro leave them alone.");
        }

        var vendored = migrated ? new List<string>() : Migration.VendoredFolders(root);
        if (vendored.Count > 0)
        {
            Console.WriteLine($"Vendored code in {string.Join(", ", vendored)}: marked generated_code = true, so formatting and StyleBro leave it alone.");
        }

        var block = Block(detected, plainHeader: own.Contains("file_header_template"), sonarLines, generated, vendored);
        var modernize = args.Contains("--modernize") ? Modernize(root, multiTargeted) : (Block: null, Notes: new List<string>());
        if (multiTargeted.Count > 0)
        {
            Console.WriteLine(MultiTargetedLine(multiTargeted));
            Console.WriteLine(MultiTargetedHint);
        }

        modernize.Notes.ForEach(Console.WriteLine);
        if (!migrated)
        {
            JsonReport.AddSettings(".editorconfig", block);
        }

        if (modernize.Block is not null)
        {
            JsonReport.AddSettings(".editorconfig", modernize.Block);
        }

        var agentsMd = args.Contains(AgentsFile.Option);

        // What --write would change: nothing when the settings are there (written by 'stylebro-migrate --write', or by an
        // earlier init). The dry run says the same as --write (mdx: it showed a block --write then said was there).
        var plain = existing?.Replace("\r\n", "\n");
        var hasBlock = migrated || (plain is not null && Migration.Apply(plain, block) == plain);
        var hasModernize = modernize.Block is null || (plain is not null && Migration.Apply(plain, modernize.Block, ModernizeBegin, ModernizeEnd) == plain);
        if (!write)
        {
            if (hasBlock)
            {
                Console.WriteLine(migrated ? MigratedLine : PresentLine);
            }

            if (!hasBlock || !hasModernize)
            {
                Console.WriteLine("== .editorconfig (run with --write to add it)");
            }

            Console.Write(hasBlock ? null : block);
            Console.Write(hasModernize ? null : modernize.Block);
            AgentsFile.Update(root, write: false, agentsMd).ForEach(Console.WriteLine);
            return 0;
        }

        var text = existing ?? "root = true\n";
        if (hasBlock)
        {
            Console.WriteLine(migrated ? MigratedLine : PresentLine);
        }
        else
        {
            text = Migration.Apply(text, block);
            Console.WriteLine("Wrote the built-in rule severities to .editorconfig.");
        }

        if (!hasModernize)
        {
            text = Migration.Apply(text, modernize.Block!, ModernizeBegin, ModernizeEnd);
            Console.WriteLine("Wrote the modernization rules to .editorconfig (see https://bisforboman.github.io/stylebro/modernizing/).");
        }

        if (text != existing)
        {
            PreviewCommand.EditFile(path, _ => text);
            if (!EnforcesCodeStyleInBuild(root))
            {
                Console.WriteLine("To report them on build too: <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> in Directory.Build.props.");
            }
        }

        AgentsFile.Update(root, write: true, agentsMd).ForEach(Console.WriteLine);
        Console.WriteLine(Program.NextHint(root));
        Program.PrintWorkspaceHint(root);
        return 0;
    }

    /// <summary>
    /// The block written into .editorconfig, between the stylebro-migrate markers. <paramref name="detected"/>: the settings
    /// for the conventions the code follows (<see cref="Detect"/>); <paramref name="plainHeader"/>: the repository has
    /// IDE0073's file_header_template, so BRO1615's XML header is off (each would add its header above the other's);
    /// <paramref name="sonar"/>: the lines a SonarQube setup adds (<see cref="SonarSetup.Apply"/>); <paramref name="generated"/>:
    /// folders marked as generated code (EF Core migrations, <see cref="Migration.MigrationFolders"/>); <paramref name="vendored"/>:
    /// vendored folders, marked the same way (<see cref="Migration.VendoredFolders"/>).
    /// </summary>
    public static string Block(IReadOnlyList<string>? detected = null, bool plainHeader = false, IReadOnlyList<string>? sonar = null, IReadOnlyList<string>? generated = null, IReadOnlyList<string>? vendored = null)
    {
        // A detected line replaces the template's line for the same key (CSharpier: IDE0055 = none).
        var keys = new HashSet<string>((detected ?? Array.Empty<string>()).Select(KeyOf).OfType<string>(), StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>();
        foreach (var line in Template().Replace("\r\n", "\n").TrimEnd('\n').Split('\n'))
        {
            if (KeyOf(line) is { } key && keys.Contains(key))
            {
                // Its comment goes with it.
                if (kept.Count > 0 && kept[^1].StartsWith('#'))
                {
                    kept.RemoveAt(kept.Count - 1);
                }
            }
            else
            {
                kept.Add(line);
            }
        }

        var template = string.Join("\n", kept);
        if (sonar is { Count: > 0 })
        {
            template += "\n" + string.Join("\n", sonar);
        }

        if (detected is { Count: > 0 })
        {
            template += "\n\n" + ConventionsHeader + "\n" + string.Join("\n", detected);
        }

        if (plainHeader)
        {
            template += $"\n# The file header is IDE0073's (file_header_template); BRO1615's XML header would add a second one\ndotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.FileHeader}.severity = none";
        }

        foreach (var folder in generated ?? Array.Empty<string>())
        {
            template += $"\n\n# EF Core migrations: generated by 'dotnet ef', so formatting and StyleBro leave them alone\n[{folder}/**]\ngenerated_code = true";
        }

        foreach (var folder in vendored ?? Array.Empty<string>())
        {
            template += $"\n\n{Migration.VendoredComment}\n[{folder}/**]\ngenerated_code = true";
        }

        return Migration.BeginMarker + " (stylebro-migrate init: built-in .NET rules for StyleBro's preset; edits inside are replaced)\n"
            + template + "\n" + Migration.EndMarker + "\n";
    }

    /// <summary>
    /// The conventions the repository's code follows (<see cref="Conventions"/>): the lines to write, and the report.
    /// Settings the root .editorconfig or the Sonar setup (<paramref name="sonarLines"/>) set win, and so does an SDK naming
    /// rule for private fields (BRO1303 follows it). <paramref name="previous"/>: the convention lines an earlier init wrote
    /// (<see cref="PreviousConventions"/>); they stay as they are, because the code was counted before StyleBro formatted
    /// it: counted again, StyleBro's own output would decide (Stateless: 28 of 29 switch sections without a blank line gave
    /// 'omit'; after a format run a second init found them mixed and turned BRO1526 off).
    /// </summary>
    public static (List<string> Lines, List<string> Report) Detect(string root, IReadOnlyDictionary<string, string> own, IReadOnlyList<string> sonarLines, IReadOnlyList<string>? previous = null)
    {
        var settings = own;
        var unenforced = Unenforced(own);
        own = own.Where(p => !unenforced.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        var csharpier = Conventions.CSharpier(root) is { } evidence ? Conventions.CSharpierLines(evidence) : default;
        var decided = new Dictionary<string, string>(StringComparer.Ordinal);
        var kept = new List<string>();
        foreach (var convention in Conventions.All)
        {
            if (new[] { convention.Key }.Concat(convention.AlsoKeys).FirstOrDefault(own.ContainsKey) is { } key)
            {
                decided[convention.Key] = $"set in .editorconfig ({own[key]})";
            }
            else if (csharpier.Lines is not null && Conventions.IsCSharpiers(convention))
            {
                decided[convention.Key] = "CSharpier formats it";
            }
            else if (sonarLines.Any(l => l.StartsWith(convention.Key + " ", StringComparison.Ordinal)))
            {
                var line = sonarLines.First(l => l.StartsWith(convention.Key + " ", StringComparison.Ordinal));
                decided[convention.Key] = $"set by the Sonar setup ({line.Substring(line.IndexOf('=') + 1).Trim()})";
            }
            else if (KeptLines(previous, [convention.Key, .. convention.AlsoKeys, .. convention.Off is { } off ? [KeyOf(off)!] : Array.Empty<string>()]) is { Count: > 0 } lines)
            {
                var setting = lines.Last();
                decided[convention.Key] = $"kept from the last init ({setting.Substring(setting.IndexOf('=') + 1).Trim()})";
                kept.AddRange(lines);
            }
        }

        if (!decided.ContainsKey(StyleBro.Analyzers.Naming.FieldNames.StyleKey) && FieldNamingRule(own) is { } rule)
        {
            decided[StyleBro.Analyzers.Naming.FieldNames.StyleKey] = $"the naming rule dotnet_naming_rule.{rule} decides";
        }

        // The using order is decided apart from the list (two keys): kept the same way.
        var usings = KeptLines(previous, [Conventions.SortUsingsKey, "dotnet_separate_import_directive_groups"]);
        kept.AddRange(usings);
        var (detected, report) = Conventions.Decide(Conventions.Count(root, settings), decided, key => own.ContainsKey(key) || sonarLines.Any(l => l.StartsWith(key + " ", StringComparison.Ordinal)) || (usings.Count > 0 && key is Conventions.SortUsingsKey or "dotnet_separate_import_directive_groups"), own, unenforced.ToDictionary(k => k, k => settings[k]));
        report.InsertRange(0, unenforced.Select(k => $"  {"note",-8} {k} = {settings[k]} in .editorconfig isn't enforced (its severity is silent or none), so your code decides"));
        if (csharpier.Lines is not null)
        {
            report.Insert(0, csharpier.Report);
        }

        return ([.. csharpier.Lines ?? [], .. kept, .. detected], report);
    }

    /// <summary>
    /// The convention keys the root .editorconfig sets without enforcing them (owner's decision 2026-10-11): severity 'silent'
    /// or 'none' after the value ('csharp_prefer_braces = true:silent', Flow.Launcher: BRO1514 then added braces in 140
    /// files), or on the IDE rule that enforces the key. Such a key is the IDE's default, not the team's choice: the code
    /// decides, like for a key it isn't set.
    /// </summary>
    public static HashSet<string> Unenforced(IReadOnlyDictionary<string, string> own)
    {
        static bool Silent(string? severity) => severity?.Trim().ToLowerInvariant() is "silent" or "none";
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in Conventions.All.SelectMany(c => new[] { c.Key }.Concat(c.AlsoKeys)).Where(own.ContainsKey))
        {
            var value = own[key];
            var colon = value.IndexOf(':');
            if ((colon >= 0 && Silent(value.Substring(colon + 1)))
                || (IdeRules.TryGetValue(key, out var ide) && own.TryGetValue($"dotnet_diagnostic.{ide}.severity", out var severity) && Silent(severity)))
            {
                result.Add(key);
            }
        }

        return result;
    }

    /// <summary>
    /// The convention lines (the part under <see cref="ConventionsHeader"/>) of the init block in an existing root
    /// .editorconfig, or none: without an init block, or one from 'stylebro-migrate --write'.
    /// </summary>
    public static List<string> PreviousConventions(string? editorConfig)
    {
        var text = editorConfig?.Replace("\r\n", "\n") ?? string.Empty;
        var begin = text.IndexOf(Migration.BeginMarker + " (stylebro-migrate init", StringComparison.Ordinal);
        var end = begin < 0 ? -1 : text.IndexOf(Migration.EndMarker, begin, StringComparison.Ordinal);
        var header = end < 0 ? -1 : text.IndexOf("# Conventions in this repository's code", begin, end - begin, StringComparison.Ordinal);
        return header < 0
            ? []
            : text.Substring(header, end - header).Split('\n').Skip(1).TakeWhile(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith('[')).ToList();
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
            notes.Add($"Modernize: the newer-C# rules (tier B) are suggestions: {Conventions.Plural(withoutLangVersion.Count, "multi-targeted project")} {(withoutLangVersion.Count == 1 ? "doesn't" : "don't")} set LangVersion ({List(withoutLangVersion)}).\n"
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

    /// <summary>
    /// The projects under the root that target several frameworks (relative paths): evaluated by MSBuild when the root has
    /// one solution or project (<see cref="FormatCommand.EvaluatedProjects"/>, the same evaluation format runs; Orleans sets them
    /// through '$(DefaultTargetFrameworks)': 9 found in the text, ~130 evaluated), else the project and props files whose text
    /// sets several (<see cref="MultiTargetedFiles"/>).
    /// </summary>
    public static IEnumerable<string> MultiTargetedProjects(string root)
    {
        if (BaselineCommand.FindWorkspace(root) is { } workspace)
        {
            try
            {
                var projects = FormatCommand.EvaluatedProjects(Path.Combine(root, workspace)).ToList();
                if (projects.Count > 0)
                {
                    return projects.Where(p => p.Frameworks.Length > 1).Select(p => Path.GetRelativePath(root, p.FullPath)).Order(StringComparer.OrdinalIgnoreCase).ToList();
                }
            }
            catch (Exception e) when (e is AggregateException or System.Text.Json.JsonException or KeyNotFoundException)
            {
                // A project MSBuild couldn't evaluate (no JSON): the text search instead.
            }
        }

        return MultiTargetedFiles(root);
    }

    /// <summary>Project and props files under the root whose text sets several target frameworks (relative paths).</summary>
    public static IEnumerable<string> MultiTargetedFiles(string root)
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
    /// Which files set several target frameworks: project files as projects, props files as props files (they aren't
    /// projects: "3 projects target several frameworks (Directory.Build.props, ...)" read wrong).
    /// </summary>
    public static string MultiTargetedLine(IReadOnlyList<string> files)
    {
        var projects = files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        var props = files.Except(projects).ToList();
        var parts = new List<string>();
        if (projects.Count > 0)
        {
            parts.Add($"{Conventions.Plural(projects.Count, "project")} {(projects.Count == 1 ? "targets" : "target")} several frameworks ({List(projects)})");
        }

        if (props.Count > 0)
        {
            parts.Add($"{Conventions.Plural(props.Count, "props file")} {(props.Count == 1 ? "sets" : "set")} several for {(projects.Count > 0 ? "other" : "the")} projects ({List(props)})");
        }

        return string.Join("; ", parts) + ".";
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

    /// <summary>Whether an MSBuild file under the root sets EnforceCodeStyleInBuild to true (then init doesn't suggest it).</summary>
    public static bool EnforcesCodeStyleInBuild(string root) =>
        StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).Any(f => Regex.IsMatch(File.ReadAllText(f), @"<EnforceCodeStyleInBuild>\s*true\s*<", RegexOptions.IgnoreCase));

    /// <summary>The previous lines that set one of the keys, each with the '# init:' comments right above it.</summary>
    private static List<string> KeptLines(IReadOnlyList<string>? previous, IReadOnlyCollection<string> keys)
    {
        var result = new List<string>();
        for (var i = 0; i < (previous?.Count ?? 0); i++)
        {
            if (KeyOf(previous![i]) is { } key && keys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                var first = i;
                while (first > 0 && previous[first - 1].StartsWith("# init:", StringComparison.Ordinal))
                {
                    first--;
                }

                result.AddRange(previous.Skip(first).Take(i - first + 1));
            }
        }

        return result;
    }

    /// <summary>The key of a 'key = value' line, or null for comments.</summary>
    private static string? KeyOf(string line)
    {
        var equals = line.IndexOf('=');
        return line.TrimStart().StartsWith('#') || equals <= 0 ? null : line.Substring(0, equals).Trim();
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
