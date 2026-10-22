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
/// when the repository has none), then 'bro format' until a run changes nothing (at most <see cref="MaxRuns"/>).
/// A '--verify-no-changes --report' run before the fixes says which rules were reported in each file the format changed.
/// Prints a summary and writes the full diff to a patch file.
/// </summary>
internal static class PreviewCommand
{
    /// <summary>The patch file's name when '--diff' names none (<see cref="DefaultPatchPath"/>).</summary>
    public const string DefaultPatch = "stylebro-preview.patch";

    /// <summary>The most format runs a preview makes (a second one should change nothing).</summary>
    public const int MaxRuns = 3;

    /// <summary>The options the preview adds to init and the migration ('--diff=file' counts as '--diff').</summary>
    public static readonly string[] Options = { "--diff", "--keep", "--all", "--project", "--no-restore" };

    /// <summary>Why a '--no-restore' preview stops when the repository has no restore output to copy.</summary>
    internal const string NoRestoreOutput = "--no-restore: the repository has no restore output (obj/project.assets.json), so 'dotnet format' would load no project and only fix whitespace. Run 'dotnet restore' first, or preview without --no-restore.";

    /// <summary>Why a preview that adds the StyleBro.Analyzers reference restores the copy despite --no-restore.</summary>
    internal const string NoRestoreIgnored = "  --no-restore: ignored, the copy needs a restore for the StyleBro.Analyzers reference the preview adds. That restore skips NuGet's audit, so vulnerability warnings treated as errors (NU1901-NU1904) don't stop it; other restore errors need fixing in the repository first.";

    /// <summary>The label of files nothing was reported in.</summary>
    private const string Other = "other";

    /// <summary>StyleCop referenced as a GlobalPackageReference.</summary>
    private static readonly Regex GlobalStyleCop = new(@"<GlobalPackageReference\b[^>]*?\bInclude\s*=\s*""StyleCop\.Analyzers(?:\.Unstable)?""", RegexOptions.IgnoreCase);

