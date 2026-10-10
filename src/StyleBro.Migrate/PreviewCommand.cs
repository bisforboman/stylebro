using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// '--diff' for init, the migration and format (owner's decision 2026-10-09, docs/decisions.md): what the command would do
/// to the repository, without touching it. The repository is copied to a temporary folder (git's tracked and untracked,
/// not ignored files; submodules' working trees too, as repositories of their own so format still skips them), committed
/// there, and the command runs on the copy: init's or the migration's settings (plus the StyleBro.Analyzers reference
/// when the repository has none), then 'stylebro-migrate format' until a run changes nothing (at most <see cref="MaxRuns"/>).
/// A '--verify-no-changes --report' run before the fixes says which rules were reported in each file the format changed.
/// Prints a summary and writes the full diff to a patch file.
/// </summary>
internal static class PreviewCommand
{
    /// <summary>The patch file when '--diff' names none.</summary>
    public const string DefaultPatch = "stylebro-preview.patch";

    /// <summary>The most format runs a preview makes (a second one should change nothing).</summary>
    public const int MaxRuns = 3;

    /// <summary>The options the preview adds to init and the migration ('--diff=file' counts as '--diff').</summary>
    public static readonly string[] Options = { "--diff", "--keep", "--all", "--project" };

    /// <summary>The label of files nothing was reported in.</summary>
    private const string Other = "other";

    /// <summary>Whether the options ask for a preview.</summary>
    public static bool Wants(IEnumerable<string> options) => options.Any(o => o == "--diff" || o.StartsWith("--diff=", StringComparison.Ordinal));

