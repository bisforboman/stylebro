using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate [path] [--write]: reads the StyleCop setup of the repository at 'path' (default: the current
/// directory) and prints the matching StyleBro and SDK settings. With --write it puts them into the repository's
/// .editorconfig files, between markers so a second run replaces them, and carries StyleCop suppressions in the code
/// ('#pragma warning disable SA1202', [SuppressMessage], &lt;NoWarn&gt;) over to the rules that replace them.
/// 'stylebro-migrate baseline' writes a baseline instead (<see cref="BaselineCommand"/>), 'stylebro-migrate init' the
/// built-in rule severities the preset relies on (<see cref="InitCommand"/>), 'stylebro-migrate format' runs 'dotnet
/// format' safely in multi-targeted repositories (<see cref="FormatCommand"/>).
/// </summary>
internal static class Program
{
    /// <summary>What 'stylebro-migrate --help' prints.</summary>
    public const string Usage = """
        stylebro-migrate: move a repository to StyleBro (https://bisforboman.github.io/stylebro/).
          Getting started: https://bisforboman.github.io/stylebro/getting-started/
          From StyleCop:   https://bisforboman.github.io/stylebro/migrating/
          Settings:        https://bisforboman.github.io/stylebro/configuration/

        Usage:
          stylebro-migrate [path] [--write] [--sonar-profile <file>]
              Coming from StyleCop: reads the StyleCop setup at 'path' (default: the current folder) and prints the
              StyleBro and .NET settings that enforce the same things. --write puts them into the .editorconfig files,
              turns StyleBro's preset off and carries StyleCop suppressions over.

          stylebro-migrate init [path] [--write] [--modernize] [--sonar-profile <file>]
              Without StyleCop: the built-in .NET rules StyleBro's preset relies on, for the root .editorconfig.
              --modernize adds the SDK's rules for newer C# and APIs. A repository with a StyleCop setup is told to
              use 'stylebro-migrate --write' instead.

              Both follow a SonarQube setup too (SonarAnalyzer.CSharp, Sonar rule severities in rulesets and configs):
              the Sonar rules that are on turn on the StyleBro and .NET rules that fix what they report. --sonar-profile
              takes a quality profile exported from the server (api/qualityprofiles/backup) instead of the defaults.

          stylebro-migrate format [folder, solution or project] [--all] [--once] [dotnet format options]
              'dotnet format' that fixes StyleBro's rules and the built-in rules init/migrate turn on (plus whitespace),
              once per target framework in multi-targeted repositories, never inside git submodules. Runs again until a
              run changes nothing (at most 3 runs) and prints the files each run changed (the first 10); --once runs once. --all also
              applies every other analyzer's and compiler fix. Other options pass through (--severity warn, --report). A
              folder with several solutions or projects needs one named.
              Then it lists the findings StyleBro's fixes keep on purpose, with the reason (a name read by reflection,
              used in generated code, ...): don't change those by hand without checking; suppress or baseline them.
              --verify-no-changes changes nothing and checks whether formatting would change a file; kept findings
              don't fail it (plain 'dotnet format --verify-no-changes' fails on them). For CI and agents.
              Exit codes: 0 clean (only kept findings left, if any); 2 a run still changed files after 3 runs, or with
              --verify-no-changes, formatting would change a file; other: 'dotnet format' failed.

          stylebro-migrate [path] --diff[=<file>] [--keep] [--all] [--project <solution or project>] [--sonar-profile <file>]
          stylebro-migrate init [path] --diff[=<file>] [--keep] [--all] [--project <solution or project>] [--modernize] [--sonar-profile <file>]
          stylebro-migrate format [folder, solution or project] --diff[=<file>] [--keep] [options]
              Preview: runs the command (--write for the first two) and then format until a run changes nothing on a
              temporary copy of the repository, which is never touched. Prints a summary (settings, files changed per
              rule, sample changes) and writes the full diff to stylebro-preview.patch (or <file>). Adds the
              StyleBro.Analyzers reference in the copy when the repository has none. --keep keeps the copy; --project
              names what format runs on when the folder has several solutions or projects.

          stylebro-migrate baseline [path] [--project <solution or project>]
              Writes stylebro.baseline with today's violations, so only new code has to follow the rules.

          stylebro-migrate help | --help | -h
              This text.
        """;

    /// <summary>The option that names a SonarQube quality profile backup (XML) to follow.</summary>
    internal const string SonarProfileOption = "--sonar-profile";

    /// <summary>How to format after init or --write: 'stylebro-migrate format', not plain 'dotnet format'.</summary>
    internal const string FormatHint = "Next: run 'stylebro-migrate format'. Plain 'dotnet format' also applies every other analyzer's and the compiler's fixes.";

    /// <summary>What to do after --write: swap the packages and format.</summary>
    internal const string NextStep = "Next: add the StyleBro.Analyzers package, remove StyleCop.Analyzers, and run 'stylebro-migrate format'.";

    public static int Main(string[] args)
    {
        if (args.FirstOrDefault() == "help" || args.Any(a => a is "--help" or "-h" or "-?"))
        {
            Console.WriteLine(Usage);
            return 0;
        }

        // 'format' passes unknown options on to 'dotnet format'.
        var (command, known) = args.FirstOrDefault() switch
        {
            "baseline" => ("baseline", new[] { "--project" }),
            "init" => ("init", new[] { "--write", "--modernize", SonarProfileOption }.Concat(PreviewCommand.Options).ToArray()),
            "format" => ("format", null),
            _ => (null, new[] { "--write", SonarProfileOption }.Concat(PreviewCommand.Options).ToArray()),
        };
        var options = args.Skip(command is null ? 0 : 1).ToArray();
        if (known is not null && options.FirstOrDefault(a => a.StartsWith('-') && !known.Contains(a.Split('=')[0])) is { } unknown)
        {
            Console.Error.WriteLine($"Unknown option: {unknown}");
            Console.Error.WriteLine(Usage);
            return 1;
        }

        if (command != "baseline" && PreviewCommand.Wants(options))
        {
            if (options.Contains("--write"))
            {
                Console.Error.WriteLine("--diff previews without writing anything: use --diff or --write, not both.");
                return 1;
            }

            return PreviewCommand.Run(command, options);
        }

        switch (command)
        {
            case "baseline":
                return BaselineCommand.Run(options);

            case "init":
                return InitCommand.Run(options);

            case "format":
                return FormatCommand.Run(options);

            default:
                return Migrate(options);
        }
    }

    /// <summary>stylebro-migrate [path] [--write]: the migration from StyleCop.</summary>
    public static int Migrate(string[] args)
    {
        var (profile, rest) = TakeOption(args, SonarProfileOption);
        var write = rest.Contains("--write");
        var root = Path.GetFullPath(rest.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        if (!ReadSonar(root, profile, out var sonar))
        {
            return 1;
        }

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root, sonar: sonar);
        var plan = Migration.Plan(setup, root, result);

        Console.WriteLine($"Read: {(setup.Sources.Count == 0 ? "no StyleCop settings (StyleCop's defaults)" : string.Join(", ", setup.Sources))}");
        if (setup.Version is { } version)
        {
            Console.WriteLine($"StyleCop.Analyzers version: {version}{(version.StartsWith("1.2", StringComparison.Ordinal) ? string.Empty : " (rules added in 1.2 count as off)")}");
        }

        Report(setup, result);
        if (result.Sonar is { } applied)
        {
            sonar!.Report(applied).ForEach(Console.WriteLine);
        }

        foreach (var note in result.Notes)
        {
            Console.WriteLine(note);
        }

        if (Migration.MigrationFolders(root) is { Count: > 0 } migrations)
        {
            Console.WriteLine($"EF Core migrations in {string.Join(", ", migrations)}: marked generated_code = true, so formatting and StyleBro leave them alone.");
        }

        if (Migration.VendoredFolders(root) is { Count: > 0 } vendored)
        {
            Console.WriteLine($"Vendored code in {string.Join(", ", vendored)}: marked generated_code = true, so formatting and StyleBro leave it alone.");
        }

        var suppressions = RewriteSuppressions(root, Suppressions.WithSonar(result.Replacements), write);
        Console.WriteLine(suppressions.Added == 0
            ? "Suppressions: none in the code to carry over."
            : $"Suppressions: {suppressions.Added} {(write ? "added" : "to add")} for the replacing rules in {suppressions.Files} files (the original ones stay).");

        Console.WriteLine();
        foreach (var (file, sections) in plan)
        {
            var path = Path.Combine(root, file);
            var block = Migration.Render(sections);
            if (write)
            {
                var existing = File.Exists(path) ? File.ReadAllText(path) : null;
                File.WriteAllText(path, Migration.Apply(existing, block));
                Console.WriteLine($"Wrote the settings to {file}.");
            }
            else
            {
                Console.WriteLine($"== {file}");
                Console.Write(block);
                Console.WriteLine();
            }
        }

        // Every Directory.Build.props a project imports: a nested one shadows the root's.
        foreach (var props in Migration.PropsFiles(root))
        {
            var disabled = Migration.DisablePreset(File.Exists(props) ? File.ReadAllText(props) : null);
            var name = Path.GetRelativePath(root, props).Replace('\\', '/');
            if (disabled is null)
            {
                continue;
            }

            if (write)
            {
                File.WriteAllText(props, disabled);
                Console.WriteLine($"Turned the preset off in {name} (<StyleBroPreset>none</StyleBroPreset>): the settings above replace it.");
            }
            else
            {
                Console.WriteLine($"--write also turns the preset off in {name} (<StyleBroPreset>none</StyleBroPreset>): the settings replace it.");
            }
        }

        Console.WriteLine(write
            ? NextStep
            : "Run with --write to put these settings into the .editorconfig files and carry the suppressions over.");
        PrintWorkspaceHint(root);
        return 0;
    }

    /// <summary>When the folder has several solutions or projects, which ones, and that format needs one named.</summary>
    internal static void PrintWorkspaceHint(string root)
    {
        if (BaselineCommand.WorkspaceCandidates(root) is { Count: > 1 } several)
        {
            Console.WriteLine($"This folder has several solutions or projects ({string.Join(", ", several)}): name the one to format, e.g. 'stylebro-migrate format {several[0]}'.");
        }
    }

    /// <summary>
    /// An option with a value ('--name value' or '--name=value'): the value (null when it's not there), and the other
    /// arguments.
    /// </summary>
    internal static (string? Value, string[] Others) TakeOption(string[] args, string name)
    {
        var rest = args.ToList();
        var at = rest.FindIndex(a => a == name || a.StartsWith(name + "=", StringComparison.Ordinal));
        if (at < 0)
        {
            return (null, args);
        }

        var value = rest[at].Length > name.Length ? rest[at].Substring(name.Length + 1) : at + 1 < rest.Count ? rest[at + 1] : string.Empty;
        rest.RemoveRange(at, rest[at].Length > name.Length || at + 1 >= rest.Count ? 1 : 2);
        return (value, rest.ToArray());
    }

    /// <summary>
    /// The SonarQube setup at the root (<paramref name="profile"/>: --sonar-profile's file), or null without one; false
    /// (with the error printed) when the profile isn't a readable file.
    /// </summary>
    internal static bool ReadSonar(string root, string? profile, out SonarSetup? sonar)
    {
        sonar = null;
        if (profile is not null && !File.Exists(profile))
        {
            Console.Error.WriteLine($"{SonarProfileOption}: not a file: {profile}");
            return false;
        }

        try
        {
            sonar = SonarSetup.Read(root, profile is null ? null : Path.GetFullPath(profile));
            return true;
        }
        catch (System.Xml.XmlException e)
        {
            Console.Error.WriteLine($"{SonarProfileOption}: {profile} isn't a quality profile backup ({e.Message}).");
            return false;
        }
    }

    /// <summary>
    /// A dropped rule's reason as the report shows it. The mapping is written for the project's own docs and carries survey
    /// evidence (finding counts in the surveyed repositories, which decision it was); users only need the reason.
    /// </summary>
    internal static string UserFacingReason(string proposal)
    {
        var reason = Regex.Replace(proposal, @"^(Drop|Not applicable)[:.]\s*", string.Empty);
        reason = Regex.Replace(reason, @"\s*\([^)]*\buser decision\)", string.Empty);
        var sentences = Regex.Split(reason, @"(?<=\.)\s+").Where(s => !Regex.IsMatch(s, @"\bfindings?\b|private app", RegexOptions.IgnoreCase));
        return string.Join(" ", sentences).Trim();
    }

    internal static Dictionary<string, string> LoadMapping()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StyleCopMapping.csv")
            ?? throw new InvalidOperationException("The StyleCop mapping isn't embedded.");
        using var reader = new StreamReader(stream);
        reader.ReadLine();
        var mapping = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var comma = line.IndexOf(',');
            var proposal = line.Substring(comma + 1).Trim('"').Replace("\"\"", "\"").Replace("**", string.Empty).Replace("`", string.Empty);
            mapping[line.Substring(0, comma)] = proposal;
        }

        return mapping;
    }

    /// <summary>
    /// Carries the suppressions in the code over (<see cref="Suppressions"/>): StyleCop's and Sonar's ids, to the rules that
    /// replace them. Returns how many were (or would be) added, in how many files.
    /// </summary>
    internal static (int Added, int Files) RewriteSuppressions(string root, IReadOnlyDictionary<string, SortedSet<string>> replacements, bool write)
    {
        int added = 0;
        int files = 0;
        var mentions = new Regex(@"\bS[AX]?\d{3,4}\b");
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || StyleCopSetup.IsMSBuild(f)))
        {
            var bytes = File.ReadAllBytes(file);
            var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
            if (!mentions.IsMatch(text))
            {
                continue;
            }

            var (rewritten, count) = StyleCopSetup.IsMSBuild(file)
                ? Suppressions.RewriteNoWarn(text, replacements)
                : Suppressions.Rewrite(text, replacements);
            if (count == 0)
            {
                continue;
            }

            added += count;
            files++;
            if (write)
            {
                File.WriteAllText(file, rewritten, new UTF8Encoding(bom));
            }
        }

        return (added, files);
    }

    /// <summary>Which StyleCop rules are on, and why the ones StyleBro and the SDK don't cover aren't.</summary>
    private static void Report(StyleCopSetup setup, Migration.Result result)
    {
        // SA0001 says that XML documentation isn't parsed: with a GenerateDocumentationFile project it can't fire, so it
        // isn't a rule the repository loses.
        var cannotFire = Migration.CannotFire(setup);
        var on = StyleCopSetup.Rules.Where(r => setup.IsOn(r.Id) && !(r.Id == "SA0001" && setup.DocumentationParsed) && !cannotFire.Contains(r.Id)).Select(r => r.Id).ToList();
        var uncovered = on.Where(id => !result.Covered.Contains(id)).ToList();
        Console.WriteLine($"StyleCop rules on: {on.Count}; enforced by StyleBro or the SDK after migrating: {on.Count - uncovered.Count}");
        if (uncovered.Count == 0)
        {
            return;
        }

        var mapping = LoadMapping();
        var titles = StyleCopSetup.Rules.ToDictionary(r => r.Id, r => r.Title);
        var groups = uncovered.GroupBy(id =>
            result.Reasons.ContainsKey(id) ? "Partly covered or not expressible"
            : mapping.TryGetValue(id, out var proposal) && (proposal.StartsWith("Drop", StringComparison.Ordinal) || proposal.StartsWith("Not applicable", StringComparison.Ordinal)) ? "Dropped by design (no safe automatic fix, or not applicable)"
            : "Not covered yet");
        Console.WriteLine($"Not enforced after removing StyleCop ({uncovered.Count}):");
        foreach (var group in groups.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"  {group.Key}:");
            foreach (var id in group)
            {
                var reason = result.Reasons.TryGetValue(id, out var r) ? r
                    : group.Key.StartsWith("Dropped", StringComparison.Ordinal) ? UserFacingReason(mapping[id])
                    : titles[id];
                Console.WriteLine($"    {id}{(reason.Length == 0 ? string.Empty : ": " + reason)}");
            }
        }
    }
}