    /// <summary>A whole StyleCop.Analyzers (global) package reference element: self-closing, or up to its closing tag.</summary>
    private static readonly Regex StyleCopItem = new(@"<((?:Global)?PackageReference)\b[^>]*?\bInclude\s*=\s*""StyleCop\.Analyzers(?:\.Unstable)?""[^>]*?(?:/>|>.*?</\1\s*>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>Why '--diff' doesn't run on old-style (non-SDK) projects with packages.config, and what to do instead.</summary>
    public static string OldStyleUnsupported(IReadOnlyList<string> projects, string? command)
    {
        var write = command switch { "init" => "'bro init --write'", "format" => null, _ => "'bro migrate --write'" };
        return $"--diff doesn't work with packages.config projects ({string.Join(", ", projects.Take(3))}{(projects.Count > 3 ? $" and {projects.Count - 3} more" : string.Empty)}): "
            + "the copy has no git-ignored packages folder, and 'dotnet restore' can't restore packages.config.\n"
            + "Instead, on a clean working tree: " + (write is null ? string.Empty : $"run {write}, ")
            + "restore with Visual Studio's MSBuild (msbuild -t:restore -p:RestorePackagesConfig=true), run 'bro format' and review 'git diff' ('git checkout .' undoes it). "
            + "See https://bisforboman.github.io/stylebro/getting-started/#old-style-non-sdk-projects.";
    }

    /// <summary>What the preview says about the packages.config projects it leaves out (relative paths), when others remain.</summary>
    public static string PackagesConfigLeftOut(IReadOnlyList<string> projects) =>
        $"packages.config projects left out of the preview: {string.Join(", ", projects.Take(3))}{(projects.Count > 3 ? $" and {projects.Count - 3} more" : string.Empty)} "
        + "('dotnet restore' can't restore packages.config, and the copy has no packages folder). The other projects are previewed; "
        + "'bro format' covers those too once Visual Studio's MSBuild restored them (msbuild -t:restore -p:RestorePackagesConfig=true).";

    /// <summary>
    /// Whether every project an <see cref="FormatCommand.IncompleteMarker"/> line names is one of the packages.config projects
    /// (relative paths) the preview left out.
    /// </summary>
    public static bool OnlyLeftOut(string incomplete, IReadOnlyList<string> leftOut)
    {
        var names = Regex.Match(incomplete, @"skipped (?<names>.*?) \(references").Groups["names"].Value.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        var left = leftOut.Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return names.Length > 0 && names.All(left.Contains);
    }

    /// <summary>
    /// Where the patch goes when '--diff' names no file: the repository's git folder (.git/stylebro-preview.patch), where it
    /// isn't an untracked file in the working tree ('git apply .git/stylebro-preview.patch' applies it), else the temp folder.
    /// </summary>
    public static string DefaultPatchPath(string folder) =>
        Path.GetFullPath(Path.Combine(Git(folder, "rev-parse", "--absolute-git-dir") is (0, var gitDir) && gitDir.Trim().Length > 0 ? gitDir.Trim() : Path.GetTempPath(), DefaultPatch));

    /// <summary>Whether the options ask for a preview.</summary>
    public static bool Wants(IEnumerable<string> options) => options.Any(o => o == "--diff" || o.StartsWith("--diff=", StringComparison.Ordinal));

    /// <summary>
    /// Runs the preview. <paramref name="command"/>: null (the migration), "init" or "format"; <paramref name="format"/>:
    /// stands in for <see cref="FormatCommand.Run"/> in tests.
    /// </summary>
    public static int Run(string? command, string[] options, Func<string[], Action<string>, int>? format = null)
    {
        // Each file keeps its own line ending, as in FormatCommand.Run (trial 28: every changed file of an LF repository was
        // CRLF in the patch, the whitespace pass writing the OS's ending without end_of_line).
        var formatOnce = format ?? FormatCommand.RunPlain;
        var keepingEndings = FormatCommand.KeepingLineEndings((a, o) => formatOnce(a, o!));
        format = (a, o) => keepingEndings(a, o);
        var clock = Stopwatch.StartNew();
        var named = options.FirstOrDefault(o => o.StartsWith("--diff=", StringComparison.Ordinal))?.Substring("--diff=".Length);
        var patch = named is null ? null : Path.GetFullPath(named);
        var keep = options.Contains("--keep");
        var (profile, others) = command == "format" ? (null, options) : Program.TakeOption(options, Program.SonarProfileOption);
        (var style, others) = command == "init" ? Program.TakeOption(others, InitCommand.StyleOption) : (null, others);
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

        // packages.config projects need Visual Studio's restore: the copy has no git-ignored packages folder, and 'dotnet
        // restore' can't restore packages.config (xbox-live-developer-tools: every project skipped, "Run 'dotnet restore'").
        // Other old-style projects preview like format runs: without a restore (format says so, the end of the preview too).
        // With other projects too, they are left out like format leaves them (tusdotnet's one Owin_net452 test site stopped the
        // whole preview): named here and at the end.
        var pathFolder = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        var packagesConfig = FormatCommand.OldStyleProjects(pathFolder).Where(p => FormatCommand.UsesPackagesConfig(Path.Combine(pathFolder, p))).ToList();
        if (packagesConfig.Count > 0 && packagesConfig.Count == StyleCopSetup.EnumerateFiles(pathFolder).Count(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
        {
            Console.Error.WriteLine(OldStyleUnsupported(packagesConfig, command));
            return 1;
        }

        // What format will run on, checked before anything runs: with several solutions it would fail after the settings step.
        if (project is null && Directory.Exists(path) && BaselineCommand.FindWorkspace(path) is null)
        {
            Console.Error.WriteLine(BaselineCommand.NoWorkspace(path, command == "format" ? "'bro format <file> --diff'" : "--project <file>"));
            return 1;
        }

        var folder = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        var source = Git(folder, "rev-parse", "--show-toplevel") is (0, var top) ? Path.GetFullPath(top.Trim()) : folder;
        var prefix = source == folder ? string.Empty : Git(folder, "rev-parse", "--show-prefix").Output.Trim();
        patch ??= DefaultPatchPath(folder);
        var temp = Directory.CreateTempSubdirectory("stylebro-preview-").FullName;
        MarkCopy(temp);
        var outside = FormatCommand.OutsideCompile(source);
        var copy = CopyLocation(temp, source, outside);
        var target = Path.GetFullPath(Path.Combine(copy, prefix, File.Exists(path) ? Path.GetFileName(path) : string.Empty));
        using var log = new StreamWriter(Path.Combine(temp, "preview.log")) { AutoFlush = true };
        var tail = new Queue<string>();
        string? incomplete = null;
        var notes = new List<string>();

        // What the last format run didn't check the way the build does: the preview ends "Not verified" then, as format does
        // (trial 36, SharpZipLib: "clean after 1 run" and exit 0 where format says "Not verified" for IDE0049, held back).
        var gaps = new List<string>();
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

                if (FormatCommand.IsGapLine(line) && !gaps.Contains(line))
                {
                    gaps.Add(line);
                }

                // What format left out on purpose (old-style projects it doesn't restore, projects without StyleBro or not built,
                // lines whose indentation '#if' decides).
                if (new[] { FormatCommand.OldStyleHeader, FormatCommand.PartlyLoadedHeader, FormatCommand.NotBuiltHeader, FormatCommand.IfBodiesHeader }.Any(h => line.StartsWith(h, StringComparison.Ordinal)) && !notes.Contains(line))
                {
                    notes.Add(line);
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
            "init" => "'bro init --write' and 'bro format'",
            "format" => "'bro format'",
            _ => "'bro migrate --write' and 'bro format'",
        };
        Console.WriteLine($"Previewing {what} on a copy; nothing in {source} changes.");
        if (packagesConfig.Count > 0)
        {
            notes.Add(PackagesConfigLeftOut(packagesConfig));
            Console.WriteLine(notes[^1]);
        }

        var ok = false;
        var keptFile = Path.Combine(temp, "kept.tsv");
        var keptVariable = Environment.GetEnvironmentVariable(StyleBro.Analyzers.KeptFinding.Variable);

        // Ctrl+C ends the process without the finally below: the copy goes then too (unless --keep).
        void Cancelled(object? sender, ConsoleCancelEventArgs e)
        {
            if (!keep)
            {
                lock (log)
                {
                    log.Dispose();
                    Delete(temp);
                }
            }
        }

        Console.CancelKeyPress += Cancelled;
        try
        {
            var ignoredTracked = new List<string>();
            Step($"Copying the repository to {copy}", () => ignoredTracked = Copy(source, copy));
            CopyOutside(source, copy, temp, outside);
            if (IgnoredNuGetConfigNote(source, copy) is { } nuGetConfig)
            {
                notes.Add(nuGetConfig);
                Console.WriteLine(nuGetConfig);
            }

            // An in-solution source generator runs from its project's build output, which git ignores: without it in the
            // copy, the preview applied renames the real run keeps back (as verify did, FormatCommand.CopyAnalyzerOutputs).
            FormatCommand.CopyAnalyzerOutputs(project is null ? path : Path.Combine(folder, project), source, copy);
            var noRestore = rest.Contains("--no-restore");
            var restored = noRestore && CopyRestoreOutput(source, copy) > 0;

            MakeRepository(copy, ignoredTracked);
            var baseline = Head(copy);

            if (command != "format")
            {
                // Its output goes to the log file only: on a failure Settings prints it, and the end of the log shown then is
                // format's (the init report came twice before).
                var settings = Settings(command, target, rest.Contains("--modernize"), rest.Contains(AgentsFile.Option), profile, LogFile, style);
                if (settings != 0)
                {
                    return Fail(tail, settings, $"{(command == "init" ? "'bro init --write'" : "'bro migrate --write'")} failed (exit code {settings}, see above).");
                }
            }

            // The settings step wrote to the copy: the report describes the repository, which isn't written.
            JsonReport.Set("path", source);
            JsonReport.Set("write", false);

            var package = AddPackage(File.Exists(target) ? Path.GetDirectoryName(target)! : target);
            AddIgnoredSettings(copy);
            Commit(copy, "settings");
            var configured = Head(copy);
            if (command != "format" || package is not null)
            {
                Console.WriteLine(SettingsSummary(copy, baseline, configured));
            }

            if (package is not null)
            {
                // The packages.config projects are left out of the patch (akka: it said "the patch includes it" for them too).
                var included = packagesConfig.Count == 0 ? "the patch includes it" : "the patch includes it, except in the packages.config projects it leaves out";
                Console.WriteLine($"  StyleBro.Analyzers wasn't referenced: added {package} for the preview, as https://bisforboman.github.io/stylebro/getting-started/ says ({included}).");
                if (noRestore)
                {
                    rest.Remove("--no-restore");
                    Console.WriteLine(NoRestoreIgnored);
                }
            }
            else if (noRestore && !restored)
            {
                // Without the restore's output 'dotnet format --no-restore' loads no project: whitespace only (obj deleted).
                return Fail(tail, 1, NoRestoreOutput);
            }

            var formatArgs = new[] { project is null ? target : Path.Combine(target, project) }.Concat(rest.Where(o => o != "--modernize" && o != AgentsFile.Option)).ToList();

            // The copy restores with NuGet's locked mode off: the reference the preview added changes the packages, and with
            // RestoreLockedMode always on the restore failed (NU1004) and told the user to fix their repository.
            formatArgs.Add(FormatCommand.UnlockedRestoreOption);

            // The first run's report says what format fixes (a '--verify-no-changes' run first reported the same, at the
            // cost of a whole run). Later runs are FormatCommand.NextRun's.
            var reportFolder = Path.Combine(temp, "report");
            var reportFile = Path.Combine(reportFolder, "format-report.json");
            var changed = new List<bool>();
            var putBack = new List<string>();
            var tree = Tree(copy);

            // CSharpier where the repository uses it, as 'bro format' runs it (whole folders where it changes nothing else).
            var formatRoot = File.Exists(target) ? FormatCommand.WorkingRoot(target) : target;
            var csharpier = CSharpierStep.Find(formatRoot);
            var unformatted = csharpier is null ? null : CSharpierStep.Unformatted(csharpier, formatRoot);

            // The findings the last run's fixes kept: 'format' baselines them after a clean run, so the patch does too (trial 25).
            Environment.SetEnvironmentVariable(StyleBro.Analyzers.KeptFinding.Variable, keptFile);
            for (var run = 1; run <= MaxRuns; run++)
            {
                var code = 0;
                var runArgs = run == 1 ? formatArgs.Concat(new[] { "--report", reportFolder }) : FormatCommand.NextRun(formatArgs.ToArray());
                File.Delete(keptFile);
                gaps.Clear();
                Step($"Format run {run}", () => code = format(runArgs.Append(FormatCommand.OnceOption).ToArray(), Log));

                if (code != 0)
                {
                    return Fail(tail, code, $"'bro format' failed (exit code {code}).");
                }

                if (run == 1 && !File.Exists(reportFile))
                {
                    return Fail(tail, 1, "'dotnet format' wrote no report.");
                }

                gaps.AddRange(FormatCommand.GeneratorGaps(formatRoot).Except(gaps, StringComparer.Ordinal).ToList());

                var next = Tree(copy);
                List<string> Between(string from, string to) => from == to ? [] : GitChecked(copy, "diff", "--name-only", from, to).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList();

                // CSharpier on the files the run changed, and on those whose line endings aren't end_of_line's (as format runs it).
                putBack = [];
                if (csharpier is not null
                    && CSharpierStep.InScope(csharpier, formatRoot, Between(tree, next).Select(f => Path.GetFullPath(Path.Combine(copy, f)))
                        .Union(CSharpierStep.WrongEndings(formatRoot, CSharpierStep.InScope(csharpier, formatRoot, StyleCopSetup.EnumerateFiles(formatRoot))), StringComparer.OrdinalIgnoreCase)) is { Count: > 0 } pretty)
                {
                    Step($"CSharpier on {pretty.Count} file{(pretty.Count == 1 ? string.Empty : "s")}", () => CSharpierStep.Run(csharpier, "format", CSharpierStep.Arguments(csharpier, formatRoot, pretty, unformatted)).Output.ForEach(Log));
                    var fixedTree = next;
                    next = Tree(copy);

                    // What CSharpier put back as it was before the run: no run settles those files, as format says (trial 26:
                    // the preview said "clean after 1 run" and exited 0 where format exits 2).
                    putBack = Between(tree, fixedTree).Except(Between(tree, next), StringComparer.Ordinal).ToList();
                }

                changed.Add(next != tree);
                JsonReport.AddRun(run, GitChecked(copy, "diff", "--name-only", tree, next).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(f => Path.Combine(source, f.Trim())), null);
                tree = next;
                if (!changed[^1])
                {
                    break;
                }
            }

            // After a clean last run, its kept findings go into the baseline the build reads, as 'format' puts them.
            var kept = FormatCommand.ReadKept(keptFile, copy, copy);
            var keptLines = changed[^1] ? [] : FormatCommand.KeptSummary(kept, formatRoot);
            if (!changed[^1] && FormatCommand.BaselineKept(formatRoot, kept) is { } baselined)
            {
                keptLines.Add(baselined.Replace("Baseline: ", "Baseline (in the patch): ", StringComparison.Ordinal));
                Tree(copy);
            }

            // The packages.config projects the note says are left out stay out: no changes in their folders (the settings step
            // installed the package there, and the whitespace pass formatted their files).
            var leftOut = packagesConfig.Select(p => (prefix + Path.GetDirectoryName(p)!.Replace('\\', '/')).Trim('/')).Where(d => d.Length > 0).Distinct().ToList();
            bool IsLeftOut(string file) => leftOut.Any(d => file.StartsWith(d + "/", StringComparison.OrdinalIgnoreCase));

            // Lock files the copy's unlocked restore updated aren't formatting (the patch has them, as the user's restore will).
            var files = Changed(copy, configured).Where(f => !IsLeftOut(f.Key) && !f.Key.EndsWith("packages.lock.json", StringComparison.OrdinalIgnoreCase) && !f.Key.EndsWith(Analyzers.Baseline.Baseline.FileName, StringComparison.OrdinalIgnoreCase)).ToDictionary(f => f.Key, f => f.Value, StringComparer.Ordinal);
            var (attribution, changes) = Attribute(File.ReadAllText(reportFile), Path.GetRelativePath(Path.GetDirectoryName(temp)!, copy).Replace('\\', '/'), files.Keys, f => WhitespaceOnly(copy, configured, f));
            JsonReport.Set("filesPerRule", new JsonObject(attribution.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => KeyValuePair.Create(a.Key, (JsonNode?)new JsonArray(a.Value.Keys.Select(f => (JsonNode?)f).ToArray())))));
            JsonReport.Set("changesPerRule", new JsonObject(JsonReport.ChangesPerRule(File.ReadAllText(reportFile)).Select(p => KeyValuePair.Create(p.Key, (JsonNode?)p.Value))));
            var notVerified = !changed[^1] && putBack.Count == 0 && gaps.Count > 0;
            JsonReport.Set("clean", !changed[^1] && putBack.Count == 0 && gaps.Count == 0);
            Console.WriteLine(FormatSummary(files.Count, changes, attribution, changed, putBack.Count, notVerified));
            foreach (var line in SampleLines(Samples(attribution, 3), (file, line) => Hunk(GitChecked(copy, "diff", "--cached", "-U0", configured, "--", file), line, 12)))
            {
                Console.WriteLine(line);
            }

            keptLines.ForEach(Console.WriteLine);
            GitChecked(copy, ["diff", "--cached", "--ignore-submodules", "--output=" + patch, baseline, "--", ".", .. leftOut.Select(d => $":(exclude){d}/")]);
            JsonReport.Set("patch", patch);
            var lines = File.Exists(patch) ? File.ReadLines(patch).Count() : 0;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Full diff: {(Path.GetRelativePath(Environment.CurrentDirectory, patch) is var shown && !shown.StartsWith("..", StringComparison.Ordinal) ? shown : patch)} ({lines:N0} lines)"));
            notes.ForEach(n => Console.WriteLine($"Note: {n}"));

            // A project that didn't load is left out, as 'bro format' does (StabilityMatrix: a legacy project the
            // solution doesn't build, already failing with NU1008, stopped the whole preview), and named at the end.
            // packages.config projects were left out on purpose (the note says why): no restore advice for them, it can't work.
            if (incomplete is not null && !OnlyLeftOut(incomplete, packagesConfig))
            {
                Console.WriteLine($"WARNING: {incomplete}");
                Console.WriteLine("  The preview has no changes for them. To include them: run 'dotnet restore' (or 'dotnet build') in the repository, fix what it reports, then preview again.");
            }

            // As format: not clean (exit code 2) when CSharpier puts back what the fixes changed, or a run still changed files.
            if (putBack.Count > 0)
            {
                // Which rules' fixes it undoes, as format says: a check of those files (they're as before the fixes again).
                var check = new List<string>();
                format([.. FormatCommand.WithInclude(FormatCommand.NoRestore(formatArgs.ToArray()), putBack.Select(f => Path.GetRelativePath(formatRoot, Path.Combine(copy, f)).Replace('\\', '/'))), "--verify-no-changes"], check.Add);
                Console.WriteLine(CSharpierStep.PutBackLine(putBack, FormatCommand.FindingIds(check.Where(l => FormatCommand.FindingPath(l) is { } path && putBack.Contains(Path.GetRelativePath(copy, path).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)))));
            }

            // As format: the parts no run checked, then "Not verified" (exit code 3) instead of clean.
            if (notVerified)
            {
                FormatCommand.ReportGaps(gaps);
                FormatCommand.GapVerdict(gaps).ForEach(Console.WriteLine);
            }

            ok = true;
            return putBack.Count > 0 || changed[^1] ? FormatCommand.NotCleanExitCode : notVerified ? FormatCommand.NotVerifiedExitCode : 0;
        }
        catch (GitFailedException e)
        {
            // Never a "nothing to change" from commits git couldn't make.
            return Fail(tail, 1, e.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(StyleBro.Analyzers.KeptFinding.Variable, keptVariable);
            Console.CancelKeyPress -= Cancelled;
            Console.WriteLine($"Done in {Elapsed(clock.Elapsed)}.");
            log.Dispose();
            if (keep || !ok)
            {
                // A copy kept on request is the user's from now on: no later command deletes it (a failed run's goes after a day).
                if (keep)
                {
                    File.Delete(MarkerOf(temp));
                }

                Console.WriteLine($"The copy is kept: {copy} (log: {Path.Combine(temp, "preview.log")}).");
            }
            else
            {
                Delete(temp);
            }
        }
    }

    /// <summary>
    /// What to say when the repository's root has a nuget.config the copy doesn't (git ignores it), or null. Coverlet's
    /// ignored nuget.config named a local feed: the copy's restore couldn't find the package and the preview failed.
    /// </summary>
    public static string? IgnoredNuGetConfigNote(string source, string copy) =>
        Directory.EnumerateFiles(source).Select(Path.GetFileName).OfType<string>().FirstOrDefault(f => string.Equals(f, "nuget.config", StringComparison.OrdinalIgnoreCase) && !File.Exists(Path.Combine(copy, f))) is { } name
            ? $"Not copied: {name} (git ignores it). If the copy's restore fails (NU1101, NU1102), the package sources only it names are why: commit them in a nuget.config, or run 'init --write' and 'format' on the repository itself."
            : null;

    /// <summary>
    /// Where the copy of <paramref name="source"/> goes in <paramref name="temp"/>: below as many folders (named like the
    /// source's parents) as the files its projects compile from outside it go up (<paramref name="outside"/>,
    /// <see cref="FormatCommand.OutsideCompile"/>), so <see cref="CopyOutside"/> puts those at the same relative place.
    /// Speckle compiles a sibling repository's files: without them the copy didn't compile, and IDE0005 removed usings.
    /// </summary>
    public static string CopyLocation(string temp, string source, IReadOnlyCollection<string> outside)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(source));
        var up = outside.Select(f => Path.GetRelativePath(source, f).Split('\\', '/').TakeWhile(p => p == "..").Count()).DefaultIfEmpty(0).Max();
        var parents = new List<string>();
        for (var folder = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(source)); parents.Count < up && folder is not null && Path.GetFileName(folder) is { Length: > 0 } parent; folder = Path.GetDirectoryName(folder))
        {
            parents.Insert(0, parent);
        }

        return Path.Combine([temp, .. parents, name]);
    }

