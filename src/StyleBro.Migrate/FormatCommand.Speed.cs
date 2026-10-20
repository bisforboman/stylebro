using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using StyleBro.Analyzers;

namespace StyleBro.Migrate;

/// <summary>
/// Phase 0 of docs/proposals/format-speed.md (owner's decisions 2026-10-17): the restore skipped when its output is current,
/// --verify-no-changes answered from the clean record, the generator configurations' builds made together, and --timings.
/// </summary>
internal static partial class FormatCommand
{
    /// <summary>Restores before the runs even when the restore output looks current (<see cref="RestoreIsCurrent"/>).</summary>
    public const string RestoreOption = "--restore";

    /// <summary>Prints how long each phase took, at the end (<see cref="PhaseTimes"/>).</summary>
    public const string TimingsOption = "--timings";

    /// <summary>The line the record's answer to --verify-no-changes starts with (<see cref="VerifyFromRecord"/>).</summary>
    public const string FromRecordHeader = "Checking StyleBro's rules: nothing changed since the last clean 'bro format' here";

    /// <summary>The record line that says the run that ended clean checked every plan entry (<see cref="SaveKept"/>).</summary>
    private const string CheckedMark = "#checked";

    /// <summary>The start of a record line with a line verify prints after its checks (<see cref="SaveKept"/>).</summary>
    private const string VerdictMark = "#verdict\t";

    /// <summary>The start of a record line with a project and configuration verify builds (<see cref="RecordUnchecked"/>).</summary>
    private const string BuildMark = "#build\t";

    /// <summary>The start of the restore stamp's line with the restore's first warning (<see cref="SaveRestoreStamp"/>).</summary>
    private const string WarningMark = "#warning\t";

    /// <summary>The start of the record line verify says before the generator builds (<see cref="GeneratorConfigurationsLine"/>).</summary>
    private const string GeneratorMark = "#before\t";

