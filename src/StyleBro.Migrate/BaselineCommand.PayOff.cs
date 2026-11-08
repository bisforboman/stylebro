using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StyleBro.Analyzers.Baseline;

namespace StyleBro.Migrate;

/// <summary>
/// bro baseline --pay-off &lt;folder or file&gt;...: fixes the baselined findings in those files (backlog 086,
/// docs/proposals/agents-in-consumer-repos.md, section 3). It takes their entries out of every baseline that has them and
/// runs 'format --files' on the files the entries name: format fixes what it can and puts the findings it keeps on purpose
/// back (as it does after any clean run), so the entries left for those files are the kept ones. Unlike turning the
/// baseline off with StyleBroBaseline=none, it works where a Directory.Build.props sets &lt;StyleBroBaseline&gt; without a
/// condition, and the other folders' entries stay in force.
/// </summary>
internal static partial class BaselineCommand
{
    public const string PayOffOption = "--pay-off";

    /// <summary>
    /// The pay-off. <paramref name="paths"/>: folders and files, relative to <paramref name="current"/>; <paramref name="format"/>
    /// runs 'bro format' with the arguments given (a test passes its own). Format's exit code, or 1 for a usage error.
    /// </summary>
    public static int PayOff(IReadOnlyList<string> paths, string current, Func<string[], int> format)
    {
        if (paths.Count == 0)
        {
            Console.Error.WriteLine($"{PayOffOption} needs the folders or files to pay off: bro baseline {PayOffOption} src/Orders");
            return 1;
        }

        var scope = new List<string>();
        foreach (var path in paths)
        {
            var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(current, path)));
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                Console.Error.WriteLine($"Not found: {path}");
                return 1;
            }

            scope.Add(full);
        }

        bool InScope(string file) => scope.Any(s => file.Equals(s, StringComparison.OrdinalIgnoreCase) || file.StartsWith(s + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

        // The baselines the builds of those files read (each folder's: a Directory.Build.props's <StyleBroBaseline>, else the
        // nearest above), with their text before, the entries taken out and the files those entries name.
        var taken = new List<(string File, string Before, Dictionary<Baseline.Key, int> Entries)>();
        foreach (var file in scope.Select(s => FormatCommand.BaselineFile(Directory.Exists(s) ? s : Path.GetDirectoryName(s)!)).OfType<string>().Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var before = File.ReadAllText(file);
            var folder = Path.GetDirectoryName(file)!;
            var entries = Baseline.Parse(before).Entries;
            var mine = entries.Where(e => InScope(FullPath(folder, e.Key.Path))).ToDictionary(e => e.Key, e => e.Value);
            if (mine.Count > 0)
            {
                File.WriteAllText(file, new Baseline(entries.Where(e => !mine.ContainsKey(e.Key)).ToDictionary(e => e.Key, e => e.Value)).Render());
                taken.Add((file, before, mine));
            }
        }

        var names = string.Join(", ", paths);
        if (taken.Count == 0)
        {
            Console.WriteLine($"Nothing to pay off: no baseline has entries for {names}.");
            return 0;
        }

        var findings = taken.Sum(t => t.Entries.Values.Sum());
        var files = taken.SelectMany(t => t.Entries.Keys.Select(k => FullPath(Path.GetDirectoryName(t.File)!, k.Path))).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (files.Count == 0)
        {
            Console.WriteLine($"Baseline: removed {Plural(taken.Sum(t => t.Entries.Count), "entry", "entries")} for files that are gone ({names}).");
            return 0;
        }

        Console.WriteLine($"Paying off {Plural(findings, "baselined finding", "baselined findings")} in {Plural(files.Count, "file", "files")} ({names}): formatting them with their baseline entries taken out.");
        var code = format([FormatCommand.FilesOption, .. files]);

        // A run that didn't end clean (exit code 2, 3 or an error) may have left findings it would fix: the entries for lines
        // that are still there go back, so the build doesn't fail on them. After a clean run the lines left are kept findings,
        // which format put back itself.
        var back = 0;
        foreach (var (file, _, entries) in taken)
        {
            var folder = Path.GetDirectoryName(file)!;
            var now = File.Exists(file) ? Baseline.Parse(File.ReadAllText(file)).Entries.ToDictionary(e => e.Key, e => e.Value) : [];
            if (code != 0)
            {
                foreach (var (key, count) in entries.Where(e => StillThere(FullPath(folder, e.Key.Path), e.Key.Fingerprint)))
                {
                    now[key] = now.TryGetValue(key, out var had) ? Math.Max(had, count) : count;
                }

                File.WriteAllText(file, new Baseline(now).Render());
            }

            back += now.Where(e => InScope(FullPath(folder, e.Key.Path))).Sum(e => e.Value);
        }

        var paid = Math.Max(0, findings - back);
        var stay = back == 0 ? string.Empty : code == 0
            ? $"; {back} kept on purpose {(back == 1 ? "stays" : "stay")} in the baseline (the reasons are above)"
            : $"; the run didn't end clean (exit code {code}), so the entries for the {Plural(back, "finding", "findings")} on lines it left are back in the baseline";
        Console.WriteLine($"Baseline: paid off {paid} of {Plural(findings, "baselined finding", "baselined findings")} in {names}{stay}.");
        if (taken.Select(t => GitAttributes.AddBaselineLine(current, t.File)).OfType<string>().FirstOrDefault() is { } attributes)
        {
            Console.WriteLine(attributes);
        }

        return code;
    }

    /// <summary>The full path of a baseline entry's file (its path is relative to the baseline's folder, '/'-separated).</summary>
    private static string FullPath(string folder, string path) => Path.GetFullPath(Path.Combine(folder, path.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Whether the file still has a line with that fingerprint.</summary>
    private static bool StillThere(string file, string fingerprint) =>
        File.Exists(file) && File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Any(l => Baseline.Fingerprint(l) == fingerprint);

    private static string Plural(int count, string one, string more) => $"{count} {(count == 1 ? one : more)}";
}
