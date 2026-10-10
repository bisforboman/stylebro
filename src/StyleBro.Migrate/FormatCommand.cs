using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using StyleBro.Analyzers;

namespace StyleBro.Migrate;

/// <summary>
/// stylebro-migrate format [folder, solution or project] [--all] [dotnet format options]: 'dotnet format' that fixes
/// only StyleBro's rules and the built-in ones init/migrate turn on (<see cref="Diagnostics"/>; --all: everything), never
/// inside git submodules, and is safe in multi-targeted repositories. There 'dotnet format' loads one copy of every
/// file per target framework, the SDK's IDE0055 fix edits each copy on its own (each sees other '#if' code), and
/// Roslyn's linked-file merge crashes on
/// the result: nothing is written. Its whitespace pass only formats the first framework's code. Loading the projects
/// for one framework at a time leaves nothing to merge, so this runs 'dotnet format' once per target framework, each
/// time on the projects that target it (a temporary solution filter). A repository without multi-targeted projects
/// gets one plain run.
/// </summary>
/// <remarks>
/// The framework is chosen with TargetFramework as an environment variable: unlike a property set later, the project
/// file's own conditions ('$(TargetFramework)' == 'net46' for DefineConstants, references) see it. It reaches every
/// project the run loads, also referenced ones, so <see cref="SelectFrameworkTargets"/> clears it again, right after
/// the project file, for projects that should load as usual (<see cref="Keep"/>): forcing a library onto a framework it
/// doesn't have failed (Newtonsoft.Json's net46 tests: no net46 reference assemblies for the library, the tests were
/// skipped silently), and clearing it for every project without the framework failed too (Serilog's TestDummies,
/// netstandard2.0, references Serilog, which then offered only net8.0: TestDummies and the tests that use it didn't load).
/// </remarks>
internal static class FormatCommand
{
    /// <summary>
    /// Imported right after each project file (BeforeMicrosoftNETSdkTargets), before the SDK decides whether it's
    /// multi-targeted: a multi-targeted project not on the keep list loads as usual (Roslyn's per-framework global
    /// property then wins over the environment).
    /// </summary>
    public const string SelectFrameworkTargets = """
        <Project>
          <!-- stylebro-migrate format: TargetFramework comes from the environment; projects not on the keep list load as usual. -->
          <PropertyGroup Condition="'$(StyleBroFormatKeep)' != '' and '$(TargetFrameworks)' != '' and !$(StyleBroFormatKeep.Contains('|$(MSBuildProjectFullPath.ToUpperInvariant())|'))">
            <TargetFramework></TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    /// <summary>The most runs 'stylebro-migrate format' makes until a run changes nothing.</summary>
    public const int MaxRuns = 3;

    /// <summary>The option for a single run (the preview counts its own runs).</summary>
    public const string OnceOption = "--once";

    /// <summary>The exit code when files are left to change: a run still changed files, or --verify-no-changes found some ('dotnet format''s code).</summary>
    public const int NotCleanExitCode = 2;

    /// <summary>Where people and agents read what a kept finding means.</summary>
    public const string KeptFindingsHelp = "https://bisforboman.github.io/stylebro/getting-started/#findings-kept-on-purpose";

    /// <summary>The start of the line that says a project wasn't formatted at all (<see cref="Incomplete"/>).</summary>
    public const string IncompleteMarker = "Incomplete:";

    /// <summary>The option that formats only the files named after it (for agents: seconds on a small change).</summary>
    public const string FilesOption = "--files";

    /// <summary>The start of the line that says which rules a run fixes (printed for the first run only).</summary>
    private const string FixingHeader = "Fixing StyleBro's rules";

    /// <summary>The start of the line that lists the target frameworks (printed for the first run only).</summary>
    private const string MultiTargetedHeader = "Multi-targeted: ";

    /// <summary>The start of the line that says the whitespace pass doesn't run (printed for the first run only).</summary>
    private const string NoWhitespaceHeader = "No whitespace pass:";

    /// <summary>The start of the line that names files with old Mac line endings (printed for the first run only).</summary>
    private const string OldMacHeader = "Not formatted:";

    /// <summary>What to say when the restore 'dotnet format' needs fails (it prints only a stack trace).</summary>
    internal const string RestoreHint = "The restore failed: run 'dotnet restore' to see why (for example NuGet audit warnings with TreatWarningsAsErrors). Once the packages are restored, '--no-restore' skips it.";

    /// <summary>
    /// Runs the command; with <paramref name="output"/>, everything it and 'dotnet format' print goes there instead of the
    /// console. It runs again until a run changes no file (at most <see cref="MaxRuns"/>), printing the files each run
    /// changed: one 'dotnet format' run doesn't always finish (a fix can make work for another rule). Then it lists the
    /// findings StyleBro's fixes keep on purpose, with the reason (<see cref="KeptFinding"/>). Exit code 0 when clean
    /// (kept findings don't count), <see cref="NotCleanExitCode"/> when a run still changed files. With
    /// --verify-no-changes it changes nothing and exits 0 when only kept findings are left (<see cref="Verify"/>). One plain
    /// run with --once or --report. <paramref name="runOnce"/> stands in for one 'dotnet format' run in tests.
    /// </summary>
    public static int Run(string[] args, Action<string>? output = null, Func<string[], Action<string>?, int>? runOnce = null)
    {
        Action<string> log = output ?? Console.WriteLine;
        runOnce ??= RunOnce;
        if (args.Contains(OnceOption) || (args.Contains("--report") && !args.Contains("--verify-no-changes")))
        {
            return runOnce(args.Where(a => a != OnceOption).ToArray(), output);
        }

        var (files, rest) = TakeFiles(args);
        if (files is not null)
        {
            return RunFiles(files, rest, log, output, runOnce);
        }

        var path = Path.GetFullPath(args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal) ? args[0] : ".");
        var root = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        if (JsonReport.Current is { } json && json["path"] is null)
        {
            json["path"] = root;
        }

        var exit = args.Contains("--verify-no-changes") ? Verify(args, root, log, runOnce) : Repeat(args, root, log, output, runOnce);
        JsonReport.Set("clean", exit == 0);
        return exit;
    }

    /// <summary>The files after '--files' (up to the next option), and the other arguments; null when there's no '--files'.</summary>
    public static (List<string>? Files, string[] Others) TakeFiles(string[] args)
    {
        var at = Array.IndexOf(args, FilesOption);
        if (at < 0)
        {
            return (null, args);
        }

        var files = args.Skip(at + 1).TakeWhile(a => !a.StartsWith("-", StringComparison.Ordinal)).ToList();
        return (files, args.Take(at).Concat(args.Skip(at + 1 + files.Count)).ToArray());
    }

    /// <summary>
    /// '--files': which workspace each file is formatted in, with the files relative to its folder (for '--include').
    /// <paramref name="workspace"/>: the folder, solution or project named on the command line; without one, each file's
    /// nearest project file above it. A file path is relative to <paramref name="currentDirectory"/>, or else to the git
    /// repository's root. Null (with the error said) when a file isn't found, has no project, or is outside the workspace.
    /// </summary>
    public static List<(string Workspace, List<string> Include)>? GroupFiles(IEnumerable<string> files, string? workspace, string currentDirectory, Action<string> error)
    {
        var repository = RepositoryRoot(currentDirectory) ?? currentDirectory;
        var groups = new List<(string Workspace, List<string> Include)>();
        foreach (var file in files)
        {
            var full = new[] { currentDirectory, repository }.Select(b => Path.GetFullPath(file, b)).FirstOrDefault(File.Exists);
            if (full is null)
            {
                error($"--files: not found: {file}");
                return null;
            }

            var target = workspace is null ? NearestProject(Path.GetDirectoryName(full)!, repository) : Path.GetFullPath(workspace, currentDirectory);
            if (target is null)
            {
                error($"--files: no project file above {file}.");
                return null;
            }

            var folder = Directory.Exists(target) ? target : Path.GetDirectoryName(target)!;
            var relative = Path.GetRelativePath(folder, full);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                error($"--files: {file} isn't under {folder}.");
                return null;
            }

            var group = groups.FindIndex(g => g.Workspace.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (group < 0)
            {
                groups.Add((target, new List<string>()));
                group = groups.Count - 1;
            }

            groups[group].Include.Add(relative.Replace('\\', '/'));
        }

        return groups;
    }

    /// <summary>
    /// The nearest project file at or above the folder, up to <paramref name="top"/> (the first by name when a folder has
    /// several), or null.
    /// </summary>
    public static string? NearestProject(string folder, string top)
    {
        // ponytail: a file a project includes from elsewhere ('<Compile Include="../x.cs"/>') is looked up by folder only.
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            if (current.GetFiles("*.csproj").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault() is { } project)
            {
                return project.FullName;
            }

            if (Path.TrimEndingDirectorySeparator(current.FullName).Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(top)), StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// The kept findings the fixes wrote, once each (every target framework's run writes its own), paths moved from
    /// <paramref name="from"/> (a copy) to <paramref name="to"/>, in file and line order.
    /// </summary>
    public static List<KeptFinding> ReadKept(string file, string from, string to)
    {
        if (!File.Exists(file))
        {
            return new List<KeptFinding>();
        }

        var prefix = Path.GetFullPath(from).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return File.ReadAllLines(file)
            .Select(KeptFinding.Parse)
            .OfType<KeptFinding>()
            .Select(k => k.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? new KeptFinding(Path.Combine(to, k.Path.Substring(prefix.Length)), k.Line, k.Column, k.Id, k.Reason, k.Message, k.Where is { } w && w.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? Path.Combine(to, w.Substring(prefix.Length)) : k.Where)
                : k)
            .GroupBy(k => (k.Path.ToUpperInvariant(), k.Line, k.Column, k.Id))
            .Select(g => g.First())
            .OrderBy(k => k.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(k => k.Line)
            .ThenBy(k => k.Column)
            .ToList();
    }

    /// <summary>The lines that list the kept findings (none when there are none).</summary>
    public static List<string> KeptSummary(IReadOnlyCollection<KeptFinding> kept, string root)
    {
        var lines = new List<string>();
        if (kept.Count == 0)
        {
            return lines;
        }

        lines.Add($"Clean: {kept.Count} finding{(kept.Count == 1 ? string.Empty : "s")} kept on purpose (listed below). StyleBro's fixes leave these: renaming by hand breaks what the check protects. Check those uses first, or suppress or baseline them ({KeptFindingsHelp}).");
        foreach (var finding in kept)
        {
            var path = Path.GetRelativePath(root, finding.Path).Replace(Path.DirectorySeparatorChar, '/');
            var where = finding.Where is { } w ? $" (first: {Relative(root, w)})" : string.Empty;
            lines.Add($"  {path}({finding.Line},{finding.Column}): {finding.Id} {finding.Message}. Kept: {KeptFinding.Describe(finding.Reason)}{where}.");
        }

        return lines;
    }

    /// <summary>Whether a 'dotnet format' output line reports one of the kept findings ('path(line,column): warning ID: ...').</summary>
    public static bool IsKeptLine(string line, ISet<(string Path, int Line, string Id)> kept) => KeptLine(line, kept) is not null;

    /// <summary>
    /// A 'dotnet format' output line, with a kept finding's severity replaced by 'kept' ('path(1,2): kept BRO1303: ...'), so
    /// it doesn't read (or count in CI) as one to fix.
    /// </summary>
    public static string MarkKept(string line, ISet<(string Path, int Line, string Id)> kept) =>
        KeptLine(line, kept) is { } severity ? line.Remove(severity.Index, severity.Length).Insert(severity.Index, "kept") : line;

    /// <summary>The C# files under the root (not bin/obj, not submodules) with their size and time, to tell which a run changed.</summary>
    public static Dictionary<string, (long Length, DateTime Written)> Snapshot(string root) =>
        StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                f => f,
                f =>
                {
                    var info = new FileInfo(f);
                    return (info.Length, info.LastWriteTimeUtc);
                },
                StringComparer.OrdinalIgnoreCase);

    /// <summary>The files that differ between two snapshots (added, removed or written), sorted.</summary>
    public static List<string> ChangedFiles(IReadOnlyDictionary<string, (long Length, DateTime Written)> before, IReadOnlyDictionary<string, (long Length, DateTime Written)> after) =>
        after.Where(a => !before.TryGetValue(a.Key, out var b) || b != a.Value).Select(a => a.Key)
            .Concat(before.Keys.Where(k => !after.ContainsKey(k)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>The changed files to print (relative to the root, indented), at most <paramref name="max"/> and a count of the rest.</summary>
    public static List<string> ListFiles(string root, IReadOnlyList<string> files, int max) =>
        files.Take(max).Select(f => "  " + Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Concat(files.Count > max ? new[] { $"  ... and {files.Count - max} more" } : Array.Empty<string>())
            .ToList();

    /// <summary>
    /// The project 'dotnet format' skipped, from its warning that the project's references didn't load (a broken restore,
    /// a corrupt package cache), else null. Its files weren't formatted, and it says nothing else about them.
    /// </summary>
    public static string? SkippedProject(string line) =>
        Regex.Match(line, "Required references did not load for (.+?) or referenced project") is { Success: true } match ? match.Groups[1].Value : null;

    /// <summary>
    /// What to say when 'dotnet format' skipped projects (<see cref="SkippedProject"/>): project name -> the frameworks whose
    /// runs skipped it ("" for a plain run). <paramref name="frameworks"/>: project name -> the frameworks it was run for. A
    /// project skipped in every run wasn't formatted at all (<see cref="IncompleteMarker"/>; the preview stops then); one
    /// skipped for some frameworks only (Bogus: netstandard1.3) only misses the code just those compile.
    /// </summary>
    public static List<string> Incomplete(IReadOnlyDictionary<string, SortedSet<string>> skipped, IReadOnlyDictionary<string, List<string>> frameworks)
    {
        var lines = new List<string>();
        var whole = skipped.Where(s => !frameworks.TryGetValue(s.Key, out var all) || all.All(s.Value.Contains)).Select(s => s.Key).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (whole.Count > 0)
        {
            lines.Add($"{IncompleteMarker} 'dotnet format' skipped {string.Join(", ", whole)} (references didn't load), so those files weren't formatted.");
            lines.Add("  Run 'dotnet restore' (or 'dotnet build') and fix what it reports, e.g. a missing SDK or package or a corrupt package cache, then run again.");
        }

        foreach (var (project, skippedFor) in skipped.Where(s => !whole.Contains(s.Key)).OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"Partly incomplete: 'dotnet format' skipped {project} for {string.Join(", ", skippedFor)} (references didn't load): code only that framework compiles (inside #if) wasn't formatted.");
        }

        return lines;
    }

    /// <summary>
    /// The ids 'stylebro-migrate format' fixes by default: every StyleBro rule, and the built-in IDE and CA rules init's template
    /// and the stylebro blocks of the .editorconfig files at and above <paramref name="root"/> name (rules set to none
    /// aren't fixed anyway).
    /// </summary>
    public static List<string> Diagnostics(string root)
    {
        var ids = new SortedSet<string>(Migration.StyleBroRules().Select(r => r.Id), StringComparer.Ordinal);
        var text = new List<string> { InitCommand.Template() };
        for (var folder = new DirectoryInfo(root); folder is not null; folder = folder.Parent)
        {
            var path = Path.Combine(folder.FullName, ".editorconfig");
            if (File.Exists(path))
            {
                text.AddRange(Regex.Matches(File.ReadAllText(path), "# BEGIN stylebro-.*?# END stylebro-", RegexOptions.Singleline).Select(m => m.Value));
            }
        }

        foreach (Match match in Regex.Matches(string.Join("\n", text), @"dotnet_diagnostic\.((?:IDE|CA)\d+)\.severity", RegexOptions.IgnoreCase))
        {
            ids.Add(match.Groups[1].Value.ToUpperInvariant());
        }

        return ids.ToList();
    }

    /// <summary>Whether a 'dotnet format' output line says its restore failed (it prints a stack trace, nothing about why).</summary>
    public static bool IsRestoreFailure(string line) => line.Contains("Restore operation failed", StringComparison.Ordinal);

    /// <summary>
    /// Whether the stylebro blocks of the .editorconfig files above, at and below the root set IDE0055 and only to 'none'
    /// (a migration from a StyleCop setup with its spacing rules off). Without a block saying so, whitespace formatting runs.
    /// </summary>
    public static bool WhitespaceOff(string root)
    {
        var files = StyleCopSetup.EnumerateFiles(root).Where(f => Path.GetFileName(f) == ".editorconfig").ToList();
        for (var folder = new DirectoryInfo(root).Parent; folder is not null; folder = folder.Parent)
        {
            files.Add(Path.Combine(folder.FullName, ".editorconfig"));
        }

        var values = files.Where(File.Exists)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), "# BEGIN stylebro-.*?# END stylebro-", RegexOptions.Singleline))
            .SelectMany(block => Regex.Matches(block.Value, @"^\s*dotnet_diagnostic\.IDE0055\.severity\s*=\s*(\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .ToList();
        return values.Count > 0 && values.All(v => v == "none");
    }

    /// <summary>
    /// The C# files under the root (relative, '/'-separated) with old Mac line endings: a CR without an LF after it. The
    /// SDK's formatter adds a line break to them on every run.
    /// </summary>
    public static List<string> OldMacLineEndings(string root)
    {
        static bool HasLoneCr(byte[] bytes)
        {
            for (var at = Array.IndexOf(bytes, (byte)'\r'); at >= 0; at = Array.IndexOf(bytes, (byte)'\r', at + 1))
            {
                if (at + 1 == bytes.Length || bytes[at + 1] != '\n')
                {
                    return true;
                }
            }

            return false;
        }

        return StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && HasLoneCr(File.ReadAllBytes(f)))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// format-report.json files merged: one entry per file (the first run's), with every distinct change of all runs
    /// (a change both frameworks see is listed once), in the order they came.
    /// </summary>
    public static string MergeReports(IEnumerable<string> reports)
    {
        var merged = new JsonArray();
        var byPath = new Dictionary<string, (JsonArray Changes, HashSet<string> Seen)>(StringComparer.OrdinalIgnoreCase);
        foreach (var report in reports)
        {
            foreach (var document in JsonNode.Parse(report)?.AsArray() ?? new JsonArray())
            {
                if (document is not JsonObject entry)
                {
                    continue;
                }

                var path = entry["FilePath"]?.GetValue<string>() ?? entry["FileName"]?.GetValue<string>() ?? string.Empty;
                var changes = entry["FileChanges"]?.AsArray() ?? new JsonArray();
                if (!byPath.TryGetValue(path, out var target))
                {
                    var copy = (JsonObject)entry.DeepClone();
                    copy["FileChanges"] = target.Changes = new JsonArray();
                    target.Seen = new HashSet<string>(StringComparer.Ordinal);
                    byPath[path] = target;
                    merged.Add(copy);
                }

                foreach (var change in changes)
                {
                    if (change is not null && target.Seen.Add(change.ToJsonString()))
                    {
                        target.Changes.Add(change.DeepClone());
                    }
                }
            }
        }

        return merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Whether an output line is shown: not when it was already (a diagnostic every framework's run reports, the
    /// workspace warning); blank lines always are.
    /// </summary>
    public static bool IsNewLine(ISet<string> shown, string line) => line.Trim().Length == 0 || shown.Add(line);

    /// <summary>
    /// The runs: every target framework with the projects that target it, in a stable order. One entry (or none) means a
    /// plain run is enough: no project targets several frameworks.
    /// </summary>
    public static List<(string Framework, List<string> Projects)> Plan(IReadOnlyDictionary<string, string[]> frameworks)
    {
        if (frameworks.Values.All(f => f.Length <= 1))
        {
            return new List<(string, List<string>)>();
        }

        return frameworks
            .SelectMany(p => p.Value.Select(f => (Framework: f, Project: p.Key)))
            .GroupBy(x => x.Framework, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Select(x => x.Project).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();
    }

    /// <summary>
    /// The projects a run loads for its framework (by full path): those that target it, and those that reference one of
    /// them, directly or not, so the reference resolves (it only offers the run's framework then). The rest load as usual.
    /// </summary>
    public static HashSet<string> Keep(IReadOnlyDictionary<string, (string[] Frameworks, string[] References)> projects, string framework)
    {
        var keep = new HashSet<string>(
            projects.Where(p => p.Value.Frameworks.Contains(framework, StringComparer.OrdinalIgnoreCase)).Select(p => p.Key),
            StringComparer.OrdinalIgnoreCase);
        for (var added = true; added;)
        {
            added = false;
            foreach (var (project, info) in projects)
            {
                if (!keep.Contains(project) && info.References.Any(keep.Contains))
                {
                    added = keep.Add(project) || added;
                }
            }
        }

        return keep;
    }

    /// <summary>
    /// The folders of a run's projects, relative to <paramref name="root"/>, for 'dotnet format --include'. Referenced
    /// projects that load as usual (multi-targeted) would get their files formatted too, merging the frameworks' copies
    /// again (Newtonsoft.Json: conflict markers in the library during the tests' net6.0 run). Only the run's own projects
    /// are edited; the others get their own runs.
    /// </summary>
    public static IEnumerable<string> Include(string root, string solutionDirectory, IEnumerable<string> projects)
    {
        // A folder needs the trailing '/': 'src/Lib' matches nothing (silently), 'src/Lib/' its files.
        return projects
            .Select(p => Path.GetRelativePath(root, Path.GetDirectoryName(Path.GetFullPath(Path.Combine(solutionDirectory, p)))!))
            .Select(folder => folder == "." ? "**/*.cs" : folder.Replace(Path.DirectorySeparatorChar, '/') + "/")
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A solution filter (.slnf) next to the solution, with the given projects (paths as the solution lists them).</summary>
    public static string SolutionFilter(string solutionFileName, IEnumerable<string> projects)
    {
        return JsonSerializer.Serialize(
            new { solution = new { path = solutionFileName, projects = projects.ToArray() } },
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>A project's target frameworks ('TargetFrameworks' split, else 'TargetFramework') and project references (full paths).</summary>
    public static (string[] Frameworks, string[] References) ParseProject(string msbuildJson)
    {
        using var json = JsonDocument.Parse(msbuildJson);
        var properties = json.RootElement.GetProperty("Properties");
        var several = properties.TryGetProperty("TargetFrameworks", out var s) ? s.GetString() : null;
        var one = properties.TryGetProperty("TargetFramework", out var o) ? o.GetString() : null;
        var frameworks = (string.IsNullOrWhiteSpace(several) ? one ?? string.Empty : several)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var references = json.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty("ProjectReference", out var refs)
            ? refs.EnumerateArray().Select(r => r.GetProperty("FullPath").GetString()!).ToArray()
            : Array.Empty<string>();
        return (frameworks, references);
    }

    /// <summary>
    /// '--files': the usual format (repeat until clean, kept findings, or --verify-no-changes) per workspace, only on the
    /// given files ('dotnet format --include'), with just the project(s) they belong to loaded: seconds instead of minutes.
    /// </summary>
    private static int RunFiles(List<string> files, string[] rest, Action<string> log, Action<string>? output, Func<string[], Action<string>?, int> runOnce)
    {
        var hasPath = rest.Length > 0 && !rest[0].StartsWith("-", StringComparison.Ordinal);
        if (files.Count == 0)
        {
            log($"{FilesOption}: name the files to format.");
            return 1;
        }

        var groups = GroupFiles(files, hasPath ? rest[0] : null, Environment.CurrentDirectory, log);
        if (groups is null)
        {
            return 1;
        }

        if (JsonReport.Current is { } json && json["path"] is null)
        {
            json["path"] = RepositoryRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;
        }

        var exit = 0;
        foreach (var (workspace, include) in groups)
        {
            var arguments = new[] { workspace }.Concat(rest.Skip(hasPath ? 1 : 0)).Append("--include").Concat(include).ToArray();
            var root = Directory.Exists(workspace) ? workspace : Path.GetDirectoryName(workspace)!;
            var code = arguments.Contains("--verify-no-changes") ? Verify(arguments, root, log, runOnce) : Repeat(arguments, root, log, output, runOnce);
            exit = exit == 0 ? code : exit;
        }

        JsonReport.Set("clean", exit == 0);
        return exit;
    }

    /// <summary>Runs 'dotnet format' until a run changes no file (at most <see cref="MaxRuns"/>), then lists the kept findings.</summary>
    private static int Repeat(string[] args, string root, Action<string> log, Action<string>? output, Func<string[], Action<string>?, int> runOnce)
    {
        var keptFile = Path.Combine(Path.GetTempPath(), $"stylebro-kept-{Guid.NewGuid():N}.tsv");
        using var kept = new KeptVariable(keptFile);
        for (var run = 1; ; run++)
        {
            File.Delete(keptFile);
            var before = Directory.Exists(root) ? Snapshot(root) : new Dictionary<string, (long, DateTime)>();

            // --json: each run's changes per rule come from its own report.
            var report = JsonReport.Current is null ? null : Path.Combine(Path.GetTempPath(), $"stylebro-format-json-{Guid.NewGuid():N}");
            var runArgs = report is null ? args : args.Concat(new[] { "--report", report }).ToArray();

            // What it fixes and the framework list are said once, not every run.
            var code = runOnce(runArgs, run == 1 ? output : line =>
            {
                if (!new[] { FixingHeader, MultiTargetedHeader, NoWhitespaceHeader, OldMacHeader }.Any(h => line.StartsWith(h, StringComparison.Ordinal)))
                {
                    log(line);
                }
            });
            var changed = ChangedFiles(before, Snapshot(root));
            if (report is not null)
            {
                JsonReport.AddRun(run, changed, Path.Combine(report, "format-report.json"));
                PreviewCommand.Delete(report);
            }

            if (code != 0)
            {
                return code;
            }

            log($"Run {run}: {changed.Count} file{(changed.Count == 1 ? string.Empty : "s")} changed{(changed.Count == 0 ? ", clean." : ":")}");
            ListFiles(root, changed, 10).ForEach(log);
            if (changed.Count == 0)
            {
                var keptFindings = ReadKept(keptFile, root, root);
                JsonReport.AddKept(keptFindings);
                KeptSummary(keptFindings, root).ForEach(log);
                return 0;
            }

            if (run == MaxRuns)
            {
                log($"Not clean: still changing after {MaxRuns} runs. Run 'stylebro-migrate format' again, and if it keeps changing the same lines, please report it.");
                return NotCleanExitCode;
            }
        }
    }

    /// <summary>
    /// --verify-no-changes: 'dotnet format --verify-no-changes' fails on every finding it would hand to a fix, also those a
    /// StyleBro fix leaves on purpose, so a loop "until clean" never ends. When it fails, this formats a temporary copy of
    /// the repository (the fixes only run when files are written) and decides by the copy: no file changed means only
    /// findings no fix changes are left, listed with the reasons the fixes recorded; exit 0. A changed file means work is
    /// left: 'dotnet format''s output and exit code. The repository itself is never touched.
    /// </summary>
    private static int Verify(string[] args, string root, Action<string> log, Func<string[], Action<string>?, int> runOnce)
    {
        var lines = new List<string>();
        var code = runOnce(args, lines.Add);
        if (code == 0)
        {
            lines.ForEach(log);
            log("Clean.");
            return 0;
        }

        // 'dotnet format --verify-no-changes' says "would change" with 2; anything else is an error of its own (an
        // unknown option, a failed restore), which formatting a copy would only repeat.
        if (code != NotCleanExitCode)
        {
            lines.ForEach(log);
            return code;
        }

        // The whole repository: settings above the folder (.editorconfig, Directory.Build.props, nuget.config) count.
        var source = RepositoryRoot(root) ?? root;
        var copy = Path.Combine(Path.GetTempPath(), $"stylebro-verify-{Guid.NewGuid():N}");
        var keptFile = copy + ".kept.tsv";
        try
        {
            PreviewCommand.Copy(source, copy);
            var copyArgs = args.Where(a => a != "--verify-no-changes").ToList();
            var report = copyArgs.IndexOf("--report");
            if (report >= 0)
            {
                copyArgs.RemoveRange(report, Math.Min(2, copyArgs.Count - report));
            }

            if (copyArgs.Count > 0 && !copyArgs[0].StartsWith("-", StringComparison.Ordinal))
            {
                copyArgs[0] = Path.GetFullPath(Path.Combine(copy, Path.GetRelativePath(source, Path.GetFullPath(copyArgs[0]))));
            }
            else
            {
                copyArgs.Insert(0, Path.GetFullPath(Path.Combine(copy, Path.GetRelativePath(source, root))));
            }

            var before = Snapshot(copy);
            int copyCode;
            List<KeptFinding> kept;
            using (new KeptVariable(keptFile))
            {
                copyCode = runOnce(copyArgs.ToArray(), _ => { });
                kept = ReadKept(keptFile, copy, source);
            }

            var changedFiles = ChangedFiles(before, Snapshot(copy));
            var changed = changedFiles.Count;
            JsonReport.AddRun(1, changedFiles.Select(f => Path.Combine(source, Path.GetRelativePath(copy, f))), null);
            var keys = new HashSet<(string, int, string)>(kept.Select(k => (k.Path.ToUpperInvariant(), k.Line, k.Id)));
            if (copyCode != 0 || changed > 0)
            {
                // The kept findings among them are marked: they aren't what makes it fail.
                lines.Select(l => MarkKept(l, keys)).ToList().ForEach(log);
                if (lines.Count(l => IsKeptLine(l, keys)) is var marked and > 0)
                {
                    log($"{marked} of these {(marked == 1 ? "is" : "are")} kept on purpose (marked 'kept'): StyleBro's fixes leave them, they don't make this fail ({KeptFindingsHelp}).");
                }

                log(changed > 0
                    ? $"Not clean: formatting would change {changed} file{(changed == 1 ? string.Empty : "s")}. Run 'stylebro-migrate format'."
                    : "Not clean: formatting a copy of the repository failed (see above).");
                return code;
            }

            JsonReport.AddKept(kept);
            lines.Where(l => !IsKeptLine(l, keys)).ToList().ForEach(log);
            KeptSummary(kept, root).ForEach(log);
            if (kept.Count == 0)
            {
                log("Clean: formatting would change nothing.");
            }

            return 0;
        }
        finally
        {
            PreviewCommand.Delete(copy);
            File.Delete(keptFile);
        }
    }

    /// <summary>The severity word of a line that reports a kept finding (its position in the line), or null.</summary>
    private static Group? KeptLine(string line, ISet<(string Path, int Line, string Id)> kept)
    {
        var match = Regex.Match(line, @"^\s*(?<path>.+?)\((?<line>\d+),\d+\): (?<severity>\w+) (?<id>\w+):");
        return match.Success
            && kept.Contains((match.Groups["path"].Value.ToUpperInvariant(), int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture), match.Groups["id"].Value))
                ? match.Groups["severity"]
                : null;
    }

    /// <summary>A 'path(line)' relative to the root when it's under it.</summary>
    private static string Relative(string root, string where)
    {
        var relative = Path.IsPathRooted(where) ? Path.GetRelativePath(root, where) : where;
        return (relative.StartsWith("..", StringComparison.Ordinal) ? where : relative).Replace('\\', '/');
    }

    /// <summary>The git repository that holds the folder (the nearest folder above with a .git), or null.</summary>
    private static string? RepositoryRoot(string folder)
    {
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }
        }

        return null;
    }

    /// <summary>One run; afterwards, the projects 'dotnet format' skipped (<see cref="Incomplete"/>).</summary>
    private static int RunOnce(string[] args, Action<string>? output)
    {
        Action<string> log = output ?? Console.WriteLine;
        Action<string> error = output ?? Console.Error.WriteLine;
        var skipped = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        var current = string.Empty;
        var restoreFailed = false;
        void Record(string line)
        {
            restoreFailed |= IsRestoreFailure(line);
            if (SkippedProject(line) is { } project)
            {
                (skipped.TryGetValue(project, out var set) ? set : skipped[project] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)).Add(current);
            }
        }

        void Watch(string line)
        {
            Record(line);
            log(line);
        }

        // Like 'dotnet format': an optional folder, solution or project first, then options, which pass through.
        var hasPath = args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal);
        var path = Path.GetFullPath(hasPath ? args[0] : ".");
        var passThrough = args.Skip(hasPath ? 1 : 0).ToList();
        var root = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        if (!Directory.Exists(root))
        {
            error($"Not found: {path}");
            return 1;
        }

        var workspace = File.Exists(path) ? Path.GetFileName(path) : BaselineCommand.FindWorkspace(root);
        if (workspace is null)
        {
            error(BaselineCommand.NoWorkspace(root, "'stylebro-migrate format <file>'"));
            return 1;
        }

        // By default only StyleBro's rules and the built-in ones init/migrate turn on: plain 'dotnet format' also applies
        // every other analyzer's fixes and compiler fixes (CS8618's 'required', a Sonar fix removing 'init;': build
        // errors in Kavita). Whitespace formatting still runs. --all keeps them.
        if (!passThrough.Remove("--all") && !passThrough.Contains("--diagnostics"))
        {
            var ids = Diagnostics(root);
            log($"{FixingHeader} and the built-in rules stylebro-migrate turns on ({ids.Count} ids); --all applies every analyzer's and compiler fix.");
            passThrough.Add("--diagnostics");
            passThrough.AddRange(ids);
        }

        void Exclude(IReadOnlyCollection<string> paths)
        {
            var exclude = passThrough.IndexOf("--exclude");
            if (exclude < 0)
            {
                passThrough.Add("--exclude");
                passThrough.AddRange(paths);
            }
            else
            {
                passThrough.InsertRange(exclude + 1, paths);
            }
        }

        // Submodules are someone else's code, even when a project compiles files from them.
        if (StyleCopSetup.NestedRepositories(root) is { Count: > 0 } submodules)
        {
            Exclude(submodules);
        }

        // The SDK's formatter adds a line break to a file with old Mac line endings on every run: it never settles.
        if (OldMacLineEndings(root) is { Count: > 0 } oldMac)
        {
            log($"{OldMacHeader} {string.Join(", ", oldMac)}: old Mac line endings (a CR alone). 'dotnet format' (a .NET SDK bug) adds a line break to such a file on every run, so it never settles. Convert them to LF or CRLF, then run again.");
            Exclude(oldMac);
        }

        // 'dotnet format' runs its whitespace pass whatever IDE0055's severity says: where the settings turn IDE0055 off,
        // only the style and analyzer passes run (LiteBus: aligned switch arms flattened, '(T)x' became '(T) x').
        var passes = WhitespaceOff(root) ? new[] { "style", "analyzers" } : new[] { string.Empty };
        if (passes.Length > 1)
        {
            log($"{NoWhitespaceHeader} IDE0055 is off in the stylebro settings, so no whitespace formatting: style and analyzer fixes only.");
        }

        int Format(string target, List<string> options, Func<IEnumerable<string>, int> run)
        {
            // Each pass would write its own format-report.json over the last one: each gets a folder, merged after.
            var at = options.IndexOf("--report");
            var report = passes.Length > 1 && at >= 0 && at + 1 < options.Count ? options[at + 1] : null;
            var folders = new List<string>();
            var exit = 0;
            foreach (var pass in passes)
            {
                var passOptions = options.ToList();
                if (report is not null)
                {
                    folders.Add(Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}"));
                    passOptions[at + 1] = folders[^1];
                }

                var code = run(new[] { "format", pass }.Where(a => a.Length > 0).Append(target).Concat(passOptions));
                exit = exit == 0 ? code : exit;
                if (code != 0 && code != NotCleanExitCode)
                {
                    break; // an error (a failed restore): the next pass would only repeat it
                }
            }

            if (report is not null)
            {
                WriteMergedReport(Path.GetFullPath(report), folders);
            }

            return exit;
        }

        int Done(int code)
        {
            if (restoreFailed)
            {
                log(RestoreHint);
            }

            return code;
        }

        var noRestore = passThrough.Remove("--no-restore");

        var workspacePath = Path.GetFullPath(Path.Combine(root, workspace));
        var solutionDirectory = Path.GetDirectoryName(workspacePath)!;
        var projects = ReadProjects(workspacePath);
        var plan = Plan(projects.ToDictionary(p => p.Key, p => p.Value.Frameworks));
        if (plan.Count <= 1)
        {
            var single = Format(workspacePath, passThrough.Concat(noRestore ? new[] { "--no-restore" } : Array.Empty<string>()).ToList(), a => Dotnet(root, null, a, Watch));
            Incomplete(skipped, new Dictionary<string, List<string>>()).ForEach(log);
            return Done(single);
        }

        log($"{MultiTargetedHeader}one 'dotnet format' run per target framework ({string.Join(", ", plan.Select(p => p.Framework))}).");

        // The restore's own output (its "Build succeeded" block) only matters when it fails.
        var restoreOutput = new List<string>();
        var restore = noRestore ? 0 : Dotnet(root, null, new[] { "restore", workspacePath }, restoreOutput.Add);
        if (restore != 0)
        {
            restoreOutput.ForEach(log);
            restoreFailed = true;
            return Done(restore);
        }

        // Every run would write its own format-report.json over the last one: each gets a folder, merged at the end.
        var reportIndex = passThrough.IndexOf("--report");
        var report = reportIndex >= 0 && reportIndex + 1 < passThrough.Count ? passThrough[reportIndex + 1] : null;
        var reportFolders = new List<string>();

        // The runs print the same diagnostics and workspace warnings once per framework: each line is shown once.
        var shown = new HashSet<string>(StringComparer.Ordinal);
        void Show(string line)
        {
            Record(line);
            if (IsNewLine(shown, line))
            {
                log(line);
            }
        }

        var isSolution = !workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        var select = Path.Combine(Path.GetTempPath(), $"stylebro-format-{Guid.NewGuid():N}.targets");
        File.WriteAllText(select, SelectFrameworkTargets);
        var exit = 0;
        try
        {
            foreach (var (framework, selected) in plan)
            {
                log($"== {framework} ({selected.Count} project(s))");
                current = framework;
                var filter = isSolution ? Path.Combine(solutionDirectory, $".stylebro-format-{framework}.slnf") : null;
                try
                {
                    if (filter is not null)
                    {
                        File.WriteAllText(filter, SolutionFilter(Path.GetFileName(workspacePath), selected));
                    }

                    var options = passThrough.ToList();
                    if (report is not null)
                    {
                        var folder = Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}");
                        reportFolders.Add(folder);
                        options[reportIndex + 1] = folder;
                    }

                    options.Insert(0, "--no-restore");
                    if (!passThrough.Contains("--include"))
                    {
                        options.Add("--include");
                        options.AddRange(Include(root, solutionDirectory, selected));
                    }

                    var keep = Keep(projects.Values.ToDictionary(p => p.FullPath, p => (p.Frameworks, p.References), StringComparer.OrdinalIgnoreCase), framework);
                    var code = Format(filter ?? workspacePath, options, a => Dotnet(root, (framework, select, "|" + string.Join("|", keep.Select(k => k.ToUpperInvariant())) + "|"), a, Show));
                    if (exit == 0)
                    {
                        exit = code;
                    }
                }
                finally
                {
                    if (filter is not null)
                    {
                        File.Delete(filter);
                    }
                }
            }
        }
        finally
        {
            File.Delete(select);
            if (report is not null)
            {
                WriteMergedReport(Path.GetFullPath(report), reportFolders);
            }
        }

        var runFor = plan.SelectMany(p => p.Projects.Select(project => (Name: Path.GetFileNameWithoutExtension(project), p.Framework)))
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Framework).ToList(), StringComparer.OrdinalIgnoreCase);
        Incomplete(skipped, runFor).ForEach(log);
        return Done(exit);
    }

    /// <summary>
    /// Merges the runs' format-report.json files into the one 'dotnet format --report' would write: a path with an
    /// extension is the file, otherwise a folder that gets format-report.json. The temporary folders are deleted.
    /// </summary>
    private static void WriteMergedReport(string report, IEnumerable<string> folders)
    {
        var reports = new List<string>();
        foreach (var folder in folders)
        {
            var file = Path.Combine(folder, "format-report.json");
            if (File.Exists(file))
            {
                reports.Add(File.ReadAllText(file));
            }

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        var path = Path.HasExtension(report) ? report : Path.Combine(report, "format-report.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, MergeReports(reports));
    }

    /// <summary>Each C# project of the solution (keyed by its path as the solution lists it) or the project itself.</summary>
    private static Dictionary<string, (string FullPath, string[] Frameworks, string[] References)> ReadProjects(string workspacePath)
    {
        var directory = Path.GetDirectoryName(workspacePath)!;
        var projects = workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? new[] { Path.GetFileName(workspacePath) }
            : Capture(directory, "sln", workspacePath, "list").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        return projects.ToDictionary(
            p => p,
            p =>
            {
                var fullPath = Path.GetFullPath(Path.Combine(directory, p));
                var (frameworks, references) = ParseProject(Capture(directory, "msbuild", fullPath, "-getProperty:TargetFrameworks", "-getProperty:TargetFramework", "-getItem:ProjectReference", "-nologo"));
                return (fullPath, frameworks, references);
            });
    }

    private static string Capture(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return output.Result;
    }

    /// <summary>Runs dotnet; with <paramref name="output"/>, its output (stdout and stderr) goes there line by line instead of the console.</summary>
    private static int Dotnet(string directory, (string Name, string SelectTargets, string Keep)? framework, IEnumerable<string> arguments, Action<string>? output = null)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = output is not null, RedirectStandardError = output is not null };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // See the remarks on the class: the environment, cleared again for projects that should load as usual.
        if (framework is { } f)
        {
            start.Environment["TargetFramework"] = f.Name;
            start.Environment["StyleBroFormatKeep"] = f.Keep;
            start.Environment["BeforeMicrosoftNETSdkTargets"] = f.SelectTargets;
        }

        using var process = new Process { StartInfo = start };
        if (output is not null)
        {
            var gate = new object();
            void Forward(object sender, DataReceivedEventArgs e)
            {
                if (e.Data is not null)
                {
                    lock (gate)
                    {
                        output(e.Data);
                    }
                }
            }

            process.OutputDataReceived += Forward;
            process.ErrorDataReceived += Forward;
        }

        process.Start();
        if (output is not null)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>Sets <see cref="KeptFinding.Variable"/> for the 'dotnet format' runs this process starts, and restores it.</summary>
    private sealed class KeptVariable : IDisposable
    {
        private readonly string? previous = Environment.GetEnvironmentVariable(KeptFinding.Variable);
        private readonly string file;

        public KeptVariable(string file)
        {
            this.file = file;
            Environment.SetEnvironmentVariable(KeptFinding.Variable, file);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(KeptFinding.Variable, previous);
            File.Delete(file);
        }
    }
}