    /// <summary>
    /// Runs the preview. <paramref name="command"/>: null (the migration), "init" or "format"; <paramref name="format"/>:
    /// stands in for <see cref="FormatCommand.Run"/> in tests.
    /// </summary>
    public static int Run(string? command, string[] options, Func<string[], Action<string>, int>? format = null)
    {
        format ??= (a, o) => FormatCommand.Run(a, o);
        var clock = Stopwatch.StartNew();
        var patch = Path.GetFullPath(options.FirstOrDefault(o => o.StartsWith("--diff=", StringComparison.Ordinal))?.Substring("--diff=".Length) ?? DefaultPatch);
        var keep = options.Contains("--keep");
        var (profile, others) = command == "format" ? (null, options) : Program.TakeOption(options, Program.SonarProfileOption);
        var rest = others.Where(o => !Wants(new[] { o }) && o != "--keep").ToList();

        // init and the migration: --project names what format runs on (relative to the path) when the folder has several.
        var projectIndex = command == "format" ? -1 : rest.IndexOf("--project");
        var project = projectIndex >= 0 && projectIndex + 1 < rest.Count ? rest[projectIndex + 1] : null;
        if (project is not null)
        {
            rest.RemoveRange(projectIndex, 2);
        }

        // format: an optional path first, the rest passes through; init and the migration: a path anywhere, --all passes through.
        var hasPath = command == "format" ? rest.Count > 0 && !rest[0].StartsWith('-') : rest.Any(o => !o.StartsWith('-'));
        var path = Path.GetFullPath(hasPath ? rest.First(o => !o.StartsWith('-')) : ".");
        if (hasPath)
        {
            rest.Remove(rest.First(o => !o.StartsWith('-')));
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Console.Error.WriteLine($"Not found: {path}");
            return 1;
        }

        // What format will run on, checked before anything runs: with several solutions it would fail after the settings step.
        if (project is null && Directory.Exists(path) && BaselineCommand.FindWorkspace(path) is null)
        {
            Console.Error.WriteLine(BaselineCommand.NoWorkspace(path, command == "format" ? "'stylebro-migrate format <file> --diff'" : "--project <file>"));
            return 1;
        }

        var folder = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        var source = Git(folder, "rev-parse", "--show-toplevel") is (0, var top) ? Path.GetFullPath(top.Trim()) : folder;
        var prefix = source == folder ? string.Empty : Git(folder, "rev-parse", "--show-prefix").Output.Trim();
        var temp = Directory.CreateTempSubdirectory("stylebro-preview-").FullName;
        var copy = Path.Combine(temp, Path.GetFileName(source.TrimEnd('\\', '/')));
        var target = Path.GetFullPath(Path.Combine(copy, prefix, File.Exists(path) ? Path.GetFileName(path) : string.Empty));
        using var log = new StreamWriter(Path.Combine(temp, "preview.log")) { AutoFlush = true };
        var tail = new Queue<string>();
        string? incomplete = null;
        void Log(string line)
        {
            lock (log)
            {
                if (line.StartsWith(FormatCommand.IncompleteMarker, StringComparison.Ordinal))
                {
                    incomplete ??= line;
                }

                log.WriteLine(line);
                tail.Enqueue(line);
                if (tail.Count > 30)
                {
                    tail.Dequeue();
                }
            }
        }

        var what = command switch
        {
            "init" => "'stylebro-migrate init --write' and 'stylebro-migrate format'",
            "format" => "'stylebro-migrate format'",
            _ => "'stylebro-migrate --write' and 'stylebro-migrate format'",
        };
        Console.WriteLine($"Previewing {what} on a copy; nothing in {source} changes.");
        var ok = false;
        try
        {
            Step($"Copying the repository to {copy}", () => Copy(source, copy));
            MakeRepository(copy);
            var baseline = Head(copy);

            if (command != "format")
            {
                var settings = Settings(command, target, rest.Contains("--modernize"), rest.Contains(AgentsFile.OptOut), profile, Log);
                if (settings != 0)
                {
                    return Fail(tail, settings);
                }
            }

            // The settings step wrote to the copy: the report describes the repository, which isn't written.
            JsonReport.Set("path", source);
            JsonReport.Set("write", false);

            var package = AddPackage(File.Exists(target) ? Path.GetDirectoryName(target)! : target);
            Commit(copy, "settings");
            var configured = Head(copy);
            if (command != "format" || package is not null)
            {
                Console.WriteLine(SettingsSummary(copy, baseline, configured));
            }

            if (package is not null)
            {
                Console.WriteLine($"  StyleBro.Analyzers wasn't referenced: added {package} for the preview, as https://bisforboman.github.io/stylebro/getting-started/ says (the patch includes it).");
            }

            var formatArgs = new[] { project is null ? target : Path.Combine(target, project) }.Concat(rest.Where(o => o != "--modernize" && o != AgentsFile.OptOut)).ToList();
            var reportFolder = Path.Combine(temp, "report");
            Step("Finding what format fixes ('dotnet format --verify-no-changes')", () => format(formatArgs.Concat(new[] { "--verify-no-changes", "--report", reportFolder, FormatCommand.OnceOption }).ToArray(), Log));
            var reportFile = Path.Combine(reportFolder, "format-report.json");
            if (!File.Exists(reportFile))
            {
                Console.Error.WriteLine("'dotnet format' wrote no report.");
                return Fail(tail, 1);
            }

            // A project that didn't load would show up as missing changes (a broken package cache: 58 files of side effects).
            if (incomplete is not null)
            {
                Console.Error.WriteLine($"The preview stopped. {incomplete}");
                Console.Error.WriteLine("  Run 'dotnet restore' (or 'dotnet build') in the repository and fix what it reports, then preview again.");
                return Fail(tail, 1);
            }

            var notLoaded = NotLoadedWarning(File.ReadAllText(reportFile));
            if (notLoaded is not null)
            {
                Console.WriteLine(notLoaded);
            }

            var changed = new List<bool>();
            var tree = Tree(copy);
            for (var run = 1; run <= MaxRuns; run++)
            {
                var code = 0;
                Step($"Format run {run}", () => code = format(formatArgs.Append(FormatCommand.OnceOption).ToArray(), Log));
                if (code != 0)
                {
                    Console.Error.WriteLine($"'stylebro-migrate format' failed (exit code {code}).");
                    return Fail(tail, code);
                }

                var next = Tree(copy);
                changed.Add(next != tree);
                JsonReport.AddRun(run, Git(copy, "diff", "--name-only", tree, next).Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(f => Path.Combine(source, f.Trim())), null);
                tree = next;
                if (!changed[^1])
                {
                    break;
                }
            }

            var files = Changed(copy, configured);
            var (attribution, changes) = Attribute(File.ReadAllText(reportFile), Path.GetFileName(temp) + "/" + Path.GetFileName(copy), files.Keys, f => WhitespaceOnly(copy, configured, f));
            JsonReport.Set("filesPerRule", new JsonObject(attribution.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => KeyValuePair.Create(a.Key, (JsonNode?)new JsonArray(a.Value.Keys.Select(f => (JsonNode?)f).ToArray())))));
            JsonReport.Set("changesPerRule", new JsonObject(JsonReport.ChangesPerRule(File.ReadAllText(reportFile)).Select(p => KeyValuePair.Create(p.Key, (JsonNode?)p.Value))));
            JsonReport.Set("clean", !changed[^1]);
            Console.WriteLine(FormatSummary(files.Count, changes, attribution, changed));
            foreach (var (id, file, line) in Samples(attribution, 3))
            {
                Console.WriteLine($"Sample ({id}, {file}):");
                foreach (var hunkLine in Hunk(Git(copy, "diff", "--cached", "-U0", configured, "--", file).Output, line, 12))
                {
                    Console.WriteLine("  " + hunkLine);
                }
            }

            Git(copy, "diff", "--cached", "--ignore-submodules", "--output=" + patch, baseline);
            JsonReport.Set("patch", patch);
            var lines = File.Exists(patch) ? File.ReadLines(patch).Count() : 0;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Full diff: {(Path.GetRelativePath(Environment.CurrentDirectory, patch) is var shown && !shown.StartsWith("..", StringComparison.Ordinal) ? shown : patch)} ({lines:N0} lines)"));
            if (notLoaded is not null)
            {
                Console.WriteLine(notLoaded);
            }

            ok = true;
            return 0;
        }
        finally
        {
            Console.WriteLine($"Done in {Elapsed(clock.Elapsed)}.");
            log.Dispose();
            if (keep || !ok)
            {
                Console.WriteLine($"The copy is kept: {copy} (log: {Path.Combine(temp, "preview.log")}).");
            }
            else
            {
                Delete(temp);
            }
        }
    }

    /// <summary>
    /// Copies a repository: git's tracked and untracked, not ignored files ('git ls-files -co --exclude-standard'), else
    /// (not a repository) everything outside bin, obj and .git. A submodule (or another repository inside) is copied the
    /// same way and made a repository of its own, so 'stylebro-migrate format' leaves it alone in the copy too.
    /// </summary>
    public static void Copy(string source, string target)
    {
        Directory.CreateDirectory(target);
        if (Git(source, "ls-files", "-co", "--exclude-standard", "-z") is not (0, var list))
        {
            CopyFolder(source, target);
            return;
        }

        foreach (var entry in list.Split('\0', StringSplitOptions.RemoveEmptyEntries).Select(e => e.TrimEnd('/')).Distinct(StringComparer.Ordinal))
        {
            var from = Path.Combine(source, entry);
            var to = Path.Combine(target, entry);
            if (File.Exists(from))
            {
                CopyFile(from, to);
            }
            else if (Directory.Exists(from) && StyleCopSetup.IsNestedRepository(from))
            {
                Copy(from, to);
                MakeRepository(to);
            }
            else if (Directory.Exists(from))
            {
                CopyFolder(from, to);
            }
        }
    }

    /// <summary>
    /// The rules behind each changed file: the ids reported in it ('WHITESPACE' counts as IDE0055), else IDE0055 when the
    /// change is whitespace only, else <see cref="Other"/>. Keys: rule id; values: the files (paths relative to the copy) with
    /// the first reported line of that rule in each (0 when unknown). <paramref name="copyMarker"/>: the copy's last two
    /// folder names, which find a reported path's part inside the copy whatever form the temporary folder's path took.
    /// Changes: the reported diagnostics in the changed files (each once, though every framework's run reports it).
    /// </summary>
    public static (Dictionary<string, SortedDictionary<string, int>> Rules, int Changes) Attribute(string reportJson, string copyMarker, IEnumerable<string> changedFiles, Func<string, bool> whitespaceOnly)
    {
        var reported = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        var counts = new Dictionary<string, HashSet<(string, int, int)>>(StringComparer.OrdinalIgnoreCase);
        using var report = JsonDocument.Parse(reportJson);
        var marker = "/" + copyMarker.Replace('\\', '/') + "/";
        foreach (var document in report.RootElement.EnumerateArray())
        {
            var file = (document.GetProperty("FilePath").GetString() ?? string.Empty).Replace('\\', '/');
            var at = file.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            var relative = at < 0 ? file : file.Substring(at + marker.Length);
            if (!reported.TryGetValue(relative, out var ids))
            {
                reported[relative] = ids = new Dictionary<string, int>(StringComparer.Ordinal);
                counts[relative] = new HashSet<(string, int, int)>();
            }

            foreach (var change in document.GetProperty("FileChanges").EnumerateArray())
            {
                var id = change.GetProperty("DiagnosticId").GetString() ?? Other;
                id = id == "WHITESPACE" ? "IDE0055" : id;
                var line = change.GetProperty("LineNumber").GetInt32();
                ids[id] = ids.TryGetValue(id, out var first) ? Math.Min(first, line) : line;
                counts[relative].Add((id, line, change.GetProperty("CharNumber").GetInt32()));
            }
        }

        var result = new Dictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
        var changes = 0;
        foreach (var file in changedFiles)
        {
            changes += counts.TryGetValue(file, out var count) ? count.Count : 0;
            var ids = reported.TryGetValue(file, out var found) && found.Count > 0
                ? found
                : new Dictionary<string, int> { [whitespaceOnly(file) ? "IDE0055" : Other] = 0 };
            foreach (var (id, line) in ids)
            {
                if (!result.TryGetValue(id, out var files))
                {
                    result[id] = files = new SortedDictionary<string, int>(StringComparer.Ordinal);
                }

                files[file] = line;
            }
        }

        return (result, changes);
    }

    /// <summary>
    /// The warning when the report before the fixes has no StyleBro rule at all, else null. With 150 rules that almost always
    /// means the analyzers didn't load (Bogus: the reference went into a Directory.Build.props its projects never imported,
    /// and the preview showed only the SDK's formatting).
    /// </summary>
    public static string? NotLoadedWarning(string reportJson)
    {
        using var report = JsonDocument.Parse(reportJson);
        var any = report.RootElement.EnumerateArray().SelectMany(d => d.GetProperty("FileChanges").EnumerateArray())
            .Any(c => c.GetProperty("DiagnosticId").GetString()?.StartsWith("BRO", StringComparison.Ordinal) == true);
        return any ? null : "WARNING: StyleBro reported nothing, so its analyzers probably didn't load (or the code already follows every rule).\n"
            + "  Check that every project gets the StyleBro.Analyzers reference: a nested Directory.Build.props shadows the root's unless it\n"
            + "  imports it, a reference in a conditional ItemGroup (one TargetFramework) misses the others, and RunAnalyzers=false turns analyzers off.";
    }

    /// <summary>The format part of the summary: files, reported changes, runs, and the rules by the number of files they changed.</summary>
    public static string FormatSummary(int files, int changes, Dictionary<string, SortedDictionary<string, int>> attribution, IReadOnlyList<bool> changedRuns)
    {
        var text = new StringBuilder();
        var runs = changedRuns.Count(c => c);
        var convergence = runs == 0 ? "nothing to change"
            : changedRuns[^1] ? $"still changing after {changedRuns.Count} runs (the diff has all of them)"
            : $"clean after {runs} run{(runs == 1 ? string.Empty : "s")}";
        text.Append(string.Create(CultureInfo.InvariantCulture, $"Format: {files:N0} file{(files == 1 ? string.Empty : "s")}, {changes:N0} reported change{(changes == 1 ? string.Empty : "s")}, {convergence}"));
        var titles = Titles();
        var ordered = attribution.OrderBy(a => a.Key == Other).ThenByDescending(a => a.Value.Count).ThenBy(a => a.Key, StringComparer.Ordinal).ToList();
        foreach (var (id, changed) in ordered.Take(10))
        {
            var title = titles.TryGetValue(id, out var t) ? t : id == Other ? "nothing reported (another fix's side effect)" : string.Empty;
            text.Append(string.Create(CultureInfo.InvariantCulture, $"\n  {id,-8} {(title.Length > 40 ? title.Substring(0, 37) + "..." : title),-40} {changed.Count,5:N0} file{(changed.Count == 1 ? string.Empty : "s")}"));
        }

        if (ordered.Count > 10)
        {
            text.Append($"\n  ... and {ordered.Count - 10} more rules");
        }

        return text.ToString();
    }

    /// <summary>One sample per top rule (not <see cref="Other"/>): its first file and line, a file not sampled yet where it can.</summary>
    public static IEnumerable<(string Id, string File, int Line)> Samples(Dictionary<string, SortedDictionary<string, int>> attribution, int count)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, files) in attribution.Where(a => a.Key != Other).OrderByDescending(a => a.Value.Count).ThenBy(a => a.Key, StringComparer.Ordinal).Take(count))
        {
            var (file, line) = files.FirstOrDefault(f => !used.Contains(f.Key)) is { Key: not null } unused ? unused : files.First();
            used.Add(file);
            yield return (id, file, line);
        }
    }

    /// <summary>
    /// The lines of the hunk (of a '-U0' diff) that covers <paramref name="line"/> of the old text (an insertion after line
    /// N covers N and N + 1), else the nearest one; at most <paramref name="max"/> lines. A byte order mark is left out
    /// (it printed as '?' before the first line of a file). A change nobody could see in the lines ('-}' / '+}' for a final
    /// newline, a byte order mark, blank lines) is described instead (<see cref="Invisible"/>).
    /// </summary>
    public static List<string> Hunk(string diff, int line, int max)
    {
        var hunks = new List<(int Start, int End, List<string> Lines)>();
        foreach (var text in diff.Replace("\r\n", "\n").Split('\n'))
        {
            var header = Regex.Match(text, @"^@@ -(\d+)(?:,(\d+))? ");
            if (header.Success)
            {
                var start = int.Parse(header.Groups[1].Value);
                var length = header.Groups[2].Success ? int.Parse(header.Groups[2].Value) : 1;
                hunks.Add((start, length == 0 ? start + 1 : start + length - 1, new List<string>()));
            }
            else if (hunks.Count > 0 && (text.StartsWith('+') || text.StartsWith('-') || text.StartsWith('\\')))
            {
                hunks[^1].Lines.Add(text);
            }
        }

        var hunk = hunks.OrderBy(h => line < h.Start ? h.Start - line : line > h.End ? line - h.End : 0).ThenBy(h => h.Start).FirstOrDefault();
        if (hunk.Lines is not null && Invisible(hunk.Lines) is { } described)
        {
            return new List<string> { described };
        }

        var lines = (hunk.Lines ?? new List<string>()).Where(l => !l.StartsWith('\\')).Select(l => l.Replace("﻿", string.Empty)).Take(max + 1).ToList();
        return lines.Count > max ? lines.Take(max).Append("  ...").ToList() : lines;
    }

    /// <summary>
    /// What a hunk changes when its lines look the same ('\ No newline at end of file' marks the line without one), or only
    /// blank lines come or go; null for a visible change.
    /// </summary>
    public static string? Invisible(IReadOnlyList<string> hunk)
    {
        static string Visible(string line) => line.Replace("﻿", string.Empty).TrimEnd();
        var removed = hunk.Where(l => l.StartsWith('-')).Select(l => l.Substring(1)).ToList();
        var added = hunk.Where(l => l.StartsWith('+')).Select(l => l.Substring(1)).ToList();
        bool NoNewlineAfter(char kind) => Enumerable.Range(1, Math.Max(0, hunk.Count - 1)).Any(i => hunk[i].StartsWith('\\') && hunk[i - 1].StartsWith(kind));
        if (removed.Select(Visible).SequenceEqual(added.Select(Visible)))
        {
            var what = new List<string>();
            var bomBefore = removed.Any(l => l.Contains('﻿'));
            var bomAfter = added.Any(l => l.Contains('﻿'));
            if (bomBefore != bomAfter)
            {
                what.Add(bomAfter ? "adds a byte order mark" : "removes the byte order mark");
            }

            if (NoNewlineAfter('-') != NoNewlineAfter('+'))
            {
                what.Add(NoNewlineAfter('-') ? "adds the final newline" : "removes the final newline");
            }

            if (!removed.Select(l => l.Replace("﻿", string.Empty)).SequenceEqual(added.Select(l => l.Replace("﻿", string.Empty))))
            {
                what.Add("removes trailing whitespace");
            }

            return "(" + (what.Count == 0 ? "changes only line endings or whitespace" : string.Join(", ", what)) + ")";
        }

        if (removed.Concat(added).All(string.IsNullOrWhiteSpace) && (removed.Count == 0) != (added.Count == 0))
        {
            var count = removed.Count + added.Count;
            return $"({(removed.Count > 0 ? "removes" : "adds")} {(count == 1 ? "a blank line" : $"{count} blank lines")})";
        }

        return null;
    }

    /// <summary>
    /// The settings part of the summary, from the diff between the copy's first two commits: lines added per settings file,
    /// and the suppressions the migration carried over, per kind.
    /// </summary>
    public static string SettingsSummary(string copy, string from, string to)
    {
        var parts = new List<string>();
        var suppressions = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (added, file) in NumStat(Git(copy, "diff", "--numstat", from, to).Output))
        {
            if (file.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)
                || file.Equals(AgentsFile.FileName, StringComparison.Ordinal)
                || Path.GetFileName(file).Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
            {
                var addedText = Git(copy, "diff", "-U0", from, to, "--", file).Output;
                var notes = new[] { ("StyleBroPreset>none", "StyleBroPreset=none"), ("Include=\"StyleBro.Analyzers\"", "StyleBro.Analyzers"), ("<NoWarn>", "NoWarn") }
                    .Where(n => addedText.Contains(n.Item1, StringComparison.Ordinal))
                    .Select(n => n.Item2)
                    .ToList();
                parts.Add($"{added} line{(added == 1 ? string.Empty : "s")} in {file}{(notes.Count == 0 ? string.Empty : $" ({string.Join(", ", notes)})")}");
                continue;
            }

            foreach (var line in Git(copy, "diff", "-U0", from, to, "--", file).Output.Split('\n').Where(l => l.StartsWith('+') && !l.StartsWith("+++", StringComparison.Ordinal)))
            {
                var kind = line.Contains("#pragma", StringComparison.Ordinal) ? "#pragma" : line.Contains("SuppressMessage", StringComparison.Ordinal) ? "[SuppressMessage]"
                    : line.Contains("NoWarn", StringComparison.Ordinal) ? "<NoWarn>" : "other lines";
                suppressions[kind] = suppressions.TryGetValue(kind, out var n) ? n + 1 : 1;
            }
        }

        if (suppressions.Count > 0)
        {
            parts.Add("suppressions carried over: " + string.Join(", ", suppressions.Select(s => $"{s.Value} {s.Key}")));
        }

        return "Settings: " + (parts.Count == 0 ? "nothing to write" : string.Join("; ", parts));
    }

    /// <summary>
    /// Adds the StyleBro.Analyzers reference (the tool's own version) when no project file mentions it, the way
    /// docs/getting-started.md says: to the Directory.Build.props every project imports (<see cref="Migration.PropsFiles"/>:
    /// a nested one shadows the root's); with central package management the version goes into Directory.Packages.props.
    /// Returns the file(s) edited, or null.
    /// </summary>
    public static string? AddPackage(string root)
    {
        if (StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild).Any(f => File.ReadAllText(f).Contains("StyleBro.Analyzers", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var version = typeof(PreviewCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";
        var packages = Path.Combine(root, "Directory.Packages.props");
        var props = Migration.PropsFiles(root);

        // The property is in Directory.Packages.props or (Polly) Directory.Build.props.
        var central = File.Exists(packages) && props.Prepend(packages).Any(f => File.Exists(f) && Regex.IsMatch(File.ReadAllText(f), @"<ManagePackageVersionsCentrally>\s*true", RegexOptions.IgnoreCase));
        if (central)
        {
            File.WriteAllText(packages, AddItem(File.ReadAllText(packages), $"<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"{version}\" />"));
        }

        foreach (var file in props)
        {
            File.WriteAllText(file, AddItem(File.Exists(file) ? File.ReadAllText(file) : null, $"<PackageReference Include=\"StyleBro.Analyzers\"{(central ? string.Empty : $" Version=\"{version}\"")} PrivateAssets=\"all\" />"));
        }

        var names = string.Join(", ", props.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')));
        return $"StyleBro.Analyzers {version} to {names}{(central ? " and Directory.Packages.props" : string.Empty)}";
    }

    /// <summary>
    /// An MSBuild file with an ItemGroup holding <paramref name="item"/> before its last '&lt;/Project&gt;' (a new file
    /// without one), in the file's line endings.
    /// </summary>
    public static string AddItem(string? project, string item)
    {
        var newLine = project?.Contains("\r\n", StringComparison.Ordinal) == true ? "\r\n" : "\n";
        var group = $"  <ItemGroup>{newLine}    {item}{newLine}  </ItemGroup>{newLine}";
        var end = project?.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase) ?? -1;
        return end < 0 ? $"<Project>{newLine}{group}</Project>{newLine}" : project!.Substring(0, end) + group + project.Substring(end);
    }

    /// <summary>Makes the folder a git repository with everything in it committed.</summary>
    public static void MakeRepository(string folder)
    {
        Git(folder, "init", "-q");

        // The format runs restore and build into bin/obj: not changes to show, also where no .gitignore says so.
        File.AppendAllText(Path.Combine(folder, ".git", "info", "exclude"), "\nbin/\nobj/\n");
        Commit(folder, "baseline");
    }

    /// <summary>Runs git; line endings and paths as they are (no autocrlf, no quoting), output as UTF-8.</summary>
    public static (int Code, string Output) Git(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var argument in new[] { "-c", "core.autocrlf=false", "-c", "core.safecrlf=false", "-c", "core.quotepath=off" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(start)!;
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            _ = error.Result;
            return (process.ExitCode, output);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (-1, string.Empty); // no git: the copy falls back to a plain folder copy, and the commits fail later
        }
    }

    /// <summary>The Sonar part of the report in the command's output (from 'Sonar: read' to the next empty line), or nothing.</summary>
    public static string SonarPart(string output) => Part(output, "Sonar: read ");

    /// <summary>The conventions init found in the code (from its header to the next empty line), or nothing.</summary>
    public static string DetectedPart(string output) => Part(output, InitCommand.DetectedHeader);

    internal static void Delete(string folder)
    {
        try
        {
            // git makes its object files read-only.
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(folder, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Couldn't delete the copy ({e.Message}): {folder}");
        }
    }

    private static string Part(string output, string firstLine)
    {
        var lines = output.Replace("\r\n", "\n").Split('\n');
        var start = Array.FindIndex(lines, l => l.StartsWith(firstLine, StringComparison.Ordinal));
        return start < 0 ? string.Empty : string.Join("\n", lines.Skip(start).TakeWhile(l => l.Length > 0)) + "\n";
    }

    /// <summary>
    /// Init's or the migration's '--write' on the copy; their output goes to the log (the dry run prints it), except the
    /// Sonar part of the report: which Sonar rules turned on which rules.
    /// </summary>
    private static int Settings(string? command, string target, bool modernize, bool noAgentsMd, string? profile, Action<string> log)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        int code;
        try
        {
            var sonar = (profile is null ? Array.Empty<string>() : new[] { Program.SonarProfileOption, Path.GetFullPath(profile) })
                .Concat(noAgentsMd ? new[] { AgentsFile.OptOut } : Array.Empty<string>())
                .ToArray();
            code = command == "init"
                ? InitCommand.Run(new[] { target, "--write" }.Concat(modernize ? new[] { "--modernize" } : Array.Empty<string>()).Concat(sonar).ToArray())
                : Program.Migrate(new[] { target, "--write" }.Concat(sonar).ToArray());
        }
        finally
        {
            Console.SetOut(original);
        }

        log(writer.ToString());
        if (code != 0)
        {
            Console.Write(writer.ToString());
        }
        else
        {
            Console.Write(SonarPart(writer.ToString()));
            Console.Write(DetectedPart(writer.ToString()));
            foreach (var line in writer.ToString().Replace("\r\n", "\n").Split('\n').Where(l => l.StartsWith("EF Core migrations", StringComparison.Ordinal) || l.StartsWith("Vendored code", StringComparison.Ordinal)))
            {
                Console.WriteLine(line);
            }
        }

        return code;
    }

    /// <summary>The rule titles: StyleBro's from the analyzers, the built-in rules' from the comments in init's templates.</summary>
    private static Dictionary<string, string> Titles()
    {
        var titles = Migration.StyleBroRules().ToDictionary(r => r.Id, r => r.Title.TrimEnd('.'), StringComparer.Ordinal);
        string? comment = null;
        foreach (var line in (InitCommand.Template() + "\n" + InitCommand.ModernizeTemplate()).Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith('#'))
            {
                comment = Regex.Replace(line.TrimStart('#', ' '), @"\s*[(:].*$", string.Empty);
            }
            else if (Regex.Match(line, @"dotnet_diagnostic\.(IDE\d+)\.severity") is { Success: true } match && comment is not null)
            {
                titles.TryAdd(match.Groups[1].Value, comment);
            }
        }

        titles["IDE0055"] = "Whitespace and formatting";
        return titles;
    }

    /// <summary>The changed files between a commit and the index after 'git add -A' (paths relative to the copy), with the lines added.</summary>
    private static Dictionary<string, int> Changed(string copy, string from)
    {
        Git(copy, "add", "-A");
        return NumStat(Git(copy, "diff", "--cached", "--numstat", "--ignore-submodules", from).Output).ToDictionary(n => n.File, n => n.Added, StringComparer.Ordinal);
    }

    private static bool WhitespaceOnly(string copy, string from, string file)
    {
        return Git(copy, "diff", "--cached", "--quiet", "--ignore-all-space", "--ignore-blank-lines", from, "--", file).Code == 0;
    }

    private static IEnumerable<(int Added, string File)> NumStat(string output)
    {
        foreach (var parts in output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r').Split('\t')).Where(p => p.Length == 3))
        {
            yield return (int.TryParse(parts[0], out var added) ? added : 0, parts[2]);
        }
    }

    /// <summary>The tree the working copy would commit: equal trees mean a format run changed nothing.</summary>
    private static string Tree(string copy)
    {
        Git(copy, "add", "-A");
        return Git(copy, "write-tree").Output.Trim();
    }

    private static void Commit(string folder, string message)
    {
        Git(folder, "add", "-A");
        Git(folder, "-c", "user.name=stylebro-preview", "-c", "user.email=preview@stylebro.invalid", "-c", "commit.gpgsign=false", "commit", "-q", "--no-verify", "--allow-empty", "-m", message);
    }

    private static string Head(string folder) => Git(folder, "rev-parse", "HEAD").Output.Trim();

    private static void CopyFolder(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source))
        {
            CopyFile(file, Path.Combine(target, Path.GetFileName(file)));
        }

        foreach (var child in Directory.EnumerateDirectories(source).Where(d => Path.GetFileName(d) is not ("bin" or "obj" or ".git" or ".vs")))
        {
            CopyFolder(child, Path.Combine(target, Path.GetFileName(child)));
        }
    }

    private static void CopyFile(string from, string to)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(to)!);
        File.Copy(from, to, overwrite: true);

        // A copy keeps the original's timestamp; temp-folder cleaners delete old files, also in a copy that is minutes old.
        File.SetLastWriteTimeUtc(to, DateTime.UtcNow);
    }

    private static void Step(string what, Action action)
    {
        Console.Write(what + "... ");
        var clock = Stopwatch.StartNew();
        action();
        Console.WriteLine(Elapsed(clock.Elapsed));
    }

    private static string Elapsed(TimeSpan time) => time.TotalMinutes >= 1 ? $"{(int)time.TotalMinutes}m {time.Seconds}s" : $"{time.TotalSeconds:0}s";

    private static int Fail(IEnumerable<string> tail, int code)
    {
        Console.Error.WriteLine("The preview failed. The end of its log:");
        foreach (var line in tail)
        {
            Console.Error.WriteLine("  " + line);
        }

        return code == 0 ? 1 : code;
    }
}
