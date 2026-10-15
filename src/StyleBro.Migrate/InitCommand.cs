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

    /// <summary>
    /// The start marker of the block init writes into an .editorconfig below the root: the whitespace keys it decides itself
    /// (<see cref="NestedWhitespace"/>) and rules set by id (<see cref="NestedHiddenFromFormat"/>); its own marker, so 'format'
    /// and the migration don't take it for the main block. Called 'stylebro-whitespace' before trial 20 (<see cref="LegacyWhitespaceBegin"/>).
    /// </summary>
    public const string WhitespaceBegin = "# BEGIN stylebro-folder";

    /// <summary>The end marker of the folder block.</summary>
    public const string WhitespaceEnd = "# END stylebro-folder";

    /// <summary>The folder block's old start marker: read and replaced.</summary>
    public const string LegacyWhitespaceBegin = "# BEGIN stylebro-whitespace";

    /// <summary>The folder block's old end marker.</summary>
    public const string LegacyWhitespaceEnd = "# END stylebro-whitespace";

    /// <summary>The comment above the detected conventions in the block.</summary>
    public const string ConventionsHeader = "# Conventions in this repository's code (stylebro-migrate init counted them; delete a line to have it counted again)";

    /// <summary>The first line of the detected conventions in the output (the preview shows that part).</summary>
    public const string DetectedHeader = "Conventions in the code";

    /// <summary>What init says, with or without --write, when 'stylebro-migrate --write' already wrote the settings.</summary>
    public const string MigratedLine = ".editorconfig already has settings from 'stylebro-migrate --write'; they include the built-in rules.";

    /// <summary>What init says, with or without --write, when the .editorconfig already has the settings it would write.</summary>
    public const string PresentLine = ".editorconfig already has these settings: nothing to add.";

    /// <summary>The comment above StyleBro's rules set by id (<see cref="HiddenFromFormat"/>).</summary>
    public const string ByIdComment =
        "# StyleBro's rules by id: this file's bulk severity (dotnet_analyzer_diagnostic.severity or a category's) would hide them from\n"
        + "# 'dotnet format', while the build reports them (the preset's own entries win over a bulk one). Turn one off below this block.";

    /// <summary>The template's key that turns IDE0009 ('this.' required) off.</summary>
    public const string ThisQualificationSeverityKey = "dotnet_diagnostic.IDE0009.severity";

    /// <summary>The comment above the lines that turn rules off in a folder that turns every analyzer off (<see cref="OptOutLines"/>).</summary>
    public const string OptOutComment =
        "# This folder turns every analyzer off (dotnet_analyzer_diagnostic.severity), but a rule's own entry wins over that (StyleBro's\n"
        + "# preset in the build, the block above in 'dotnet format'): StyleBro's rules and the built-in ones init sets, off by id.";

    /// <summary>The comment above the lines that turn the built-in rules off in a folder that turns the Style category off (<see cref="NestedStyleOff"/>).</summary>
    public const string StyleOffComment =
        "# This folder turns the Style category off (dotnet_analyzer_diagnostic.category-Style.severity), but a rule's own entry wins\n"
        + "# over that: the built-in rules the block above turns on by id, off here by id too.";

    /// <summary>What the header of <see cref="OptOutBlock"/> says.</summary>
    private const string OptOutHeader = "stylebro-migrate init: rules off in a folder that turns every analyzer off";

    /// <summary>IDE0055's severity key.</summary>
    private const string IdeSeverityKey = "dotnet_diagnostic.IDE0055.severity";

    /// <summary>The line that turns IDE0055 off.</summary>
    private const string IdeOff = IdeSeverityKey + " = none";

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

        // --project names the solution or project format will run on (relative to the path), as for the preview and format.
        (var workspace, args) = Program.TakeOption(rest, "--project");
        var write = args.Contains("--write");
        var root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        if (workspace is { Length: > 0 } && !File.Exists(Path.Combine(root, workspace)))
        {
            Console.Error.WriteLine($"Not found: {Path.Combine(root, workspace)} (--project is relative to {root})");
            return 1;
        }

        root = Program.RepositoryFolder(root, ref workspace);
        JsonReport.Set("path", root);
        JsonReport.Set("write", write);

        var path = Path.Combine(root, ".editorconfig");
        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        var migrated = existing?.Contains(Migration.BeginMarker) == true && !existing.Contains("stylebro-migrate init");

        // A StyleCop repository gets StyleBro's rules only where StyleCop enforced them: that's the migration, not init.
        // Settings StyleCop.Analyzers no longer reads (Spectre.Console dropped the package, kept its 'sa1101 = none' lines)
        // describe nothing: init proceeds and follows the code (docs/decisions/2026-10-13-stylecop-settings-without-the-stylecop-package-are-leftovers.md).
        var evidence = migrated ? new List<string>() : StyleCopSetup.Evidence(root);
        if (evidence.Count > 0 && !StyleCopSetup.IsInstalled(root))
        {
            Console.WriteLine(Program.LeftoverLine(evidence) + " init ignores them and follows the code's own conventions; delete them when convenient.");
        }
        else if (evidence.Count > 0)
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

        var multiTargeted = MultiTargetedProjects(root, workspace).ToList();
        var own = Migration.OwnKeys(root);

        // A Sonar setup turns on what fixes its rules, like in the migration; StyleBro's own rules are on in the preset.
        var sonarLines = new List<string>();
        var sonarScopes = new List<(string Section, List<string> Lines)>();
        if (sonar is not null && !migrated)
        {
            var applied = sonar.Apply(sonarLines, _ => true, own);
            var optionScopes = Suppressions.OptionScopes(root, sonar, sonarLines);
            Suppressions.Annotate(sonar.Report(applied), optionScopes).ForEach(Console.WriteLine);
            sonarScopes = [.. SonarScopes(root, sonar, sonarLines, own), .. optionScopes];

            // Code where the team suppressed a Sonar rule stays exempt from the rule that now fixes it.
            var (added, files, _) = Program.RewriteSuppressions(root, Suppressions.WithSonar(new Dictionary<string, SortedSet<string>>(), sonar), write);
            if (added > 0)
            {
                Console.WriteLine($"Suppressions: {added} {(write ? "added" : "to add")} for the rules that fix Sonar's in {files} files (the Sonar ones stay).");
            }
        }

        // The root, then every folder with C# code whose .editorconfig says 'root = true': the root's block never reaches it.
        // Without an .editorconfig of its own and C# code it would reach, the root gets none: the starter kit's code is all
        // under src/, whose .editorconfig says root = true, and init created a root one nothing read.
        var modernizing = args.Contains("--modernize");
        var folders = StyleCopSetup.EditorConfigRoots(root).Where(f => StyleCopSetup.EnumerateFiles(Path.Combine(root, f), ownRoot: true).Any(IsCSharp)).ToList();
        var skipRoot = existing is null && folders.Count > 0 && !StyleCopSetup.EnumerateFiles(root, ownRoot: true).Any(IsCSharp);
        var targets = skipRoot ? new List<Target>() : [Prepare(root, string.Empty, sonarLines, multiTargeted, modernizing, sonarScopes)];
        if (skipRoot)
        {
            Console.WriteLine($"No C# code outside {string.Join(", ", folders.Select(f => f + "/"))}, whose .editorconfig says root = true: no root .editorconfig is written.");
        }

        if (multiTargeted.Count > 0)
        {
            Console.WriteLine(MultiTargetedLine(multiTargeted));

            // Not where IDE0055 is off (CSharpier formats the code: sharpcompress got the crash warning anyway).
            if (targets.FirstOrDefault()?.FormatsWhitespace != false)
            {
                Console.WriteLine(MultiTargetedHint);
            }
        }

        targets.FirstOrDefault()?.ModernizeNotes.ForEach(Console.WriteLine);
        foreach (var folder in folders)
        {
            Console.WriteLine();
            if (!OptsOut(Migration.OwnSettings(Path.Combine(root, folder))))
            {
                Console.WriteLine($"{folder}/.editorconfig says root = true, so the root's settings don't reach {folder}/: it gets a block of its own, from its own code.");
            }

            targets.Add(Prepare(root, folder, sonarLines, multiTargeted, modernizing));
            if (targets.Count == 1)
            {
                targets[0].ModernizeNotes.ForEach(Console.WriteLine);
            }
        }

        var agentsMd = args.Contains(AgentsFile.Option);
        targets = targets.SelectMany(t => t.Nested.Prepend(t)).ToList();
        if (!write)
        {
            foreach (var target in targets)
            {
                if (target.HasBlock)
                {
                    Console.WriteLine(target.Prefix + (target.Migrated ? MigratedLine : PresentLine));
                }

                if (target.Block.Length == 0)
                {
                    Console.WriteLine($"== {target.Name}: its stylebro-folder block is no longer needed (run with --write to remove it)");
                }
                else if (!target.HasBlock || !target.HasModernize)
                {
                    Console.WriteLine($"== {target.Name} (run with --write to add it)");
                }

                Console.Write(target.HasBlock ? null : target.Block);
                Console.Write(target.HasModernize ? null : target.Modernize);
            }

            PinPreset(root, write: false);
            AgentsFile.Update(root, write: false, agentsMd).ForEach(Console.WriteLine);
            return 0;
        }

        var changed = false;
        foreach (var target in targets)
        {
            var text = target.Existing ?? "root = true\n";
            if (target.HasBlock)
            {
                Console.WriteLine(target.Prefix + (target.Migrated ? MigratedLine : PresentLine));
            }
            else if (target.Block.Length == 0)
            {
                text = RemoveWhitespaceBlock(text);
                Console.WriteLine($"Removed the stylebro-folder block from {target.Name}: nothing needs it any more.");
            }
            else
            {
                text = Migration.Apply(target.Begin == WhitespaceBegin ? RemoveWhitespaceBlock(text, legacyOnly: true) : text, target.Block, target.Begin, target.End);
                Console.WriteLine(WroteLine(target.Name, target.Block, target.Begin == WhitespaceBegin));
            }

            if (!target.HasModernize)
            {
                text = Migration.Apply(text, target.Modernize!, ModernizeBegin, ModernizeEnd);
                Console.WriteLine($"Wrote the modernization rules to {target.Name} (see https://bisforboman.github.io/stylebro/modernizing/).");
            }

            if (text != target.Existing)
            {
                PreviewCommand.EditFile(target.Path, _ => text);
                changed = true;
            }
        }

        PinPreset(root, write: true);
        if (changed && !EnforcesCodeStyleInBuild(root))
        {
            // The file the projects import (Moonglade has only src/Directory.Build.props).
            var props = string.Join(", ", Migration.PropsFiles(root).Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')));
            Console.WriteLine($"To report them on build too: <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild> in {props}.");
        }

        AgentsFile.Update(root, write: true, agentsMd).ForEach(Console.WriteLine);
        Console.WriteLine(Program.NextHint(root));
        Program.PrintWorkspaceHint(root, workspace);
        return 0;
    }

    /// <summary>
    /// The block written into .editorconfig, between the stylebro-migrate markers. <paramref name="detected"/>: the settings
    /// for the conventions the code follows (<see cref="Detect"/>); <paramref name="plainHeader"/>: the repository has
    /// IDE0073's file_header_template, so BRO1615's XML header is off (each would add its header above the other's);
    /// <paramref name="sonar"/>: the lines a SonarQube setup adds (<see cref="SonarSetup.Apply"/>); <paramref name="generated"/>:
    /// folders marked as generated code (EF Core migrations, <see cref="Migration.MigrationFolders"/>); <paramref name="vendored"/>:
    /// vendored folders, marked the same way (<see cref="Migration.VendoredFolders"/>); <paramref name="sonarScopes"/>: sections
    /// for the parts of the repository with their own Sonar severities (<see cref="SonarScopes"/>); <paramref name="byId"/>: StyleBro's
    /// rules by id, where a bulk severity would hide them from 'dotnet format' (<see cref="HiddenFromFormat"/>);
    /// <paramref name="leftOut"/>: template keys the repository's own setting decides (<see cref="RequiresThisQualification"/>).
    /// </summary>
    public static string Block(IReadOnlyList<string>? detected = null, bool plainHeader = false, IReadOnlyList<string>? sonar = null, IReadOnlyList<string>? generated = null, IReadOnlyList<string>? vendored = null, IReadOnlyList<(string Section, List<string> Lines)>? sonarScopes = null, IReadOnlyList<string>? byId = null, IReadOnlyList<string>? leftOut = null)
    {
        // A detected line replaces the template's line for the same key (CSharpier: IDE0055 = none).
        var keys = new HashSet<string>((detected ?? Array.Empty<string>()).Select(KeyOf).OfType<string>().Concat(leftOut ?? []), StringComparer.OrdinalIgnoreCase);
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

        if (byId is { Count: > 0 })
        {
            template += "\n\n" + ByIdComment + "\n" + string.Join("\n", byId);
        }

        if (detected is { Count: > 0 })
        {
            template += "\n\n" + ConventionsHeader + "\n" + string.Join("\n", detected);
        }

        if (plainHeader)
        {
            template += $"\n# The file header is IDE0073's (file_header_template); BRO1615's XML header would add a second one\ndotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.FileHeader}.severity = none";
        }

        foreach (var (section, lines) in sonarScopes ?? [])
        {
            template += $"\n\n[{section}]\n" + string.Join("\n", lines);
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

    /// <summary>The block for a 'root = true' folder that turns every analyzer off (<see cref="OptOutLines"/>).</summary>
    public static string OptOutBlock(IReadOnlyList<string> lines) =>
        Migration.BeginMarker + " (" + OptOutHeader + "; edits inside are replaced)\n[*.cs]\n"
        + "# This folder turns every analyzer off (dotnet_analyzer_diagnostic.severity), but StyleBro's preset sets its rules by id, which wins\n"
        + "# in the build: StyleBro's rules and the built-in ones init sets, off by id, so the build and 'dotnet format' leave it alone.\n"
        + string.Join("\n", lines) + "\n" + Migration.EndMarker + "\n";

    /// <summary>
    /// The conventions the repository's code follows (<see cref="Conventions"/>): the lines to write, and the report.
    /// Settings the root .editorconfig or the Sonar setup (<paramref name="sonarLines"/>) set win, and so does an SDK naming
    /// rule for private fields (BRO1303 follows it). <paramref name="previous"/>: the convention lines an earlier init wrote
    /// (<see cref="PreviousConventions"/>); they stay as they are, because the code was counted before StyleBro formatted
    /// it: counted again, StyleBro's own output would decide (Stateless: 28 of 29 switch sections without a blank line gave
    /// 'omit'; after a format run a second init found them mixed and turned BRO1526 off). Scopes: the section for the folder
    /// CSharpier formats when it formats only part of the code.
    /// </summary>
    public static (List<string> Lines, List<string> Report, Dictionary<string, List<string>> Nested, List<(string Section, List<string> Lines)> Scopes) Detect(string root, IReadOnlyDictionary<string, string> own, IReadOnlyList<string> sonarLines, IReadOnlyList<string>? previous = null)
    {
        var settings = own;
        var unenforced = Unenforced(own);
        var counts = Conventions.Count(root, settings);

        // The whitespace keys per file, from the .editorconfig that decides each (the nearest that sets it): this block only
        // reaches the files nothing nearer decides (Bogus's Source/.editorconfig says insert_final_newline = false).
        var fileSettings = Conventions.FileSettings(root);
        Conventions.FileSettingGroup? RootGroup(string key) => fileSettings.FirstOrDefault(g => g.Key == key && g.Source == string.Empty && g.Value is not null && settings.ContainsKey(key));
        foreach (var group in fileSettings.Where(g => g.Key == Conventions.EndOfLineKey && g.Source is { Length: > 0 }))
        {
            foreach (var (key, values) in group.Counts.Where(p => p.Key.StartsWith(Conventions.EndOfLineKey, StringComparison.Ordinal)))
            {
                counts[key][0] -= values[0];
                counts[key][1] -= values[1];
            }
        }

        // The repository's end_of_line against git's checkout conversion: endings stay out of it, as in the migration.
        var rootEnding = RootGroup(Conventions.EndOfLineKey);
        var gitEndings = rootEnding is null ? null : Conventions.EndingContradiction(rootEnding.Value!, rootEnding.PickedPerMachine);

        // An end_of_line the committed files clearly contradict was never enforced either: the files decide.
        var contradictedEnding = gitEndings is null && rootEnding is not null ? Conventions.ContradictedEnding(rootEnding.Counts, settings) : null;
        if (contradictedEnding is not null)
        {
            unenforced.Add(Conventions.EndOfLineKey);
        }

        own = own.Where(p => !unenforced.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);

        // CSharpier formats all the code, or only a folder: then its settings go into that folder's section, and the rest of
        // the code is judged as usual.
        var csharpierScopes = Conventions.CSharpierScopes(root);
        var csharpier = csharpierScopes is [({ } evidence, "")] ? Conventions.CSharpierLines(evidence, root, settings) : default;
        var scopes = new List<(string Section, List<string> Lines)>();
        var partReports = new List<string>();
        foreach (var (partEvidence, part) in csharpierScopes.Where(s => s.Folder.Length > 0))
        {
            var (partLines, partReport) = Conventions.CSharpierLines(partEvidence, root, settings, part);
            scopes.Add(($"{part}/**.cs", partLines));
            partReports.Add(partReport.Replace("CSharpier (", $"CSharpier in {part}/ (", StringComparison.Ordinal));
        }

        // ReSharper/Rider's column alignment: IDE0055 off, like with CSharpier (RestSharp: 414 aligned hunks flattened).
        var alignment = csharpier.Lines is null ? Conventions.AlignmentLines(settings) : null;
        var decided = new Dictionary<string, string>(StringComparer.Ordinal);
        var kept = new List<string>();
        foreach (var convention in Conventions.All)
        {
            if (csharpier.Lines is not null && Conventions.IsCSharpiers(convention))
            {
                // Before the repository's key: its rule is off with CSharpier anyway (a second 'off' line moved on every init).
                decided[convention.Key] = "CSharpier formats it";
            }
            else if (new[] { convention.Key }.Concat(convention.AlsoKeys).FirstOrDefault(own.ContainsKey) is { } key)
            {
                // A rule the last init turned off against this key (the code contradicted it, or was mixed) stays off: counted
                // again after 'format', the numbers move (sourcegit: 263 operators, then 264, and the block changed). Also with
                // CSharpier (its own rules are decided above): recomputed, sharpcompress' BRO1406 line moved on a second init.
                if (convention.Enforcer is { } enforcer && KeptLines(previous, [$"dotnet_diagnostic.{enforcer}.severity"]) is { Count: > 0 } offLines)
                {
                    decided[convention.Key] = $"kept from the last init ({enforcer} off; .editorconfig says {own[key]})";
                    kept.AddRange(offLines);
                }
                else
                {
                    decided[convention.Key] = $"set in .editorconfig ({own[key]})";
                }
            }
            else if (sonarLines.Any(l => l.StartsWith(convention.Key + " ", StringComparison.Ordinal)))
            {
                var line = sonarLines.First(l => l.StartsWith(convention.Key + " ", StringComparison.Ordinal));
                decided[convention.Key] = $"set by the Sonar setup ({line.Substring(line.IndexOf('=') + 1).Trim()})";
            }
            else if (KeptLines(previous, [convention.Key, .. convention.AlsoKeys, .. convention.Off is { } off ? [KeyOf(off)!] : Array.Empty<string>()], SharesOff(convention) ? convention.What : null) is { Count: > 0 } lines)
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

        if (!decided.ContainsKey(Conventions.StaticPrefixKey) && Conventions.PrefixRequiredByNamingRule(own) is { } prefix)
        {
            decided[Conventions.StaticPrefixKey] = $"a naming rule requires '{prefix}', which BRO1307 keeps";
        }

        // The casing of private constants and static readonly fields (three forms, decided apart from the list too).
        var staticCasing = KeptLines(previous, [Conventions.StaticCasingKey, $"dotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.FieldPascalCase}.severity"]);
        if (own.TryGetValue(Conventions.StaticCasingKey, out var staticValue))
        {
            decided[Conventions.StaticCasingKey] = $"set in .editorconfig ({staticValue})";
        }
        else if (!NamingRulesOff(own) && StyleBro.Analyzers.Naming.FieldNames.GetStyles(new Conventions.Options(own)) is { Constant: not null } or { StaticReadOnly: not null })
        {
            decided[Conventions.StaticCasingKey] = "a dotnet_naming_rule decides";
        }
        else if (staticCasing.Count > 0)
        {
            decided[Conventions.StaticCasingKey] = $"kept from the last init ({staticCasing[^1].Substring(staticCasing[^1].IndexOf('=') + 1).Trim()})";
        }

        // The using order is decided apart from the list (two keys): kept the same way.
        // Not the keys the CSharpier lines write again (CSharpier's using order).
        // With CSharpier, no sort key unless the repository sets one: CSharpier sorts usings itself, and 'dotnet format' sorts
        // whenever a key is set, with another comparer (tusdotnet: 'App3._0' and 'App3_0' swapped back and forth).
        var usings = csharpier.Lines is not null ? [] : KeptLines(previous, [Conventions.SortUsingsKey, "dotnet_separate_import_directive_groups"]);
        if (csharpier.Lines is not null)
        {
            decided[Conventions.SortUsingsKey] = "CSharpier sorts them";
        }

        kept.AddRange(usings);
        var usingsAt = kept.Count;

        // In the order a first run writes them (Decide: the list, the using order, the static casing, the spacing), so a
        // second init that keeps them all writes the same block (EntityGraphQL's swapped two lines).
        kept.AddRange(staticCasing);

        // IDE0055's spacing: another formatter's, the team's, or the last init's call.
        var keptSpacing = KeptSpacing(previous);
        kept.AddRange(keptSpacing);
        var keptKeys = new HashSet<string>(kept.Select(KeyOf).OfType<string>(), StringComparer.OrdinalIgnoreCase);
        var spacing = csharpier.Lines is not null ? "CSharpier formats it"
            : alignment is not null ? "IDE0055 is off for column alignment"
            : settings.TryGetValue("dotnet_diagnostic.IDE0055.severity", out var ideSeverity) ? $"your .editorconfig sets IDE0055 ({ideSeverity})"
            : keptSpacing.Count > 0 ? "kept from the last init"
            : null;
        var (detected, report) = Conventions.Decide(counts, decided, key => own.ContainsKey(key) || keptKeys.Contains(key) || sonarLines.Any(l => l.StartsWith(key + " ", StringComparison.Ordinal)) || (usings.Count > 0 && key is Conventions.SortUsingsKey or "dotnet_separate_import_directive_groups"), csharpier.Lines is null ? own : own.Where(p => !Conventions.All.Any(c => Conventions.IsCSharpiers(c) && (c.Key == p.Key || c.AlsoKeys.Contains(p.Key)))).ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase), unenforced.ToDictionary(k => k, k => settings[k]), spacing);

        // BRO1302 has no switch for primary-constructor parameters alone: say what turning it off covers.
        if (detected.FindIndex(l => l.StartsWith("# init: primary-constructor parameter names:", StringComparison.Ordinal)) is >= 0 and var primary)
        {
            const string Whole = "BRO1302 has no switch for primary-constructor parameters alone, so it's off for every parameter";
            detected[primary] = detected[primary].Replace("so BRO1302 is off.", $"so BRO1302 is off ({Whole}).", StringComparison.Ordinal);
            report.Add($"  {"note",-8} {Whole}");
        }

        // 'always' asks for 'public' on interface members too: only when the code has them or the build enforces it (BaGetter):
        // IDE0040 a warning or an error by id, category or bulk (trial 20: 'always' alone, or ':suggestion', reported nothing).
        if (Conventions.InterfaceAccess(counts, settings, EnforcesCodeStyleInBuild(root) && IsEnforced(settings, "IDE0040"), EnforcesCodeStyleInBuild(root)) is var (interfaceLines, interfaceReport))
        {
            detected.AddRange(interfaceLines);
            report.Insert(report.FindIndex(l => l.Contains(" private-by-default members: ", StringComparison.Ordinal)) + 1, interfaceReport);
        }

        // A using verdict without a setting ("they aren't sorted") goes where the last init wrote it, before the kept static
        // casing: appended after it, a second init wrote the block in another order (Fluid).
        var usingsComment = $"# init: {Conventions.UsingsWhat}:";
        if (usings.Count == 0 && previous?.Any(l => l.StartsWith(usingsComment, StringComparison.Ordinal)) == true
            && detected.FindIndex(l => l.StartsWith(usingsComment, StringComparison.Ordinal)) is >= 0 and var at
            && (at + 1 == detected.Count || detected[at + 1].StartsWith('#')))
        {
            kept.Insert(usingsAt, detected[at]);
            detected.RemoveAt(at);
        }

        // Whether IDE0055 formats whitespace at all (not with CSharpier, column alignment or mixed spacing).
        var formatsWhitespace = !(csharpier.Lines ?? []).Concat(alignment?.Lines ?? []).Concat(keptSpacing).Concat(detected).Contains(IdeOff);
        report.InsertRange(0, unenforced.Select(k => k == Conventions.EndOfLineKey
            ? $"  {"note",-8} {contradictedEnding!.Value.Note}"
            : $"  {"note",-8} {k} = {settings[k]} in .editorconfig isn't enforced (its severity is silent or none), so your code decides"));

        // A folder with too little code to judge (a root = true folder with a sample or two): StyleBro's defaults, said once.
        if (Conventions.All.All(c => !counts.TryGetValue(c.Key, out var count) || count.Sum() < Conventions.MinimumAgreeing))
        {
            report.Insert(0, $"  {"note",-8} too little code here to tell its conventions (fewer than {Conventions.MinimumAgreeing} places each), so StyleBro's defaults apply");
        }

        report.InsertRange(0, csharpier.Lines is not null ? [csharpier.Report] : partReports);

        if (alignment is var (_, alignmentReport))
        {
            report.Insert(0, alignmentReport);
        }

        // CSharpier (IDE0055 off) or the team's own IDE0055 severity keep theirs.
        var endings = new List<string>();
        if (gitEndings is ({ } endingLines, { } endingReport))
        {
            report.Add($"  {"note",-8} {endingReport}");
            endings.AddRange(endingLines.Take(formatsWhitespace && !settings.ContainsKey("dotnet_diagnostic.IDE0055.severity") ? 3 : 2));
        }

        if (Conventions.EmptyHeaderLines(counts, own) is { } headerLines)
        {
            report.Add($"  {"off",-8} {headerLines[0].Substring("# init: ".Length)}");
            endings.AddRange(headerLines);
        }

        // A charset or final line break the files don't follow wasn't enforced either: the files decide, like the line endings.
        // What the last init wrote stays: counted again after 'format', the files are StyleBro's output.
        foreach (var key in new[] { Conventions.CharsetKey, Conventions.FinalNewlineKey })
        {
            if (RootGroup(key) is not { } group)
            {
                continue;
            }

            if (KeptLines(previous, [key]) is { Count: > 0 } keptLines)
            {
                report.Add($"  {"note",-8} {key}: kept from the last init ({keptLines[^1].Substring(keptLines[^1].IndexOf('=') + 1).Trim()})");
                endings.AddRange(keptLines);
            }
            else if (Conventions.ContradictedFileSetting(group.Counts, settings, key) is var (fileLine, fileNote))
            {
                report.Add($"  {"note",-8} {fileNote}");
                endings.Add($"# init: {fileNote}");
                endings.Add(fileLine);
            }
        }

        if (formatsWhitespace && RootGroup(Conventions.TrailingWhitespaceKey) is { } trailing && Conventions.TrailingWhitespaceNote(trailing.Counts, settings) is { } whitespaceNote)
        {
            report.Add($"  {"note",-8} {whitespaceNote}");
        }

        var nested = NestedWhitespace(root, fileSettings, report, formatsWhitespace && !settings.ContainsKey("dotnet_diagnostic.IDE0055.severity"), formatsWhitespace);
        return (WithoutRepeats([.. csharpier.Lines ?? [], .. alignment?.Lines ?? [], .. kept, .. detected, .. endings]), report, nested, scopes);
    }

    /// <summary>
    /// The lines for the .editorconfig files below the root that decide a whitespace key for C# files themselves (not 'root =
    /// true': those get a block of their own), per file (folder relative to the root): the same decisions as for the root's
    /// keys, written where they take effect, since a nearer file wins over any line in the root's block (Bogus: the root
    /// block's 'insert_final_newline = unset' never reached Source/, and 'format' stripped 48 files' last line break). What
    /// the last init wrote there stays. <paramref name="ideSeverity"/>: IDE0055 can be lowered for line endings git converts.
    /// </summary>
    public static Dictionary<string, List<string>> NestedWhitespace(string root, IReadOnlyList<Conventions.FileSettingGroup> fileSettings, List<string> report, bool ideSeverity, bool formatsWhitespace)
    {
        var nested = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        // ponytail: one value per file and key (its largest group); path sections in that file giving other values are overridden too.
        foreach (var group in fileSettings.Where(g => g.Source is { Length: > 0 } && g.Value is not null)
            .GroupBy(g => (g.Source, g.Key))
            .Select(g => g.MaxBy(x => x.Counts[Conventions.CharsetKey].Sum())!)
            .OrderBy(g => g.Source, StringComparer.Ordinal)
            .ThenBy(g => Array.IndexOf(Conventions.FileKeys, g.Key)))
        {
            var file = group.File!;
            var own = new Dictionary<string, string> { [group.Key] = group.Value! };
            var path = Path.Combine(root, group.Source!, ".editorconfig");
            var keys = group.Key == Conventions.EndOfLineKey ? new[] { group.Key, "dotnet_diagnostic.IDE0055.severity" } : new[] { group.Key };
            List<string>? lines = null;
            string? note = null;
            if (group.Key == Conventions.TrailingWhitespaceKey)
            {
                note = formatsWhitespace ? Conventions.TrailingWhitespaceNote(group.Counts, own, file) : null;
            }
            else if (KeptLines(PreviousWhitespace(File.Exists(path) ? File.ReadAllText(path) : null), keys) is { Count: > 0 } kept)
            {
                (lines, note) = (kept, $"{file}: {group.Key} kept from the last init");
            }
            else if (group.Key == Conventions.EndOfLineKey && Conventions.EndingContradiction(group.Value!, group.PickedPerMachine, file) is var (endingLines, endingReport))
            {
                (lines, note) = (endingLines.Take(ideSeverity ? 3 : 2).ToList(), endingReport);
            }
            else if (group.Key == Conventions.EndOfLineKey && Conventions.ContradictedEnding(group.Counts, own, file) is var (ending, endingNote))
            {
                (lines, note) = ([$"# init: {endingNote}", $"{Conventions.EndOfLineKey} = {ending}"], endingNote);
            }
            else if (group.Key != Conventions.EndOfLineKey && Conventions.ContradictedFileSetting(group.Counts, own, group.Key, file) is var (line, fileNote))
            {
                (lines, note) = ([$"# init: {fileNote}", line], fileNote);
            }

            if (note is not null)
            {
                report.Add($"  {"note",-8} {note}");
            }

            if (lines is not null)
            {
                (nested.TryGetValue(group.Source!, out var list) ? list : nested[group.Source!] = []).AddRange(lines);
            }
        }

        return nested;
    }

    /// <summary>
    /// The spacing lines the last init wrote (<see cref="Conventions.SpacingComment"/> comments and the lines below each up to
    /// the next comment): kept, like the other conventions, since 'format' changes the spacing they were counted on.
    /// </summary>
    public static List<string> KeptSpacing(IReadOnlyList<string>? previous)
    {
        var result = new List<string>();
        for (var i = 0; i < (previous?.Count ?? 0); i++)
        {
            if (previous![i].StartsWith(Conventions.SpacingComment, StringComparison.Ordinal))
            {
                result.Add(previous[i]);
                for (; i + 1 < previous.Count && !previous[i + 1].TrimStart().StartsWith('#'); i++)
                {
                    result.Add(previous[i + 1]);
                }
            }
        }

        return result;
    }

    /// <summary>The lines of the folder block (<see cref="WhitespaceBegin"/>, or the legacy one) in a nested .editorconfig's text, or none.</summary>
    public static List<string> PreviousWhitespace(string? editorConfig)
    {
        var text = editorConfig?.Replace("\r\n", "\n") ?? string.Empty;
        var legacy = !text.Contains(WhitespaceBegin, StringComparison.Ordinal);
        var begin = text.IndexOf(legacy ? LegacyWhitespaceBegin : WhitespaceBegin, StringComparison.Ordinal);
        var end = begin < 0 ? -1 : text.IndexOf(legacy ? LegacyWhitespaceEnd : WhitespaceEnd, begin, StringComparison.Ordinal);
        return end < 0 ? [] : text.Substring(begin, end - begin).Split('\n').Skip(1).Where(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith('[')).ToList();
    }

    /// <summary>
    /// The folders (relative to <paramref name="root"/>) whose .editorconfig has a whitespace block that <paramref name="nested"/>
    /// no longer writes: the file stopped deciding a key (removed, or 'unset'), so the block is stale and goes, else a later
    /// run would keep it forever.
    /// </summary>
    public static List<string> StaleWhitespace(string root, IReadOnlyDictionary<string, List<string>> nested) =>
        StyleCopSetup.EnumerateFiles(root, ownRoot: true)
            .Where(f => Path.GetFileName(f) == ".editorconfig")
            .Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/'))
            .Where(s => s != "." && !nested.ContainsKey(s) && File.ReadAllText(Path.Combine(root, s, ".editorconfig")) is var text && (text.Contains(WhitespaceBegin, StringComparison.Ordinal) || text.Contains(LegacyWhitespaceBegin, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>The .editorconfig text without its folder block (and the blank line above it); <paramref name="legacyOnly"/>: only an old 'stylebro-whitespace' one.</summary>
    public static string RemoveWhitespaceBlock(string text, bool legacyOnly = false) =>
        Regex.Replace(text.Replace("\r\n", "\n"), $@"\n*# BEGIN stylebro-({(legacyOnly ? string.Empty : "folder|")}whitespace)\b.*?# END stylebro-\1[^\n]*\n?", "\n", RegexOptions.Singleline);

    /// <summary>
    /// What init says after writing a block (<paramref name="nested"/>: a folder block): it holds whitespace settings, rule
    /// severities by id, or both (trial 20: "Wrote the whitespace settings" for a block of 119 severities).
    /// </summary>
    public static string WroteLine(string name, string block, bool nested)
    {
        var keys = block.Replace("\r\n", "\n").Split('\n').Select(KeyOf).OfType<string>().ToList();
        var ids = keys.Count(k => IdOf(k) is not null);
        var whitespace = keys.Any(k => IdOf(k) is null);
        return !nested ? (block.Contains(OptOutHeader, StringComparison.Ordinal) ? $"Turned StyleBro's rules and the built-in ones off by id in {name} ({ids} rules)." : $"Wrote the built-in rule severities to {name}.")
            : !whitespace ? $"Wrote {ids} rule severities by id to {name}."
            : ids == 0 ? $"Wrote the whitespace settings it decides to {name}."
            : $"Wrote the whitespace settings it decides and {ids} rule severities by id to {name}.";
    }

    /// <summary>The block for a nested .editorconfig (<see cref="NestedWhitespace"/>, <see cref="NestedHiddenFromFormat"/>).</summary>
    public static string WhitespaceBlock(IEnumerable<string> lines) =>
        WhitespaceBegin + " (stylebro-migrate init: settings for this folder's C# files; edits inside are replaced)\n[*.cs]\n"
        + string.Join("\n", lines) + "\n" + WhitespaceEnd + "\n";

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
    /// Whether the repository's .editorconfig enforces 'this.' (owner's choice 2026-10-14): IDE0009 at suggestion or above, or,
    /// without an IDE0009 severity, a dotnet_style_qualification_for_* key = true with a severity that isn't silent or none
    /// ('true:warning'), like <see cref="Unenforced"/> judges a key, or with IDE0009 shown through its category or the bulk
    /// severity (trial 20, MoreLINQ: 'true' and 'dotnet_analyzer_diagnostic.category-Style.severity = warning'). Then init
    /// keeps the repository's rule.
    /// </summary>
    public static bool RequiresThisQualification(IReadOnlyDictionary<string, string> settings)
    {
        var effective = StyleBro.Analyzers.Severities.FromSettings(new Conventions.Options(settings), "IDE0009", "Style");
        var shown = effective is Microsoft.CodeAnalysis.ReportDiagnostic.Info or Microsoft.CodeAnalysis.ReportDiagnostic.Warn or Microsoft.CodeAnalysis.ReportDiagnostic.Error;
        if (settings.ContainsKey(ThisQualificationSeverityKey))
        {
            return shown;
        }

        return new[] { "field", "property", "method", "event" }.Any(kind =>
            settings.TryGetValue($"dotnet_style_qualification_for_{kind}", out var value) && value.Split(':') is [var on, .. var rest]
            && on.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            && (shown || (rest is [var severity] && severity.Trim().ToLowerInvariant() is "suggestion" or "warning" or "error")));
    }

    /// <summary>
    /// Whether <paramref name="settings"/> make a built-in rule a warning or an error: its own severity, else its category's
    /// (Style), else the bulk one (the shared <see cref="StyleBro.Analyzers.Severities.FromSettings"/>). The build reports it then.
    /// </summary>
    public static bool IsEnforced(IReadOnlyDictionary<string, string> settings, string id) =>
        StyleBro.Analyzers.Severities.FromSettings(new Conventions.Options(settings), id, "Style") is Microsoft.CodeAnalysis.ReportDiagnostic.Warn or Microsoft.CodeAnalysis.ReportDiagnostic.Error;

    /// <summary>The StyleBro rules the preset turns on, with their severity, from every layer in the package (init pins the latest).</summary>
    public static SortedDictionary<string, string> PresetSeverities()
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith("Preset.", StringComparison.Ordinal)))
        {
            foreach (Match match in Regex.Matches(Read(name), @"^dotnet_diagnostic\.(BRO\d+)\.severity\s*=\s*(\w+)", RegexOptions.Multiline))
            {
                if (match.Groups[2].Value != "none")
                {
                    result[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// The lines that set, by id, the StyleBro rules the preset turns on but <paramref name="settings"/> (an .editorconfig's
    /// for all C# files) hide from 'dotnet format' with a bulk or category severity below warning ('dotnet_analyzer_diagnostic
    /// .severity = silent', sharpcompress). 'dotnet format' reads severities from .editorconfig only, so it skipped every
    /// rule and said clean, while the build takes the preset's own entries (a rule's own entry wins over a bulk one) and
    /// failed with ~40,000 warnings as errors. Rules the settings set by id are the team's. Empty when nothing is hidden.
    /// </summary>
    public static List<string> HiddenFromFormat(IReadOnlyDictionary<string, string> settings)
    {
        static bool Below(string? severity) => severity?.Trim().ToLowerInvariant() is "silent" or "none" or "suggestion" or "hidden" or "info";
        settings.TryGetValue("dotnet_analyzer_diagnostic.severity", out var bulk);
        var categories = Migration.RuleCategories();
        var result = new List<string>();
        foreach (var (id, severity) in PresetSeverities())
        {
            if (settings.ContainsKey($"dotnet_diagnostic.{id}.severity"))
            {
                continue;
            }

            var category = categories.TryGetValue(id, out var name) && settings.TryGetValue($"dotnet_analyzer_diagnostic.category-{name}.severity", out var value) ? value : bulk;
            if (Below(category))
            {
                result.Add($"dotnet_diagnostic.{id}.severity = {severity}");
            }
        }

        return result;
    }

    /// <summary>
    /// <see cref="HiddenFromFormat"/> for each .editorconfig below <paramref name="root"/> (folder relative to it, '/'-separated;
    /// not below a 'root = true' one), with the settings that reach it: <paramref name="rootSettings"/>, then each .editorconfig
    /// on the way down (with <paramref name="withBlocks"/>, also their generated blocks) and the lines found for the folders
    /// above it. Trial 19: tests/.editorconfig with 'dotnet_analyzer_diagnostic.severity = silent' hid StyleBro's rules from
    /// 'dotnet format' there, and the build failed. A folder that turns every analyzer off itself (<see cref="OptsOut"/>) gets
    /// <see cref="OptOutLines"/> instead, with <paramref name="written"/>: the keys StyleBro's blocks set in <paramref name="rootSettings"/>.
    /// </summary>
    public static SortedDictionary<string, List<string>> NestedHiddenFromFormat(string root, IReadOnlyDictionary<string, string> rootSettings, bool withBlocks, IReadOnlyCollection<string>? written = null)
    {
        var result = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        var sources = StyleCopSetup.EnumerateFiles(root, ownRoot: true)
            .Where(f => Path.GetFileName(f) == ".editorconfig")
            .Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/'))
            .Where(s => s != ".")
            .OrderBy(s => s.Count(c => c == '/'))
            .ThenBy(s => s, StringComparer.Ordinal)
            .ToList();
        foreach (var source in sources)
        {
            // The folder's own chain (its .editorconfig files and the lines found for the folders above) over the root's settings.
            var settings = new Dictionary<string, string>(rootSettings, StringComparer.OrdinalIgnoreCase);
            var chain = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = source.Split('/');
            for (var i = 1; i <= parts.Length; i++)
            {
                var folder = string.Join("/", parts.Take(i));
                var found = i < parts.Length && result.TryGetValue(folder, out var above) ? above.Where(l => KeyOf(l) is not null).Select(l => new KeyValuePair<string, string>(KeyOf(l)!, l.Substring(l.IndexOf('=') + 1).Trim())) : [];
                foreach (var (key, value) in Migration.OwnSettings(Path.Combine(root, folder), withBlocks).Concat(found))
                {
                    settings[key] = value;
                    chain[key] = value;
                }
            }

            if ((OptsOut(chain) ? OptOutLines(settings.Where(p => written?.Contains(p.Key, StringComparer.OrdinalIgnoreCase) == true), chain) : HiddenFromFormat(settings)) is { Count: > 0 } hidden)
            {
                result[source] = hidden;
            }
        }

        return result;
    }

    /// <summary>
    /// Whether <paramref name="settings"/> (a folder's own .editorconfig files) turn every analyzer off: the bulk severity
    /// 'none' or 'silent' (owner's choice 2026-10-14: coverlet's sample folders say "ensure no rules are enabled for these
    /// asset source files"). Init then turns StyleBro's rules off there by id, so the build and 'format' leave it alone.
    /// </summary>
    public static bool OptsOut(IReadOnlyDictionary<string, string> settings) =>
        settings.TryGetValue("dotnet_analyzer_diagnostic.severity", out var bulk) && bulk.Trim().ToLowerInvariant() is "none" or "silent" or "hidden";

    /// <summary>
    /// The lines for a folder that opts out (<see cref="OptsOut"/>): 'none' for every StyleBro rule the preset turns on, every
    /// built-in rule init's block sets, and every other id <paramref name="settings"/> (what StyleBro's blocks above set)
    /// turn on: a rule's own entry wins over the folder's bulk one (the preset's in the build, the root block's in 'format';
    /// trial 20: 'format' re-indented coverlet's samples and 49 tests failed). IDE0055 'none' also keeps 'format''s
    /// whitespace pass out. Ids <paramref name="own"/> (the folder's own files) set are the team's and stay.
    /// </summary>
    public static List<string> OptOutLines(IEnumerable<KeyValuePair<string, string>> settings, IReadOnlyDictionary<string, string> own)
    {
        var ids = new SortedSet<string>(PresetSeverities().Keys, StringComparer.Ordinal);
        ids.UnionWith(Template().Replace("\r\n", "\n").Split('\n').Select(KeyOf).Select(IdOf).OfType<string>());
        ids.UnionWith(settings.Where(p => !p.Value.Trim().Equals("none", StringComparison.OrdinalIgnoreCase)).Select(p => IdOf(p.Key)).OfType<string>());
        return ids.Where(id => !own.ContainsKey($"dotnet_diagnostic.{id}.severity")).Select(id => $"dotnet_diagnostic.{id}.severity = none").ToList();
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
    /// for interfaces, constants or static fields don't decide the private field style. Nor does any rule while IDE1006 is
    /// off (<see cref="NamingRulesOff"/>): a naming rule nothing enforces doesn't win (owner's decision 2026-10-13), the code
    /// decides, and the key init then writes wins over the rule in the analyzer too.
    /// </summary>
    public static string? FieldNamingRule(IReadOnlyDictionary<string, string> settings)
    {
        if (NamingRulesOff(settings))
        {
            return null;
        }

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

    /// <summary>Whether the repository turns IDE1006 off (none or silent), so no naming rule is enforced.</summary>
    public static bool NamingRulesOff(IReadOnlyDictionary<string, string> settings) =>
        settings.TryGetValue("dotnet_diagnostic.IDE1006.severity", out var severity) && severity.Trim().ToLowerInvariant() is "none" or "silent";

    /// <summary>
    /// The --modernize block: the SDK's rules that rewrite code into newer C# (tier B) and newer APIs (tier C), at warning
    /// where that's safe, else at suggestion with a note why. In a multi-targeted project the rules fire per target
    /// framework and 'dotnet format' writes the newer framework's edit into the shared file: the language rules break the
    /// older framework's build unless LangVersion is set (else each framework gets its own C# version). The API rules would
    /// too, but StyleBro's MultiTargetSuppressor hides them there, so they are always warnings. Keys the root .editorconfig
    /// (or the one in <paramref name="keysFrom"/>, a folder whose .editorconfig says root = true) sets itself are left out.
    /// </summary>
    public static (string? Block, List<string> Notes) Modernize(string root, IReadOnlyList<string> multiTargeted, string? keysFrom = null)
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

        var own = Migration.OwnKeys(keysFrom ?? root);
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
    public static IEnumerable<string> MultiTargetedProjects(string root, string? named = null)
    {
        if ((named is { Length: > 0 } ? named : BaselineCommand.FindWorkspace(root)) is { } workspace)
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

    /// <summary>
    /// The folders below <paramref name="root"/> whose .editorconfig chain turns the Style category off (not every analyzer: that's
    /// <see cref="OptsOut"/>), with a 'none' line for each built-in rule <paramref name="blockLines"/> turn on by id: a rule's own
    /// entry wins over the category, so the block's IDE0055 = warning reformatted the folder (trial 22, spot 9e). Ids the
    /// folder's own files set stay theirs.
    /// </summary>
    public static SortedDictionary<string, List<string>> NestedStyleOff(string root, IEnumerable<string> blockLines)
    {
        static bool Off(string? severity) => severity?.Trim().ToLowerInvariant() is "none" or "silent" or "hidden";
        var last = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in blockLines)
        {
            if (IdOf(KeyOf(line)) is { } id && id.StartsWith("IDE", StringComparison.Ordinal))
            {
                last[id] = line.Substring(line.IndexOf('=') + 1);
            }
        }

        var ids = last.Where(p => !Off(p.Value)).Select(p => p.Key).ToList();
        var result = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var source in StyleCopSetup.EnumerateFiles(root, ownRoot: true).Where(f => Path.GetFileName(f) == ".editorconfig")
            .Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/')).Where(s => s != "."))
        {
            var chain = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = source.Split('/');
            for (var i = 1; i <= parts.Length; i++)
            {
                foreach (var (key, value) in Migration.OwnSettings(Path.Combine(root, string.Join("/", parts.Take(i)))))
                {
                    chain[key] = value;
                }
            }

            if (!OptsOut(chain) && chain.TryGetValue("dotnet_analyzer_diagnostic.category-Style.severity", out var style) && Off(style)
                && ids.Where(id => !chain.ContainsKey($"dotnet_diagnostic.{id}.severity")).Order(StringComparer.Ordinal).Select(id => $"dotnet_diagnostic.{id}.severity = none").ToList() is { Count: > 0 } offLines)
            {
                result[source] = offLines;
            }
        }

        return result;
    }

    /// <summary>
    /// <paramref name="settings"/> with the Style category at warning when an MSBuild file under the root sets AnalysisMode (or
    /// AnalysisModeStyle) to All and EnforceCodeStyleInBuild to true: the build then reports every code-style rule as a warning,
    /// IDE0009 too, unless the settings say otherwise.
    /// </summary>
    public static IReadOnlyDictionary<string, string> WithAnalysisModeAll(string root, IReadOnlyDictionary<string, string> settings)
    {
        const string Category = "dotnet_analyzer_diagnostic.category-Style.severity";
        if (settings.ContainsKey(Category) || settings.ContainsKey("dotnet_analyzer_diagnostic.severity") || !EnforcesCodeStyleInBuild(root)
            || !StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).Any(f => Regex.IsMatch(File.ReadAllText(f), @"<AnalysisMode(?:Style)?>\s*All\s*<", RegexOptions.IgnoreCase)))
        {
            return settings;
        }

        return new Dictionary<string, string>(settings.ToDictionary(p => p.Key, p => p.Value), StringComparer.OrdinalIgnoreCase) { [Category] = "warning" };
    }

    /// <summary>Whether an MSBuild file under the root sets EnforceCodeStyleInBuild to true (then init doesn't suggest it).</summary>
    public static bool EnforcesCodeStyleInBuild(string root) =>
        StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).Any(f => Regex.IsMatch(File.ReadAllText(f), @"<EnforceCodeStyleInBuild>\s*true\s*<", RegexOptions.IgnoreCase));

    /// <summary>
    /// The sections for the parts of the repository with their own Sonar severities (a path section of the root
    /// .editorconfig, a sub-directory's .editorconfig: <see cref="Scope"/>), each with the lines that differ from the
    /// repository-wide <paramref name="sonarLines"/> (<see cref="Migration.ScopeLines"/>).
    /// </summary>
    internal static List<(string Section, List<string> Lines)> SonarScopes(string root, SonarSetup sonar, List<string> sonarLines, ISet<string> own)
    {
        var result = new List<(string Section, List<string> Lines)>();
        var ownRoots = StyleCopSetup.EditorConfigRoots(root);
        foreach (var scope in StyleCopSetup.Read(root).Scopes.Where(s => s.Sonar is not null))
        {
            // ponytail: a sub-directory's section that isn't for all its C# files, or one below a 'root = true' folder, is
            // skipped; translate its glob into the root's when one turns up.
            var folder = (Path.GetDirectoryName(scope.File) ?? string.Empty).Replace('\\', '/');
            var section = folder.Length == 0 ? scope.Section
                : StyleCopSetup.AppliesToCSharp(scope.Section) && !ownRoots.Any(r => folder == r || folder.StartsWith(r + "/", StringComparison.Ordinal)) ? StyleCopSetup.SectionFor([folder])
                : null;
            var inScope = new List<string>();
            sonar.For(scope).Apply(inScope, _ => true, own);
            if (section is not null && Migration.ScopeLines(sonarLines, inScope, own) is { Count: > 0 } differing)
            {
                result.Add((section, [$"# Sonar's severities in {scope.File} [{scope.Section}]", .. differing]));
            }
        }

        return result;
    }

    /// <summary>
    /// The lines without a setting line that repeats an earlier one, with the '# init:' comment right above it (BRO1404's off
    /// line, from the members' and the types' conventions, or kept from the last init and decided again).
    /// </summary>
    internal static List<string> WithoutRepeats(List<string> lines)
    {
        for (var i = lines.Count - 1; i > 0; i--)
        {
            if (i < lines.Count && !lines[i].StartsWith('#') && lines.IndexOf(lines[i]) < i)
            {
                lines.RemoveAt(i);
                if (lines[i - 1].StartsWith("# init:", StringComparison.Ordinal) && (i == lines.Count || lines[i].StartsWith('#')))
                {
                    lines.RemoveAt(i - 1);
                }
            }
        }

        return lines;
    }

    /// <summary>
    /// The previous lines that set one of the keys, each with the '# init:' comments right above it that are about the same
    /// thing (the topic before the comment's first ': '). Not a comment-only verdict above it (Fluid: the using order's
    /// "they aren't sorted" sat above BRO1306's comment, was kept with it and written again: a second copy on every re-run).
    /// </summary>
    internal static List<string> KeptLines(IReadOnlyList<string>? previous, IReadOnlyCollection<string> keys, string? topic = null)
    {
        static string Topic(string comment) => comment.Split(": ", 3) is { Length: 3 } parts ? parts[1] : comment;
        var result = new List<string>();
        for (var i = 0; i < (previous?.Count ?? 0); i++)
        {
            // With a topic, only a line under that convention's comment (BRO1404's off line is the members' or the types').
            if (KeyOf(previous![i]) is { } key && keys.Contains(key, StringComparer.OrdinalIgnoreCase)
                && (topic is null || i == 0 || !previous[i - 1].StartsWith("# init:", StringComparison.Ordinal) || Topic(previous[i - 1]) == topic))
            {
                var first = i;
                while (first > 0 && previous[first - 1].StartsWith("# init:", StringComparison.Ordinal)
                    && (first == i || Topic(previous[first - 1]) == Topic(previous[i - 1])))
                {
                    first--;
                }

                result.AddRange(previous.Skip(first).Take(i - first + 1));
            }
        }

        return result;
    }

    /// <summary>Whether another convention turns the same rule off (BRO1404: members and types).</summary>
    private static bool SharesOff(Conventions.Convention convention) =>
        convention.Off is { } off && Conventions.All.Count(c => c.Off == off) > 1;

    /// <summary>
    /// Pins the preset version (docs/versioning.md) in every Directory.Build.props the projects import: a new setup takes
    /// every rule this version knows, and later package upgrades add none by themselves. A file that already sets
    /// StyleBroPreset (a migration's 'none', the team's own) is left alone.
    /// </summary>
    private static void PinPreset(string root, bool write)
    {
        var property = $"<StyleBroPreset>{StyleBro.Analyzers.PresetVersions.Latest}</StyleBroPreset>";
        var solutions = Directory.EnumerateFiles(root).Where(f => Path.GetExtension(f).ToLowerInvariant() is ".sln" or ".slnx" or ".slnf").Select(f => File.ReadAllText(f).Replace('\\', '/')).ToList();
        foreach (var props in Migration.PropsFiles(root).Where(p => !IsOutsideTheBuild(root, p, solutions)))
        {
            var pinned = Migration.SetPreset(
                File.Exists(props) ? File.ReadAllText(props) : null,
                StyleBro.Analyzers.PresetVersions.Latest,
                "StyleBro's rules up to this preset version: raise it (or 'latest') for the rules of newer releases.",
                importParent: !File.Exists(props) && Migration.HasParentProps(props));
            if (pinned is null)
            {
                continue;
            }

            var name = Path.GetRelativePath(root, props).Replace('\\', '/');
            if (write)
            {
                PreviewCommand.EditFile(props, _ => pinned);
                Console.WriteLine($"Set the preset version in {name} ({property}): rules of newer releases stay off until you raise it.");
            }
            else
            {
                Console.WriteLine($"--write also sets the preset version in {name} ({property}).");
            }
        }
    }

    /// <summary>
    /// Whether a nested Directory.Build.props serves only projects no solution lists and that don't get StyleBro (trial 20:
    /// coverlet's Documentation/Examples and Altinn's test apps got a preset version nothing reads). The solutions are the
    /// root's (an example's own solution doesn't count), a project matched by its path. Not the root's props, and not when
    /// the root has no solution or StyleBro.Analyzers is mentioned under the props' folder.
    /// </summary>
    private static bool IsOutsideTheBuild(string root, string props, IReadOnlyList<string> solutions)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(props))!;
        if (solutions.Count == 0 || string.Equals(folder, Path.GetFullPath(root).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase) || !Directory.Exists(folder))
        {
            return false;
        }

        var files = StyleCopSetup.EnumerateFiles(folder).Where(StyleCopSetup.IsMSBuild).ToList();
        return !files.Any(f => File.ReadAllText(f).Contains("StyleBro.Analyzers", StringComparison.OrdinalIgnoreCase))
            && !files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).Any(p => solutions.Any(s => s.Contains(Path.GetRelativePath(root, p).Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// One .editorconfig's block, with its convention report printed: the root's (<paramref name="folder"/> empty), or the
    /// one of a folder whose .editorconfig says 'root = true', judged from that folder's own code and settings (the root's
    /// don't reach it; CleanArchitecture's sample/ got CRLF lines in LF files and 'is null' against its '== null').
    /// </summary>
    private static Target Prepare(string repository, string folder, IReadOnlyList<string> sonarLines, IReadOnlyList<string> multiTargeted, bool modernizing, IReadOnlyList<(string Section, List<string> Lines)>? sonarScopes = null)
    {
        var root = folder.Length == 0 ? repository : Path.Combine(repository, folder);
        var prefix = folder.Length == 0 ? string.Empty : folder + "/";
        var path = Path.Combine(root, ".editorconfig");
        var existing = File.Exists(path) ? File.ReadAllText(path) : null;
        var migrated = existing?.Contains(Migration.BeginMarker) == true && !existing.Contains("stylebro-migrate init");
        var ownSettings = Migration.OwnSettings(root);

        // The root's .globalconfig counts like its .editorconfig for what the team set and enforces (not for the bulk severities
        // 'dotnet format' would skip: it reads none from a global config anyway).
        var settings = folder.Length == 0 ? Migration.WithGlobalConfig(repository, ownSettings) : ownSettings;
        string Source(params string[] keys) => keys.Any(ownSettings.ContainsKey) ? prefix + ".editorconfig" : ".globalconfig";
        var plain = existing?.Replace("\r\n", "\n");
        var name = prefix + ".editorconfig";

        // A 'root = true' folder that turns every analyzer off (coverlet's samples; owner's choice 2026-10-14): its block only
        // turns StyleBro's rules and the built-in ones off by id, which would win over the bulk 'none'. Nothing is counted.
        if (folder.Length > 0 && !migrated && OptsOut(ownSettings))
        {
            var optOut = OptOutBlock(OptOutLines([], ownSettings));
            Console.WriteLine($"{name} turns every analyzer off (dotnet_analyzer_diagnostic.severity = {ownSettings["dotnet_analyzer_diagnostic.severity"]}): its block turns StyleBro's rules and the built-in ones off there by id too, so the build and 'format' leave {folder}/ alone.");
            JsonReport.AddSettings(name, optOut);
            return new Target
            {
                Name = name,
                Prefix = name + ": ",
                Path = path,
                Existing = existing,
                Block = optOut,
                ModernizeNotes = [],
                HasBlock = plain is not null && Migration.Apply(plain, optOut) == plain,
                HasModernize = true,
                FormatsWhitespace = false,
            };
        }

        // The conventions the code follows: what the code already does stays (Ocelot: '=>' at the start of a line, '_field').
        var detected = new List<string>();
        var nestedLines = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (!migrated)
        {
            (var lines, var report, nestedLines, var scopes) = Detect(root, settings, sonarLines, PreviousConventions(existing));
            detected = lines;
            sonarScopes = [.. sonarScopes ?? [], .. scopes];
            if (report.Count > 0)
            {
                Console.WriteLine($"{DetectedHeader}{(prefix.Length == 0 ? string.Empty : " under " + prefix)} (judged from {Conventions.MinimumAgreeing} places: all must agree below {Conventions.MinimumSample}, {Conventions.Share * 100:0}% from {Conventions.MinimumSample} on).");
                Console.WriteLine("  off: mixed, or not what your setting says, so the rule that enforces it is turned off; both: mixed, both allowed;");
                Console.WriteLine("  mixed: the default stays, nothing is turned off (no StyleBro rule enforces it, or your .editorconfig sets the rule):");
                report.ForEach(Console.WriteLine);
                Console.WriteLine();
            }
        }

        var generated = migrated ? new List<string>() : Migration.MigrationFolders(root);
        if (generated.Count > 0)
        {
            Console.WriteLine($"EF Core migrations in {string.Join(", ", generated.Select(g => prefix + g))}: marked generated_code = true, so formatting and StyleBro leave them alone.");
        }

        var vendored = migrated ? new List<string>() : Migration.VendoredFolders(root);
        if (vendored.Count > 0)
        {
            Console.WriteLine($"Vendored code in {string.Join(", ", vendored.Select(v => prefix + v))}: marked generated_code = true, so formatting and StyleBro leave it alone.");
        }

        // A bulk 'silent' hides StyleBro from 'dotnet format' but not from the build: set by id, they agree (docs/decisions).
        var byId = migrated ? [] : HiddenFromFormat(ownSettings).Where(l => !detected.Concat(sonarLines).Any(d => KeyOf(d) == KeyOf(l))).ToList();
        if (byId.Count > 0)
        {
            Console.WriteLine($"{prefix}.editorconfig sets a bulk severity below warning: 'dotnet format' would skip {byId.Count} StyleBro rules the build reports. The block sets them by id, so both agree.");
        }

        // The same for a nested .editorconfig's bulk severity (trial 19: tests/.editorconfig with 'none'), in its own block.
        var nested = new List<Target>();
        if (!migrated)
        {
            var blockSettings = new Dictionary<string, string>(ownSettings, StringComparer.OrdinalIgnoreCase);
            foreach (var line in detected.Concat(sonarLines).Concat(byId).Where(l => KeyOf(l) is not null))
            {
                blockSettings[KeyOf(line)!] = line.Substring(line.IndexOf('=') + 1).Trim();
            }

            var written = detected.Concat(sonarLines).Concat(byId).Concat(modernizing ? ModernizeTemplate().Split('\n') : []).Select(KeyOf).OfType<string>().ToList();
            foreach (var line in modernizing ? ModernizeTemplate().Replace("\r\n", "\n").Split('\n').Where(l => KeyOf(l) is not null) : [])
            {
                blockSettings[KeyOf(line)!] = line.Substring(line.IndexOf('=') + 1).Trim();
            }

            foreach (var (source, hidden) in NestedHiddenFromFormat(root, blockSettings, withBlocks: false, written))
            {
                var optOut = hidden.All(l => l.EndsWith(" = none", StringComparison.Ordinal));
                Console.WriteLine(optOut
                    ? $"{prefix}{source}/.editorconfig turns every analyzer off: its block turns {hidden.Count} rules off by id too (StyleBro's and the built-in ones init sets), which would win over it, so the build and 'format' leave the folder alone."
                    : $"{prefix}{source}/.editorconfig sets a bulk severity below warning: 'dotnet format' would skip {hidden.Count} StyleBro rules the build reports. Its block sets them by id, so both agree.");
                (nestedLines.TryGetValue(source, out var list) ? list : nestedLines[source] = []).AddRange([.. (optOut ? OptOutComment : ByIdComment).Split('\n'), .. hidden]);
            }

            foreach (var (source, off) in NestedStyleOff(root, Template().Split('\n').Concat(detected).Concat(sonarLines)))
            {
                Console.WriteLine($"{prefix}{source}/.editorconfig turns the Style category off: its block turns {off.Count} built-in rules off by id too ({List(off.Select(l => IdOf(KeyOf(l))!).ToList())}), which the block above would otherwise turn on there.");
                (nestedLines.TryGetValue(source, out var list) ? list : nestedLines[source] = []).AddRange([.. StyleOffComment.Split('\n'), .. off]);
            }

            nested = nestedLines.Select(p => NestedTarget(root, prefix, p.Key, p.Value)).Concat(StaleWhitespace(root, nestedLines).Select(s => NestedTarget(root, prefix, s, null))).ToList();
        }

        // The repository requires 'this.': the template's IDE0009 = none would turn its rule off (RecyclableMemoryStream).
        var leftOut = new List<string>();
        if (!migrated && RequiresThisQualification(folder.Length == 0 ? WithAnalysisModeAll(repository, settings) : settings))
        {
            leftOut.Add(ThisQualificationSeverityKey);
            Console.WriteLine($"{Source(ThisQualificationSeverityKey, "dotnet_style_qualification_for_field", "dotnet_style_qualification_for_property", "dotnet_style_qualification_for_method", "dotnet_style_qualification_for_event")} requires 'this.' (dotnet_style_qualification_for_*, IDE0009): kept, the block doesn't turn IDE0009 off.");
        }

        // The repository's own IDE0055 severity stays (MoreLINQ: 'suggestion'; the block's 'warning' overrode it while the report
        // said it was kept). 'format' still runs its whitespace pass: it does whatever IDE0055's severity says.
        if (!migrated && settings.TryGetValue(IdeSeverityKey, out var ideSeverity) && !detected.Any(l => KeyOf(l) == IdeSeverityKey))
        {
            leftOut.Add(IdeSeverityKey);
            Console.WriteLine($"{Source(IdeSeverityKey)} sets IDE0055 ({ideSeverity}): kept, the block doesn't set it. 'stylebro-migrate format' still formats whitespace (its whitespace pass runs whatever IDE0055's severity is).");
        }

        var block = Block(detected, plainHeader: settings.TryGetValue("file_header_template", out var template) && !template.Trim().Equals("unset", StringComparison.OrdinalIgnoreCase), sonarLines, generated, vendored, sonarScopes, byId, leftOut);
        var modernize = modernizing ? Modernize(repository, multiTargeted, root) : (Block: null, Notes: new List<string>());
        if (!migrated)
        {
            JsonReport.AddSettings(name, block);
        }

        if (modernize.Block is not null)
        {
            JsonReport.AddSettings(name, modernize.Block);
        }

        // What --write would change: nothing when the settings are there (written by 'stylebro-migrate --write', or by an
        // earlier init). The dry run says the same as --write (mdx: it showed a block --write then said was there).
        return new Target
        {
            Name = name,
            Prefix = prefix.Length == 0 ? string.Empty : name + ": ",
            Path = path,
            Existing = existing,
            Migrated = migrated,
            Block = block,
            Modernize = modernize.Block,
            ModernizeNotes = modernize.Notes,
            HasBlock = migrated || (plain is not null && Migration.Apply(plain, block) == plain),
            HasModernize = modernize.Block is null || (plain is not null && Migration.Apply(plain, modernize.Block, ModernizeBegin, ModernizeEnd) == plain),
            Nested = nested,
            FormatsWhitespace = !detected.Contains(IdeOff),
        };
    }

    /// <summary>
    /// The whitespace block for an .editorconfig below <paramref name="root"/> (<paramref name="source"/>: its folder) that decides
    /// a key itself; <paramref name="lines"/> null: its stale block goes (<see cref="StaleWhitespace"/>, an empty block).
    /// </summary>
    private static Target NestedTarget(string root, string prefix, string source, List<string>? lines)
    {
        var path = Path.Combine(root, source, ".editorconfig");
        var existing = File.ReadAllText(path);
        var block = lines is null ? string.Empty : WhitespaceBlock(lines);
        var name = prefix + source + "/.editorconfig";
        if (lines is not null)
        {
            JsonReport.AddSettings(name, block);
        }

        var plain = existing.Replace("\r\n", "\n");
        return new Target
        {
            Name = name,
            Prefix = name + ": ",
            Path = path,
            Existing = existing,
            Block = block,
            Begin = WhitespaceBegin,
            End = WhitespaceEnd,
            ModernizeNotes = [],
            HasBlock = lines is not null && Migration.Apply(plain, block, WhitespaceBegin, WhitespaceEnd) == plain,
            HasModernize = true,
        };
    }

    private static bool IsCSharp(string file) => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    /// <summary>The key of a 'key = value' line, or null for comments.</summary>
    private static string? KeyOf(string line)
    {
        var equals = line.IndexOf('=');
        return line.TrimStart().StartsWith('#') || equals <= 0 ? null : line.Substring(0, equals).Trim();
    }

    /// <summary>The id of a 'dotnet_diagnostic.ID.severity' key, or null.</summary>
    private static string? IdOf(string? key) =>
        key is not null && key.StartsWith("dotnet_diagnostic.", StringComparison.OrdinalIgnoreCase) && key.EndsWith(".severity", StringComparison.OrdinalIgnoreCase)
            ? key.Substring("dotnet_diagnostic.".Length, key.Length - "dotnet_diagnostic.".Length - ".severity".Length)
            : null;

    private static string List(IReadOnlyList<string> items) => string.Join(", ", items.Take(3)) + (items.Count > 3 ? ", ..." : string.Empty);

    private static string Read(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"{resource} is missing from the tool.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>One .editorconfig init writes to (<see cref="Prepare"/>).</summary>
    private sealed class Target
    {
        public required string Name { get; init; }

        public required string Prefix { get; init; }

        public required string Path { get; init; }

        public string? Existing { get; init; }

        public bool Migrated { get; init; }

        public required string Block { get; init; }

        public string? Modernize { get; init; }

        public required List<string> ModernizeNotes { get; init; }

        public bool HasBlock { get; init; }

        public bool HasModernize { get; init; }

        /// <summary>Gets the block's start marker (<see cref="WhitespaceBegin"/> in a nested .editorconfig).</summary>
        public string Begin { get; init; } = Migration.BeginMarker;

        /// <summary>Gets the block's end marker.</summary>
        public string End { get; init; } = Migration.EndMarker;

        /// <summary>Gets a value indicating whether the block leaves IDE0055 on for all C# files (not with CSharpier or column alignment).</summary>
        public bool FormatsWhitespace { get; init; } = true;

        /// <summary>Gets the whitespace blocks for the .editorconfig files below this one that decide a key themselves.</summary>
        public List<Target> Nested { get; init; } = [];
    }
}
