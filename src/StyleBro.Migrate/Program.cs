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
        stylebro-migrate: move a repository to StyleBro (https://github.com/bisforboman/stylebro).

        Usage:
          stylebro-migrate [path] [--write]
              Coming from StyleCop: reads the StyleCop setup at 'path' (default: the current folder) and prints the
              StyleBro and .NET settings that enforce the same things. --write puts them into the .editorconfig files,
              turns StyleBro's preset off and carries StyleCop suppressions over.

          stylebro-migrate init [path] [--write] [--modernize]
              Without StyleCop: the built-in .NET rules StyleBro's preset relies on, for the root .editorconfig.
              --modernize adds the SDK's rules for newer C# and APIs. A repository with a StyleCop setup is told to
              use 'stylebro-migrate --write' instead.

          stylebro-migrate format [folder, solution or project] [--all] [dotnet format options]
              'dotnet format' that fixes StyleBro's rules and the built-in rules init/migrate turn on (plus whitespace),
              once per target framework in multi-targeted repositories, never inside git submodules. --all also applies
              every other analyzer's and compiler fix. Other options pass through (--verify-no-changes, --severity warn).

          stylebro-migrate baseline [path] [--project <solution or project>]
              Writes stylebro.baseline with today's violations, so only new code has to follow the rules.

          stylebro-migrate help | --help | -h
              This text.
        """;

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
            "init" => ("init", new[] { "--write", "--modernize" }),
            "format" => ("format", null),
            _ => (null, new[] { "--write" }),
        };
        var options = args.Skip(command is null ? 0 : 1).ToArray();
        if (known is not null && options.FirstOrDefault(a => a.StartsWith('-') && !known.Contains(a)) is { } unknown)
        {
            Console.Error.WriteLine($"Unknown option: {unknown}");
            Console.Error.WriteLine(Usage);
            return 1;
        }

        switch (command)
        {
            case "baseline":
                return BaselineCommand.Run(options);
            case "init":
                return InitCommand.Run(options);
            case "format":
                return FormatCommand.Run(options);
        }

        var write = args.Contains("--write");
        var root = Path.GetFullPath(args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? ".");
        if (!Directory.Exists(root))
        {
            Console.Error.WriteLine($"Not a directory: {root}");
            return 1;
        }

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);
        var plan = Migration.Plan(setup, root, result);

        Console.WriteLine($"Read: {(setup.Sources.Count == 0 ? "no StyleCop settings (StyleCop's defaults)" : string.Join(", ", setup.Sources))}");
        if (setup.Version is { } version)
        {
            Console.WriteLine($"StyleCop.Analyzers version: {version}{(version.StartsWith("1.2", StringComparison.Ordinal) ? string.Empty : " (rules added in 1.2 count as off)")}");
        }

        Report(setup, result);
        foreach (var note in result.Notes)
        {
            Console.WriteLine(note);
        }

        var suppressions = RewriteSuppressions(root, result, write);
        Console.WriteLine(suppressions.Added == 0
            ? "Suppressions: no StyleCop suppressions in the code to carry over."
            : $"Suppressions: {suppressions.Added} {(write ? "added" : "to add")} for the replacing rules in {suppressions.Files} files (the StyleCop ones stay).");

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

        var props = Path.Combine(root, "Directory.Build.props");
        var disabled = Migration.DisablePreset(File.Exists(props) ? File.ReadAllText(props) : null);
        if (disabled is not null)
        {
            if (write)
            {
                File.WriteAllText(props, disabled);
                Console.WriteLine("Turned the preset off in Directory.Build.props (<StyleBroPreset>none</StyleBroPreset>): the settings above replace it.");
            }
            else
            {
                Console.WriteLine("--write also turns the preset off in Directory.Build.props (<StyleBroPreset>none</StyleBroPreset>): the settings replace it.");
            }
        }

        Console.WriteLine(write
            ? NextStep
            : "Run with --write to put these settings into the .editorconfig files and carry the suppressions over.");
        return 0;
    }

    /// <summary>How to format after init or --write: 'stylebro-migrate format', not plain 'dotnet format'.</summary>
    internal const string FormatHint = "Next: run 'stylebro-migrate format'. Plain 'dotnet format' also applies every other analyzer's and the compiler's fixes.";

    /// <summary>What to do after --write: swap the packages and format.</summary>
    internal const string NextStep = "Next: add the StyleBro.Analyzers package, remove StyleCop.Analyzers, and run 'stylebro-migrate format'.";

    /// <summary>docs/stylecop-mapping.md's proposal for every StyleCop rule, without Markdown.</summary>
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

    /// <summary>Which StyleCop rules are on, and why the ones StyleBro and the SDK don't cover aren't.</summary>
    private static void Report(StyleCopSetup setup, Migration.Result result)
    {
        // SA0001 says that XML documentation isn't parsed: with a GenerateDocumentationFile project it can't fire, so it
        // isn't a rule the repository loses.
        var on = StyleCopSetup.Rules.Where(r => setup.IsOn(r.Id) && !(r.Id == "SA0001" && setup.DocumentationParsed)).Select(r => r.Id).ToList();
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

    private static (int Added, int Files) RewriteSuppressions(string root, Migration.Result result, bool write)
    {
        int added = 0;
        int files = 0;
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || StyleCopSetup.IsMSBuild(f)))
        {
            var bytes = File.ReadAllBytes(file);
            var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var text = Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
            if (!text.Contains("SA1") && !text.Contains("SA0"))
            {
                continue;
            }

            var (rewritten, count) = StyleCopSetup.IsMSBuild(file)
                ? Suppressions.RewriteNoWarn(text, result.Replacements)
                : Suppressions.Rewrite(text, result.Replacements);
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
}