    /// <summary>The roots whose last 'format' run (not --verify-no-changes) was a check of every plan entry that was clean (<see cref="IsCleanCheck"/>).</summary>
    private static readonly ConcurrentDictionary<string, bool> CheckedClean = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The findings the checks of a root's last 'format' run found where a run's check wasn't clean (not kept, not whitespace):
    /// its fixes ran on their files, so they are what no fix changes when that run changed nothing (<see cref="TakeCheckFindings"/>).
    /// </summary>
    private static readonly ConcurrentDictionary<string, List<string>> CheckFindings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The projects and configurations a 'format' run on a root couldn't check (<see cref="RecordUnchecked"/>), for its record.</summary>
    private static readonly ConcurrentDictionary<string, List<(string Project, string Configuration)>> UncheckedByFormat = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The line a 'format' run on a root would say about its generator builds as --verify-no-changes (<see cref="GeneratorConfigurationsLine"/>), for its record.</summary>
    private static readonly ConcurrentDictionary<string, string> GeneratorLines = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the restore output of <paramref name="projects"/> (project file, ProjectAssetsFile) is current, so 'dotnet restore'
    /// can be skipped (owner's decision 2026-10-17): each assets file and the project.nuget.cache next to it exist, the restore
    /// succeeded, every package file it expects is there, the assets list StyleBro.Analyzers, and they are newer than every
    /// input (<see cref="RestoreInputs"/>). A no-op restore doesn't write its output again: the time of format's own last
    /// restore of the same output counts too (<see cref="SaveRestoreStamp"/>). Not with no project (nothing to tell by).
    /// </summary>
    public static bool RestoreIsCurrent(string root, IReadOnlyCollection<(string Project, string AssetsFile)> projects, string? stampFolder = null)
    {
        if (projects.Count == 0)
        {
            return false;
        }

        var restored = DateTime.MaxValue;
        foreach (var (_, assets) in projects)
        {
            var cache = assets.Length == 0 ? string.Empty : Path.Combine(Path.GetDirectoryName(assets)!, "project.nuget.cache");
            if (assets.Length == 0 || !File.Exists(assets) || !File.Exists(cache) || !RestoreSucceeded(cache) || !File.ReadAllText(assets).Contains("\"StyleBro.Analyzers/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var written = new[] { File.GetLastWriteTimeUtc(assets), File.GetLastWriteTimeUtc(cache) }.Max();
            restored = written < restored ? written : restored;
        }

        if (RestoreStamp(root, projects.Select(p => p.AssetsFile), stampFolder) is ({ } stamp, _) && stamp > restored)
        {
            restored = stamp;
        }

        return RestoreInputs(root).All(f => File.GetLastWriteTimeUtc(f) < restored);
    }

    /// <summary>
    /// The files a restore's result depends on: the repository's project, props, targets, lock and NuGet/SDK settings files
    /// (not bin/obj), the props/targets/settings files MSBuild and NuGet read in the folders above it, and the user's NuGet.Config.
    /// </summary>
    public static List<string> RestoreInputs(string root)
    {
        var top = RepositoryRoot(root) ?? root;
        static bool IsInput(string file) => Path.GetExtension(file).ToLowerInvariant() is ".csproj" or ".vbproj" or ".fsproj" or ".props" or ".targets" or ".slnx" or ".sln" or ".slnf"
            || Path.GetFileName(file).ToLowerInvariant() is "nuget.config" or "global.json" or "packages.lock.json" or "packages.config";
        var files = StyleCopSetup.EnumerateFiles(top).Where(IsInput).ToList();
        for (var folder = Path.GetDirectoryName(Path.GetFullPath(top)); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            files.AddRange(Directory.EnumerateFiles(folder).Where(IsInput));
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        files.AddRange(new[] { Path.Combine(home, ".nuget", "NuGet", "NuGet.Config"), Path.Combine(appData, "NuGet", "NuGet.Config") }.Where(File.Exists));
        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Records that format's restore of <paramref name="assetsFiles"/> succeeded now, with the first warning it printed
    /// (<see cref="RestoreWarning"/>), for <see cref="RestoreIsCurrent"/> and <see cref="SkippedRestoreWarning"/> (a courtesy, never an error).
    /// </summary>
    public static void SaveRestoreStamp(string root, IEnumerable<string> assetsFiles, string? warning, string? stampFolder = null)
    {
        try
        {
            var file = RestoreStampFile(root, stampFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllLines(file, StampLines(assetsFiles).Append(WarningMark + (warning ?? string.Empty)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// The warning the skipped restore would have printed (<see cref="RestoreWarning"/>; 'dotnet restore' says a no-op restore's
    /// warnings again): the one format's last restore of the same output printed (<see cref="SaveRestoreStamp"/>), else the first
    /// warning an assets file keeps in its logs, as the restore prints it ('Lib/Lib.csproj: NU1603: ...').
    /// </summary>
    public static string? SkippedRestoreWarning(string root, IReadOnlyCollection<(string Project, string AssetsFile)> projects, string? stampFolder = null)
    {
        if (RestoreStamp(root, projects.Select(p => p.AssetsFile), stampFolder) is (not null, var warning))
        {
            return warning;
        }

        foreach (var (project, assets) in projects)
        {
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(assets));
                if (json.RootElement.TryGetProperty("logs", out var logs) && logs.ValueKind == JsonValueKind.Array
                    && logs.EnumerateArray().FirstOrDefault(l => l.TryGetProperty("level", out var level) && level.GetString() == "Warning") is { ValueKind: JsonValueKind.Object } first
                    && first.TryGetProperty("code", out var code) && first.TryGetProperty("message", out var message))
                {
                    var path = first.TryGetProperty("projectPath", out var own) && own.GetString() is { Length: > 0 } ownPath ? ownPath : project;
                    return RestoreWarning(root, [$"{path} : warning {code.GetString()}: {message.GetString()}"]);
                }
            }
            catch (Exception e) when (e is JsonException or IOException or InvalidOperationException)
            {
            }
        }

        return null;
    }

    /// <summary>The projects whose assets <see cref="RestoreIsCurrent"/> checks: the SDK-style ones, by their evaluated ProjectAssetsFile.</summary>
    public static List<(string Project, string AssetsFile)> RestoredProjects(IEnumerable<(string FullPath, string AssetsFile)> projects) =>
        projects.Where(p => !IsOldStyle(p.FullPath)).Select(p => (p.FullPath, p.AssetsFile)).ToList();

    /// <summary>
    /// The project and configuration lists of a configuration's generator builds (<see cref="UncheckedLine"/>): the projects
    /// and configurations to check, in their order, grouped by configuration (one build per configuration, the projects together).
    /// </summary>
    public static List<(string Configuration, List<string> Projects)> BuildGroups(IEnumerable<(string Project, string Configuration)> builds) =>
        builds.GroupBy(u => u.Configuration, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Select(u => u.Project).Distinct(StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();

    /// <summary>
    /// The text of a temporary MSBuild project that rebuilds <paramref name="projects"/> in one build (shared references once,
    /// in parallel), as 'dotnet build &lt;project&gt; --no-incremental' rebuilds each: <see cref="UncheckedLine"/>.
    /// </summary>
    public static string TogetherProject(IEnumerable<string> projects) =>
        "<Project>\n  <ItemGroup>\n"
        + string.Concat(projects.Select(p => $"    <StyleBroCheck Include=\"{System.Security.SecurityElement.Escape(p)}\" />\n"))
        + "  </ItemGroup>\n"
        + "  <Target Name=\"Build\">\n    <MSBuild Projects=\"@(StyleBroCheck)\" Targets=\"Rebuild\" BuildInParallel=\"true\" />\n  </Target>\n"
        + "  <Target Name=\"Rebuild\" DependsOnTargets=\"Build\" />\n"
        + "</Project>\n";

    /// <summary>
    /// The lines of a successful build of several projects together (<see cref="TogetherProject"/>) that count for one of them
    /// (<see cref="UncheckedLine"/>): its own ('[...\App.csproj]', also '::TargetFramework=...') and those that name no project.
    /// Its findings are what a build of it alone reports; the other projects' lines never counted for it.
    /// </summary>
    public static List<string> OwnOutput(string project, IEnumerable<string> output)
    {
        var own = Path.GetFullPath(project);
        return output.Where(l => Regex.Match(l, @"\[(?<project>[^\]]*\.\w+proj(?:::[^\]]*)?)\]\s*$") is not { Success: true } m
            || string.Equals(Path.GetFullPath(BuildLineProject(m.Groups["project"].Value)), own, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Whether to skip the restore: not with --no-restore (no restore at all) or --restore (always), else when its output is <paramref name="current"/>.</summary>
    public static bool SkipsRestore(bool noRestore, bool forceRestore, Func<bool> current) => !noRestore && !forceRestore && current();

    /// <summary>A project (full path) and everything it references, transitively, as format evaluated them (<see cref="ReadProjects"/>).</summary>
    public static HashSet<string> ProjectGraph(string project, Func<string, IEnumerable<string>> references)
    {
        var graph = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var next = new Stack<string>([Path.GetFullPath(project)]);
        while (next.Count > 0)
        {
            var current = next.Pop();
            if (graph.Add(current))
            {
                foreach (var reference in references(current))
                {
                    next.Push(Path.GetFullPath(reference));
                }
            }
        }

        return graph;
    }

    /// <summary>
    /// The views (project, framework, configuration) <see cref="Separate"/> asks for when it splits <paramref name="runs"/> (the
    /// configuration runs): every project a run loads (its projects and what they reference, transitively), so they can be
    /// evaluated at once (owner's decision 2026-10-17, phase 0: PalCalc evaluated 11 of them one at a time, ~3 s each).
    /// </summary>
    public static List<(string Project, string Framework, string Configuration)> RunViews(IEnumerable<(string Framework, string Configuration, List<string> Projects)> runs, Func<string, IEnumerable<string>> references) =>
        runs.SelectMany(r => r.Projects.SelectMany(p => ProjectGraph(p, references)).Distinct(StringComparer.OrdinalIgnoreCase).Select(p => (p, r.Framework, r.Configuration)))
            .Distinct()
            .ToList();

    /// <summary>
    /// The versions a run's result depends on besides the files and options (<see cref="StateKey"/>): the SDK that 'dotnet'
    /// resolves in the folder (its global.json; 'dotnet format' ships with it), and the StyleBro.Analyzers versions the
    /// projects' restore output lists (project.assets.json in each project's obj folder, and under an artifacts folder).
    /// </summary>
    public static string Versions(string root, Func<string, string>? sdk = null)
    {
        string version;
        try
        {
            version = (sdk ?? (folder => Capture(folder, "--version")))(root).Trim();
        }
        catch (Exception e) when (e is ToolException or System.ComponentModel.Win32Exception or IOException)
        {
            version = "unknown";
        }

        var source = RepositoryRoot(root) ?? root;
        var assets = StyleCopSetup.EnumerateFiles(source).Where(f => Path.GetExtension(f).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.Combine(Path.GetDirectoryName(p)!, "obj", "project.assets.json"))
            .Concat(Directory.Exists(Path.Combine(source, "artifacts", "obj")) ? Directory.EnumerateFiles(Path.Combine(source, "artifacts", "obj"), "project.assets.json", SearchOption.AllDirectories) : [])
            .Where(File.Exists);
        var packages = assets.SelectMany(a => Regex.Matches(File.ReadAllText(a), "\"StyleBro\\.Analyzers/(?<version>[^\"]+)\"", RegexOptions.IgnoreCase).Select(m => m.Groups["version"].Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);
        return $"sdk {version}; StyleBro.Analyzers {string.Join(",", packages)}";
    }

    /// <summary>Marks the check of every plan entry of a 'format' run on <paramref name="root"/> as clean (<see cref="CheckedClean"/>; tests stand in for 'dotnet format' with it).</summary>
    public static void MarkCheckedClean(string root) => CheckedClean[Key(root)] = true;

    /// <summary>Whether the last 'format' run on <paramref name="root"/> was a clean check of every plan entry (and forgets it).</summary>
    public static bool TakeCheckedClean(string root) => CheckedClean.TryRemove(Key(root), out var clean) && clean;

    /// <summary>Keeps the findings a 'format' run's checks found on <paramref name="root"/> (<see cref="FindingsToFix"/>) for the end of that run.</summary>
    public static void RecordCheckFindings(string root, List<string> findings) => CheckFindings[Key(root)] = findings;

    /// <summary>The findings the checks of the last 'format' run on <paramref name="root"/> found (<see cref="CheckFindings"/>), and forgets them.</summary>
    public static List<string> TakeCheckFindings(string root) => CheckFindings.TryRemove(Key(root), out var lines) ? lines : [];

    /// <summary>
    /// The findings among a run's check <paramref name="lines"/> that its fixes should change: not whitespace or IDE0055
    /// (<see cref="IsFormattingLine"/>: only line endings where the run changes nothing, or kept indentation) and not one the
    /// fixes keep on purpose (<paramref name="known"/>).
    /// </summary>
    public static List<string> FindingsToFix(IEnumerable<string> lines, IReadOnlyCollection<KeptFinding> known) =>
        lines.Where(l => IsFindingLine(l) && !IsFormattingLine(l) && AsKept(l, known) is null).Select(l => l.Trim()).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>What 'format' and --verify-no-changes say about findings no fix changes.</summary>
    public static string UnfixedLine(int count) =>
        $"Not clean: {count} finding{(count == 1 ? string.Empty : "s")} above no fix changes (the build reports {(count == 1 ? "it" : "them")}): fix {(count == 1 ? "it" : "them")} by hand, or suppress or baseline {(count == 1 ? "it" : "them")}.";

    /// <summary>The record <see cref="SaveKept"/> wrote for this state of the repository and these options, else null.</summary>
    public static CleanRecord? LoadRecord(string root, IEnumerable<string> args)
    {
        try
        {
            var lines = File.Exists(KeptRecord(root)) ? File.ReadAllLines(KeptRecord(root)) : [];
            if (lines.Length == 0 || lines[0] != StateKey(root, args))
            {
                return null;
            }

            var body = lines.Skip(1).ToList();
            return new CleanRecord(
                body.Select(KeptFinding.Parse).OfType<KeptFinding>().ToList(),
                body.Contains(CheckedMark),
                body.Where(l => l.StartsWith(VerdictMark, StringComparison.Ordinal)).Select(l => l.Substring(VerdictMark.Length)).ToList(),
                body.Where(l => l.StartsWith(BuildMark, StringComparison.Ordinal)).Select(l => l.Split('\t')).Where(p => p.Length == 3).Select(p => (p[1], p[2])).ToList(),
                body.FirstOrDefault(l => l.StartsWith(GeneratorMark, StringComparison.Ordinal))?.Substring(GeneratorMark.Length));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The lines --verify-no-changes prints after a clean check that reported <paramref name="reported"/> (kept findings), as <see cref="Verify"/> prints them.</summary>
    public static List<string> CleanVerdict(string root, IReadOnlyCollection<KeptFinding> reported) =>
        reported.Count == 0
            ? ["Clean."]
            : [.. KeptSummary(reported, root, clean: true), .. new[] { BaselineOffLine(root, reported.Count) }.OfType<string>()];

    /// <summary>
    /// --verify-no-changes from the record of the last clean 'format' on this state (owner's decision 2026-10-17): when that
    /// run's check of every plan entry was clean (<see cref="CheckedClean"/>), verify's checks would find the same, so it says
    /// what verify says after them (the verdict the record holds) without running them; the builds of the configurations an
    /// in-solution generator keeps from the checks still run (<see cref="UncheckedLine"/>: the build is the check there). Null
    /// (run the checks) without such a record.
    /// </summary>
    internal static int? VerifyFromRecord(string[] args, string root, Action<string> log)
    {
        using var phase = PhaseTimes.Measure("verify: state key");
        if (LoadRecord(root, args) is not { Checked: true } record)
        {
            return null;
        }

        log($"{FromRecordHeader} (the code, settings, options and the SDK, package and tool versions are the same), so its check of every run stands: nothing to run again.");
        if (record.Builds.Count > 0)
        {
            if (record.GeneratorLine is { } generatorLine)
            {
                log(generatorLine);
            }

            RecordUnchecked(root, record.Builds);
        }

        // As verify reports a check that found only the kept findings it knows (with --json).
        if (record.Kept.Count > 0)
        {
            JsonReport.AddRun(1, [], null);
            JsonReport.AddKept(record.Kept);
        }

        record.Verdict.ForEach(log);
        return 0;
    }

    /// <summary>Whether a project.nuget.cache says the restore succeeded and every package file it expects is in the packages folder.</summary>
    private static bool RestoreSucceeded(string cache)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(cache));
            var rootElement = json.RootElement;
            return rootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True
                && (!rootElement.TryGetProperty("expectedPackageFiles", out var expected) || expected.EnumerateArray().All(e => e.GetString() is { } path && File.Exists(path)));
        }
        catch (Exception e) when (e is JsonException or IOException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// When format's last restore of these assets files succeeded, and the first warning it printed, if none of them was
    /// written since (<see cref="SaveRestoreStamp"/>); (null, null) otherwise.
    /// </summary>
    private static (DateTime? Time, string? Warning) RestoreStamp(string root, IEnumerable<string> assetsFiles, string? stampFolder)
    {
        try
        {
            var file = RestoreStampFile(root, stampFolder);
            var lines = File.Exists(file) ? File.ReadAllLines(file) : [];
            if (lines.Length == 0 || !lines.SkipLast(1).SequenceEqual(StampLines(assetsFiles)) || !lines[^1].StartsWith(WarningMark, StringComparison.Ordinal))
            {
                return (null, null);
            }

            return (File.GetLastWriteTimeUtc(file), lines[^1].Length > WarningMark.Length ? lines[^1].Substring(WarningMark.Length) : null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return (null, null);
        }
    }

    /// <summary>The assets files that exist with their write times, as <see cref="SaveRestoreStamp"/> records them.</summary>
    private static IEnumerable<string> StampLines(IEnumerable<string> assetsFiles) =>
        assetsFiles.Where(File.Exists).Order(StringComparer.OrdinalIgnoreCase).Select(a => $"{a}\t{File.GetLastWriteTimeUtc(a).Ticks}");

    /// <summary>Where <see cref="SaveRestoreStamp"/> records a folder's last restore.</summary>
    private static string RestoreStampFile(string root, string? stampFolder) =>
        Path.Combine(stampFolder ?? Path.Combine(Path.GetTempPath(), "stylebro-restore"), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Key(root).ToUpperInvariant())))[..16] + ".txt");

    /// <summary>A root as the per-root records key it.</summary>
    private static string Key(string root) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));

    /// <summary>
    /// How long each phase of a command took (--timings): measured where they run, printed at the end in the order they
    /// started (phases that ran at once overlap). Per command (an async-local recorder, which the parallel checks and the
    /// output threads see too), so commands that run at once (tests) don't mix.
    /// </summary>
    public static class PhaseTimes
    {
        private static readonly System.Threading.AsyncLocal<Recorder?> Current = new();

        /// <summary>Measures the phases of this command until the result is disposed.</summary>
        public static IDisposable Start()
        {
            var before = Current.Value;
            Current.Value = new Recorder();
            return new Scope(() => Current.Value = before);
        }

        /// <summary>Starts measuring a phase; disposing the result ends it (nothing when this command doesn't measure).</summary>
        public static IDisposable Measure(string name) => Current.Value is { } recorder ? recorder.Phase(name) : new Scope(null);

        /// <summary>The lines that list this command's phases, or none when nothing was measured.</summary>
        public static List<string> Lines()
        {
            var phases = Current.Value?.Phases() ?? [];
            if (phases.Count == 0)
            {
                return [];
            }

            var width = phases.Max(p => p.Name.Length);
            return phases.Select(p => $"  {p.Name.PadRight(width)}  {p.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),7} s").Prepend("Timings (phases that ran at once overlap):").ToList();
        }

        private sealed class Recorder
        {
            private readonly List<(long Order, string Name, TimeSpan Elapsed)> phases = [];
            private long started;

            public IDisposable Phase(string name)
            {
                var order = System.Threading.Interlocked.Increment(ref started);
                var watch = Stopwatch.StartNew();
                return new Scope(() =>
                {
                    lock (phases)
                    {
                        phases.Add((order, name, watch.Elapsed));
                    }
                });
            }

            public List<(long Order, string Name, TimeSpan Elapsed)> Phases()
            {
                lock (phases)
                {
                    return phases.OrderBy(p => p.Order).ToList();
                }
            }
        }

        private sealed class Scope(Action? end) : IDisposable
        {
            public void Dispose() => end?.Invoke();
        }
    }

    /// <summary>What a clean 'format' recorded for --verify-no-changes (<see cref="LoadRecord"/>).</summary>
    public sealed record CleanRecord(List<KeptFinding> Kept, bool Checked, List<string> Verdict, List<(string Project, string Configuration)> Builds, string? GeneratorLine);
}
