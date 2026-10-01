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
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
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
            ? "Next: add the StyleBro.Analyzers package, remove StyleCop.Analyzers, and run 'dotnet format'."
            : "Run with --write to put these settings into the .editorconfig files and carry the suppressions over.");
        return 0;
    }

    /// <summary>Which StyleCop rules are on, and why the ones StyleBro and the SDK don't cover aren't.</summary>
    private static void Report(StyleCopSetup setup, Migration.Result result)
    {
        var on = StyleCopSetup.Rules.Where(r => setup.IsOn(r.Id)).Select(r => r.Id).ToList();
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
                    : group.Key.StartsWith("Dropped", StringComparison.Ordinal) ? Regex.Replace(mapping[id], @"^(Drop|Not applicable)[:.]\s*", string.Empty)
                    : titles[id];
                Console.WriteLine($"    {id}{(reason.Length == 0 ? string.Empty : ": " + reason)}");
            }
        }
    }

    private static (int Added, int Files) RewriteSuppressions(string root, Migration.Result result, bool write)
    {
        int added = 0, files = 0;
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

    /// <summary>docs/stylecop-mapping.md's proposal for every StyleCop rule, without Markdown.</summary>
    private static Dictionary<string, string> LoadMapping()
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
}
