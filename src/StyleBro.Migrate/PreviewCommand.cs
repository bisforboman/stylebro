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
    public static readonly string[] Options = { "--diff", "--keep", "--all", "--project", "--no-restore" };

    /// <summary>Why a '--no-restore' preview stops when the repository has no restore output to copy.</summary>
    internal const string NoRestoreOutput = "--no-restore: the repository has no restore output (obj/project.assets.json), so 'dotnet format' would load no project and only fix whitespace. Run 'dotnet restore' first, or preview without --no-restore.";

    /// <summary>The label of files nothing was reported in.</summary>
    private const string Other = "other";

    /// <summary>StyleCop referenced as a GlobalPackageReference.</summary>
    private static readonly Regex GlobalStyleCop = new(@"<GlobalPackageReference\b[^>]*?\bInclude\s*=\s*""StyleCop\.Analyzers(?:\.Unstable)?""", RegexOptions.IgnoreCase);

    /// <summary>Why '--diff' doesn't run on old-style (non-SDK) projects, and what to do instead.</summary>
    public static string OldStyleUnsupported(IReadOnlyList<string> projects, string? command)
    {
        var write = command switch { "init" => "'stylebro-migrate init --write'", "format" => null, _ => "'stylebro-migrate --write'" };
        return $"--diff doesn't work with old-style (non-SDK) projects ({string.Join(", ", projects.Take(3))}{(projects.Count > 3 ? $" and {projects.Count - 3} more" : string.Empty)}): "
            + "the copy has no git-ignored packages folder, and 'dotnet restore' can't restore packages.config.\n"
            + "Instead, on a clean working tree: " + (write is null ? string.Empty : $"run {write}, ")
            + "restore with Visual Studio's MSBuild (msbuild -t:restore -p:RestorePackagesConfig=true), run 'stylebro-migrate format' and review 'git diff' ('git checkout .' undoes it). "
            + "See https://bisforboman.github.io/stylebro/getting-started/#old-style-non-sdk-projects.";
    }

    /// <summary>Whether the options ask for a preview.</summary>
    public static bool Wants(IEnumerable<string> options) => options.Any(o => o == "--diff" || o.StartsWith("--diff=", StringComparison.Ordinal));

    /// <summary>
    /// Runs the preview. <paramref name="command"/>: null (the migration), "init" or "format"; <paramref name="format"/>:
    /// stands in for <see cref="FormatCommand.Run"/> in tests.
    /// </summary>
    public static int Run(string? command, string[] options, Func<string[], Action<string>, int>? format = null)
    {
        format ??= FormatCommand.RunPlain;
        var clock = Stopwatch.StartNew();
        var patch = Path.GetFullPath(options.FirstOrDefault(o => o.StartsWith("--diff=", StringComparison.Ordinal))?.Substring("--diff=".Length) ?? DefaultPatch);
        var keep = options.Contains("--keep");
        var (profile, others) = command == "format" ? (null, options) : Program.TakeOption(options, Program.SonarProfileOption);
        var rest = others.Where(o => !Wants(new[] { o }) && o != "--keep").ToList();

        // --project names what format runs on (relative to the path) when the folder has several; format takes it too
        // (passed on, 'dotnet format' printed its usage text).
        var projectIndex = rest.IndexOf("--project");
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

        // Old-style projects need Visual Studio's restore: the copy has no git-ignored packages folder, and 'dotnet restore'
        // can't restore packages.config (xbox-live-developer-tools: every project skipped, "Run 'dotnet restore'").
        if (FormatCommand.OldStyleProjects(File.Exists(path) ? Path.GetDirectoryName(path)! : path) is { Count: > 0 } oldStyle)
        {
            Console.Error.WriteLine(OldStyleUnsupported(oldStyle, command));
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
        void LogFile(string line)
        {
            lock (log)
            {
                log.WriteLine(line);
            }
        }

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
            var noRestore = rest.Contains("--no-restore");
            var restored = noRestore && CopyRestoreOutput(source, copy) > 0;

            MakeRepository(copy);
            var baseline = Head(copy);

            if (command != "format")
            {
                // Its output goes to the log file only: on a failure Settings prints it, and the end of the log shown then is
                // format's (the init report came twice before).
                var settings = Settings(command, target, rest.Contains("--modernize"), rest.Contains(AgentsFile.Option), profile, LogFile);
                if (settings != 0)
                {
                    return Fail(tail, settings, $"{(command == "init" ? "'stylebro-migrate init --write'" : "'stylebro-migrate --write'")} failed (exit code {settings}, see above).");
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
                if (noRestore)
                {
                    rest.Remove("--no-restore");
                    Console.WriteLine("  --no-restore: ignored, the copy needs a restore for that reference.");
                }
            }
            else if (noRestore && !restored)
            {
                // Without the restore's output 'dotnet format --no-restore' loads no project: whitespace only, and "StyleBro
                // reported nothing" (obj deleted).
                return Fail(tail, 1, NoRestoreOutput);
            }

            var formatArgs = new[] { project is null ? target : Path.Combine(target, project) }.Concat(rest.Where(o => o != "--modernize" && o != AgentsFile.Option)).ToList();

            // The first run's report says what format fixes (a '--verify-no-changes' run first reported the same, at the
            // cost of a whole run). Later runs are FormatCommand.NextRun's.
            var reportFolder = Path.Combine(temp, "report");
            var reportFile = Path.Combine(reportFolder, "format-report.json");
            string? notLoaded = null;
            var changed = new List<bool>();
            var tree = Tree(copy);
            for (var run = 1; run <= MaxRuns; run++)
            {
                var code = 0;
                var runArgs = run == 1 ? formatArgs.Concat(new[] { "--report", reportFolder }) : FormatCommand.NextRun(formatArgs.ToArray());
                Step($"Format run {run}", () => code = format(runArgs.Append(FormatCommand.OnceOption).ToArray(), Log));

                if (code != 0)
                {
                    return Fail(tail, code, $"'stylebro-migrate format' failed (exit code {code}).");
                }

                if (run == 1)
                {
                    if (!File.Exists(reportFile))
                    {
                        return Fail(tail, 1, "'dotnet format' wrote no report.");
                    }

                    // Printed once, after the summary.
                    notLoaded = NotLoadedWarning(File.ReadAllText(reportFile));
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
            foreach (var (id, file, line, also) in Samples(attribution, 3))
            {
                Console.WriteLine($"Sample ({id}, {file}{(also.Length == 0 ? string.Empty : $"; {string.Join(", ", also)} changed this file too")}):");
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

            // A project that didn't load is left out, as 'stylebro-migrate format' does (StabilityMatrix: a legacy project the
            // solution doesn't build, already failing with NU1008, stopped the whole preview), and named at the end.
            if (incomplete is not null)
            {
                Console.WriteLine($"WARNING: {incomplete}");
                Console.WriteLine("  The preview has no changes for them. To include them: run 'dotnet restore' (or 'dotnet build') in the repository, fix what it reports, then preview again.");
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

    /// <summary>
    /// One sample per top rule (not <see cref="Other"/>): a file and its first line, preferring files no other rule changed
    /// (the report's line numbers come from documents earlier fixes already changed, so in a file several rules changed the
    /// hunk may show another rule's change: Stateless' BRO1603 sample showed BRO1606's sentence), then files not sampled yet.
    /// <c>Also</c>: the other rules that changed the file, for the label.
    /// </summary>
    public static IEnumerable<(string Id, string File, int Line, string[] Also)> Samples(Dictionary<string, SortedDictionary<string, int>> attribution, int count)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        string[] Others(string id, string file) => attribution.Where(a => a.Key != id && a.Value.ContainsKey(file)).Select(a => a.Key).Order(StringComparer.Ordinal).ToArray();
        foreach (var (id, files) in attribution.Where(a => a.Key != Other).OrderByDescending(a => a.Value.Count).ThenBy(a => a.Key, StringComparer.Ordinal).Take(count))
        {
            var (file, line) = files.OrderBy(f => Others(id, f.Key).Length).ThenBy(f => used.Contains(f.Key)).First();
            used.Add(file);
            yield return (id, file, line, Others(id, file));
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
    /// docs/getting-started.md says: next to the StyleCop.Analyzers references (a project that removes StyleCop's gets a
    /// Remove item for StyleBro's too), else to the Directory.Build.props every project imports
    /// (<see cref="Migration.PropsFiles"/>: a nested one shadows the root's). Where central package management is on for a
    /// file (<see cref="CentralPackages"/>), the version goes into that Directory.Packages.props. Returns what it edited, or null.
    /// </summary>
    public static string? AddPackage(string root)
    {
        var files = StyleCopSetup.EnumerateFiles(root).ToList();
        if (ReferencesStyleBro(files))
        {
            return null;
        }

        var version = typeof(PreviewCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";
        var (_, styleCop, removes) = StyleCopSetup.Projects(root, files);
        var targets = styleCop.Count > 0 ? styleCop : Migration.PropsFiles(root);
        var versions = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var optedOut = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in targets)
        {
            // A GlobalPackageReference (central package management) gets one of its own: a PackageReference with a version
            // there fails the restore (NU1008), and one without adds the package a second time.
            if (File.Exists(file) && GlobalStyleCop.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(file))))
            {
                EditFile(file, text => AddItem(text, $"<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"{version}\" />"));
                continue;
            }

            var packages = CentralPackages(file, root);
            if (packages is not null && versions.Add(packages))
            {
                EditFile(packages, text => AddPackageVersion(text!, $"<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"{version}\" />"));
            }

            // StabilityMatrix: its analyzer projects turn central package management off, and the version-less reference from
            // Directory.Build.props failed their restore (NU1015). Such projects get a reference with a version instead (those
            // that remove StyleCop's reference remove StyleBro's too, below).
            var optOuts = packages is null ? [] : OptOuts(file, files, root).Where(p => !removes.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
            if (optOuts.Count > 0)
            {
                optedOut.UnionWith(optOuts);
                EditFile(file, text => AddItem(
                    text,
                    "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" Condition=\"'$(ManagePackageVersionsCentrally)' == 'true'\" />",
                    $"<PackageReference Include=\"StyleBro.Analyzers\" Version=\"{version}\" PrivateAssets=\"all\" Condition=\"'$(ManagePackageVersionsCentrally)' != 'true'\" />"));
                continue;
            }

            EditFile(file, text => AddItem(text, $"<PackageReference Include=\"StyleBro.Analyzers\"{(packages is not null ? string.Empty : $" Version=\"{version}\"")} PrivateAssets=\"all\" />"));
        }

        // LiteBus' analyzer project removes the StyleCop reference its Directory.Build.props adds, and turns central package
        // management off: StyleBro's version-less reference failed its restore (NU1015).
        foreach (var file in removes)
        {
            EditFile(file, text => AddItem(text, "<PackageReference Remove=\"StyleBro.Analyzers\" />"));
        }

        string Names(IEnumerable<string> paths) => string.Join(", ", paths.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')));
        return $"StyleBro.Analyzers {version} to {Names(targets)}"
            + (versions.Count > 0 ? $" (version in {Names(versions)})" : string.Empty)
            + (optedOut.Count > 0 ? $", with a version for {Names(optedOut)} (central package management off there)" : string.Empty)
            + (removes.Count > 0 ? $", removed again in {Names(removes)} (they remove StyleCop.Analyzers)" : string.Empty);
    }

    /// <summary>Whether an MSBuild file under the root mentions StyleBro.Analyzers (a reference to it).</summary>
    public static bool ReferencesStyleBro(string root) => ReferencesStyleBro(StyleCopSetup.EnumerateFiles(root));

    /// <summary>
    /// The Directory.Packages.props that holds the versions for an MSBuild file's package references, or null when central
    /// package management is off there: the nearest one above the file, when it, the file or a Directory.Build.props above
    /// the file sets ManagePackageVersionsCentrally to true (and the file itself doesn't set it to false).
    /// </summary>
    public static string? CentralPackages(string file, string root)
    {
        bool Sets(string path, string value) => SetsCentral(path, value, root);
        if (Sets(file, "false"))
        {
            return null;
        }

        var rootFolder = Path.GetFullPath(root).TrimEnd('\\', '/');
        string? packages = null;
        var props = new List<string>();
        for (var folder = Path.GetDirectoryName(Path.GetFullPath(file)); folder is not null && folder.Length >= rootFolder.Length; folder = Path.GetDirectoryName(folder))
        {
            props.Add(Path.Combine(folder, "Directory.Build.props"));
            if (packages is null && File.Exists(Path.Combine(folder, "Directory.Packages.props")))
            {
                packages = StyleCopSetup.OnDisk(Path.Combine(folder, "Directory.Packages.props"));
            }
        }

        return packages is not null && props.Prepend(file).Prepend(packages).Any(f => Sets(f, "true")) ? packages : null;
    }

    /// <summary>
    /// The projects that get <paramref name="props"/> (a Directory.Build.props: those under its folder) and turn central
    /// package management off themselves.
    /// </summary>
    public static List<string> OptOuts(string props, IEnumerable<string> files, string root)
    {
        if (!Path.GetFileName(props).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(props))! + Path.DirectorySeparatorChar;
        return files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && Path.GetFullPath(f).StartsWith(folder, StringComparison.OrdinalIgnoreCase) && SetsCentral(f, "false", root)).ToList();
    }

    /// <summary>
    /// An MSBuild file with an ItemGroup holding <paramref name="items"/> before its last '&lt;/Project&gt;' (a new file
    /// without one), in the file's line endings.
    /// </summary>
    public static string AddItem(string? project, params string[] items)
    {
        var newLine = project?.Contains("\r\n", StringComparison.Ordinal) == true ? "\r\n" : "\n";
        var group = $"  <ItemGroup>{newLine}{string.Concat(items.Select(item => $"    {item}{newLine}"))}  </ItemGroup>{newLine}";
        var end = project?.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase) ?? -1;
        return end < 0 ? $"<Project>{newLine}{group}</Project>{newLine}" : project!.Substring(0, end) + group + project.Substring(end);
    }

    /// <summary>
    /// A Directory.Packages.props with <paramref name="item"/> (a PackageVersion) among its PackageVersion items, in
    /// alphabetical order of Include like the usual sorted list (else in an ItemGroup of its own at the end).
    /// </summary>
    public static string AddPackageVersion(string packages, string item)
    {
        var name = Regex.Match(item, @"Include=""([^""]+)""").Groups[1].Value;
        var versions = Regex.Matches(packages, @"<PackageVersion\b[^>]*?\bInclude\s*=\s*""([^""]+)""[^>]*?(?:/>|>.*?</PackageVersion\s*>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (versions.Count == 0)
        {
            return AddItem(packages, item);
        }

        var newLine = packages.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var next = versions.FirstOrDefault(m => string.Compare(m.Groups[1].Value, name, StringComparison.OrdinalIgnoreCase) > 0);
        var anchor = next ?? versions[^1];
        var lineStart = packages.LastIndexOf('\n', anchor.Index) + 1;
        var indentation = packages.Substring(lineStart, anchor.Index - lineStart);
        indentation = indentation.Trim().Length == 0 ? indentation : "    ";
        return next is not null
            ? packages.Insert(anchor.Index, item + newLine + indentation)
            : packages.Insert(anchor.Index + anchor.Length, newLine + indentation + item);
    }

    /// <summary>
    /// Whether an MSBuild file sets ManagePackageVersionsCentrally to <paramref name="value"/>, itself or in what it imports
    /// (a nested Directory.Packages.props that imports the root's, which turns it on).
    /// </summary>
    private static bool SetsCentral(string path, string value, string root, int depth = 0)
    {
        // ponytail: text-level; a property set by a Condition isn't seen.
        return File.Exists(path)
            && (Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(path)), $@"<ManagePackageVersionsCentrally\b[^>]*>\s*{value}\s*<", RegexOptions.IgnoreCase)
                || (depth < 8 && StyleCopSetup.Imports(path, root).Any(i => SetsCentral(i, value, root, depth + 1))));
    }

    /// <summary>Makes the folder a git repository with everything in it committed.</summary>
    public static void MakeRepository(string folder)
    {
        Git(folder, "init", "-q");

        // The format runs restore and build into bin/obj: not changes to show, also where no .gitignore says so.
        File.AppendAllText(Path.Combine(folder, ".git", "info", "exclude"), "\nbin/\nobj/\n");
        Commit(folder, "baseline");
    }

    /// <summary>
    /// Rewrites a file (null text: it doesn't exist yet) keeping its UTF-8 byte order mark: File.ReadAllText drops it and
    /// File.WriteAllText wouldn't write it back (Soulseek.NET's .csproj lost its BOM in the preview).
    /// </summary>
    public static void EditFile(string file, Func<string?, string> edit)
    {
        var bytes = File.Exists(file) ? File.ReadAllBytes(file) : null;
        var bom = bytes is [0xEF, 0xBB, 0xBF, ..];
        var text = bytes is null ? null : Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
        File.WriteAllText(file, edit(text), new UTF8Encoding(bom));
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

    /// <summary>
    /// For '--no-restore': the restore's output (project.assets.json and the files next to it) copied into the copy, so
    /// 'dotnet format --no-restore' finds the packages restored in the repository. Returns how many projects' it found.
    /// </summary>
    internal static int CopyRestoreOutput(string source, string copy)
    {
        var count = 0;
        foreach (var assets in Directory.EnumerateFiles(source, "project.assets.json", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }))
        {
            count++;
            var folder = Path.GetDirectoryName(assets)!;
            foreach (var file in Directory.EnumerateFiles(folder).Where(f => Path.GetFileName(f) is "project.assets.json" or "project.nuget.cache" || f.EndsWith(".nuget.g.props", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".nuget.g.targets", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".nuget.dgspec.json", StringComparison.OrdinalIgnoreCase)))
            {
                CopyFile(file, Path.Combine(copy, Path.GetRelativePath(source, file)));
            }
        }

        return count;
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
    private static int Settings(string? command, string target, bool modernize, bool agentsMd, string? profile, Action<string> log)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        int code;
        try
        {
            var sonar = (profile is null ? Array.Empty<string>() : new[] { Program.SonarProfileOption, Path.GetFullPath(profile) })
                .Concat(agentsMd ? new[] { AgentsFile.Option } : Array.Empty<string>())
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

    private static bool ReferencesStyleBro(IEnumerable<string> files) =>
        files.Where(StyleCopSetup.IsMSBuild).Any(f => File.ReadAllText(f).Contains("StyleBro.Analyzers", StringComparison.OrdinalIgnoreCase));

    private static string Elapsed(TimeSpan time) => time.TotalMinutes >= 1 ? $"{(int)time.TotalMinutes}m {time.Seconds}s" : $"{time.TotalSeconds:0}s";

    /// <summary>Says why the preview failed, then the end of its log (format's output; the settings step prints its own).</summary>
    private static int Fail(IReadOnlyCollection<string> tail, int code, string reason)
    {
        Console.Error.WriteLine($"The preview failed: {reason}");
        if (tail.Count > 0)
        {
            Console.Error.WriteLine("The end of its log:");
            foreach (var line in tail)
            {
                Console.Error.WriteLine("  " + line);
            }
        }

        return code == 0 ? 1 : code;
    }
}
