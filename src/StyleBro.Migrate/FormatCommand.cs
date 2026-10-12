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
          <PropertyGroup Condition="'$(TargetFrameworks)' != '' and (('$(StyleBroFormatKeep)' != '' and !$(StyleBroFormatKeep.Contains('|$(MSBuildProjectFullPath.ToUpperInvariant())|'))) or '$(StyleBroFormatLoose)' == 'true')">
            <TargetFramework></TargetFramework>
          </PropertyGroup>
          <!-- A project without the run's framework asks the projects it references for all of theirs (StyleBroFormatLoose). -->
          <ItemGroup Condition="'$(StyleBroFormatOther)' != '' and $(StyleBroFormatOther.Contains('|$(MSBuildProjectFullPath.ToUpperInvariant())|'))">
            <ProjectReference Update="@(ProjectReference)" AdditionalProperties="%(ProjectReference.AdditionalProperties);StyleBroFormatLoose=true" />
          </ItemGroup>
          <!-- A project that writes the run's framework another way ('net6' for 'net6.0') gets its own spelling. -->
          <PropertyGroup Condition="'$(StyleBroFormatAliases)' != '' and '$(TargetFramework)' != ''">
            <_StyleBroAliasKey>|$(MSBuildProjectFullPath.ToUpperInvariant())=</_StyleBroAliasKey>
            <_StyleBroAliasAt>$(StyleBroFormatAliases.IndexOf('$(_StyleBroAliasKey)'))</_StyleBroAliasAt>
          </PropertyGroup>
          <PropertyGroup Condition="'$(_StyleBroAliasAt)' != '' and '$(_StyleBroAliasAt)' != '-1'">
            <_StyleBroAliasFrom>$([MSBuild]::Add($(_StyleBroAliasAt), $(_StyleBroAliasKey.Length)))</_StyleBroAliasFrom>
            <TargetFramework>$(StyleBroFormatAliases.Substring($(_StyleBroAliasFrom), $([MSBuild]::Subtract($(StyleBroFormatAliases.IndexOf('|', $(_StyleBroAliasFrom))), $(_StyleBroAliasFrom)))))</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    /// <summary>The most runs 'stylebro-migrate format' makes until a run changes nothing.</summary>
    public const int MaxRuns = 3;

    /// <summary>
    /// The folder 'dotnet format' runs in when it isn't the workspace's (its '--include' paths are relative to it): '--files'
    /// for a file a project links from outside its folder. Internal: <see cref="RunFiles"/> adds it.
    /// </summary>
    public const string WorkingFolderOption = "--working-folder";

    /// <summary>The option for a single run (the preview counts its own runs).</summary>
    public const string OnceOption = "--once";

    /// <summary>
    /// The option for a run that likely changes nothing (the one after a run that changed files): a multi-targeted
    /// repository checks all frameworks at once first (<see cref="IsCleanCheck"/>).
    /// </summary>
    public const string CheckFirstOption = "--check-first";

    /// <summary>The exit code when files are left to change: a run still changed files, or --verify-no-changes found some ('dotnet format''s code).</summary>
    public const int NotCleanExitCode = 2;

    /// <summary>Where people and agents read what a kept finding means.</summary>
    public const string KeptFindingsHelp = "https://bisforboman.github.io/stylebro/getting-started/#findings-kept-on-purpose";

    /// <summary>The start of the line that names projects 'dotnet format' can't load (F#) as the reason for skipped ones.</summary>
    public const string LikelyCause = "  Likely cause:";

    /// <summary>What --verify-no-changes says when a project wasn't loaded in any run (<see cref="IncompleteMarker"/>).</summary>
    public const string NotVerified = "Not verified: the files of the projects 'dotnet format' skipped weren't checked.";

    /// <summary>The start of the line that says a project wasn't formatted at all (<see cref="Incomplete"/>).</summary>
    public const string IncompleteMarker = "Incomplete:";

    /// <summary>The option that formats only the files named after it (for agents: seconds on a small change).</summary>
    public const string FilesOption = "--files";

    /// <summary>How to restore old-style (non-SDK) projects with packages.config before 'format' (docs/getting-started.md, "Old-style (non-SDK) projects").</summary>
    public const string OldStyleRestore = "Restore them with Visual Studio's MSBuild first: msbuild -t:restore -p:RestorePackagesConfig=true (https://bisforboman.github.io/stylebro/getting-started/#old-style-non-sdk-projects).";

    /// <summary>The same for old-style projects without packages.config (PackageReference, or no packages).</summary>
    public const string OldStylePackageReferenceRestore = "Restore them with Visual Studio's MSBuild first: msbuild -t:restore (https://bisforboman.github.io/stylebro/getting-started/#old-style-non-sdk-projects).";

    /// <summary>The SDK bug a run that loads two projects compiling the same files runs into (<see cref="Separate"/>).</summary>
    public const string LinkedMergeIssue = "https://github.com/dotnet/roslyn/issues/86001";

    /// <summary>The start of the line that says old-style projects aren't restored (printed for the first run only).</summary>
    public const string OldStyleHeader = "Old-style projects:";

    /// <summary>The start of the line that says StyleBro's analyzers didn't load, so the run stopped.</summary>
    public const string NotLoadedHeader = "StyleBro's analyzers aren't loaded:";

    /// <summary>The start of the line that names projects StyleBro's analyzers don't load in (printed for the first run only).</summary>
    public const string PartlyLoadedHeader = "Without StyleBro:";

    /// <summary>What to say when the restore fails without a NuGet error to quote.</summary>
    internal const string RestoreHint = "The restore failed: run 'dotnet restore' to see why. Once the packages are restored, '--no-restore' skips it.";

    /// <summary>
    /// The restore's options: NuGet's audit (NU1901-NU1904, errors under TreatWarningsAsErrors) is the build's business, not
    /// formatting's (CleanArchitecture: a vulnerable test package failed every format run on a clean clone).
    /// </summary>
    internal static readonly string[] RestoreOptions = { "-p:NuGetAudit=false" };

    /// <summary>The start of the line that says a Traversal project's projects are formatted (printed for the first run only).</summary>
    private const string TraversalHeader = "Traversal:";

    /// <summary>The start of the line that names the projects formatted in separate runs (printed for the first run only).</summary>
    private const string SharedFilesHeader = "Shared files:";

    /// <summary>The start of the line that says which rules a run fixes (printed for the first run only).</summary>
    private const string FixingHeader = "Fixing StyleBro's rules";

    /// <summary>The same line for --verify-no-changes.</summary>
    private const string CheckingHeader = "Checking StyleBro's rules";

    /// <summary>The start of the line that lists the target frameworks (printed for the first run only).</summary>
    private const string MultiTargetedHeader = "Multi-targeted: ";

    /// <summary>The start of the line that says the whitespace pass doesn't run (printed for the first run only).</summary>
    private const string NoWhitespaceHeader = "No whitespace pass:";

    /// <summary>The start of the line that names files with old Mac line endings (printed for the first run only).</summary>
    private const string OldMacHeader = "Not formatted:";

    /// <summary>The start of the line that names the folders without StyleCop format leaves alone (printed for the first run only).</summary>
    private const string UnformattedHeader = "Leaving alone:";

    /// <summary>The projects <see cref="ReadProjects"/> evaluated, per workspace.</summary>
    private static readonly Dictionary<string, Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile)>> ProjectCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the command; with <paramref name="output"/>, everything it and 'dotnet format' print goes there instead of the
    /// console. It runs again until a run changes no file (at most <see cref="MaxRuns"/>), printing the files each run
    /// changed: one 'dotnet format' run doesn't always finish (a fix can make work for another rule). Then it lists the
    /// findings StyleBro's fixes keep on purpose, with the reason (<see cref="KeptFinding"/>). Exit code 0 when clean
    /// (kept findings don't count), <see cref="NotCleanExitCode"/> when a run still changed files. With
    /// --verify-no-changes it changes nothing and exits 0 when only kept findings are left (<see cref="Verify"/>). One plain
    /// run with --report; --once makes one run, reported like the others. <paramref name="runOnce"/> stands in for one 'dotnet format' run in tests.
    /// </summary>
    public static int Run(string[] args, Action<string>? output = null, Func<string[], Action<string>?, int>? runOnce = null)
    {
        Action<string> log = output ?? Console.WriteLine;
        runOnce ??= RunOnce;

        // --report: the plain run (the preview reads the report). --once: one run, reported like the usual ones.
        if (args.Contains("--report") && !args.Contains("--verify-no-changes"))
        {
            return runOnce(args.Where(a => a != OnceOption).ToArray(), output);
        }

        // Every mode takes the documented options ('--files a.cs --once' passed --once on to 'dotnet format').
        var once = args.Contains(OnceOption);
        args = ProjectAsPath(args.Where(a => a != OnceOption).ToArray());
        var (files, rest) = TakeFiles(args);
        if (files is not null)
        {
            return RunFiles(files, rest, log, output, runOnce, once ? 1 : MaxRuns);
        }

        var path = Path.GetFullPath(args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal) ? args[0] : ".");
        var root = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        if (JsonReport.Current is { } json && json["path"] is null)
        {
            json["path"] = root;
        }

        var exit = args.Contains("--verify-no-changes") ? Verify(args, root, log, runOnce) : Repeat(args, root, log, output, runOnce, once ? 1 : MaxRuns);
        JsonReport.Set("clean", exit == 0);
        return exit;
    }

    /// <summary>The line that says which rules a run fixes, or (<paramref name="verify"/>) checks without changing anything.</summary>
    public static string RulesLine(bool verify, int ids) => verify
        ? $"{CheckingHeader} and the built-in rules stylebro-migrate turns on ({ids} ids), changing nothing; --all checks every analyzer's and compiler fix too."
        : $"{FixingHeader} and the built-in rules stylebro-migrate turns on ({ids} ids); --all applies every analyzer's and compiler fix.";

    /// <summary>One plain 'dotnet format' run (the preview's: it counts the runs and reads the reports itself).</summary>
    public static int RunPlain(string[] args, Action<string>? output) => RunOnce(args.Where(a => a != OnceOption).ToArray(), output);

    /// <summary>
    /// The arguments with '--project &lt;solution or project&gt;' (the preview's and baseline's option) as the path 'format'
    /// takes first: passed on, 'dotnet format' printed its usage text.
    /// </summary>
    public static string[] ProjectAsPath(string[] args)
    {
        var at = Array.IndexOf(args, "--project");
        if (at < 0 || at + 1 >= args.Length)
        {
            return args;
        }

        var hasPath = args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal);
        var project = hasPath ? Path.Combine(args[0], args[at + 1]) : args[at + 1];
        return new[] { project }.Concat(args.Where((_, i) => i != at && i != at + 1 && !(hasPath && i == 0))).ToArray();
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
    /// '--files': which workspace each file is formatted in, the folder 'dotnet format' runs in (the workspace's), and the
    /// files relative to it (for '--include'). <paramref name="workspace"/>: the folder, solution or project named on the
    /// command line; without one, each file's nearest project file above it, else a project that compiles it from outside its
    /// own folder (run from the repository's root: '--include' matches nothing above the folder it runs in). A file path is
    /// relative to <paramref name="currentDirectory"/>, or else to the git repository's root. Null (with the error said) when
    /// a file isn't found, has no project, or is outside the workspace.
    /// </summary>
    public static List<(string Workspace, string Folder, List<string> Include)>? GroupFiles(IEnumerable<string> files, string? workspace, string currentDirectory, Action<string> error)
    {
        var repository = RepositoryRoot(currentDirectory) ?? currentDirectory;
        var groups = new List<(string Workspace, string Folder, List<string> Include)>();
        foreach (var file in files)
        {
            var full = new[] { currentDirectory, repository }.Select(b => Path.GetFullPath(file, b)).FirstOrDefault(File.Exists);
            if (full is null)
            {
                error($"--files: not found: {file}");
                return null;
            }

            var target = workspace is null ? NearestProject(Path.GetDirectoryName(full)!, repository) : Path.GetFullPath(workspace, currentDirectory);
            var folder = target is null ? null : Directory.Exists(target) ? target : Path.GetDirectoryName(target)!;

            // Orleans links src/Orleans.CodeGenerator.Shared/*.cs into its projects: no project above those files.
            if (target is null && LinkingProject(full, RepositoryRoot(Path.GetDirectoryName(full)!) ?? repository) is { } linking)
            {
                (target, folder) = (linking, RepositoryRoot(Path.GetDirectoryName(full)!) ?? repository);
            }

            if (target is null || folder is null)
            {
                error($"--files: no project file above {file}, and no project compiles it.");
                return null;
            }

            var relative = Path.GetRelativePath(folder, full);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                error($"--files: {file} isn't under {folder}.");
                return null;
            }

            var group = groups.FindIndex(g => g.Workspace.Equals(target, StringComparison.OrdinalIgnoreCase) && g.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase));
            if (group < 0)
            {
                groups.Add((target, folder, new List<string>()));
                group = groups.Count - 1;
            }

            groups[group].Include.Add(relative.Replace('\\', '/'));
        }

        return groups;
    }

    /// <summary>
    /// A project under <paramref name="repository"/> that compiles <paramref name="file"/> from outside its own folder, or
    /// null. Only projects whose file names a Compile item going up a folder are evaluated ('dotnet msbuild -getItem').
    /// </summary>
    public static string? LinkingProject(string file, string repository)
    {
        return StyleCopSetup.EnumerateFiles(repository)
            .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                && Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(f)), @"<Compile\b[^>]*\bInclude\s*=\s*""[^""]*\.\.", RegexOptions.IgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(p => ParseProject(Capture(Path.GetDirectoryName(p)!, "msbuild", p, "-getItem:Compile", "-nologo"), Path.GetDirectoryName(p)).Linked
                .Contains(Path.GetFullPath(file), StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The nearest project file at or above the folder, up to <paramref name="top"/> (the first by name when a folder has
    /// several), or null.
    /// </summary>
    public static string? NearestProject(string folder, string top)
    {
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
            .Select(g => g.OrderBy(k => k.Reason == KeptReason.UsedInProjectNotLoaded).First()) // a run that loads every use knows better
            .OrderBy(k => k.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(k => k.Line)
            .ThenBy(k => k.Column)
            .ToList();
    }

    /// <summary>The lines that list the kept findings (none when there are none).</summary>
    public static List<string> KeptSummary(IReadOnlyCollection<KeptFinding> kept, string root, bool clean = true)
    {
        var lines = new List<string>();
        if (kept.Count == 0)
        {
            return lines;
        }

        lines.Add($"{(clean ? "Clean: " : string.Empty)}{kept.Count} finding{(kept.Count == 1 ? string.Empty : "s")} kept on purpose (listed below). StyleBro's fixes leave these: renaming by hand breaks what the check protects. Check those uses first, or suppress or baseline them ({KeptFindingsHelp}).");
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

    /// <summary>
    /// The C# files under the root (not bin/obj, not submodules) with a hash of their content, to tell which a run changed: by
    /// size and time, a file 'dotnet format' wrote back unchanged counted as changed.
    /// </summary>
    public static Dictionary<string, string> Snapshot(string root) =>
        StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .ToDictionary(f => f, f => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f))), StringComparer.OrdinalIgnoreCase);

    /// <summary>The files that differ between two snapshots (added, removed or with other content), sorted.</summary>
    public static List<string> ChangedFiles(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after) =>
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

    /// <summary>Whether a line of 'dotnet format''s output says it crashed (an unhandled exception: its changes may be half written).</summary>
    public static bool IsCrash(string line) => line.StartsWith("Unhandled exception", StringComparison.Ordinal);

    /// <summary>What to say after a run crashed: the run's projects, and the SDK bug when the crash is the linked-file merge's.</summary>
    public static string CrashMessage(IEnumerable<string> projects) =>
        $"'dotnet format' crashed in the run for {string.Join(", ", projects.Order(StringComparer.OrdinalIgnoreCase))}, possibly after writing part of its changes (listed below). "
        + $"If the error says \"Changes must be within bounds of SourceText\", it's Roslyn's linked-file merge ({LinkedMergeIssue}): two projects of that run compile the same file. Please report it with the projects named here.";

    /// <summary>The names of the projects that compile a file another project compiles too (<see cref="Separate"/>).</summary>
    public static List<string> SharingProjects(IEnumerable<(string FullPath, string[] Compile)> projects)
    {
        var list = projects.ToList();
        var owners = list.SelectMany(p => p.Compile.Distinct(StringComparer.OrdinalIgnoreCase).Select(f => (File: f, p.FullPath)))
            .GroupBy(x => x.File, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(x => x.FullPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return list.Where(p => owners.Contains(p.FullPath)).Select(p => Path.GetFileNameWithoutExtension(p.FullPath)).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The file name of a project 'dotnet format' can't load (F#: "Could not format '...fsproj'. Format currently supports only C# ..."), else null.</summary>
    public static string? UnsupportedProject(string line) =>
        Regex.Match(line, @"Could not format '(?:[^']*[\\/])?(?<name>[^'\\/]+)'\. Format currently supports only") is { Success: true } match ? match.Groups["name"].Value : null;

    /// <summary>
    /// What to say when 'dotnet format' skipped projects (<see cref="SkippedProject"/>): project name -> the frameworks whose
    /// runs skipped it ("" for a plain run). <paramref name="frameworks"/>: project name -> the frameworks it was run for. A
    /// project skipped in every run wasn't formatted at all (<see cref="IncompleteMarker"/>; the preview stops then); one
    /// skipped for some frameworks only (Bogus: netstandard1.3) only misses the code just those compile. <paramref name="unsupported"/>:
    /// the projects 'dotnet format' said it can't load (F#), named once as the likely cause.
    /// </summary>
    public static List<string> Incomplete(IReadOnlyDictionary<string, SortedSet<string>> skipped, IReadOnlyDictionary<string, List<string>> frameworks, IReadOnlyCollection<string>? unsupported = null)
    {
        var lines = new List<string>();
        var whole = skipped.Where(s => !frameworks.TryGetValue(s.Key, out var all) || all.All(s.Value.Contains)).Select(s => s.Key).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (whole.Count > 0)
        {
            lines.Add($"{IncompleteMarker} 'dotnet format' skipped {string.Join(", ", whole)} (references didn't load), so those files weren't formatted.");
            lines.Add("  Run 'dotnet restore' (or 'dotnet build') and fix what it reports, e.g. a missing SDK or package or a corrupt package cache, then run again.");
            if (whole.Any(w => skipped[w].Any(f => f.Length > 0)))
            {
                // Autofac: the build was fine; the per-framework run made a reference fail (fixed since, see Other).
                lines.Add("  If the build succeeds, the per-framework runs are the cause: please report it at https://github.com/bisforboman/stylebro/issues with the output of 'dotnet format <solution> --verbosity diagnostic'.");
            }
        }

        // One line per set of frameworks (Orleans: 27 test projects for net10.0, one line each before).
        foreach (var group in skipped.Where(s => !whole.Contains(s.Key)).GroupBy(s => string.Join(", ", s.Value)).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var names = group.Select(s => s.Key).Order(StringComparer.OrdinalIgnoreCase).ToList();
            var shown = names.Count > 5 ? $"{string.Join(", ", names.Take(5))} and {names.Count - 5} more" : string.Join(", ", names);
            lines.Add($"Partly incomplete: 'dotnet format' skipped {shown} for {group.Key} (references didn't load): code only that framework compiles (inside #if) wasn't formatted.");
        }

        // Orleans: the test projects reference F# projects, which 'dotnet format' can't load; a restore doesn't help.
        if (lines.Count > 0 && unsupported is { Count: > 0 })
        {
            lines.Add($"{LikelyCause} they reference {string.Join(", ", unsupported.Order(StringComparer.OrdinalIgnoreCase))}, which 'dotnet format' can't load (C# and Visual Basic only), so a restore won't help. Nothing is broken; that code just isn't formatted.");
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

    /// <summary>
    /// Whether the frameworks' '--verify-no-changes' runs (exit code, output) show that nothing is left: each exited 0, and
    /// none skipped a project (a project that didn't load reports nothing, which proves nothing).
    /// </summary>
    public static bool IsCleanCheck(IEnumerable<(int Code, IReadOnlyList<string> Lines)> runs) =>
        runs.All(r => r.Code == 0 && !r.Lines.Any(l => SkippedProject(l) is not null || IsRestoreFailure(l)));

    /// <summary>The arguments with '--no-restore' (once).</summary>
    public static string[] NoRestore(string[] args) => args.Contains("--no-restore") ? args : args.Append("--no-restore").ToArray();

    /// <summary>
    /// The arguments for the run after one that changed files: no restore (the first run restored, and the runs only edit
    /// C# files), and <see cref="CheckFirstOption"/> (it likely changes nothing).
    /// </summary>
    public static string[] NextRun(string[] args)
    {
        var next = NoRestore(args);
        return next.Contains(CheckFirstOption) ? next : next.Append(CheckFirstOption).ToArray();
    }

    /// <summary>
    /// What to say when the restore failed: the first NuGet error in the output when there is one (NU1008: a version on a
    /// PackageReference under central package management), else <see cref="RestoreHint"/>.
    /// </summary>
    public static string RestoreHintFor(IEnumerable<string> output)
    {
        var error = output.Select(l => Regex.Match(l, @"\berror (?<code>NU\d{4}): (?<message>.*?)(?: For more information|\s*\[[^\]]*\]\s*$|$)")).FirstOrDefault(m => m.Success);
        if (error is null)
        {
            return RestoreHint;
        }

        // NU1102 for StyleBro itself: a local feed that isn't a package source yet, or one packageSourceMapping doesn't map.
        var feed = error.Groups["code"].Value == "NU1102" && error.Groups["message"].Value.Contains("StyleBro.", StringComparison.OrdinalIgnoreCase)
            ? " NuGet searched no source that has it: add the folder or feed with the StyleBro packages to nuget.config's <packageSources> (<add key=\"stylebro\" value=\"path/to/feed\" />), and when nuget.config has a <packageSourceMapping>, map StyleBro to it too (<packageSource key=\"stylebro\"><package pattern=\"StyleBro.*\" /></packageSource>): https://bisforboman.github.io/stylebro/getting-started/."
            : string.Empty;
        return $"The restore failed with {error.Groups["code"].Value}: {error.Groups["message"].Value.Trim().TrimEnd('.')}.{feed} Fix that (https://learn.microsoft.com/nuget/reference/errors-and-warnings/{error.Groups["code"].Value.ToLowerInvariant()}), then run again. Once the packages are restored, '--no-restore' skips the restore.";
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
    /// The old-style (non-SDK) projects under the root (relative, '/'-separated): no 'Sdk' on the Project element, no Sdk
    /// import or element. 'dotnet restore' can't restore their packages.config and rewrites their PackageReference restore
    /// output in a way Visual Studio's MSBuild then rejects.
    /// </summary>
    public static List<string> OldStyleProjects(string root)
    {
        return StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && IsOldStyle(f))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Whether a project file is old-style (non-SDK): no 'Sdk' on the Project element, no Sdk import or element.</summary>
    public static bool IsOldStyle(string project) =>
        File.Exists(project) && !Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(project)), @"<Project\b[^>]*\bSdk\s*=|<Import\b[^>]*\bSdk\s*=|<Sdk\b", RegexOptions.IgnoreCase);

    /// <summary>
    /// Whether a project restores through packages.config, which 'dotnet restore' can't do: an old-style project with one
    /// next to it. NuGet ignores packages.config in an SDK-style project (Mapster.EFCore has a stray one).
    /// </summary>
    public static bool UsesPackagesConfig(string project) =>
        File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(project))!, "packages.config")) && IsOldStyle(project);

    /// <summary>
    /// What 'format' says about the old-style projects (relative paths) it doesn't restore: the SDK-style ones are restored,
    /// and the MSBuild restore those need (the packages.config one only when one of them has packages.config).
    /// </summary>
    public static string OldStyleNote(IReadOnlyList<string> projects, bool packagesConfig) =>
        $"{OldStyleHeader} {string.Join(", ", projects.Take(3))}{(projects.Count > 3 ? $" and {projects.Count - 3} more" : string.Empty)} (not SDK-style): "
        + $"restoring the SDK-style projects only ({(packagesConfig ? "'dotnet restore' can't restore packages.config, and it" : "'dotnet restore'")} writes restore output Visual Studio's MSBuild rejects for old-style projects). "
        + (packagesConfig ? OldStyleRestore : OldStylePackageReferenceRestore);

    /// <summary>
    /// Whether StyleBro's analyzers load in a project: StyleBro.Analyzers in its restore output (<paramref name="assetsFile"/>,
    /// project.assets.json: a PackageReference, also from Directory.Build.props or a GlobalPackageReference), or an Analyzer
    /// item for its DLL that exists (packages.config) or a reference to its project. Without them 'dotnet format' fixes only
    /// whitespace and the built-in rules and says nothing (actions/runner: "clean", then ~3,000 BRO errors in the build).
    /// </summary>
    public static bool LoadsStyleBro(string project, string assetsFile)
    {
        if (assetsFile.Length > 0 && File.Exists(assetsFile) && File.ReadAllText(assetsFile).Contains("\"StyleBro.Analyzers/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var text = File.Exists(project) ? StyleCopSetup.WithoutComments(File.ReadAllText(project)) : string.Empty;
        return text.Contains("StyleBro.Analyzers.csproj", StringComparison.OrdinalIgnoreCase)
            || Regex.Matches(text, @"<Analyzer\b[^>]*\bInclude\s*=\s*""(?<dll>[^""]*StyleBro\.Analyzers\.dll)""", RegexOptions.IgnoreCase)
                .Any(m => File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(project))!, m.Groups["dll"].Value.Replace('\\', Path.DirectorySeparatorChar))));
    }

    /// <summary>
    /// What to say when StyleBro's analyzers don't load in the projects a run formats (<see cref="LoadsStyleBro"/>, relative
    /// paths): null when they load everywhere; when they load nowhere the run stops (<paramref name="all"/>), else a warning.
    /// </summary>
    public static string? NotLoadedMessage(IReadOnlyList<string> without, bool all)
    {
        if (without.Count == 0)
        {
            return null;
        }

        if (all)
        {
            return $"{NotLoadedHeader} no project has StyleBro.Analyzers in its restore output (obj/project.assets.json), so 'dotnet format' would fix only whitespace and the built-in rules and report clean. "
                + "Add the StyleBro.Analyzers package (https://bisforboman.github.io/stylebro/getting-started/), restore (without --no-restore, 'format' restores), then run again.";
        }

        var names = string.Join(", ", without.Take(5)) + (without.Count > 5 ? $" and {without.Count - 5} more" : string.Empty);
        return $"{PartlyLoadedHeader} {names} {(without.Count == 1 ? "has" : "have")} no StyleBro.Analyzers in {(without.Count == 1 ? "its" : "their")} restore output, so StyleBro's rules don't run there. Add the package there, or restore {(without.Count == 1 ? "it" : "them")}, then run again.";
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
            .SelectMany(p => p.Value.Select(f => (Framework: NormalizeFramework(f), Project: p.Key)))
            .Distinct()
            .GroupBy(x => x.Framework, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.Select(x => x.Project).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();
    }

    /// <summary>
    /// A target framework in its usual spelling, so the spellings of one framework share a run: NuGet reads the digits of a
    /// short name without dots as one version part each ('net6' and 'net60' are 'net6.0', 'netcoreapp31' is
    /// 'netcoreapp3.1'), and a 'net' version from 5 on is .NET, written with a dot ('net48' stays .NET Framework 4.8).
    /// </summary>
    public static string NormalizeFramework(string framework)
    {
        var match = Regex.Match(framework.Trim().ToLowerInvariant(), @"^(?<name>net|netcoreapp|netstandard)(?<digits>\d+)(?<platform>-.*)?$");
        if (!match.Success || (match.Groups["name"].Value == "net" && match.Groups["digits"].Value[0] < '5'))
        {
            return framework.Trim().ToLowerInvariant();
        }

        var digits = match.Groups["digits"].Value;
        var version = digits.Length == 1 ? digits + ".0" : string.Join(".", digits.Select(d => d.ToString()));
        return match.Groups["name"].Value + version + match.Groups["platform"].Value;
    }

    /// <summary>
    /// The projects a run loads for its framework (by full path): those that target it, and those that reference one of
    /// them, directly or not, so the reference resolves (it only offers the run's framework then). The rest load as usual.
    /// </summary>
    public static HashSet<string> Keep(IReadOnlyDictionary<string, (string[] Frameworks, string[] References)> projects, string framework)
    {
        var keep = new HashSet<string>(
            projects.Where(p => p.Value.Frameworks.Any(f => NormalizeFramework(f) == NormalizeFramework(framework))).Select(p => p.Key),
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
    /// The projects of <paramref name="keep"/>'s run that don't target its framework but reference a project it forces onto
    /// it. Their references ask for the referenced project's own frameworks (StyleBroFormatLoose clears the forced one, see
    /// <see cref="SelectFrameworkTargets"/>): Autofac's ScannedAssembly (netstandard2.1) references Autofac, which offered
    /// only net10.0 then ("It cannot be referenced by a project that targets .NETStandard,Version=v2.1"), and the test
    /// projects that reference ScannedAssembly didn't load in the net10.0 run.
    /// </summary>
    public static HashSet<string> Other(IReadOnlyDictionary<string, (string[] Frameworks, string[] References)> projects, IReadOnlySet<string> keep, string framework) =>
        new(
            projects.Where(p => !p.Value.Frameworks.Any(f => NormalizeFramework(f) == NormalizeFramework(framework)) && p.Value.References.Any(keep.Contains)).Select(p => p.Key),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A run's projects split into groups that run one after another, so no file is compiled by two projects one
    /// 'dotnet format' run loads (a group's projects and what they reference, transitively). kubernetes-client: KubernetesClient
    /// and KubernetesClient.Aot compile the same files with other DefineConstants; loaded together, Roslyn's linked-file
    /// merge crashed on their different edits (dotnet/roslyn#86001) after writing part of the files. First fit, in the
    /// given order; one group when nothing is shared.
    /// </summary>
    public static List<List<string>> Separate(IEnumerable<string> projects, Func<string, IEnumerable<string>> references, Func<string, IEnumerable<string>> compile)
    {
        HashSet<string> Loaded(string project)
        {
            var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var next = new Stack<string>(new[] { project });
            while (next.Count > 0)
            {
                var current = next.Pop();
                if (loaded.Add(current))
                {
                    foreach (var reference in references(current))
                    {
                        next.Push(reference);
                    }
                }
            }

            return loaded;
        }

        var groups = new List<(List<string> Projects, HashSet<string> Loaded, Dictionary<string, string> Owner)>();
        foreach (var project in projects)
        {
            var loaded = Loaded(project);
            var group = groups.FirstOrDefault(g => loaded.Where(p => !g.Loaded.Contains(p))
                .All(p => compile(p).All(f => !g.Owner.TryGetValue(f, out var owner) || owner.Equals(p, StringComparison.OrdinalIgnoreCase))));
            if (group.Projects is null)
            {
                group = (new List<string>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                groups.Add(group);
            }

            group.Projects.Add(project);
            foreach (var p in loaded.Where(group.Loaded.Add))
            {
                foreach (var file in compile(p))
                {
                    group.Owner.TryAdd(file, p);
                }
            }
        }

        return groups.Select(g => g.Projects).ToList();
    }

    /// <summary>
    /// The folders of a run's projects, relative to <paramref name="root"/>, for 'dotnet format --include'. Referenced
    /// projects that load as usual (multi-targeted) would get their files formatted too, merging the frameworks' copies
    /// again (Newtonsoft.Json: conflict markers in the library during the tests' net6.0 run). Only the run's own projects
    /// are edited; the others get their own runs.
    /// </summary>
    public static IEnumerable<string> Include(string root, string solutionDirectory, IEnumerable<string> projects, IEnumerable<string>? linked = null)
    {
        // A folder needs the trailing '/': 'src/Lib' matches nothing (silently), 'src/Lib/' its files.
        var folders = projects
            .Select(p => Path.GetRelativePath(root, Path.GetDirectoryName(Path.GetFullPath(Path.Combine(solutionDirectory, p)))!))
            .Select(folder => folder == "." ? "**/*.cs" : folder.Replace(Path.DirectorySeparatorChar, '/') + "/");

        // Files the projects compile from elsewhere (Orleans: src/AdoNet/Shared/, src/Orleans.CodeGenerator.Shared/) weren't
        // formatted at all. Their folder when no project lives there, else just the file (that project's own files would be
        // formatted in this run too, merging its frameworks' copies). Paths are relative to the working folder, the root:
        // 'dotnet format --include' matches nothing above it (checked: '../Shared/S.cs' and absolute paths match nothing).
        var outside = (linked ?? [])
            .Select(f => (File: f, Relative: Path.GetRelativePath(root, f)))
            .Where(f => !f.Relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(f.Relative))
            .Select(f => Path.GetDirectoryName(f.Relative) is { Length: > 0 } folder && Directory.Exists(Path.GetDirectoryName(f.File)) && !Directory.EnumerateFiles(Path.GetDirectoryName(f.File)!, "*.csproj").Any()
                ? folder.Replace(Path.DirectorySeparatorChar, '/') + "/"
                : f.Relative.Replace(Path.DirectorySeparatorChar, '/'));
        return folders.Concat(outside).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Whether a workspace is a solution filter (.slnf).</summary>
    public static bool IsSolutionFilter(string path) => path.EndsWith(".slnf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A solution filter's solution (full path; 'solution.path' is relative to the filter) and its projects (as listed:
    /// relative to the solution).
    /// </summary>
    public static (string? Solution, string[] Projects) ReadSolutionFilter(string path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var solution = json.RootElement.GetProperty("solution");
        var file = solution.GetProperty("path").GetString()!.Replace('\\', Path.DirectorySeparatorChar);
        var projects = solution.TryGetProperty("projects", out var list) ? list.EnumerateArray().Select(p => p.GetString()!).ToArray() : Array.Empty<string>();
        return (Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, file)), projects);
    }

    /// <summary>What's wrong with a solution filter (malformed JSON, no 'solution.path'), or null.</summary>
    public static string? SolutionFilterError(string path)
    {
        try
        {
            ReadSolutionFilter(path);
            return null;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException or NullReferenceException)
        {
            return $"{Path.GetFileName(path)} isn't a valid solution filter ({e.Message.Split('\n')[0].Trim()}). A .slnf is JSON like {{ \"solution\": {{ \"path\": \"App.sln\", \"projects\": [ \"src/App/App.csproj\" ] }} }}.";
        }
    }

    /// <summary>A solution filter (.slnf) next to the solution, with the given projects (paths as the solution lists them).</summary>
    public static string SolutionFilter(string solutionFileName, IEnumerable<string> projects)
    {
        return JsonSerializer.Serialize(
            new { solution = new { path = solutionFileName, projects = projects.ToArray() } },
            new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// A project's target frameworks ('TargetFrameworks' split, else 'TargetFramework'), project references (full paths) and,
    /// with <paramref name="projectDirectory"/>, the C# files it compiles from outside its folder (full paths): Orleans links
    /// '..\Orleans.CodeGenerator.Shared\*.cs' into three projects. Also its restore output (project.assets.json, "" when not asked).
    /// </summary>
    public static (string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile) ParseProject(string msbuildJson, string? projectDirectory = null)
    {
        using var json = JsonDocument.Parse(msbuildJson);
        var hasProperties = json.RootElement.TryGetProperty("Properties", out var properties);
        var several = hasProperties && properties.TryGetProperty("TargetFrameworks", out var s) ? s.GetString() : null;
        var one = hasProperties && properties.TryGetProperty("TargetFramework", out var o) ? o.GetString() : null;
        var frameworks = (string.IsNullOrWhiteSpace(several) ? one ?? string.Empty : several)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] FullPaths(string item) => json.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty(item, out var found)
            ? found.EnumerateArray().Select(r => r.GetProperty("FullPath").GetString()!).ToArray()
            : Array.Empty<string>();
        var folder = projectDirectory is null ? null : Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory)) + Path.DirectorySeparatorChar;
        var linked = folder is null ? Array.Empty<string>() : FullPaths("Compile").Where(f => !Path.GetFullPath(f).StartsWith(folder, StringComparison.OrdinalIgnoreCase)).ToArray();

        // ProjectAssetsFile is set by the restore's own props: before the first restore only the folder is known.
        var assets = hasProperties && properties.TryGetProperty("ProjectAssetsFile", out var a) && !string.IsNullOrEmpty(a.GetString()) ? a.GetString()!
            : hasProperties && properties.TryGetProperty("MSBuildProjectExtensionsPath", out var e) && !string.IsNullOrEmpty(e.GetString()) ? Path.Combine(e.GetString()!, "project.assets.json")
            : string.Empty;
        return (frameworks, FullPaths("ProjectReference"), linked, FullPaths("Compile"), assets);
    }

    /// <summary>
    /// The TargetFramework a run sets (the spelling most of its projects use, <see cref="NormalizeFramework"/>), and the kept
    /// projects that spell it another way, as '|PATH=spelling|' (upper-case paths) for <see cref="SelectFrameworkTargets"/>:
    /// a project whose TargetFramework isn't one of its own spellings has no restore output for it (SmartEnum's EFCore
    /// tests, 'net6.0', in the run for the library's 'net6': the workspace didn't load).
    /// </summary>
    public static (string Alias, string Aliases) Aliases(IReadOnlyDictionary<string, (string[] Frameworks, string[] References)> projects, IEnumerable<string> keep, string framework)
    {
        var own = keep.Where(projects.ContainsKey)
            .Select(p => (Path: p, Alias: projects[p].Frameworks.FirstOrDefault(f => NormalizeFramework(f) == framework)))
            .Where(p => p.Alias is not null)
            .ToList();
        var alias = own.GroupBy(p => p.Alias!, StringComparer.Ordinal).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => g.Key).FirstOrDefault() ?? framework;
        var others = own.Where(p => p.Alias != alias).Select(p => $"{p.Path.ToUpperInvariant()}={p.Alias}|").ToList();
        return (alias, others.Count == 0 ? string.Empty : "|" + string.Concat(others));
    }

    /// <summary>
    /// Writes the names the renaming fixes check outside the projects a run loads (<see cref="RepositoryNames"/>), for every
    /// C# file under the root.
    /// </summary>
    public static void WriteRepositoryNames(string root, string file)
    {
        // ponytail: parsed without preprocessor symbols, so names only inside '#if' code aren't seen.
        var lines = StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .AsOrdered()
            .SelectMany(f => RepositoryNames.Lines(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f).GetRoot(), f).ToList());
        File.WriteAllLines(file, lines);
    }

    /// <summary>Whether a file is a Traversal project (Microsoft.Build.Traversal: a .proj that only lists projects to build).</summary>
    public static bool IsTraversal(string path) =>
        path.EndsWith(".proj", StringComparison.OrdinalIgnoreCase) && File.Exists(path) && File.ReadAllText(path).Contains("Microsoft.Build.Traversal", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The C# projects a Traversal project lists (full paths: its ProjectReference items as MSBuild evaluates them, so globs
    /// and conditions count), nested Traversal projects expanded.
    /// </summary>
    public static List<string> TraversalProjects(string path)
    {
        var references = ParseProject(Capture(Path.GetDirectoryName(path)!, "msbuild", path, "-getItem:ProjectReference", "-nologo")).References;
        return references.SelectMany(r => IsTraversal(r) ? TraversalProjects(r) : r.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ? new List<string> { r } : new List<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>A solution (.slnx) with the given projects (full paths), written relative to <paramref name="folder"/>.</summary>
    public static string SolutionText(string folder, IEnumerable<string> projects) =>
        "<Solution>\n" + string.Concat(projects.Select(p => $"  <Project Path=\"{System.Security.SecurityElement.Escape(Path.GetRelativePath(folder, p).Replace('\\', '/'))}\" />\n")) + "</Solution>\n";

    /// <summary>Writes a temporary solution next to a Traversal project, with the projects it lists; returns its path (the caller deletes it).</summary>
    public static string TraversalSolution(string path)
    {
        var folder = Path.GetDirectoryName(path)!;
        var solution = Path.Combine(folder, $".stylebro-{Path.GetFileNameWithoutExtension(path)}.slnx");
        File.WriteAllText(solution, SolutionText(folder, TraversalProjects(path)));
        return solution;
    }

    /// <summary>Each project of a solution (or the project itself) with its evaluated target frameworks, for init's count.</summary>
    public static IEnumerable<(string FullPath, string[] Frameworks)> EvaluatedProjects(string workspacePath) =>
        ReadProjects(workspacePath).Values.Select(p => (p.FullPath, p.Frameworks));

    /// <summary>
    /// --verify-no-changes: 'dotnet format --verify-no-changes' fails on every finding it would hand to a fix, also those a
    /// StyleBro fix leaves on purpose, so a loop "until clean" never ends. When it fails, this formats a temporary copy of
    /// the repository (the fixes only run when files are written) and decides by the copy: no file changed means only
    /// findings no fix changes are left, listed with the reasons the fixes recorded; exit 0. A changed file means work is
    /// left: 'dotnet format''s output and exit code. The repository itself is never touched.
    /// </summary>
    internal static int Verify(string[] args, string root, Action<string> log, Func<string[], Action<string>?, int> runOnce)
    {
        var lines = new List<string>();
        var code = runOnce(args, lines.Add);
        if (code == 0)
        {
            lines.ForEach(log);

            // A project no run loaded wasn't checked at all: not "Clean." (Autofac's tests). One skipped for some frameworks
            // only ("Partly incomplete") was checked in the others' runs, all but code only those frameworks compile; one that
            // references an F# project ("Likely cause") can never load, which a restore doesn't change: both stay warnings.
            if (lines.Any(l => l.StartsWith(IncompleteMarker, StringComparison.Ordinal)) && !lines.Any(l => l.StartsWith(LikelyCause, StringComparison.Ordinal)))
            {
                log(NotVerified);
                return 1;
            }

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

            var working = copyArgs.IndexOf(WorkingFolderOption);
            if (working >= 0 && working + 1 < copyArgs.Count)
            {
                copyArgs[working + 1] = Path.GetFullPath(Path.Combine(copy, Path.GetRelativePath(source, Path.GetFullPath(copyArgs[working + 1]))));
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

            // --files: the repository's names, written again for the copy's paths (the files a run loads are told apart by path).
            var namesFile = copy + ".names.tsv";
            var namesInCopy = string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RepositoryNames.Variable)) ? null : new KeptVariable(namesFile, RepositoryNames.Variable);
            if (namesInCopy is not null)
            {
                WriteRepositoryNames(copy, namesFile);
            }

            using (namesInCopy)
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

    /// <summary>
    /// '--files': the usual format (repeat until clean, kept findings, or --verify-no-changes) per workspace, only on the
    /// given files ('dotnet format --include'), with just the project(s) they belong to loaded: seconds instead of minutes.
    /// </summary>
    private static int RunFiles(List<string> files, string[] rest, Action<string> log, Action<string>? output, Func<string[], Action<string>?, int> runOnce, int maxRuns)
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

        // Only the files' projects load: a string or name elsewhere in the repository (a test project reading a field with
        // GetField) counts as in a per-framework run. Without it, --files renamed fields the whole-repository run kept.
        var names = Path.Combine(Path.GetTempPath(), $"stylebro-names-{Guid.NewGuid():N}.tsv");
        var first = Path.GetDirectoryName(Path.GetFullPath(groups[0].Workspace))!;
        WriteRepositoryNames(RepositoryRoot(first) ?? first, names);
        using var namesVariable = new KeptVariable(names, RepositoryNames.Variable);
        var exit = 0;
        foreach (var (workspace, root, include) in groups)
        {
            var own = Directory.Exists(workspace) ? workspace : Path.GetDirectoryName(workspace)!;
            var arguments = new[] { workspace }.Concat(rest.Skip(hasPath ? 1 : 0))
                .Concat(own.Equals(root, StringComparison.OrdinalIgnoreCase) ? Array.Empty<string>() : new[] { WorkingFolderOption, root })
                .Append("--include")
                .Concat(include)
                .ToArray();
            var code = arguments.Contains("--verify-no-changes") ? Verify(arguments, root, log, runOnce) : Repeat(arguments, root, log, output, runOnce, maxRuns);
            exit = exit == 0 ? code : exit;
        }

        JsonReport.Set("clean", exit == 0);
        return exit;
    }

    /// <summary>
    /// Runs 'dotnet format' until a run changes no file (at most <paramref name="maxRuns"/>), then lists the kept findings.
    /// One run (--once) lists them after it, whether it changed files or not, and exits 0 when it succeeded.
    /// </summary>
    private static int Repeat(string[] args, string root, Action<string> log, Action<string>? output, Func<string[], Action<string>?, int> runOnce, int maxRuns = MaxRuns)
    {
        var keptFile = Path.Combine(Path.GetTempPath(), $"stylebro-kept-{Guid.NewGuid():N}.tsv");
        using var kept = new KeptVariable(keptFile);
        for (var run = 1; ; run++)
        {
            File.Delete(keptFile);
            var before = Directory.Exists(root) ? Snapshot(root) : new Dictionary<string, string>();

            // --json: each run's changes per rule come from its own report.
            var report = JsonReport.Current is null ? null : Path.Combine(Path.GetTempPath(), $"stylebro-format-json-{Guid.NewGuid():N}");
            var runArgs = report is null ? args : args.Concat(new[] { "--report", report }).ToArray();

            // What it fixes and the framework list are said once, not every run.
            var code = runOnce(runArgs, run == 1 ? output : line =>
            {
                if (!new[] { FixingHeader, MultiTargetedHeader, SharedFilesHeader, TraversalHeader, "Using ", NoWhitespaceHeader, OldMacHeader, UnformattedHeader, OldStyleHeader, PartlyLoadedHeader }.Any(h => line.StartsWith(h, StringComparison.Ordinal)))
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
                // A crash can come after 'dotnet format' wrote part of its changes (kubernetes-client: 242 files).
                if (changed.Count > 0)
                {
                    log($"Run {run} failed after changing {changed.Count} file{(changed.Count == 1 ? string.Empty : "s")}:");
                    ListFiles(root, changed, int.MaxValue).ForEach(log);
                }

                return code;
            }

            log($"Run {run}: {changed.Count} file{(changed.Count == 1 ? string.Empty : "s")} changed{(changed.Count == 0 ? ", clean." : ":")}");
            ListFiles(root, changed, 10).ForEach(log);
            if (changed.Count == 0 || maxRuns == 1)
            {
                var keptFindings = ReadKept(keptFile, root, root);
                JsonReport.AddKept(keptFindings);
                KeptSummary(keptFindings, root, clean: changed.Count == 0).ForEach(log);
                return 0;
            }

            if (run == maxRuns)
            {
                // The kept findings too: with TreatWarningsAsErrors they fail the build whether or not the rest settles.
                var keptFindings = ReadKept(keptFile, root, root);
                JsonReport.AddKept(keptFindings);
                KeptSummary(keptFindings, root, clean: false).ForEach(log);
                log($"Not clean: still changing after {MaxRuns} runs. Run 'stylebro-migrate format' again, and if it keeps changing the same lines, please report it.");
                return NotCleanExitCode;
            }

            args = NextRun(args);
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
        var unsupported = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = string.Empty;
        var restoreFailed = false;
        var crashed = false;
        var nuGetErrors = new List<string>();
        void Record(string line)
        {
            restoreFailed |= IsRestoreFailure(line);
            crashed |= IsCrash(line);
            if (line.Contains("error NU", StringComparison.Ordinal))
            {
                nuGetErrors.Add(line);
            }

            if (SkippedProject(line) is { } project)
            {
                (skipped.TryGetValue(project, out var set) ? set : skipped[project] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)).Add(current);
            }

            if (UnsupportedProject(line) is { } other)
            {
                unsupported.Add(other);
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

        var workspacePath = Path.GetFullPath(Path.Combine(root, workspace));
        if (IsSolutionFilter(workspacePath) && SolutionFilterError(workspacePath) is { } invalid)
        {
            error(invalid);
            return 1;
        }

        if (!File.Exists(path) && BaselineCommand.PickedNote(root, workspace) is { } picked)
        {
            log(picked);
        }

        // 'dotnet format' can't load a Traversal project ("Format currently supports only C# and Visual Basic projects"): a
        // temporary solution with the projects it lists, next to it (the per-framework filters name it relative to there).
        if (IsTraversal(workspacePath))
        {
            var solution = TraversalSolution(workspacePath);
            try
            {
                log($"{TraversalHeader} {Path.GetFileName(workspacePath)} is a Traversal project: formatting the {ReadProjects(solution).Count} C# projects it lists.");
                return RunOnce(new[] { solution }.Concat(args.Skip(hasPath ? 1 : 0)).ToArray(), output);
            }
            finally
            {
                File.Delete(solution);
            }
        }

        var workingAt = passThrough.IndexOf(WorkingFolderOption);
        if (workingAt >= 0 && workingAt + 1 < passThrough.Count)
        {
            root = Path.GetFullPath(passThrough[workingAt + 1]);
            passThrough.RemoveRange(workingAt, 2);
        }

        // By default only StyleBro's rules and the built-in ones init/migrate turn on: plain 'dotnet format' also applies
        // every other analyzer's fixes and compiler fixes (CS8618's 'required', a Sonar fix removing 'init;': build
        // errors in Kavita). Whitespace formatting still runs. --all keeps them.
        if (!passThrough.Remove("--all") && !passThrough.Contains("--diagnostics"))
        {
            var ids = Diagnostics(root);
            log(RulesLine(passThrough.Contains("--verify-no-changes"), ids.Count));
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
        var excluded = new List<string>();
        if (StyleCopSetup.NestedRepositories(root) is { Count: > 0 } submodules)
        {
            Exclude(submodules);
            excluded.AddRange(submodules);
        }

        // Folders the migration left without settings (no StyleCop there): the whitespace pass would still reformat them.
        if (Migration.UnformattedFolders(root) is { Count: > 0 } unformatted)
        {
            log($"{UnformattedHeader} {string.Join(", ", unformatted)}: StyleCop didn't run there, so the migration set nothing up there (the stylebro-migrate block in .editorconfig names them).");
            Exclude(unformatted);
            excluded.AddRange(unformatted);
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
                    break; // an error: the next pass would only repeat it
                }
            }

            if (report is not null)
            {
                WriteMergedReport(Path.GetFullPath(report), folders);
            }

            return exit;
        }

        // A crash leaves part of the files written: say which run it was (Repeat lists the files it changed).
        void Crashed(IEnumerable<string> runProjects)
        {
            if (crashed)
            {
                log(CrashMessage(runProjects.Select(Path.GetFileNameWithoutExtension).OfType<string>()));
                crashed = false;
            }
        }

        int Done(int code)
        {
            if (restoreFailed)
            {
                log(RestoreHintFor(nuGetErrors));
            }

            return code;
        }

        var noRestore = passThrough.Remove("--no-restore");
        var checkFirst = passThrough.Remove(CheckFirstOption);

        // A solution filter's per-framework filters and restore filter name its solution.
        var solutionPath = IsSolutionFilter(workspacePath) ? ReadSolutionFilter(workspacePath).Solution! : workspacePath;
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var isSolution = !workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        var projects = ReadProjects(workspacePath);
        if (projects.Count == 0)
        {
            error($"No C# project in {Path.GetFileName(workspacePath)}, so there's nothing to format. Name a solution or project with C# projects.");
            return 1;
        }

        // One restore before the runs, which then all get --no-restore. Old-style projects are left out: 'dotnet restore'
        // rewrote their restore output (xbox-live-developer-tools: the next Visual Studio build failed, "doesn't list 'win'
        // as a RuntimeIdentifier") and can't restore packages.config at all. Skipping the restore for the whole repository
        // instead left the SDK-style projects without the just-added StyleBro.Analyzers (actions/runner: "clean").
        if (!noRestore)
        {
            var oldStyle = projects.Values.Select(p => p.FullPath).Where(IsOldStyle).ToList();
            var restoreTarget = workspacePath;
            string? restoreFilter = null;
            if (oldStyle.Count > 0)
            {
                log(OldStyleNote(oldStyle.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).Order(StringComparer.OrdinalIgnoreCase).ToList(), oldStyle.Any(UsesPackagesConfig)));
                var sdkStyle = projects.Where(p => !oldStyle.Contains(p.Value.FullPath)).Select(p => p.Key).ToList();
                restoreTarget = sdkStyle.Count == 0 ? null : isSolution ? restoreFilter = Path.Combine(solutionDirectory, $".stylebro-restore-{Guid.NewGuid():N}.slnf") : workspacePath;
                if (restoreFilter is not null)
                {
                    File.WriteAllText(restoreFilter, SolutionFilter(Path.GetFileName(solutionPath), sdkStyle));
                }
            }

            // The restore's own output (its "Build succeeded" block) only matters when it fails.
            var restoreOutput = new List<string>();
            try
            {
                var restore = restoreTarget is null ? 0 : Dotnet(root, null, new[] { "restore", restoreTarget }.Concat(RestoreOptions), restoreOutput.Add);
                if (restore != 0)
                {
                    restoreOutput.ForEach(Watch);
                    restoreFailed = true;
                    return Done(restore);
                }
            }
            finally
            {
                if (restoreFilter is not null)
                {
                    File.Delete(restoreFilter);
                }
            }
        }

        passThrough.Insert(0, "--no-restore");

        // Without StyleBro's analyzers 'dotnet format' fixes whitespace and the built-in rules and says clean.
        var checkedProjects = projects.Values
            .Select(p => (p.FullPath, p.AssetsFile, Relative: Path.GetRelativePath(root, p.FullPath).Replace('\\', '/')))
            .Where(p => !excluded.Any(e => p.Relative.StartsWith(e.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var without = checkedProjects.Where(p => !LoadsStyleBro(p.FullPath, p.AssetsFile)).Select(p => p.Relative).Order(StringComparer.OrdinalIgnoreCase).ToList();
        var none = without.Count > 0 && without.Count == checkedProjects.Count;
        if (NotLoadedMessage(without, none) is { } notLoaded)
        {
            if (none)
            {
                error(notLoaded);
                return 1;
            }

            log(notLoaded);
        }

        var frameworkPlan = Plan(projects.ToDictionary(p => p.Key, p => p.Value.Frameworks));
        var byFullPath = projects.ToDictionary(p => p.Value.FullPath, p => p.Key, StringComparer.OrdinalIgnoreCase);
        List<List<string>> Apart(List<string> selected) => Separate(
                selected.Select(p => projects[p].FullPath),
                r => byFullPath.TryGetValue(r, out var key) ? projects[key].References : Array.Empty<string>(),
                c => byFullPath.TryGetValue(c, out var key) ? projects[key].Compile : Array.Empty<string>())
            .Select(g => g.Select(f => byFullPath[f]).ToList())
            .ToList();

        // "" for a run without a framework of its own (no project is multi-targeted).
        var plan = (frameworkPlan.Count <= 1 ? new List<(string Framework, List<string> Projects)> { (string.Empty, projects.Keys.ToList()) } : frameworkPlan)
            .SelectMany(p => Apart(p.Projects) is var parts && parts.Count > 1
                ? parts.Select((part, i) => (p.Framework, Projects: part, Label: $"{(p.Framework.Length == 0 ? string.Empty : p.Framework + ", ")}part {i + 1} of {parts.Count}"))
                : new[] { (p.Framework, p.Projects, Label: p.Framework) })
            .ToList();
        if (plan.Count <= 1)
        {
            var single = Format(workspacePath, passThrough, a => Dotnet(root, null, a, Watch));
            Incomplete(skipped, new Dictionary<string, List<string>>(), unsupported).ForEach(log);
            Crashed(projects.Keys);
            return Done(single);
        }

        if (frameworkPlan.Count > 1)
        {
            log($"{MultiTargetedHeader}one 'dotnet format' run per target framework ({string.Join(", ", frameworkPlan.Select(p => p.Framework))}).");
        }

        if (SharingProjects(projects.Values.Select(p => (p.FullPath, p.Compile))) is { Count: > 0 } sharing)
        {
            log($"{SharedFilesHeader} {string.Join(", ", sharing)} compile the same files, so they're formatted in separate runs (loaded together, 'dotnet format' crashes: {LinkedMergeIssue}).");
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

        var select = Path.Combine(Path.GetTempPath(), $"stylebro-format-{Guid.NewGuid():N}.targets");
        File.WriteAllText(select, SelectFrameworkTargets);

        // A run loads only the projects that target its framework: the renaming fixes check the names of all the C# files.
        // --files names the whole repository's already (the root here is the files' project).
        var givenNames = Environment.GetEnvironmentVariable(RepositoryNames.Variable);
        var names = string.IsNullOrEmpty(givenNames) ? Path.Combine(Path.GetTempPath(), $"stylebro-names-{Guid.NewGuid():N}.tsv") : givenNames!;
        if (string.IsNullOrEmpty(givenNames))
        {
            WriteRepositoryNames(root, names);
        }

        // Each run's own report folder, in the plan's order (the runs may run at once).
        reportFolders.AddRange(plan.Select(_ => Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}")).Where(_ => report is not null));
        int RunFramework(int index, bool verify, Action<string> output)
        {
            var (framework, selected, _) = plan[index];
            var filter = isSolution ? Path.Combine(solutionDirectory, $".stylebro-format-{index}.slnf") : null;
            try
            {
                if (filter is not null)
                {
                    File.WriteAllText(filter, SolutionFilter(Path.GetFileName(solutionPath), selected));
                }

                var options = passThrough.ToList();
                if (report is not null)
                {
                    options[reportIndex + 1] = reportFolders[index];
                }

                if (verify && !options.Contains("--verify-no-changes"))
                {
                    options.Add("--verify-no-changes");
                }

                if (!passThrough.Contains("--include"))
                {
                    options.Add("--include");
                    options.AddRange(Include(root, solutionDirectory, selected, selected.SelectMany(p => projects[p].Linked)));
                }

                // A run without a framework of its own still gets the repository's names (it loads only some projects).
                var byPath = projects.Values.ToDictionary(p => p.FullPath, p => (p.Frameworks, p.References), StringComparer.OrdinalIgnoreCase);
                var keep = framework.Length == 0 ? new HashSet<string>() : Keep(byPath, framework);
                var (alias, aliases) = framework.Length == 0 ? (string.Empty, string.Empty) : Aliases(byPath, keep, framework);
                var other = framework.Length == 0 ? new HashSet<string>() : Other(byPath, keep, framework);
                var environment = new RunEnvironment(alias, select, PathList(keep), PathList(other), aliases, names);
                return Format(filter ?? workspacePath, options, a => Dotnet(root, environment, a, output));
            }
            finally
            {
                if (filter is not null)
                {
                    File.Delete(filter);
                }
            }
        }

        var exit = 0;
        void RunOrReplay(int index, (int Code, List<string> Lines)? ran)
        {
            log($"== {(plan[index].Label.Length == 0 ? "all projects" : plan[index].Label)} ({plan[index].Projects.Count} project(s))");
            current = plan[index].Framework;
            ran?.Lines.ForEach(Show);
            var code = ran?.Code ?? RunFramework(index, verify: false, Show);
            Crashed(plan[index].Projects);
            exit = exit == 0 ? code : exit;
        }

        try
        {
            // Runs that write nothing can run at once (one 'dotnet format' per framework), their output shown afterwards in
            // the plan's order: '--verify-no-changes', and the check before a run that likely changes nothing (after a run
            // that changed files). When every framework's check is clean (which '--verify-no-changes' isn't for findings a
            // fix keeps either), that is what the runs one after another would find; otherwise they run as usual.
            var verifying = passThrough.Contains("--verify-no-changes");
            var atOnce = verifying || (checkFirst && report is null)
                ? Enumerable.Range(0, plan.Count).AsParallel().AsOrdered().WithDegreeOfParallelism(plan.Count)
                    .Select(i =>
                    {
                        var lines = new List<string>();
                        return (Code: RunFramework(i, verify: true, lines.Add), Lines: lines);
                    })
                    .ToList()
                : null;
            var replay = atOnce is not null && (verifying || IsCleanCheck(atOnce.Select(c => (c.Code, (IReadOnlyList<string>)c.Lines))));
            for (var i = 0; i < plan.Count; i++)
            {
                RunOrReplay(i, replay ? atOnce![i] : null);
            }
        }
        finally
        {
            File.Delete(select);
            if (string.IsNullOrEmpty(givenNames))
            {
                File.Delete(names);
            }

            if (report is not null)
            {
                WriteMergedReport(Path.GetFullPath(report), reportFolders);
            }
        }

        var runFor = plan.SelectMany(p => p.Projects.Select(project => (Name: Path.GetFileNameWithoutExtension(project), p.Framework)))
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Framework).ToList(), StringComparer.OrdinalIgnoreCase);
        Incomplete(skipped, runFor, unsupported).ForEach(log);
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

    /// <summary>
    /// Each C# project of the solution (keyed by its path as the solution lists it) or the project itself. The projects are
    /// evaluated in parallel (one 'dotnet msbuild' each, ~1 s: LiteBus has 99) and once per process: the runs until clean
    /// and the preview's runs only edit C# files.
    /// </summary>
    private static Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile)> ReadProjects(string workspacePath)
    {
        // ponytail: keyed by the workspace only; a project file changed between two runs of one process isn't seen.
        lock (ProjectCache)
        {
            if (ProjectCache.TryGetValue(workspacePath, out var known))
            {
                return known;
            }
        }

        // A solution filter's projects are relative to its solution ('dotnet sln x.slnf list' fails: "could not be found").
        var filter = IsSolutionFilter(workspacePath) ? ReadSolutionFilter(workspacePath) : default;
        var directory = Path.GetDirectoryName(filter.Solution ?? workspacePath)!;
        var projects = filter.Solution is not null
            ? filter.Projects.Where(l => l.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToArray()
            : workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            ? new[] { Path.GetFileName(workspacePath) }
            : IsTraversal(workspacePath)
            ? TraversalProjects(workspacePath).Select(p => Path.GetRelativePath(directory, p)).ToArray()
            : Capture(directory, "sln", workspacePath, "list").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile) Evaluate(string p)
        {
            var fullPath = Path.GetFullPath(Path.Combine(directory, p.Replace('\\', Path.DirectorySeparatorChar)));
            var (frameworks, references, linked, compile, assets) = ParseProject(Capture(directory, "msbuild", fullPath, "-getProperty:TargetFrameworks", "-getProperty:TargetFramework", "-getProperty:ProjectAssetsFile", "-getProperty:MSBuildProjectExtensionsPath", "-getItem:ProjectReference", "-getItem:Compile", "-nologo"), Path.GetDirectoryName(fullPath));

            // A multi-targeted project's outer evaluation has only its explicit Compile items, not the SDK's default
            // ones (kubernetes-client's KubernetesClient: none, so its files didn't look shared with KubernetesClient.Aot's).
            // ponytail: the first framework's files only; a file only another framework compiles isn't seen as shared.
            if (frameworks.Length > 1)
            {
                compile = compile.Union(ParseProject(Capture(directory, "msbuild", fullPath, $"-property:TargetFramework={frameworks[0]}", "-getItem:Compile", "-nologo")).Compile, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            return (fullPath, frameworks, references, linked, compile, assets);
        }

        Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile)> read;
        try
        {
            read = projects.AsParallel().AsOrdered().Select(p => (Key: p, Value: Evaluate(p))).ToDictionary(p => p.Key, p => p.Value);
        }
        catch (AggregateException e) when (e.InnerException is ToolException failed)
        {
            // MSBuild's message (an invalid NuGet.Config, a missing SDK), not PLINQ's wrapper.
            throw failed;
        }

        lock (ProjectCache)
        {
            ProjectCache[workspacePath] = read;
        }

        return read;
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
        var errors = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // A project MSBuild can't evaluate prints its error instead of JSON (GameDemo: "NuGet.Config is not valid XML"; it
        // was a JsonReaderException with a stack trace before).
        if (process.ExitCode != 0)
        {
            throw new ToolException($"'dotnet {string.Join(" ", arguments.Where(a => a != "-nologo"))}' failed (exit code {process.ExitCode}):{Environment.NewLine}{string.Join(Environment.NewLine, (output.Result + errors).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).Select(l => "  " + l.TrimStart()))}");
        }

        return output.Result;
    }

    /// <summary>Runs dotnet; with <paramref name="output"/>, its output (stdout and stderr) goes there line by line instead of the console.</summary>
    private static int Dotnet(string directory, RunEnvironment? framework, IEnumerable<string> arguments, Action<string>? output = null)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = output is not null, RedirectStandardError = output is not null };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        // See the remarks on the class: the environment, cleared again for projects that should load as usual.
        if (framework is { } f)
        {
            if (f.Name.Length > 0)
            {
                start.Environment["TargetFramework"] = f.Name;
            }

            start.Environment["StyleBroFormatKeep"] = f.Keep;
            start.Environment["StyleBroFormatOther"] = f.Other;
            start.Environment["BeforeMicrosoftNETSdkTargets"] = f.SelectTargets;
            start.Environment["StyleBroFormatAliases"] = f.Aliases;
            start.Environment[RepositoryNames.Variable] = f.Names;
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

    /// <summary>Full paths as the hook matches them: '|A|B|', upper-case; empty for none.</summary>
    private static string PathList(IReadOnlyCollection<string> paths) => paths.Count == 0 ? string.Empty : "|" + string.Join("|", paths.Select(k => k.ToUpperInvariant())) + "|";

    /// <summary>What a 'dotnet format' run for one framework gets in its environment (see the remarks on the class).</summary>
    private sealed record RunEnvironment(string Name, string SelectTargets, string Keep, string Other, string Aliases, string Names);

    /// <summary>
    /// Sets <see cref="KeptFinding.Variable"/> (or another variable that names a file) for the 'dotnet format' runs this
    /// process starts, and restores it.
    /// </summary>
    private sealed class KeptVariable : IDisposable
    {
        private readonly string variable;
        private readonly string? previous;
        private readonly string file;

        public KeptVariable(string file, string variable = KeptFinding.Variable)
        {
            this.file = file;
            this.variable = variable;
            previous = Environment.GetEnvironmentVariable(variable);
            Environment.SetEnvironmentVariable(variable, file);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(variable, previous);
            File.Delete(file);
        }
    }
}

/// <summary>A dotnet command 'format' depends on failed (MSBuild couldn't evaluate a project): its output is the message.</summary>
internal sealed class ToolException(string message) : Exception(message);