    /// <summary>Copies the files the projects compile from outside <paramref name="source"/> next to its <paramref name="copy"/>, where they land inside <paramref name="temp"/>.</summary>
    public static void CopyOutside(string source, string copy, string temp, IEnumerable<string> outside)
    {
        var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(temp)) + Path.DirectorySeparatorChar;
        foreach (var file in outside)
        {
            var to = Path.GetFullPath(Path.Combine(copy, Path.GetRelativePath(source, file)));
            if (to.StartsWith(top, StringComparison.OrdinalIgnoreCase) && File.Exists(file))
            {
                CopyFile(file, to);
            }
        }
    }

    /// <summary>
    /// Copies a repository: git's tracked and untracked, not ignored files ('git ls-files -co --exclude-standard'), else
    /// (not a repository) everything outside bin, obj and .git. A submodule (or another repository inside) is copied the
    /// same way and made a repository of its own, so 'bro format' leaves it alone in the copy too. Returns the
    /// tracked files a .gitignore ignores, for <see cref="MakeRepository"/>: MoreLINQ tracks bld/ExtensionsGenerator under an
    /// ignored bld/, and the copy's repository left them out, so the preview showed none of their changes.
    /// </summary>
    public static List<string> Copy(string source, string target)
    {
        Directory.CreateDirectory(target);
        if (Git(source, "ls-files", "-co", "--exclude-standard", "-z") is not (0, var list))
        {
            CopyFolder(source, target);
            return [];
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
                MakeRepository(to, Copy(from, to));
            }
            else if (Directory.Exists(from))
            {
                CopyFolder(from, to);
            }
        }

        return Git(source, "ls-files", "-c", "-i", "--exclude-standard", "-z") is (0, var ignored)
            ? ignored.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToList()
            : [];
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

    /// <summary>The format part of the summary: files, reported changes, runs, and the rules by the number of files they changed.</summary>
    public static string FormatSummary(int files, int changes, Dictionary<string, SortedDictionary<string, int>> attribution, IReadOnlyList<bool> changedRuns, int putBack = 0, bool notVerified = false)
    {
        var text = new StringBuilder();
        var runs = changedRuns.Count(c => c);
        var convergence = changedRuns.Count > 0 && changedRuns[^1] ? $"still changing after {changedRuns.Count} runs (the diff has all of them)"
            : putBack > 0 ? $"not clean: CSharpier puts back what the fixes change in {putBack} file{(putBack == 1 ? string.Empty : "s")} (below)"
            : notVerified ? $"{(runs == 0 ? "nothing to change" : $"nothing left to change after {runs} run{(runs == 1 ? string.Empty : "s")}")}, but not verified (below)"
            : runs == 0 ? "nothing to change"
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
    /// The samples as printed: one block per file and hunk, naming every rule whose sample it is (a file with three rules
    /// printed the same hunk three times, once per rule).
    /// </summary>
    public static List<string> SampleLines(IEnumerable<(string Id, string File, int Line, string[] Also)> samples, Func<string, int, List<string>> hunk)
    {
        var blocks = new List<(List<string> Ids, string File, string[] Also, List<string> Hunk)>();
        foreach (var (id, file, line, also) in samples)
        {
            var lines = hunk(file, line);
            if (blocks.FirstOrDefault(b => b.File == file && b.Hunk.SequenceEqual(lines)) is { Ids: not null } same)
            {
                same.Ids.Add(id);
                continue;
            }

            blocks.Add((new List<string> { id }, file, also, lines));
        }

        var result = new List<string>();
        foreach (var (ids, file, also, lines) in blocks)
        {
            var others = also.Except(ids, StringComparer.Ordinal).ToList();
            result.Add($"Sample ({string.Join(", ", ids)}, {file}{(others.Count == 0 ? string.Empty : $"; {string.Join(", ", others)} changed this file too")}):");
            result.AddRange(lines.Select(l => "  " + l));
        }

        return result;
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
        foreach (var (added, file) in NumStat(GitChecked(copy, "diff", "--numstat", from, to)))
        {
            if (file.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)
                || file.Equals(AgentsFile.FileName, StringComparison.Ordinal)
                || Path.GetFileName(file).Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase))
            {
                var addedText = GitChecked(copy, "diff", "-U0", from, to, "--", file);
                var notes = new[] { ("StyleBroPreset>none", "StyleBroPreset=none"), ("Include=\"StyleBro.Analyzers\"", "StyleBro.Analyzers"), ("<NoWarn>", "NoWarn") }
                    .Where(n => addedText.Contains(n.Item1, StringComparison.Ordinal))
                    .Select(n => n.Item2)
                    .ToList();
                parts.Add($"{added} line{(added == 1 ? string.Empty : "s")} in {file}{(notes.Count == 0 ? string.Empty : $" ({string.Join(", ", notes)})")}");
                continue;
            }

            // In a project file or packages.config only <NoWarn> is a suppression: the rest is the StyleBro.Analyzers reference
            // (Autofac: its 9 PackageReference edits counted as "27 other lines").
            // A '#pragma' or '<NoWarn>' counts its ids added, like the migration's own count (SSH.NET: 25 lines here, 27 there).
            var projectFile = Path.GetExtension(file).ToLowerInvariant() is ".csproj" or ".props" or ".targets" || Path.GetFileName(file).Equals("packages.config", StringComparison.OrdinalIgnoreCase);
            foreach (var line in GitChecked(copy, "diff", "-U0", from, to, "--", file).Split('\n').Where(l => (l.StartsWith('+') && !l.StartsWith("+++", StringComparison.Ordinal)) || (l.StartsWith('-') && !l.StartsWith("---", StringComparison.Ordinal))))
            {
                var kind = line.Contains("#pragma", StringComparison.Ordinal) ? "#pragma" : line.Contains("SuppressMessage", StringComparison.Ordinal) ? "[SuppressMessage]"
                    : line.Contains("NoWarn", StringComparison.Ordinal) ? "<NoWarn>" : "other lines";
                if ((projectFile && kind != "<NoWarn>") || (line[0] == '-' && kind is not ("#pragma" or "<NoWarn>")))
                {
                    continue;
                }

                var count = kind is "#pragma" or "<NoWarn>" ? (line[0] == '+' ? 1 : -1) * Regex.Matches(line, @"\b(?:BRO|IDE|CA)\d{4}\b").Count : 1;
                suppressions[kind] = (suppressions.TryGetValue(kind, out var n) ? n : 0) + count;
            }
        }

        if (suppressions.Any(s => s.Value > 0))
        {
            parts.Add("suppressions carried over: " + string.Join(", ", suppressions.Where(s => s.Value > 0).Select(s => $"{s.Value} {s.Key}")));
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
        var (_, styleCop, removes, _) = StyleCopSetup.Projects(root, files);

        // packages.config projects get it the packages.config way, like StyleCop came in there (PackagesConfig).
        var packagesConfig = PackagesConfig.Projects(files);
        foreach (var project in packagesConfig)
        {
            PackagesConfig.Install(project, version);
        }

        var targets = (styleCop.Count > 0 ? styleCop : Migration.PropsFiles(root)).Except(packagesConfig, StringComparer.OrdinalIgnoreCase).ToList();
        var versions = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var optedOut = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in targets)
        {
            // A Directory.Build.props that packages.config projects import keeps its reference away from them.
            var folder = Path.GetDirectoryName(Path.GetFullPath(file))! + Path.DirectorySeparatorChar;
            var guard = file.EndsWith(".props", StringComparison.OrdinalIgnoreCase) && packagesConfig.Any(p => Path.GetFullPath(p).StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                ? PackagesConfig.NotPackagesConfig
                : null;
            string Condition(string? central) => (central, guard) switch
            {
                (null, null) => string.Empty,
                (null, _) => $" Condition=\"{guard}\"",
                (_, null) => $" Condition=\"{central}\"",
                _ => $" Condition=\"{central} and ({guard})\"",
            };

            // A GlobalPackageReference (central package management) gets one of its own: a PackageReference with a version
            // there fails the restore (NU1008), and one without adds the package a second time. Next to StyleCop's, so the
            // ItemGroup's Condition holds for both (DynamicData's '!$(IsTestProject) AND !$(IsBenchmarkProject)').
            if (File.Exists(file) && GlobalStyleCop.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(file))))
            {
                EditFile(file, text => AddNextToStyleCop(text, $"<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"{version}\" />", always: true));
                continue;
            }

            // Blazor.Diagrams: site/Site/Site.csproj turns central package management on in its own file, below a
            // Directory.Build.props that doesn't: a version there failed its restore (NU1008). Such projects get the
            // version-less reference (and the version in their Directory.Packages.props), the others one with a version.
            var packages = CentralPackages(file, root);

            // markdig: no Directory.Build.props, and src/Directory.Packages.props turns it on for the projects under src/ only.
            var optIns = packages is null ? OptOuts(file, files, root, "true").Union(CentralProjects(file, files, root), StringComparer.OrdinalIgnoreCase).ToList() : [];
            foreach (var central in optIns.Select(p => CentralPackages(p, root)).Prepend(packages).OfType<string>())
            {
                if (versions.Add(central))
                {
                    EditFile(central, text => AddPackageVersion(text!, $"<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"{version}\" />"));
                }
            }

            // StabilityMatrix: its analyzer projects turn central package management off, and the version-less reference from
            // Directory.Build.props failed their restore (NU1015). Such projects get a reference with a version instead (those
            // that remove StyleCop's reference remove StyleBro's too, below).
            var optOuts = packages is null ? [] : OptOuts(file, files, root).Where(p => !removes.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
            if (optOuts.Count > 0 || optIns.Count > 0)
            {
                optedOut.UnionWith(optOuts);
                EditFile(file, text => AddItem(
                    text,
                    $"<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\"{Condition("'$(ManagePackageVersionsCentrally)' == 'true'")} />",
                    $"<PackageReference Include=\"StyleBro.Analyzers\" Version=\"{version}\" PrivateAssets=\"all\"{Condition("'$(ManagePackageVersionsCentrally)' != 'true'")} />"));
                continue;
            }

            EditFile(file, text => AddNextToStyleCop(text, $"<PackageReference Include=\"StyleBro.Analyzers\"{(packages is not null ? string.Empty : $" Version=\"{version}\"")} PrivateAssets=\"all\"{Condition(null)} />"));
        }

        // LiteBus' analyzer project removes the StyleCop reference its Directory.Build.props adds, and turns central package
        // management off: StyleBro's version-less reference failed its restore (NU1015).
        foreach (var file in removes)
        {
            EditFile(file, text => AddItem(text, "<PackageReference Remove=\"StyleBro.Analyzers\" />"));
        }

        string Names(IEnumerable<string> paths) => string.Join(", ", paths.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')));
        var where = new[] { targets.Count > 0 ? $"to {Names(targets)}" : null, packagesConfig.Count > 0 ? $"the packages.config way to {Names(packagesConfig)}" : null };
        return $"StyleBro.Analyzers {version} {string.Join(", ", where.OfType<string>())}"
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
    /// The projects that get <paramref name="props"/> (a Directory.Build.props: those under its folder) with central package
    /// management on (<see cref="CentralPackages"/>): a Directory.Packages.props between them and the props turns it on.
    /// </summary>
    public static List<string> CentralProjects(string props, IEnumerable<string> files, string root)
    {
        if (!Path.GetFileName(props).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(props))! + Path.DirectorySeparatorChar;
        return files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && Path.GetFullPath(f).StartsWith(folder, StringComparison.OrdinalIgnoreCase) && CentralPackages(f, root) is not null).ToList();
    }

    /// <summary>
    /// The projects that get <paramref name="props"/> (a Directory.Build.props: those under its folder) and turn central
    /// package management off themselves (<paramref name="value"/> "true": on).
    /// </summary>
    public static List<string> OptOuts(string props, IEnumerable<string> files, string root, string value = "false")
    {
        if (!Path.GetFileName(props).Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(props))! + Path.DirectorySeparatorChar;
        return files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && Path.GetFullPath(f).StartsWith(folder, StringComparison.OrdinalIgnoreCase) && SetsCentral(f, value, root)).ToList();
    }

    /// <summary>
    /// An MSBuild file with an ItemGroup holding <paramref name="items"/> before its last '&lt;/Project&gt;' (a new file
    /// without one), in the file's line endings.
    /// </summary>
    public static string AddItem(string? project, params string[] items)
    {
        var newLine = project?.Contains("\r\n", StringComparison.Ordinal) == true ? "\r\n" : "\n";
        var (child, unit) = Migration.MSBuildIndentation(project);
        var group = $"{child}<ItemGroup>{newLine}{string.Concat(items.Select(item => $"{child}{unit}{item}{newLine}"))}{child}</ItemGroup>{newLine}";
        if (project is not null && Regex.Match(project, @"<Project\b[^>]*?/>") is { Success: true } closed)
        {
            // A self-closing '<Project Sdk="..." />' keeps its attributes.
            project = project.Remove(closed.Index, closed.Length).Insert(closed.Index, closed.Value[..^2].TrimEnd() + ">" + newLine + "</Project>");
        }

        var end = project?.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase) ?? -1;
        if (end < 0)
        {
            return $"<Project>{newLine}{group}</Project>{newLine}";
        }

        // '</Project>' after other content on its line ('<Project Sdk="..."></Project>') moves to a line of its own.
        var lineStart = project!.LastIndexOf('\n', Math.Max(end - 1, 0)) + 1;
        return project.Substring(0, end) + (project[lineStart..end].Trim().Length == 0 ? string.Empty : newLine) + group + project.Substring(end);
    }

    /// <summary>
    /// <see cref="AddItem"/>, except when the file's StyleCop.Analyzers reference depends on a Condition
    /// (<see cref="StyleCopSetup.ConditionalReference"/>): then the item goes right after it, in its ItemGroup, under the same
    /// Condition. DynamicData's '&lt;ItemGroup Condition="!$(IsTestProject) AND !$(IsBenchmarkProject)"&gt;' kept StyleCop
    /// out of its tests, and StyleBro's reference in a new unconditional ItemGroup brought StyleBro into them. With
    /// <paramref name="always"/>, next to it in any case (a GlobalPackageReference: Nerdbank.Streams' list of global analyzers
    /// got StyleBro's in a new ItemGroup at the end, trial 24).
    /// </summary>
    public static string AddNextToStyleCop(string? text, string item, bool always = false)
    {
        if (text is null || (!always && !StyleCopSetup.ConditionalReference(text)))
        {
            return AddItem(text, item);
        }

        // The first reference outside a comment: its element ends at '/>' or its closing tag.
        var reference = StyleCopItem.Matches(text).FirstOrDefault(m => text.LastIndexOf("<!--", m.Index, StringComparison.Ordinal) <= text.LastIndexOf("-->", m.Index, StringComparison.Ordinal));
        if (reference is null)
        {
            return AddItem(text, item);
        }

        // A Condition on StyleCop's item itself goes onto StyleBro's too.
        var startTag = reference.Value.Substring(0, reference.Value.IndexOf('>') + 1);
        if (Regex.Match(startTag, @"\bCondition\s*=\s*""[^""]*""", RegexOptions.IgnoreCase) is { Success: true } condition && !item.Contains(" Condition=", StringComparison.Ordinal))
        {
            item = item.Substring(0, item.LastIndexOf("/>", StringComparison.Ordinal)).TrimEnd() + " " + condition.Value + " />";
        }

        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lineStart = text.LastIndexOf('\n', reference.Index) + 1;
        var indentation = text.Substring(lineStart, reference.Index - lineStart);
        indentation = indentation.Trim().Length == 0 ? indentation : "    ";
        return text.Insert(reference.Index + reference.Length, newLine + indentation + item);
    }

    /// <summary>
    /// A Directory.Packages.props with <paramref name="item"/> (a PackageVersion) among its PackageVersion items, in
    /// alphabetical order of Include like the usual sorted list (else in an ItemGroup of its own at the end).
    /// </summary>
    public static string AddPackageVersion(string packages, string item)
    {
        // Only among unconditional items: markdig's list ends in an ItemGroup for net462/netstandard2.0, and StyleBro's version
        // there left the other frameworks without one (NU1010).
        static bool Conditional(string element) => Regex.IsMatch(element.Substring(0, element.IndexOf('>') + 1), @"\bCondition\s*=", RegexOptions.IgnoreCase);
        var name = Regex.Match(item, @"Include=""([^""]+)""").Groups[1].Value;
        var versions = Regex.Matches(packages, @"<PackageVersion\b[^>]*?\bInclude\s*=\s*""([^""]+)""[^>]*?(?:/>|>.*?</PackageVersion\s*>)", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .Where(m => !Conditional(m.Value) && (Regex.Matches(packages.Substring(0, m.Index), @"<ItemGroup\b[^>]*>", RegexOptions.IgnoreCase).LastOrDefault() is not { } group || !Conditional(group.Value)))
            .ToList();
        if (versions.Count == 0)
        {
            return AddItem(packages, item);
        }

        // In the ItemGroup with the other analyzers when there is one: AngleSharp's labelled groups put StyleBro's version
        // into the one for the package's only shipped dependency.
        int GroupOf(Match m) => packages.LastIndexOf("<ItemGroup", m.Index, StringComparison.OrdinalIgnoreCase);
        if (versions.FirstOrDefault(m => m.Groups[1].Value.Contains("Analyzer", StringComparison.OrdinalIgnoreCase)) is { } analyzer)
        {
            versions = versions.Where(m => GroupOf(m) == GroupOf(analyzer)).ToList();
        }

        var newLine = packages.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var next = versions.FirstOrDefault(m => string.Compare(m.Groups[1].Value, name, StringComparison.OrdinalIgnoreCase) > 0);
        var anchor = next ?? versions[^1];
        var lineStart = packages.LastIndexOf('\n', anchor.Index) + 1;
        var indentation = packages.Substring(lineStart, anchor.Index - lineStart);
        indentation = indentation.Trim().Length == 0 ? indentation : "    ";
        if (next is null)
        {
            return packages.Insert(anchor.Index + anchor.Length, newLine + indentation + item);
        }

        // Above the comment lines right above the next item: they describe it (Fluid's '<!-- Only used with netstandard2.0 -->'
        // got StyleBro's line between it and its item).
        var at = lineStart;
        var inComment = false;
        while (at >= 2)
        {
            var above = packages.LastIndexOf('\n', at - 2) + 1;
            var line = packages[above..at].Trim();
            if (!inComment && !line.EndsWith("-->", StringComparison.Ordinal))
            {
                break;
            }

            inComment = !line.Contains("<!--", StringComparison.Ordinal);
            at = above;
        }

        return packages.Insert(at, indentation + item + newLine);
    }

    /// <summary>Makes the folder a git repository with everything in it committed.</summary>
    public static void MakeRepository(string folder, IReadOnlyCollection<string>? ignoredTracked = null)
    {
        GitChecked(folder, "init", "-q");
        foreach (var chunk in (ignoredTracked ?? []).Chunk(100))
        {
            GitChecked(folder, ["add", "-f", "--", .. chunk]);
        }

        AddIgnoredSettings(folder);

        // The format runs restore and build into bin/obj: not changes to show, also where no .gitignore says so. Nor the
        // packages folder, where a nuget.config puts it inside the repository.
        var packages = PackagesFolderInside(folder) is { } inside ? $"/{inside}/\n" : string.Empty;
        File.AppendAllText(Path.Combine(folder, ".git", "info", "exclude"), "\nbin/\nobj/\n" + packages);
        Commit(folder, "baseline");
    }

    /// <summary>
    /// Rewrites a file (null text: it doesn't exist yet) keeping its UTF-8 byte order mark (File.ReadAllText drops it and
    /// File.WriteAllText wouldn't write it back: Soulseek.NET's .csproj lost its BOM in the preview) and its line endings
    /// (<see cref="KeepLineEndings"/>). Every settings and project file the tool edits goes through here.
    /// </summary>
    public static void EditFile(string file, Func<string?, string> edit)
    {
        var bytes = File.Exists(file) ? File.ReadAllBytes(file) : null;
        var bom = bytes is [0xEF, 0xBB, 0xBF, ..];
        var text = bytes is null ? null : Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
        var edited = edit(text);

        // Never an empty MSBuild file: one with only whitespace breaks every build below it (an untracked src/Directory.Build.props
        // of a BOM and a line break turned up in StyleBro's own checkout during trial 24, from an unknown source).
        if (string.IsNullOrWhiteSpace(edited) && StyleCopSetup.IsMSBuild(file))
        {
            return;
        }

        File.WriteAllText(file, KeepLineEndings(text ?? FolderLineEnding(file), edited), new UTF8Encoding(bom));
    }

    /// <summary>
    /// The line ending of the files around a new one (its folder's, else the nearest folder above with files of one kind),
    /// or null: tusdotnet's CRLF checkout got an LF Source/Directory.Build.props from init, which CSharpier's check rejected.
    /// </summary>
    public static string? FolderLineEnding(string file)
    {
        // Up to the repository's root (a folder with .git); outside a repository only the file's own folder.
        var start = Path.GetDirectoryName(Path.GetFullPath(file))!;
        var top = start;
        for (var f = start; f is not null; f = Path.GetDirectoryName(f))
        {
            if (Path.Exists(Path.Combine(f, ".git")))
            {
                top = f;
                break;
            }
        }

        for (var folder = start; folder is not null && folder.StartsWith(top, StringComparison.OrdinalIgnoreCase); folder = Path.GetDirectoryName(folder))
        {
            var (crlf, lf) = (0, 0);
            foreach (var other in (Directory.Exists(folder) ? Directory.EnumerateFiles(folder) : []).Where(f => new FileInfo(f).Length < 1_000_000).Take(50))
            {
                if (Migration.ReadOrNull(other) is not { } text)
                {
                    continue;
                }

                var pairs = Regex.Count(text, "\r\n");
                var bare = text.Count(c => c == '\n') - pairs;
                if (text.Contains('\0'))
                {
                    continue;
                }

                crlf += pairs > 0 && bare == 0 ? 1 : 0;
                lf += bare > 0 && pairs == 0 ? 1 : 0;
            }

            if (crlf != lf)
            {
                return crlf > lf ? "\r\n" : "\n";
            }
        }

        return null;
    }

    /// <summary>
    /// The new text of a file in the line endings of its <paramref name="original"/> text when those are all one kind (mdx's
    /// CRLF .editorconfig came back LF from init: the edits write LF); a file with mixed or no line endings gets the text as is.
    /// </summary>
    public static string KeepLineEndings(string? original, string text)
    {
        var crlf = original is null ? 0 : Regex.Count(original, "\r\n");
        var lf = original is null ? 0 : original.Count(c => c == '\n') - crlf;
        var plain = text.Replace("\r\n", "\n");
        return crlf > 0 && lf == 0 ? plain.Replace("\n", "\r\n") : lf > 0 && crlf == 0 ? plain : text;
    }

    /// <summary>Runs git; line endings and paths as they are (no autocrlf, no quoting), output as UTF-8.</summary>
    public static (int Code, string Output) Git(string directory, params string[] arguments)
    {
        var (code, output, _) = RunGit(directory, arguments);
        return (code, output);
    }

    /// <summary>The Sonar part of the report in the command's output (from 'Sonar: read' to the next empty line), or nothing.</summary>
    public static string SonarPart(string output) => Part(output, "Sonar: read ");

    /// <summary>The conventions init found in the code (from its header to the next empty line), or nothing.</summary>
    public static string DetectedPart(string output) => Part(output, InitCommand.DetectedHeader);

    /// <summary>
    /// NuGet's global packages folder (relative, '/'-separated) when a nuget.config puts it inside the folder, else null. The
    /// format runs' restore fills it (trial 31: Hangfire's 'globalPackagesFolder = %USERPROFILE%/.nuget/TrustedPackages' isn't
    /// expanded off Windows, and the preview counted 12,576 package files as changed).
    /// </summary>
    internal static string? PackagesFolderInside(string folder)
    {
        string output;
        try
        {
            output = FormatCommand.Capture(folder, "nuget", "locals", "global-packages", "--list");
        }
        catch (Exception e) when (e is ToolException or System.ComponentModel.Win32Exception or IOException)
        {
            return null;
        }

        const string Prefix = "global-packages:";
        if (output.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith(Prefix, StringComparison.Ordinal)) is not { } line)
        {
            return null;
        }

        var relative = Path.GetRelativePath(Path.GetFullPath(folder), Path.GetFullPath(line.Substring(Prefix.Length).Trim())).Replace('\\', '/').TrimEnd('/');
        return relative is "." or "" || relative.StartsWith("../", StringComparison.Ordinal) || relative == ".." || Path.IsPathRooted(relative) ? null : relative;
    }

    /// <summary>
    /// Runs git in the preview's copy, where every command must work: a failure throws <see cref="GitFailedException"/>
    /// with git's error. Fitnet: 'git add' failed on paths of 261+ characters, the commits were empty, and the preview
    /// reported "nothing to change" while the real run changed 117 files.
    /// </summary>
    internal static string GitChecked(string directory, params string[] arguments)
    {
        var (code, output, error) = RunGit(directory, arguments);
        if (code == 0)
        {
            return output;
        }

        throw new GitFailedException($"'git {string.Join(' ', arguments)}' failed (exit code {code}){(error.Trim().Length == 0 ? "." : ": " + error.Trim())}");
    }

    /// <summary>
    /// Marks <paramref name="folder"/> (in the shared temp folder) as a copy this tool made: <see cref="DeleteStaleCopies"/>
    /// deletes only marked folders, never one another program or the user named alike, nor a copy kept with --keep.
    /// </summary>
    internal static void MarkCopy(string folder) => File.WriteAllText(MarkerOf(folder), string.Empty);

    /// <summary>Whether <paramref name="folder"/> is a copy this tool made and may delete (marked, not a link).</summary>
    internal static bool IsOwnCopy(string folder) =>
        File.Exists(MarkerOf(folder)) && !new DirectoryInfo(folder).Attributes.HasFlag(FileAttributes.ReparsePoint);

    /// <summary>
    /// Deletes the copies of previews and verify runs in <paramref name="temp"/> last written more than <paramref name="age"/>
    /// ago: a killed run leaves its copy (a running one writes to its copy all the time). Only folders this tool marked
    /// (<see cref="MarkCopy"/>). Quietly: one in use stays.
    /// </summary>
    internal static void DeleteStaleCopies(string temp, TimeSpan age)
    {
        try
        {
            foreach (var folder in new[] { "stylebro-preview-*", "stylebro-verify-*" }.SelectMany(p => Directory.EnumerateDirectories(temp, p)))
            {
                if (IsOwnCopy(folder) && DateTime.UtcNow - Directory.GetLastWriteTimeUtc(folder) > age)
                {
                    try
                    {
                        ClearAttributes(folder);
                        Directory.Delete(folder, recursive: true);
                        File.Delete(MarkerOf(folder));
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                    }
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    internal static void Delete(string folder)
    {
        try
        {
            ClearAttributes(folder);
            Directory.Delete(folder, recursive: true);
            File.Delete(MarkerOf(folder));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Couldn't delete the copy ({e.Message}): {folder}");
        }
    }

    /// <summary>The marker next to a copy (<see cref="MarkCopy"/>): beside it, so the copy itself stays the repository's files.</summary>
    internal static string MarkerOf(string folder) => Path.TrimEndingDirectorySeparator(folder) + ".stylebro-copy";

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

    /// <summary>
    /// Adds the .editorconfig files git ignores (bin/obj aside) to the copy's index: NSwag's .gitignore lists '.editorconfig',
    /// so the settings init wrote there were missing from the patch (trial 43). Before the baseline commit (the files as they
    /// were) and before the settings commit (the ones the settings step wrote); once added, 'git add -A' keeps them.
    /// </summary>
    internal static void AddIgnoredSettings(string folder)
    {
        var ignored = GitChecked(folder, "ls-files", "--others", "--ignored", "--exclude-standard", "--", ":(glob)**/.editorconfig")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !Regex.IsMatch(f, "(^|/)(bin|obj)/"))
            .ToList();
        foreach (var chunk in ignored.Chunk(100))
        {
            GitChecked(folder, ["add", "-f", "--", .. chunk]);
        }
    }

    private static (int Code, string Output, string Error) RunGit(string directory, string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        // core.longpaths: Windows' git can't add paths of 260+ characters without it (Fitnet).
        foreach (var argument in new[] { "-c", "core.autocrlf=false", "-c", "core.safecrlf=false", "-c", "core.quotepath=off", "-c", "core.longpaths=true" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(start)!;
            var error = string.Empty;
            var reader = FormatCommand.ReadOnThread(process.StandardError, text => error = text);
            var output = process.StandardOutput.ReadToEnd();
            reader.Join();
            process.WaitForExit();
            return (process.ExitCode, output, error);
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            return (-1, string.Empty, e.Message); // no git: the copy falls back to a plain folder copy, and the commits fail later
        }
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
    private static int Settings(string? command, string target, bool modernize, bool agentsMd, string? profile, Action<string> log, string? style = null)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        int code;
        try
        {
            var sonar = (profile is null ? Array.Empty<string>() : new[] { Program.SonarProfileOption, Path.GetFullPath(profile) })
                .Concat(agentsMd ? new[] { AgentsFile.Option } : Array.Empty<string>())
                .Concat(style is null ? Array.Empty<string>() : new[] { InitCommand.StyleOption, style })
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
        GitChecked(copy, "add", "-A");
        return NumStat(GitChecked(copy, "diff", "--cached", "--numstat", "--ignore-submodules", from)).ToDictionary(n => n.File, n => n.Added, StringComparer.Ordinal);
    }

    private static bool WhitespaceOnly(string copy, string from, string file)
    {
        // --quiet: 0 the same, 1 different, anything else an error.
        return Git(copy, "diff", "--cached", "--quiet", "--ignore-all-space", "--ignore-blank-lines", from, "--", file).Code switch
        {
            0 => true,
            1 => false,
            var code => throw new GitFailedException($"'git diff --quiet' on {file} failed (exit code {code})."),
        };
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
        GitChecked(copy, "add", "-A");
        return GitChecked(copy, "write-tree").Trim();
    }

    private static void Commit(string folder, string message)
    {
        GitChecked(folder, "add", "-A");
        GitChecked(folder, "-c", "user.name=stylebro-preview", "-c", "user.email=preview@stylebro.invalid", "-c", "commit.gpgsign=false", "commit", "-q", "--no-verify", "--allow-empty", "-m", message);
    }

    private static string Head(string folder) => GitChecked(folder, "rev-parse", "HEAD").Trim();

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

    /// <summary>git makes its object files read-only; links inside the copy aren't followed (their targets aren't ours).</summary>
    private static void ClearAttributes(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }
}

/// <summary>A git command in the preview's copy failed: the preview stops instead of showing an incomplete diff.</summary>
internal sealed class GitFailedException(string message) : Exception(message);
