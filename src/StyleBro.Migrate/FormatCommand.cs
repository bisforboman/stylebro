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
/// bro format [folder, solution or project] [--all] [dotnet format options]: 'dotnet format' that fixes
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
internal static partial class FormatCommand
{
    /// <summary>
    /// Imported right after each project file (BeforeMicrosoftNETSdkTargets), before the SDK decides whether it's
    /// multi-targeted: a multi-targeted project not on the keep list loads as usual (Roslyn's per-framework global
    /// property then wins over the environment).
    /// </summary>
    public const string SelectFrameworkTargets = """
        <Project>
          <!-- bro format: TargetFramework comes from the environment; projects not on the keep list load as usual. -->
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
          <!-- 'dotnet format' skips the rules WarningsNotAsErrors lists, which the build reports as warnings: not for its runs. -->
          <PropertyGroup Condition="'$(StyleBroFormatWarnings)' == 'true'">
            <WarningsNotAsErrors>$([System.Text.RegularExpressions.Regex]::Replace('$(WarningsNotAsErrors)', '(?i)\bBRO\d+\b', ''))</WarningsNotAsErrors>
          </PropertyGroup>
        </Project>
        """;

    /// <summary>The most runs 'bro format' makes until a run changes nothing.</summary>
    public const int MaxRuns = 3;

    /// <summary>
    /// The folder 'dotnet format' runs in when it isn't the workspace's (its '--include' paths are relative to it): '--files'
    /// for a file a project links from outside its folder. Internal: <see cref="RunFiles"/> adds it.
    /// </summary>
    public const string WorkingFolderOption = "--working-folder";

    /// <summary>The SDK's rule for unnecessary using directives.</summary>
    public const string UnusedUsings = "IDE0005";

    /// <summary>The option for a single run (the preview counts its own runs).</summary>
    public const string OnceOption = "--once";

    /// <summary>
    /// The option for a run that likely changes nothing (the one after a run that changed files): a multi-targeted
    /// repository checks all frameworks at once first (<see cref="IsCleanCheck"/>).
    /// </summary>
    public const string CheckFirstOption = "--check-first";

    /// <summary>
    /// The preview's option: its copy restores with NuGet's locked mode off, so the StyleBro.Analyzers reference it adds
    /// updates the copy's lock files instead of failing (NU1004 with RestoreLockedMode always on).
    /// </summary>
    public const string UnlockedRestoreOption = "--unlocked-restore";

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

    /// <summary>The start of the line that says a project was skipped for some frameworks only (<see cref="Incomplete"/>).</summary>
    public const string PartlyIncompleteMarker = "Partly incomplete:";

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

    /// <summary>The start of the line that names the solution's projects no configuration builds (printed for the first run only).</summary>
    public const string NotBuiltHeader = "Not built:";

    /// <summary>The start of the line a check of several runs at once prints as each run finishes.</summary>
    public const string ProgressPrefix = "Checked ";

    /// <summary>The start of the line that names the lines whose indentation '#if' decides (printed for the first run only).</summary>
    public const string IfBodiesHeader = "Indentation kept:";

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

    /// <summary>What <see cref="RulesLine"/> adds when the repository references xunit (<see cref="ReferencesXunit"/>).</summary>
    private const string XunitPart = ", xunit.analyzers' assertion rules where they are warnings or errors";

    /// <summary>The start of the line that lists the target frameworks (printed for the first run only).</summary>
    private const string MultiTargetedHeader = "Multi-targeted: ";

    /// <summary>How format names the run of the projects without a target framework (old-style), see <see cref="Plan"/>.</summary>
    private const string NoFrameworkLabel = "old-style projects";

    /// <summary>The start of the line that names the runs for other configurations (printed for the first run only).</summary>
    private const string ConfigurationsHeader = "Configurations:";

    /// <summary>The start of the line that names files the projects compile that are missing (printed for the first run only).</summary>
    private const string MissingHeader = "Missing:";

    /// <summary>The start of the line that says the whitespace pass doesn't run (printed for the first run only).</summary>
    private const string NoWhitespaceHeader = "No whitespace pass:";

    /// <summary>The start of the line that names what the whitespace pass leaves out, where it runs elsewhere (printed for the first run only).</summary>
    private const string PartlyWhitespaceHeader = "Whitespace pass leaves out:";

    /// <summary>How 'dotnet format' starts its note about workspace load warnings.</summary>
    private const string WorkspaceWarnings = "Warnings were encountered while loading the workspace";

    /// <summary>The start of the line that names files with old Mac line endings (printed for the first run only).</summary>
    private const string OldMacHeader = "Not formatted:";

    /// <summary>The start of the line that names the folders without StyleCop format leaves alone (printed for the first run only).</summary>
    private const string UnformattedHeader = "Leaving alone:";

    /// <summary>The start of the line that names the StyleBro rules in WarningsNotAsErrors (printed for the first run only).</summary>
    private const string NotAsErrorsHeader = "Warnings not as errors:";

    /// <summary>The start of the line that names the files <see cref="DirectiveMask"/> protects (printed for the first run only).</summary>
    private const string ProtectedHeader = "Protected:";

    /// <summary>The start of the line that names the Visual Basic projects format leaves alone (printed for the first run only).</summary>
    private const string VisualBasicHeader = "Visual Basic:";

    /// <summary>The start of the lines about the source generators format builds first (<see cref="BuildGenerators"/>).</summary>
    private const string GeneratorsHeader = "Generators:";

    /// <summary>
    /// A reference to a package that brings xunit.analyzers (not xunit.assert or the runners): &lt;PackageReference
    /// Include="xunit" /&gt;, &lt;PackageVersion Include="xunit.v3.mtp-v2" ... /&gt;, packages.config's id="xunit".
    /// </summary>
    private static readonly Regex XunitReference = new(@"\b(?:Include|Update|id)\s*=\s*""xunit(?:\.v3(?:\.mtp-[\w-]+)?|\.analyzers)?""", RegexOptions.IgnoreCase);

    /// <summary>The source generator projects built or found built in this process (<see cref="BuildGenerators"/>).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> BuiltGenerators = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The lines a run prints before its 'dotnet format' runs, which later runs and checks say once.</summary>
    private static readonly string[] Headers = [FixingHeader, IfBodiesHeader, ProtectedHeader, NotAsErrorsHeader, VisualBasicHeader, GeneratorsHeader, CheckingHeader, MultiTargetedHeader, ConfigurationsHeader, MissingHeader, SharedFilesHeader, TraversalHeader, "Using ", NoWhitespaceHeader, PartlyWhitespaceHeader, OldMacHeader, UnformattedHeader, OldStyleHeader, PartlyLoadedHeader, NotBuiltHeader];

    /// <summary>The projects and configurations a --verify-no-changes run on a folder couldn't check (<see cref="RecordUnchecked"/>), per folder.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, List<(string Project, string Configuration)>> Unchecked = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The findings the last run on a folder kept, per folder (<see cref="Remember"/>).</summary>
    private static readonly Dictionary<string, List<KeptFinding>> LastKept = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>What <see cref="Explain"/> has said in this command.</summary>
    private static readonly HashSet<string> Explained = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The roots whose projects compile missing files (<see cref="MissingCompileLine"/>): no IDE0005 run there.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> MissingCompileRoots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The projects <see cref="ReadProjects"/> evaluated, per workspace.</summary>
    private static readonly Dictionary<string, Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers)>> ProjectCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>What <see cref="EvaluateProject"/> asks MSBuild for: what format needs, and the items that tell whether StyleCop runs.</summary>
    private static readonly string[] ProjectQuery =
    [
        "-getProperty:TargetFrameworks", "-getProperty:TargetFramework", "-getProperty:ProjectAssetsFile", "-getProperty:MSBuildProjectExtensionsPath", "-getProperty:WarningsNotAsErrors",
        "-getProperty:Configuration", "-getProperty:Configurations", "-getProperty:Platforms", "-getProperty:StyleBroBaseline", "-getProperty:GenerateDocumentationFile",
        "-getItem:ProjectReference", "-getItem:Compile", "-getItem:PackageReference", "-getItem:GlobalPackageReference", "-getItem:Analyzer", "-getItem:MissingWorkloadPack", "-nologo",
    ];

    /// <summary>What <see cref="TestedSymbols"/> found, per file (full path).</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string[]> TestedSymbolsCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a folder holds a NuGet package (<see cref="IsPackageFile"/>), per folder.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> PackageFolders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The projects <see cref="EvaluateProject"/> evaluated (full path), once per process.</summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<string>> Evaluations = new(StringComparer.OrdinalIgnoreCase);

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
        // --timings: how long each phase took, after everything else (docs/proposals/format-speed.md, "Also seen").
        using var timings = args.Contains(TimingsOption) ? PhaseTimes.Start() : null;
        int exit;
        using (PhaseTimes.Measure("total"))
        {
            exit = RunMeasured(args.Where(a => a != TimingsOption).ToArray(), output, runOnce);
        }

        PhaseTimes.Lines().ForEach(output ?? Console.WriteLine);
        return exit;
    }

    /// <summary>
    /// What to say when the root .editorconfig's bulk severity hides StyleBro rules the build reports from 'dotnet format'
    /// (<see cref="InitCommand.HiddenFromFormat"/>), or null: then a clean run isn't clean (sharpcompress: "clean", then
    /// ~40,000 BRO errors in the build). Not with the preset off: a migration's block sets every rule by id.
    /// </summary>
    public static string? HiddenLine(string root)
    {
        // The project's folder up to the .editorconfig that says root = true: the outer files first, nearer ones win.
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var props = (string?)null;
        var folders = new List<string>();
        for (var directory = Path.GetFullPath(root); directory is not null; directory = Path.GetDirectoryName(directory))
        {
            folders.Add(directory);
            props ??= File.Exists(Path.Combine(directory, "Directory.Build.props")) ? Path.Combine(directory, "Directory.Build.props") : null;
            var editorConfig = Path.Combine(directory, ".editorconfig");
            if (File.Exists(editorConfig) && Regex.IsMatch(File.ReadAllText(editorConfig), @"^\s*root\s*=\s*true\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                break;
            }
        }

        folders.Reverse();
        var plain = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        folders.ForEach(f => Migration.OwnSettings(f, withBlocks: true).ToList().ForEach(p => settings[p.Key] = p.Value));
        folders.ForEach(f => Migration.OwnSettings(f).ToList().ForEach(p => plain[p.Key] = p.Value));
        if (props is not null && Regex.IsMatch(File.ReadAllText(props), @"<StyleBroPreset>\s*none\s*<", RegexOptions.IgnoreCase))
        {
            return null;
        }

        // Also a nested .editorconfig's (trial 19: tests/.editorconfig with 'none'; a folder that turns every analyzer off gets
        // StyleBro's rules off by id, trial 20), and a 'root = true' folder's own (coverlet's samples).
        var written = settings.Keys.Where(k => !plain.TryGetValue(k, out var value) || value != settings[k]).ToList();
        var top = Path.GetFullPath(root);
        var ownRoots = StyleCopSetup.EditorConfigRoots(top).Select(f => Path.Combine(top, f)).ToList();
        var ids = InitCommand.HiddenFromFormat(settings)
            .Concat(InitCommand.NestedHiddenFromFormat(top, settings, withBlocks: true, written).Values.SelectMany(l => l))
            .Concat(ownRoots.SelectMany(f => Migration.OwnSettings(f, withBlocks: true) is var own && InitCommand.OptsOut(Migration.OwnSettings(f))
                ? InitCommand.OptOutLines([], own)
                : InitCommand.HiddenFromFormat(own).Concat(InitCommand.NestedHiddenFromFormat(f, own, withBlocks: true).Values.SelectMany(l => l))))
            .Select(l => l.Split('.')[1])
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return null;
        }

        return $"Not clean: .editorconfig's bulk severity (dotnet_analyzer_diagnostic.severity or a category's) hides {ids.Count} StyleBro rules from 'dotnet format' "
            + $"({string.Join(", ", ids.Take(3))}{(ids.Count > 3 ? ", ..." : string.Empty)}), which the build reports. Run 'bro init --write': it sets them by id.";
    }

    /// <summary>The line that says which rules a run fixes, or (<paramref name="verify"/>) checks without changing anything.</summary>
    public static string RulesLine(bool verify, int ids, bool xunit = false) => verify
        ? $"{CheckingHeader}{(xunit ? XunitPart : string.Empty)} and the built-in rules init and migrate turn on ({ids} ids), changing nothing; --all checks every analyzer's and compiler fix too."
        : $"{FixingHeader}{(xunit ? XunitPart : string.Empty)} and the built-in rules init and migrate turn on ({ids} ids); --all applies every analyzer's and compiler fix.";

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

            // A solution's files: the folder that holds all its projects (trial 26: 'src/NLog.sln --files tests/...' said the
            // test file "isn't under src").
            var folder = target is null ? null : Directory.Exists(target) ? target : WorkingRoot(target);

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
    /// The files (full paths) projects under <paramref name="root"/> compile from outside it (Speckle: a sibling repository's
    /// '../../../speckle-sharp/*.cs'), which a copy of the root doesn't have. Only projects whose file names a Compile item
    /// going up a folder are evaluated. ponytail: such an item in a Directory.Build.props isn't seen.
    /// </summary>
    public static List<string> OutsideCompile(string root)
    {
        var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        return StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                && Regex.IsMatch(StyleCopSetup.WithoutComments(File.ReadAllText(f)), @"<Compile\b[^>]*\bInclude\s*=\s*""[^""]*\.\.", RegexOptions.IgnoreCase))
            .AsParallel()
            .SelectMany(p =>
            {
                try
                {
                    return ParseProject(Capture(Path.GetDirectoryName(p)!, "msbuild", p, "-getItem:Compile", "-nologo")).Compile;
                }
                catch (ToolException)
                {
                    return [];
                }
            })
            .Select(Path.GetFullPath)
            .Where(f => !f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && File.Exists(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The files of <paramref name="compile"/> (full paths) that exist outside <paramref name="repository"/>, sorted. Not a
    /// package's (NuGet's folder, <paramref name="packages"/>, or any other with the package's files, <see cref="IsPackageFile"/>: a package's contentFiles, NLog's benchmark diagnoser config).
    /// </summary>
    public static List<string> OutsideRepository(IEnumerable<string> compile, string repository, string? packages = null)
    {
        var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repository)) + Path.DirectorySeparatorChar;
        packages ??= Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } set ? set : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var nuget = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packages)) + Path.DirectorySeparatorChar;
        return compile.Select(Path.GetFullPath)
            .Where(f => File.Exists(f) && !f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && !f.StartsWith(nuget, StringComparison.OrdinalIgnoreCase) && !IsPackageFile(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>What format says about <see cref="OutsideRepository"/>'s files (relative to <paramref name="root"/>).</summary>
    public static string OutsideRepositoryLine(string root, IReadOnlyList<string> files) =>
        $"{UnformattedHeader} {string.Join(", ", files.Take(5).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')))}{(files.Count > 5 ? $" and {files.Count - 5} more" : string.Empty)} "
        + $"(outside the repository): a project compiles {(files.Count == 1 ? "it" : "them")}, but {(files.Count == 1 ? "it's" : "they're")} someone else's code, like a git submodule's, so 'format' doesn't change {(files.Count == 1 ? "it" : "them")} and {(files.Count == 1 ? "its" : "their")} findings don't count here.";

    /// <summary>The full path of a finding line's file ('path(line,column): ...'), or null.</summary>
    public static string? FindingPath(string line) =>
        Regex.Match(line, @"^\s*(?<path>.+?)\(\d+,\d+\): ") is { Success: true } match && !match.Groups["path"].Value.Any(c => Path.GetInvalidPathChars().Contains(c))
            ? Path.GetFullPath(match.Groups["path"].Value)
            : null;

    /// <summary>
    /// What format says when files the projects compile are missing (<paramref name="missing"/>, full paths): code that uses
    /// them doesn't compile, so usings look unused and IDE0005 is left out (trial 25: the preview's copy of Speckle lacked a
    /// sibling repository's files, and IDE0005 removed 'using' lines the code needs).
    /// </summary>
    public static string MissingCompileLine(string root, IReadOnlyList<string> missing) =>
        $"{MissingHeader} {missing.Count} file{(missing.Count == 1 ? string.Empty : "s")} the projects compile {(missing.Count == 1 ? "isn't" : "aren't")} there "
        + $"({string.Join(", ", missing.Take(3).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')))}{(missing.Count > 3 ? $" and {missing.Count - 3} more" : string.Empty)}): "
        + $"the code that uses {(missing.Count == 1 ? "it" : "them")} doesn't compile, so 'using' directives can look unused, and IDE0005 (unused usings) is left out. "
        + $"Get what provides {(missing.Count == 1 ? "it" : "them")} (another repository next to this one, a generated file), then run again.";

    /// <summary>
    /// The options without IDE0005 (<see cref="MissingCompileLine"/>): taken out of '--diagnostics' when others are left there,
    /// else '--exclude-diagnostics IDE0005' added when no '--diagnostics' names the rules (an emptied list would mean all of them).
    /// </summary>
    public static List<string> WithoutUnusedUsings(IReadOnlyList<string> options)
    {
        var result = options.ToList();
        var diagnostics = result.IndexOf("--diagnostics");
        if (diagnostics < 0)
        {
            result.AddRange(["--exclude-diagnostics", UnusedUsings]);
        }
        else if (OptionValues(result, "--diagnostics") is { Count: > 1 } ids && ids.FindIndex(i => i.Equals(UnusedUsings, StringComparison.OrdinalIgnoreCase)) is var at and >= 0)
        {
            result.RemoveAt(diagnostics + 1 + at);
        }

        return result;
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

    /// <summary>Whether a 'dotnet format' output line is a finding at warning or error ('path(1,2): error IDE0049: ...').</summary>
    public static bool IsFindingLine(string line) => Regex.IsMatch(line, @"^\s*.+?\(\d+,\d+\): (?:error|warning) \w+:");

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
    public static Dictionary<string, string> Snapshot(string root, bool endingsAside = false) =>
        StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .ToDictionary(f => f, f => Hash(File.ReadAllBytes(f), endingsAside), StringComparer.OrdinalIgnoreCase);

    /// <summary>A file's hash for <see cref="Snapshot"/>; <paramref name="endingsAside"/>: without a UTF-8 byte order mark, CRLF read as LF.</summary>
    public static string Hash(byte[] bytes, bool endingsAside = false)
    {
        if (endingsAside)
        {
            var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            var plain = new List<byte>(bytes.Length);
            for (var i = start; i < bytes.Length; i++)
            {
                if (bytes[i] != '\r' || i + 1 >= bytes.Length || bytes[i + 1] != '\n')
                {
                    plain.Add(bytes[i]);
                }
            }

            bytes = plain.ToArray();
        }

        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    /// <summary>
    /// <paramref name="runOnce"/>, after which a C# file that used one line ending throughout and now mixes them gets its own
    /// back. Without end_of_line, the SDK's whitespace pass writes the OS's ending into the line breaks it rewrites (after
    /// '=>' followed by a comment, probed with plain 'dotnet format whitespace'): a CRLF file got LF lines on Linux, an LF
    /// file CRLF lines on Windows, and '--verify-no-changes' was clean on one OS and failed on the other (issue #133).
    /// A file the run converts completely (end_of_line set) doesn't mix and stays converted.
    /// </summary>
    public static Func<string[], Action<string>?, int> KeepingLineEndings(Func<string[], Action<string>?, int> runOnce) => (args, output) =>
    {
        var path = Path.GetFullPath(args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal) ? args[0] : ".");
        var folder = File.Exists(path) ? Path.GetDirectoryName(path)! : path;
        var root = RepositoryRoot(folder) ?? folder;
        var endings = Directory.Exists(root) ? LineEndings(root) : new Dictionary<string, bool>();
        var code = runOnce(args, output);
        RestoreLineEndings(endings, files => EndOfLineSet(root, files));
        return code;
    };

    /// <summary>The C# files under the root that use one line ending throughout (true: CRLF, false: LF).</summary>
    public static Dictionary<string, bool> LineEndings(string root) =>
        StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .Select(f => (File: f, Crlf: Ending(File.ReadAllBytes(f))))
            .Where(e => e.Crlf is not null)
            .ToDictionary(e => e.File, e => e.Crlf!.Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>true: CRLF throughout, false: LF throughout; null: both, a lone CR, no line break, or UTF-16 (not touched).</summary>
    public static bool? Ending(byte[] bytes)
    {
        const byte Cr = 13;
        const byte Lf = 10;
        if (bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
        {
            return null;
        }

        int crlf = 0;
        int lf = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == Cr)
            {
                if (i + 1 >= bytes.Length || bytes[i + 1] != Lf)
                {
                    return null;
                }

                crlf++;
                i++;
            }
            else if (bytes[i] == Lf)
            {
                lf++;
            }
        }

        return crlf > 0 && lf == 0 ? true : lf > 0 && crlf == 0 ? false : null;
    }

    /// <summary>
    /// Gives each file of <paramref name="endings"/> that now mixes CRLF and LF (and nothing else) its own ending back, and
    /// each one the run converted to the other ending throughout, unless <paramref name="endOfLineSet"/> (given the converted
    /// files) says end_of_line asks for that: without it, the whitespace pass wrote the OS's ending into every line (d4b:
    /// 'end_of_line = unset' below '[*] end_of_line = lf', and the LF files of an autocrlf checkout became CRLF in every run).
    /// Without <paramref name="endOfLineSet"/>, converted files stay converted.
    /// </summary>
    public static void RestoreLineEndings(IReadOnlyDictionary<string, bool> endings, Func<IReadOnlyCollection<string>, ISet<string>>? endOfLineSet = null)
    {
        var changed = endings.AsParallel()
            .Where(e => File.Exists(e.Key))
            .Select(e => (File: e.Key, Crlf: e.Value, Now: Ending(File.ReadAllBytes(e.Key))))
            .Where(e => e.Now != e.Crlf)
            .ToList();
        var converted = changed.Where(e => e.Now is not null).Select(e => e.File).ToList();
        var keep = converted.Count == 0 ? new HashSet<string>() : endOfLineSet?.Invoke(converted) ?? converted.ToHashSet(StringComparer.OrdinalIgnoreCase);
        changed.Where(e => e.Now is null || !keep.Contains(e.File)).AsParallel().ForAll(e =>
        {
            const byte Cr = 13;
            const byte Lf = 10;
            var bytes = File.ReadAllBytes(e.File);
            var restored = new List<byte>(bytes.Length + 64);
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == Cr)
                {
                    // A lone CR: not a mix of CRLF and LF, left as it was.
                    if (i + 1 >= bytes.Length || bytes[i + 1] != Lf)
                    {
                        return;
                    }

                    continue;
                }

                if (bytes[i] == Lf && e.Crlf)
                {
                    restored.Add(Cr);
                }

                restored.Add(bytes[i]);
            }

            File.WriteAllBytes(e.File, restored.ToArray());
        });
    }

    /// <summary>
    /// The files of <paramref name="files"/> (full paths) whose .editorconfig settings set end_of_line to an ending (lf, crlf,
    /// cr): not 'unset', not missing.
    /// </summary>
    public static HashSet<string> EndOfLineSet(string root, IReadOnlyCollection<string> files)
    {
        var set = ConfigSet(root);
        return files.Where(f => set.GetOptionsForSourcePath(Path.GetFullPath(f)).AnalyzerOptions.TryGetValue("end_of_line", out var value) && value.Trim().ToLowerInvariant() is "lf" or "crlf" or "cr")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The .editorconfig and .globalconfig files under <paramref name="root"/> and above it, with their text.</summary>
    public static List<(string Path, string Text)> ConfigTexts(string root)
    {
        // ponytail: every .globalconfig under the root counts for every file; a project-specific one would apply too widely.
        var configs = StyleCopSetup.EnumerateFiles(root).Where(f => Path.GetFileName(f) is ".editorconfig" or ".globalconfig").Select(Path.GetFullPath).ToList();
        for (var folder = new DirectoryInfo(root).Parent; folder is not null; folder = folder.Parent)
        {
            configs.Add(Path.Combine(folder.FullName, ".editorconfig"));
            configs.Add(Path.Combine(folder.FullName, ".globalconfig"));
        }

        return configs.Where(File.Exists).Select(f => (Path: f, Text: File.ReadAllText(f))).ToList();
    }

    /// <summary>The settings of <see cref="ConfigTexts"/> as the compiler reads them.</summary>
    public static Microsoft.CodeAnalysis.AnalyzerConfigSet ConfigSet(string root, IEnumerable<(string Path, string Text)>? texts = null) =>
        Microsoft.CodeAnalysis.AnalyzerConfigSet.Create((texts ?? ConfigTexts(root)).Select(t => Microsoft.CodeAnalysis.AnalyzerConfig.Parse(t.Text, t.Path)).ToList());

    /// <summary>A line of 'dotnet format --verify-no-changes' output for its whitespace pass ('error WHITESPACE: ...').</summary>
    public static bool IsWhitespaceLine(string line) => line.Contains(" WHITESPACE: ", StringComparison.Ordinal) || line.Contains(" ENDOFLINE: ", StringComparison.Ordinal);

    /// <summary>
    /// The files that differ between two snapshots (removed or with other content), sorted. Not new files: 'dotnet format'
    /// never adds one, a build step does (recyclarr's git-ignored Generated/*.g.cs, missing in verify's copy and written
    /// during its run: "would change 3 files" for 1).
    /// </summary>
    public static List<string> ChangedFiles(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after) =>
        after.Where(a => before.TryGetValue(a.Key, out var b) && b != a.Value).Select(a => a.Key)
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
    /// The project and framework a skip (<see cref="SkippedProject"/>) in the run for <paramref name="run"/> is about: 'Lib' in
    /// that run, or 'Lib(netstandard1.3)', a multi-targeted project the run loaded with all its frameworks (a reference whose
    /// own frameworks don't include the run's), for that framework. Trial 31 (Hangfire): 'Hangfire.Core(netstandard1.3)' from
    /// the net461 run was a project skipped in every run, "so those files weren't formatted", though its other frameworks were.
    /// </summary>
    public static (string Project, string Framework) SkippedFor(string skipped, string run) =>
        Regex.Match(skipped, @"^(?<name>.+)\((?<framework>[^()]+)\)$") is { Success: true } match
            ? (match.Groups["name"].Value, match.Groups["framework"].Value)
            : (skipped, run);

    /// <summary>Whether a line of 'dotnet format''s output says it crashed (an unhandled exception: its changes may be half written).</summary>
    public static bool IsCrash(string line) => line.StartsWith("Unhandled exception", StringComparison.Ordinal);

    /// <summary>
    /// A code fix 'dotnet format' couldn't apply, from its output line, as 'ID (fix): error', else null: "Failed to apply code
    /// fix X for ID: error" (the fix threw; akka: BRO1309's "Unexpected value 'UnresolvedAnalyzerReference'", and the run
    /// said "clean"), or "Unable to fix ID. Code fix X ..." for a StyleBro rule. Not "No associated code fix found" for
    /// other analyzers' rules, which have no fix to begin with.
    /// </summary>
    public static string? FixFailure(string line)
    {
        if (Regex.Match(line, @"Failed to apply code fix (?<fix>\S+) for (?<id>\w+): (?<error>.*)$") is { Success: true } failed)
        {
            return $"{failed.Groups["id"].Value} ({failed.Groups["fix"].Value}): {failed.Groups["error"].Value.Trim()}";
        }

        return Regex.Match(line, @"Unable to fix (?<id>BRO\d+)\. (?<error>.*)$") is { Success: true } unable
            ? $"{unable.Groups["id"].Value}: {unable.Groups["error"].Value.Trim()}"
            : null;
    }

    /// <summary>What to say when code fixes failed (<see cref="FixFailure"/>, distinct): their findings are left, so it isn't clean.</summary>
    public static string FixFailuresLine(IReadOnlyCollection<string> failures)
    {
        // Where to report it: StyleBro's own rules here; another analyzer's fix (trial 23: a third-party XX0001 fix that threw
        // was sent to StyleBro's tracker) to whoever ships it, the SDK's rules to Roslyn.
        var ours = failures.Where(f => f.StartsWith("BRO", StringComparison.Ordinal)).ToList();
        var sdk = failures.Where(f => Regex.IsMatch(f, @"^(IDE|CA|CS)\d")).ToList();
        var others = failures.Except(ours).Except(sdk).ToList();
        var where = new List<string>();
        if (ours.Count > 0)
        {
            where.Add($"{(ours.Count == failures.Count ? "please" : "StyleBro's (" + string.Join(", ", ours.Select(f => f.Split(':', ' ')[0])) + "): please")} report it at https://github.com/bisforboman/stylebro/issues with this output");
        }

        if (sdk.Count > 0)
        {
            where.Add($"{string.Join(", ", sdk.Select(f => f.Split(':', ' ')[0]))} {(sdk.Count == 1 ? "is" : "are")} the .NET SDK's: https://github.com/dotnet/roslyn/issues");
        }

        if (others.Count > 0)
        {
            where.Add($"{string.Join(", ", others.Select(f => f.Split(':', ' ')[0]))} {(others.Count == 1 ? "is" : "are")} not StyleBro's: report it to the analyzer package or project that has {(others.Count == 1 ? "that rule" : "those rules")} (the fix's name is in parentheses)");
        }

        return $"Not clean: {(failures.Count == 1 ? "a code fix" : $"{failures.Count} code fixes")} failed, so {(failures.Count == 1 ? "its" : "their")} findings are left: "
            + $"{string.Join("; ", failures)}. {char.ToUpperInvariant(where[0][0])}{where[0][1..]}{string.Concat(where.Skip(1).Select(w => "; " + w))}.";
    }

    /// <summary>What to say after a run crashed: the run's projects, and the SDK bug when the crash is the linked-file merge's.</summary>
    public static string CrashMessage(IEnumerable<string> projects) =>
        $"'dotnet format' crashed in the run for {string.Join(", ", projects.Order(StringComparer.OrdinalIgnoreCase))}, possibly after writing part of its changes (listed below). "
        + $"If the error says \"Changes must be within bounds of SourceText\", it's Roslyn's linked-file merge ({LinkedMergeIssue}): two projects of that run compile the same file. Please report it with the projects named here.";

    /// <summary>The names (else full paths) of the projects that compile a file another project compiles too (<see cref="Separate"/>).</summary>
    public static List<string> SharingProjects(IEnumerable<(string FullPath, string[] Compile)> projects, bool byName = true)
    {
        var list = projects.ToList();
        var owners = list.SelectMany(p => p.Compile.Distinct(StringComparer.OrdinalIgnoreCase).Select(f => (File: f, p.FullPath)))
            .GroupBy(x => x.File, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(x => x.FullPath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return list.Where(p => owners.Contains(p.FullPath)).Select(p => byName ? Path.GetFileNameWithoutExtension(p.FullPath) : p.FullPath).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Starts a command: <see cref="Explain"/> says its notes again.</summary>
    public static void ResetExplained()
    {
        lock (Explained)
        {
            Explained.Clear();
        }
    }

    /// <summary>
    /// The first warning a restore that worked printed for a C# project ('Text-Grab/Text-Grab.csproj: NU1603: ...'), or null:
    /// what <see cref="Explain"/> names for 'dotnet format''s workspace warnings, which it doesn't show (trial 28, Text-Grab:
    /// "e.g. one in another language" was a guess, the cause NU1603).
    /// </summary>
    public static string? RestoreWarning(string root, IEnumerable<string> output) =>
        output.Select(l => Regex.Match(l, @"^\s*(?<path>.+?\.csproj)\s*:\s*warning (?<id>\w+):\s*(?<message>.*?)(?:\s*\[[^\]]*\])?\s*$"))
            .Where(m => m.Success)
            .Select(m => $"{Relative(root, m.Groups["path"].Value)}: {m.Groups["id"].Value}: {m.Groups["message"].Value.TrimEnd('.')}.")
            .FirstOrDefault();

    /// <summary>
    /// 'dotnet format''s notes said in plain words, once per command (every run and framework printed them again, unexplained:
    /// FakeItEasy's F# test helper, "Warnings were encountered while loading the workspace"); null: already said.
    /// </summary>
    public static string? Explain(string line, string run = "", string? restoreWarning = null)
    {
        // The per-framework runs load the projects their own way: a plain 'dotnet format' may not show what they saw (trial 26).
        var see = "add '--verbosity diagnostic' to this 'bro format' command";
        var where = run.Length > 0 ? $" in its {run} run" : string.Empty;

        // "Required references did not load ... Run `dotnet restore`" after a restore that worked (FluentAssertions, trial 23):
        // MSBuild failed to evaluate the project or one it references for the run (there: a netstandard2.0 project loaded for
        // net47, whose reference assemblies weren't there). The restore isn't the cause; MSBuild's message is in the diagnostic output.
        var (key, text) = UnsupportedProject(line) is { } name
            ? (name, $"Not formatted: {name}: 'dotnet format' formats C# and Visual Basic projects only, so it's left alone; the C# projects are formatted.")
            : SkippedProject(line) is { } skipped
            ? ("skipped:" + skipped, $"'dotnet format' skipped {skipped}: MSBuild couldn't load it or a project it references for this run (often reference assemblies missing for its target framework). The restore isn't the cause; {see} to see MSBuild's message (\"Msbuild failed when processing the file ...\"){where}.")
            : line.StartsWith(WorkspaceWarnings, StringComparison.Ordinal)
                ? (WorkspaceWarnings, restoreWarning is null
                    ? $"'dotnet format' had warnings loading the workspace{where}; they don't stop the run. To see them, {see}."
                    : $"'dotnet format' had warnings loading the workspace{where}; they don't stop the run. The restore warned too, likely the same: {restoreWarning} To see them all, {see}.")
                : (null, line);
        if (key is null)
        {
            return line;
        }

        lock (Explained)
        {
            return Explained.Add(key) ? text : null;
        }
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
            lines.Add($"{PartlyIncompleteMarker} 'dotnet format' skipped {shown} for {group.Key} (references didn't load): code only that framework compiles (inside #if) wasn't formatted.");
        }

        // Orleans: the test projects reference F# projects, which 'dotnet format' can't load; a restore doesn't help.
        if (lines.Count > 0 && unsupported is { Count: > 0 })
        {
            lines.Add($"{LikelyCause} they reference {string.Join(", ", unsupported.Order(StringComparer.OrdinalIgnoreCase))}, which 'dotnet format' can't load (C# and Visual Basic only), so a restore won't help. Nothing is broken; that code just isn't formatted.");
        }

        return lines;
    }

    /// <summary>
    /// Whether a --verify-no-changes run's output says code went unchecked: a project no run loaded (Autofac's tests), or one
    /// skipped for some frameworks with no other framework of the same code loading in their place (trial 28: "Partly
    /// incomplete" said clean, the code only those frameworks compile unchecked). Not when the cause is a project 'dotnet
    /// format' can never load (an F# reference, "Likely cause"), which a restore doesn't change.
    /// </summary>
    public static bool Unverified(IReadOnlyCollection<string> lines) =>
        lines.Any(l => l.StartsWith(IncompleteMarker, StringComparison.Ordinal) || l.StartsWith(PartlyIncompleteMarker, StringComparison.Ordinal))
        && !lines.Any(l => l.StartsWith(LikelyCause, StringComparison.Ordinal));

    /// <summary>
    /// The ids 'bro format' fixes by default: every StyleBro rule, and the built-in IDE and CA rules init's template
    /// and the stylestylebro-migrate blocks of the .editorconfig files at and above <paramref name="root"/> name (rules set to none
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

        // Usings a fix leaves unused ('UInt32' -> 'uint' keeps 'using System;'): where the repository enforces IDE0005, the
        // build failed on them (coverlet) while format neither removed nor reported them.
        if (EnforcesUnusedUsings(root))
        {
            ids.Add(UnusedUsings);
        }

        // xunit.analyzers' fixes that keep each test's meaning (owner's decision 2026-10-18); 'dotnet format --severity warn'
        // applies them only where the repository leaves the rule at warning or error.
        if (ReferencesXunit(root))
        {
            ids.UnionWith(Analyzers.Baseline.BaselineSuppressor.XunitIds);
        }

        return ids.ToList();
    }

    /// <summary>
    /// Whether a project, props, targets or packages.config file at or below <paramref name="root"/> references xunit
    /// (xunit, xunit.v3 or xunit.analyzers itself), which brings xunit.analyzers and its fixes along.
    /// </summary>
    public static bool ReferencesXunit(string root) =>
        StyleCopSetup.EnumerateFiles(root)
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".csproj" or ".props" or ".targets" || Path.GetFileName(f).Equals("packages.config", StringComparison.OrdinalIgnoreCase))
            .Any(f => XunitReference.IsMatch(File.ReadAllText(f)));

    /// <summary>
    /// Whether the .editorconfig files at and above <paramref name="root"/> (nearer ones win) make IDE0005 (unnecessary
    /// usings) a warning or error: by id, its category (Style) or the bulk severity.
    /// </summary>
    public static bool EnforcesUnusedUsings(string root)
    {
        // Every C# file's own settings: a nested .editorconfig that enforces it counts (spectre-console's src/.editorconfig,
        // with format run from the repository's root: the build failed on usings format neither removed nor reported).
        var texts = ConfigTexts(root);
        if (!texts.Any(t => Regex.IsMatch(t.Text, @"IDE0005|category-Style|dotnet_analyzer_diagnostic\.severity", RegexOptions.IgnoreCase)))
        {
            return false;
        }

        var set = ConfigSet(root, texts);
        return StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Prepend(Path.Combine(root, "_.cs"))
            .Any(f => Severity(set, Path.GetFullPath(f), UnusedUsings) is Microsoft.CodeAnalysis.ReportDiagnostic.Warn or Microsoft.CodeAnalysis.ReportDiagnostic.Error);
    }

    /// <summary>
    /// A Style rule's severity for a file as the compiler reads <paramref name="set"/>: its own (a .globalconfig's too) beats
    /// the Style category's or the bulk one; null when nothing sets it.
    /// </summary>
    public static Microsoft.CodeAnalysis.ReportDiagnostic? Severity(Microsoft.CodeAnalysis.AnalyzerConfigSet set, string file, string id)
    {
        var options = set.GetOptionsForSourcePath(file);
        return options.TreeOptions.TryGetValue(id, out var own) ? own
            : set.GlobalConfigOptions.TreeOptions.TryGetValue(id, out var global) ? global
            : Severities.Configured(options.AnalyzerOptions, id, "Style") ?? Severities.Configured(set.GlobalConfigOptions.AnalyzerOptions, id, "Style");
    }

    /// <summary>
    /// Whether the frameworks' '--verify-no-changes' runs (exit code, output) show that nothing is left but findings in
    /// <paramref name="kept"/> (<see cref="AsKept"/>): each exited 0, or said "would change" for kept findings only; and none
    /// skipped a project (a project that didn't load reports nothing, which proves nothing).
    /// </summary>
    public static bool IsCleanCheck(IEnumerable<(int Code, IReadOnlyList<string> Lines)> runs, IReadOnlyCollection<KeptFinding>? kept = null) =>
        runs.All(r => !r.Lines.Any(l => SkippedProject(l) is not null || IsRestoreFailure(l))
            && (r.Code == 0 || (r.Code == NotCleanExitCode && kept is { Count: > 0 } && r.Lines.Any(IsFindingLine) && r.Lines.Where(IsFindingLine).All(l => AsKept(l, kept) is not null))));

    /// <summary>
    /// A 'dotnet format --verify-no-changes' finding line ('path(line,column): warning ID: message [project]') as the kept
    /// finding of <paramref name="kept"/> with the same file, rule and message, at the line's position (other fixes in the run
    /// that kept it may have moved it); null when it's no kept finding.
    /// </summary>
    public static KeptFinding? AsKept(string line, IReadOnlyCollection<KeptFinding> kept)
    {
        var match = Regex.Match(line, @"^\s*(?<path>.+?)\((?<line>\d+),(?<column>\d+)\): \w+ (?<id>\w+): (?<message>.*?)(?: \(https?://[^)\s]*\))?(?: \[[^\]]*\])?\s*$");
        if (!match.Success || kept.Count == 0)
        {
            return null;
        }

        static string Text(string message) => message.Trim().TrimEnd('.');
        var path = match.Groups["path"].Value;
        var found = kept.FirstOrDefault(k => k.Id == match.Groups["id"].Value
            && string.Equals(Text(k.Message), Text(match.Groups["message"].Value), StringComparison.Ordinal)
            && string.Equals(Path.GetFullPath(k.Path), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
        return found is null
            ? null
            : new KeptFinding(found.Path, int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture), int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture), found.Id, found.Reason, found.Message, found.Where);
    }

    /// <summary>
    /// What a run does after its own check (<see cref="CheckFirstOption"/>, when not every run's check was clean): replayed
    /// when clean (<see cref="IsCleanCheck"/>), else run on the files with findings only (<see cref="FindingFiles"/>), or on
    /// all its files (null) when the check failed otherwise. <paramref name="touched"/>: the files it formats an earlier run
    /// changed (relative, like <see cref="FindingFiles"/>), where the check is out of date: those files run too, not all of
    /// the run's (trial 25: --files on a multi-targeted project ran all three frameworks in full). A clean check is replayed
    /// even then: the next run's check finds what the earlier run's edits start in this view. Findings aren't skipped: code
    /// only this framework or configuration compiles ('#if !DEBUG') is no other run's to fix.
    /// </summary>
    public static (bool Replay, List<string>? Only) AfterCheck(IReadOnlyCollection<string> touched, int code, IReadOnlyList<string> lines, IReadOnlyCollection<KeptFinding> known, string root)
    {
        if (IsCleanCheck([(code, lines)], known))
        {
            return (true, null);
        }

        if (code != NotCleanExitCode || FindingFiles(lines, root) is not { } files)
        {
            return (false, null);
        }

        return (false, files.Union(touched, StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>What --verify-no-changes says about the projects that take an in-solution source generator: a build checks their other configurations too.</summary>
    public static string GeneratorConfigurationsLine(IReadOnlyList<string> names) =>
        $"{ConfigurationsHeader} {string.Join(", ", names.Take(4))}{(names.Count > 4 ? $" and {names.Count - 4} more" : string.Empty)}: "
        + $"{(names.Count == 1 ? "it takes" : "they take")} an in-solution source generator, so a build of {(names.Count == 1 ? "its" : "their")} other configurations checks the code only those compile ('#if !DEBUG') at the end too.";

    /// <summary>Keeps what a --verify-no-changes run on <paramref name="root"/> couldn't check (project full path, configuration), for <see cref="UncheckedLine"/>.</summary>
    public static void RecordUnchecked(string root, List<(string Project, string Configuration)> projects) =>
        Unchecked[Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))] = projects;

    /// <summary>
    /// --verify-no-changes: builds each project and configuration <see cref="RecordUnchecked"/> kept for <paramref name="root"/>
    /// (<paramref name="build"/>: the output of a build, in tests) and returns what makes it not clean, or null when those
    /// builds report none of format's rules (<see cref="Diagnostics"/>) in the project. Owner's decision 2026-10-16: verify said
    /// clean while a Release build of a project with an in-solution source generator failed on Release-only code. The projects
    /// of one configuration build together (<paramref name="together"/>; <see cref="TogetherProject"/>: their shared references
    /// once, CliFx's two builds took 99 s one after another); when that build fails, each builds on its own as before, which
    /// tells whose build failed.
    /// </summary>
    public static string? UncheckedLine(string root, Action<string> log, Func<string, string, (int ExitCode, IReadOnlyList<string> Output)>? build = null, Func<IReadOnlyList<string>, string, (int ExitCode, IReadOnlyList<string> Output)>? together = null)
    {
        if (!Unchecked.TryRemove(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), out var projects) || projects.Count == 0)
        {
            return null;
        }

        var ids = Diagnostics(root).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var findings = new List<string>();
        var failed = new List<string>();
        var built = new Dictionary<(string, string), (int ExitCode, IReadOnlyList<string> Output)>();
        foreach (var (configuration, group) in BuildGroups(projects))
        {
            group.ForEach(project => log($"Building {Path.GetFileNameWithoutExtension(project)} in {configuration} to check it (it takes an in-solution source generator)..."));
            using var phase = PhaseTimes.Measure($"verify: build {configuration} ({group.Count} project{(group.Count == 1 ? string.Empty : "s")})");
            if (group.Count > 1 && (together ?? BuildTogether)(group, configuration) is { ExitCode: 0 } all)
            {
                // Each project's own lines: those of the build of it alone (it succeeded, so no other line can fail it).
                group.ForEach(project => built[(project, configuration)] = (0, OwnOutput(project, all.Output)));
                continue;
            }

            group.ForEach(project => built[(project, configuration)] = (build ?? BuildOutput)(project, configuration));
        }

        foreach (var (project, configuration) in projects)
        {
            var name = Path.GetFileNameWithoutExtension(project);
            var (exitCode, output) = built[(project, configuration)];
            var lines = output.Select(l => Regex.Match(l, @"^\s*(?<path>.+?)\((?<line>\d+),(?<column>\d+)\): (?<kind>warning|error) (?<id>\w+): (?<message>.*?)(?: \[(?<project>[^\]]*)\])?\s*$"))
                .Where(m => m.Success)
                .ToList();
            string Shown(Match m) => $"  {Relative(root, m.Groups["path"].Value)}({m.Groups["line"].Value},{m.Groups["column"].Value}): {m.Groups["id"].Value}: {m.Groups["message"].Value}";

            // A multi-targeted project's lines name the framework too ('[...\Lib.csproj::TargetFramework=net8.0]'): all were dropped.
            var found = lines
                .Where(m => ids.Contains(m.Groups["id"].Value) && (!m.Groups["project"].Success || string.Equals(Path.GetFullPath(BuildLineProject(m.Groups["project"].Value)), Path.GetFullPath(project), StringComparison.OrdinalIgnoreCase)))
                .Select(Shown)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            // A build that failed on more than the findings (an error in a referenced project, a compile error) checked nothing
            // past it, with findings or not (trial 28, ZLogger: 18 findings shown, the failed Release build not).
            var errors = lines.Where(m => m.Groups["kind"].Value == "error" && !ids.Contains(m.Groups["id"].Value)).Select(Shown).Distinct(StringComparer.Ordinal).ToList();

            // Failed on the findings alone only when they are errors (TreatWarningsAsErrors); else on something no line names.
            var buildFailed = exitCode != 0 && (found.Count == 0 || errors.Count > 0 || !lines.Any(m => m.Groups["kind"].Value == "error" && ids.Contains(m.Groups["id"].Value)));
            if (found.Count > 0 || !buildFailed)
            {
                log($"{configuration} build of {name}: {(found.Count == 0 ? "no findings." : $"{found.Count} finding{(found.Count == 1 ? string.Empty : "s")}:")}");
            }

            found.ForEach(log);
            findings.AddRange(found);
            if (buildFailed)
            {
                log($"{configuration} build of {name} failed{(errors.Count == 0 ? $" (exit code {exitCode})." : $", first error{(errors.Count == 1 ? string.Empty : "s")}:")}");
                errors.Take(5).ToList().ForEach(log);
                failed.Add($"{name} in {configuration}");
            }
        }

        var findingsLine = $"{findings.Count} finding{(findings.Count == 1 ? string.Empty : "s")} in code only another configuration compiles (above), which 'format' can't fix there: fix {(findings.Count == 1 ? "it" : "them")} by hand, or suppress or baseline {(findings.Count == 1 ? "it" : "them")}.";
        var failedLine = $"the build of {string.Join(", ", failed)} failed (above), so the code only that configuration compiles wasn't checked";
        return findings.Count > 0 && failed.Count > 0 ? $"Not clean: {findingsLine} And {failedLine}: baselining doesn't fix that, fix the build, then run again."
            : findings.Count > 0 ? $"Not clean: {findingsLine}"
            : failed.Count > 0 ? $"Not clean: {failedLine}: fix the build, then run again."
            : null;
    }

    /// <summary>The project file of a build line's '[...]' tag, without what a multi-targeted build adds ('::TargetFramework=net8.0').</summary>
    public static string BuildLineProject(string tag) => tag.IndexOf("::", StringComparison.Ordinal) is var at and >= 0 ? tag.Substring(0, at) : tag;

    /// <summary>Keeps the kept findings of a run on <paramref name="root"/> for the check after it (<see cref="CheckFirstOption"/>).</summary>
    public static void Remember(string root, List<KeptFinding> kept)
    {
        lock (LastKept)
        {
            LastKept[Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))] = kept;
        }
    }

    /// <summary>The kept findings of the last run on <paramref name="root"/> in this process (<see cref="Remember"/>).</summary>
    public static List<KeptFinding> Remembered(string root)
    {
        lock (LastKept)
        {
            return LastKept.TryGetValue(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), out var kept) ? kept : [];
        }
    }

    /// <summary>The arguments with '--include' naming <paramref name="include"/> only (an '--include' they had is replaced), and without '--report'.</summary>
    public static List<string> WithInclude(IEnumerable<string> args, IEnumerable<string> include)
    {
        var result = args.ToList();
        foreach (var option in new[] { "--include", "--report" })
        {
            if (result.IndexOf(option) is var at && at >= 0)
            {
                result.RemoveRange(at, 1 + (option == "--report" ? Math.Min(1, result.Count - at - 1) : OptionValues(result, option)!.Count));
            }
        }

        return [.. result, "--include", .. include];
    }

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
            : error.Groups["code"].Value == "NU1004"
            ? " The lock files (packages.lock.json) don't list a package added since, StyleBro.Analyzers likely: update them once with 'dotnet restore -p:RestoreLockedMode=false' and commit them (https://bisforboman.github.io/stylebro/getting-started/#1-add-the-package)."
            : string.Empty;
        return $"The restore failed with {error.Groups["code"].Value}: {error.Groups["message"].Value.Trim().TrimEnd('.')}.{feed} Fix that (https://learn.microsoft.com/nuget/reference/errors-and-warnings/{error.Groups["code"].Value.ToLowerInvariant()}), then run again. Once the packages are restored, '--no-restore' skips the restore.";
    }

    /// <summary>Whether a 'dotnet format' output line says its restore failed (it prints a stack trace, nothing about why).</summary>
    public static bool IsRestoreFailure(string line) => line.Contains("Restore operation failed", StringComparison.Ordinal);

    /// <summary>Whether the whitespace pass is off everywhere: <see cref="WhitespaceExclusions"/> leaves every C# file out.</summary>
    public static bool WhitespaceOff(string root) => WhitespaceExclusions(root) is { Count: 0 };

    /// <summary>
    /// Where the whitespace pass must not run: null when it runs everywhere, empty when it runs nowhere, else the folders
    /// (relative, '/'-terminated) and files it leaves out. Each C# file's effective IDE0055 decides (its own severity, else
    /// the Style category's, else the bulk one), read through the whole .editorconfig chain and the .globalconfig files like
    /// the compiler does: a stylestylebro-migrate block's or the repository's own (splat's 'none' was "kept", and the whitespace pass still
    /// de-indented its code). 'dotnet format' runs its whitespace pass whatever IDE0055 says, so a CSharpier folder
    /// ('[src/GuardClauses/**.cs]' with IDE0055 = none) has to be left out by path, else the two formatters undo each other
    /// forever (GuardClauses: '[NotNull] [ValidatedNotNull]' against '[NotNull][ValidatedNotNull]').
    /// </summary>
    public static List<string>? WhitespaceExclusions(string root)
    {
        var texts = ConfigTexts(root);
        if (!texts.Any(t => Regex.IsMatch(t.Text, @"IDE0055|category-Style|dotnet_analyzer_diagnostic\.severity", RegexOptions.IgnoreCase)))
        {
            return null;
        }

        var set = ConfigSet(root, texts);
        var top = Path.GetFullPath(root);
        var off = new List<string>();
        var onFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Select(Path.GetFullPath))
        {
            var relative = Path.GetRelativePath(top, file).Replace(Path.DirectorySeparatorChar, '/');

            // An id's severity (a .globalconfig's too) beats a category's or the bulk one, as in the compiler.
            if (Severity(set, file, "IDE0055") == Microsoft.CodeAnalysis.ReportDiagnostic.Suppress)
            {
                off.Add(relative);
                continue;
            }

            for (var folder = Parent(relative); folder.Length > 0; folder = Parent(folder))
            {
                onFolders.Add(folder);
            }

            onFolders.Add(string.Empty);
        }

        static string Parent(string path) => path.LastIndexOf('/') is var at && at < 0 ? string.Empty : path[..at];

        if (off.Count == 0)
        {
            return null;
        }

        if (!onFolders.Contains(string.Empty))
        {
            return [];
        }

        // Each off file's folder when nothing in it is formatted, else the file itself; folders below one already left out go.
        var result = new SortedSet<string>(off.Select(f => Parent(f) is var folder && folder.Length > 0 && !onFolders.Contains(folder) ? folder + "/" : f), StringComparer.OrdinalIgnoreCase);
        result.RemoveWhere(p => result.Any(other => other != p && other.EndsWith('/') && p.StartsWith(other, StringComparison.OrdinalIgnoreCase)));
        return result.ToList();
    }

    /// <summary>
    /// What format says about the folders its whitespace pass leaves out (<see cref="WhitespaceExclusions"/>): another
    /// formatter's code, where the rest still runs, or a folder whose .editorconfig turns every analyzer off, where nothing
    /// runs (the opt-out spot was told "style and analyzer fixes still run there").
    /// </summary>
    public static string NoWhitespaceLine(string root, IReadOnlyCollection<string> exclusions)
    {
        // The folder whose .editorconfig opts out may be above the excluded one (samples/.editorconfig, samples/Demo/ excluded:
        // trial 23 said "style and analyzer fixes still run there").
        string? OptOutFolder(string exclusion)
        {
            for (var folder = exclusion.TrimEnd('/'); folder.Length > 0; folder = folder.LastIndexOf('/') is var at && at < 0 ? string.Empty : folder[..at])
            {
                if (InitCommand.OptsOut(Migration.OwnSettings(Path.Combine(root, folder))))
                {
                    return folder + "/";
                }
            }

            return null;
        }

        var optOutOf = exclusions.ToDictionary(e => e, OptOutFolder);
        var optOut = optOutOf.Values.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        var other = exclusions.Where(e => optOutOf[e] is null).ToList();
        string Them(int count) => count == 1 ? "it" : "them";
        var parts = new List<string>();
        if (other.Count > 0)
        {
            // Not "another formatter's code" unless one is there: Splat just has IDE0055 = none (trial 21).
            var formatter = Conventions.CSharpier(root) is null ? string.Empty : " (CSharpier's code)";
            parts.Add($"{string.Join(", ", other)}: IDE0055 is off there in the settings{formatter}, so whitespace formatting leaves {Them(other.Count)} alone; style and analyzer fixes still run there.");
        }

        if (optOut.Count > 0)
        {
            parts.Add($"{string.Join(", ", optOut)}: {(optOut.Count == 1 ? "its" : "their")} .editorconfig turns every analyzer off, so 'format' leaves {Them(optOut.Count)} alone.");
        }

        return $"{PartlyWhitespaceHeader} {string.Join(" ", parts)}";
    }

    /// <summary>
    /// The documentation comment lines the whitespace pass leaves and IDE0055 reports (<see cref="DocIndentation"/>) in the
    /// files a run on <paramref name="root"/> with <paramref name="args"/> formats: what the workspace's projects compile under
    /// the root ('format' evaluated them), the whitespace pass's (not <see cref="WhitespaceExclusions"/>, '--include' only,
    /// not '--exclude', submodules or the folders a migration left alone), not generated code. <paramref name="reported"/>:
    /// only in the files of projects whose build reports them (<see cref="BuildReportsDocIndentation"/>), what verify counts.
    /// </summary>
    public static List<(string File, List<int> Lines, bool UseTabs, int TabWidth)> DocIndentationFindings(string root, IReadOnlyList<string> args, bool reported = false)
    {
        var exclusions = WhitespaceExclusions(root);
        if (exclusions is { Count: 0 })
        {
            return [];
        }

        List<(string FullPath, string[] Compile)> evaluated;
        lock (ProjectCache)
        {
            evaluated = ProjectCache.Values.SelectMany(d => d.Values).Select(p => (p.FullPath, p.Compile)).ToList();
        }

        var compiled = evaluated.Where(p => !reported || BuildReportsDocIndentation(EvaluatedProperty(p.FullPath, "GenerateDocumentationFile")))
            .SelectMany(p => p.Compile)
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        var include = OptionValues(args, "--include");
        List<string> exclude = [.. OptionValues(args, "--exclude") ?? [], .. exclusions ?? [], .. StyleCopSetup.NestedRepositories(root), .. Migration.UnformattedFolders(root)];
        bool Matches(string path, string relative) => Covers(path, relative) || Covers(path.TrimEnd('/') + "/", relative);
        var files = compiled.Where(f => f.StartsWith(top, StringComparison.OrdinalIgnoreCase) && File.Exists(f))
            .Select(f => (File: f, Relative: Path.GetRelativePath(root, f).Replace('\\', '/')))
            .Where(f => (include is null || include.Any(i => Matches(i.Replace('\\', '/'), f.Relative))) && !exclude.Any(x => Matches(x.Replace('\\', '/'), f.Relative)))
            .Select(f => f.File);
        return DocIndentation.Find(root, files).Where(f => !IsGeneratedCode(f.File)).ToList();
    }

    /// <summary>
    /// Whether the build of a project with this GenerateDocumentationFile value reports the lines <see cref="DocIndentation"/>
    /// rewrites (owner's decision 2026-10-17, "count them only when the build would"): with documentation generated the
    /// compiler parses the comments, and the build's IDE0055 reported none of them (space indentation) or only some (tabs:
    /// two lines of three, the closing tag's not), so verify counts them only where it isn't.
    /// </summary>
    public static bool BuildReportsDocIndentation(string generateDocumentationFile) =>
        !generateDocumentationFile.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

    /// <summary>The paths of <paramref name="paths"/> ('--include'/'--exclude' style) no folder of <paramref name="excluded"/> holds.</summary>
    public static List<string> NotExcluded(IEnumerable<string> paths, IReadOnlyCollection<string> excluded) =>
        paths.Where(p => !excluded.Any(x => Covers(x.TrimEnd('/') + "/", p) || x.TrimEnd('/').Equals(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))).ToList();

    /// <summary>
    /// The 'dotnet format' passes for <see cref="WhitespaceExclusions"/>:one run of all three (null), style and analyzers
    /// (empty: no whitespace pass), or the whitespace pass on its own (<see cref="WhitespaceOptions"/>) then the other two.
    /// </summary>
    public static string[] Passes(IReadOnlyCollection<string>? whitespaceExclusions) => whitespaceExclusions switch
    {
        null => [string.Empty],
        { Count: 0 } => ["style", "analyzers"],
        _ => ["whitespace", "style", "analyzers"],
    };

    /// <summary>
    /// The passes of a run that formats only <paramref name="include"/> ('--include' paths): no whitespace pass when the
    /// whitespace exclusions cover all of it (splat: CSharpier everywhere but one file outside the projects, an empty
    /// whitespace pass per run, ~10 minutes), and one plain run of all three when they touch none of it.
    /// </summary>
    public static string[] Passes(IReadOnlyCollection<string>? whitespaceExclusions, IReadOnlyCollection<string> include)
    {
        if (whitespaceExclusions is not { Count: > 0 } || include.Count == 0)
        {
            return Passes(whitespaceExclusions);
        }

        if (include.All(i => whitespaceExclusions.Any(e => Covers(e, i))))
        {
            return Passes([]);
        }

        return include.Any(i => whitespaceExclusions.Any(e => Covers(e, i) || Covers(i, e))) ? Passes(whitespaceExclusions) : Passes(null);
    }

    /// <summary>
    /// The passes that can fix one of <paramref name="diagnostics"/> (all when null): the style pass only the SDK's code
    /// style rules (IDE), the analyzer pass only the others. The whitespace pass and a plain run stay.
    /// </summary>
    public static string[] Relevant(string[] passes, IReadOnlyCollection<string>? diagnostics)
    {
        static bool IsStyle(string id) => id.StartsWith("IDE", StringComparison.OrdinalIgnoreCase);
        var relevant = diagnostics is null ? passes : passes.Where(p => p switch
        {
            "style" => diagnostics.Any(IsStyle),
            "analyzers" => diagnostics.Any(d => !IsStyle(d)),
            _ => true,
        }).ToArray();
        return relevant.Length == 0 ? passes : relevant;
    }

    /// <summary>
    /// Whether the '--include' path <paramref name="outer"/> matches every file <paramref name="inner"/> matches: '**/*.cs'
    /// everything, a folder ('src/A/') what's below it, a file itself.
    /// </summary>
    public static bool Covers(string outer, string inner) =>
        outer == "**/*.cs" || (outer.EndsWith('/') ? inner.StartsWith(outer, StringComparison.OrdinalIgnoreCase) : outer.Equals(inner, StringComparison.OrdinalIgnoreCase));

    /// <summary>The values after an option ('--include a b --x' gives a, b), or null without it.</summary>
    public static List<string>? OptionValues(IReadOnlyList<string> options, string name)
    {
        var at = options.ToList().IndexOf(name);
        return at < 0 ? null : options.Skip(at + 1).TakeWhile(o => !o.StartsWith("-", StringComparison.Ordinal)).ToList();
    }

    /// <summary>
    /// The '--include' paths of one run: its own (<paramref name="own"/>), or the files of <paramref name="requested"/> it
    /// covers when the command names files (empty: nothing of it is in this run, which is skipped), or the requested
    /// folders and patterns as they are.
    /// </summary>
    public static List<string> RunInclude(IReadOnlyCollection<string>? requested, IReadOnlyCollection<string> own)
    {
        if (requested is null)
        {
            return own.ToList();
        }

        return requested.Any(r => r.EndsWith('/') || r.Contains('*', StringComparison.Ordinal))
            ? requested.ToList()
            : requested.Where(r => own.Any(o => Covers(o, r.Replace('\\', '/')))).ToList();
    }

    /// <summary>
    /// The options for 'dotnet format whitespace': without the diagnostic options it rejects ('--diagnostics',
    /// '--exclude-diagnostics', '--severity' and their values), and with <paramref name="exclusions"/> added to '--exclude'.
    /// </summary>
    public static List<string> WhitespaceOptions(IReadOnlyList<string> options, IReadOnlyCollection<string> exclusions)
    {
        var result = new List<string>();
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i] is "--diagnostics" or "--exclude-diagnostics" or "--severity")
            {
                while (i + 1 < options.Count && !options[i + 1].StartsWith('-'))
                {
                    i++;
                }

                continue;
            }

            result.Add(options[i]);
        }

        return Excluding(result, exclusions);
    }

    /// <summary>The options with <paramref name="exclusions"/> added to '--exclude'.</summary>
    public static List<string> Excluding(IReadOnlyList<string> options, IReadOnlyCollection<string> exclusions)
    {
        var result = options.ToList();
        var exclude = result.IndexOf("--exclude");
        if (exclusions.Count == 0)
        {
            return result;
        }

        if (exclude < 0)
        {
            result.Add("--exclude");
            result.AddRange(exclusions);
        }
        else
        {
            result.InsertRange(exclude + 1, exclusions);
        }

        return result;
    }

    /// <summary>
    /// The C# files under the root (relative, '/'-separated) with old Mac line endings: a CR without an LF after it that
    /// ends a line of code, not one inside a string or character literal (jint: a verbatim regex with a CR character in
    /// it, which converting the file's line endings would change). The SDK's formatter adds a line break to them on every run.
    /// </summary>
    public static List<string> OldMacLineEndings(string root)
    {
        return StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && HasLoneCr(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Whether a C# text has a CR without an LF after it outside its string and character literals (<see cref="OldMacLineEndings"/>).</summary>
    public static bool HasLoneCr(string text) => LoneCrs(text).Code;

    /// <summary>
    /// Where a C# text has a CR without an LF after it: outside its string and character literals (<see cref="HasLoneCr"/>),
    /// and inside one (<see cref="LoneCrStringFiles"/>).
    /// </summary>
    public static (bool Code, bool Literal) LoneCrs(string text)
    {
        List<Microsoft.CodeAnalysis.Text.TextSpan>? literals = null;
        var (code, literal) = (false, false);
        for (var at = text.IndexOf('\r', StringComparison.Ordinal); at >= 0 && !(code && literal); at = text.IndexOf('\r', at + 1))
        {
            if (at + 1 < text.Length && text[at + 1] == '\n')
            {
                continue;
            }

            literals ??= Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text).GetRoot().DescendantTokens()
                .Where(t => Microsoft.CodeAnalysis.CSharp.CSharpExtensions.Kind(t) is Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken
                    or Microsoft.CodeAnalysis.CSharp.SyntaxKind.CharacterLiteralToken or Microsoft.CodeAnalysis.CSharp.SyntaxKind.InterpolatedStringTextToken
                    or Microsoft.CodeAnalysis.CSharp.SyntaxKind.MultiLineRawStringLiteralToken or Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineRawStringLiteralToken
                    or Microsoft.CodeAnalysis.CSharp.SyntaxKind.Utf8StringLiteralToken or Microsoft.CodeAnalysis.CSharp.SyntaxKind.Utf8MultiLineRawStringLiteralToken)
                .Select(t => t.Span)
                .ToList();
            if (literals.Any(s => s.Contains(at)))
            {
                literal = true;
            }
            else
            {
                code = true;
            }
        }

        return (code, literal);
    }

    /// <summary>
    /// The C# files under the root (relative, '/'-separated) with a CR without an LF inside a string literal (jint: a verbatim
    /// regex), whose settings set end_of_line: the whitespace pass then rewrites every line ending, the one in the string
    /// too, and the program's string changed (trial 24). So does the style pass (its using sort). They're left out of both. Not the files with old
    /// Mac line endings in code (<see cref="OldMacLineEndings"/>), which are left out of every pass.
    /// </summary>
    public static List<string> LoneCrStringFiles(string root)
    {
        var files = StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .Where(f => File.ReadAllText(f) is var text && text.Contains('\r', StringComparison.Ordinal) && LoneCrs(text) is { Code: false, Literal: true })
            .ToList();
        return files.Count == 0
            ? []
            : EndOfLineSet(root, files).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>What format says about the files <see cref="LoneCrStringFiles"/> found.</summary>
    public static string LoneCrStringLine(IReadOnlyList<string> files) =>
        $"{PartlyWhitespaceHeader} {string.Join(", ", files.Take(5))}{(files.Count > 5 ? $" and {files.Count - 5} more" : string.Empty)}: a string there holds a CR without an LF, "
        + "which the whitespace and style passes of 'dotnet format' would turn into end_of_line's line break, changing the string. They leave "
        + $"{(files.Count == 1 ? "it" : "them")} alone; StyleBro's fixes still run there. Write the CR as \\r in a regular string to have {(files.Count == 1 ? "it" : "them")} formatted.";

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
    /// plain run is enough: no project targets several frameworks. Projects without a target framework (old-style, with
    /// TargetFrameworkVersion) get a run of their own, framework "" (trial 28, EpubReader's WpfDemo: no run, and verify said clean).
    /// </summary>
    public static List<(string Framework, List<string> Projects)> Plan(IReadOnlyDictionary<string, string[]> frameworks)
    {
        if (frameworks.Values.All(f => f.Length <= 1))
        {
            return new List<(string, List<string>)>();
        }

        return frameworks
            .SelectMany(p => p.Value.Length == 0 ? [(Framework: string.Empty, Project: p.Key)] : p.Value.Select(f => (Framework: NormalizeFramework(f), Project: p.Key)))
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
        // .NET Framework written with dots ('net4.7', 'net4.7.2') is 'net47'/'net472' to NuGet: one run, not two.
        var dotted = Regex.Match(framework.Trim().ToLowerInvariant(), @"^net(?<version>[1-4](?:\.\d){1,2})$");
        if (dotted.Success)
        {
            return "net" + dotted.Groups["version"].Value.Replace(".", string.Empty);
        }

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
    /// given order; one group when nothing is shared. With <paramref name="settings"/> (how a project parses a file for the
    /// run's framework, null when unknown), projects that compile a file the same way may share a run: they parse it the
    /// same, so the fixes make the same edits in both copies, which the merge takes (splat: every library links one polyfill
    /// file, 33 runs for net10.0 before, each loading the shared file through the project's references anyway).
    /// </summary>
    public static List<List<string>> Separate(IEnumerable<string> projects, Func<string, IEnumerable<string>> references, Func<string, IEnumerable<string>> compile, Func<string, string, string?>? settings = null)
    {
        bool Same(string owner, string project, string file) =>
            owner.Equals(project, StringComparison.OrdinalIgnoreCase) || (settings?.Invoke(owner, file) is { } a && settings(project, file) is { } b && a == b);

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
                .All(p => compile(p).All(f => !g.Owner.TryGetValue(f, out var owner) || Same(owner, p, f))));
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
    /// The runs of <paramref name="plan"/> without the projects an earlier chosen run already formats the same way: a
    /// project's frameworks with the same <paramref name="key"/> (project, framework; null: unknown, always run) compile the
    /// same code, so one run covers them all. Frameworks with more projects are chosen first; runs left without projects go.
    /// </summary>
    public static List<(string Framework, List<string> Projects)> MergeFrameworks(IReadOnlyList<(string Framework, List<string> Projects)> plan, Func<string, string, string?> key)
    {
        var covered = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var chosen = new HashSet<(string, string)>();
        foreach (var (framework, projects) in plan.OrderByDescending(p => p.Projects.Count).ThenBy(p => p.Framework, StringComparer.Ordinal))
        {
            foreach (var project in projects)
            {
                var keys = covered.TryGetValue(project, out var known) ? known : covered[project] = new HashSet<string>(StringComparer.Ordinal);
                if (key(project, framework) is not { } k || keys.Add(k))
                {
                    chosen.Add((framework, project));
                }
            }
        }

        return plan.Select(p => (p.Framework, p.Projects.Where(x => chosen.Contains((p.Framework, x))).ToList())).Where(p => p.Item2.Count > 0).ToList();
    }

    /// <summary>
    /// The runs to make for the projects 'dotnet format' skipped (<paramref name="skipped"/>: project -> the frameworks of the
    /// runs that skipped it) in a framework <see cref="MergeFrameworks"/> chose for frameworks with the same <paramref name="key"/>
    /// (<paramref name="all"/>: the plan before merging, <paramref name="chosen"/>: after it): another of those frameworks,
    /// one it didn't run (Framework), for the project, with the skipped one it stands in for (Instead). <paramref name="tried"/>
    /// (project, framework) collects what was given, so each is tried once.
    /// </summary>
    public static List<(string Framework, string Project, string Instead)> StandIns(
        IReadOnlyList<(string Framework, List<string> Projects)> all,
        IReadOnlyList<(string Framework, List<string> Projects)> chosen,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> skipped,
        Func<string, string, string?> key,
        ISet<(string Project, string Framework)> tried)
    {
        bool Ran(string project, string framework) => chosen.Any(c => c.Framework.Equals(framework, StringComparison.OrdinalIgnoreCase) && c.Projects.Contains(project, StringComparer.OrdinalIgnoreCase));
        var result = new List<(string, string, string)>();
        foreach (var (project, frameworks) in skipped.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var instead in frameworks.Where(f => Ran(project, f)).Order(StringComparer.OrdinalIgnoreCase))
            {
                if (key(project, instead) is not { } wanted)
                {
                    continue;
                }

                var other = all.Where(a => a.Projects.Contains(project, StringComparer.OrdinalIgnoreCase) && !Ran(project, a.Framework) && key(project, a.Framework) == wanted)
                    .Select(a => a.Framework)
                    .FirstOrDefault(f => tried.Add((project, f)));
                if (other is not null)
                {
                    result.Add((other, project, instead));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// What a framework's references are like, for <see cref="MergeFrameworks"/>: .NET (5 and on, and .NET Core), .NET Standard
    /// or .NET Framework, with its platform ('-windows10.0.17763.0'). Only frameworks of one kind share a run: their APIs
    /// differ less (netstandard2.0 and net472 test the same symbols in CommunityToolkit, but their APIs don't match).
    /// </summary>
    public static string FrameworkFamily(string framework)
    {
        var normalized = NormalizeFramework(framework);
        var dash = normalized.IndexOf('-', StringComparison.Ordinal);
        var name = dash < 0 ? normalized : normalized[..dash];
        var platform = dash < 0 ? string.Empty : normalized[dash..];
        var family = name.StartsWith("netstandard", StringComparison.Ordinal) ? "netstandard"
            : name.StartsWith("netcoreapp", StringComparison.Ordinal) || Regex.IsMatch(name, @"^net\d+\.\d") ? "net"
            : Regex.IsMatch(name, @"^net\d+$") ? "netframework"
            : name;
        return family + platform;
    }

    /// <summary>
    /// Whether a project's file, or a Directory.Build.props/.targets above it in its repository, picks Compile items by
    /// configuration (a Compile item, or an ItemGroup/When around one, with a '$(Configuration)' condition): its files then
    /// differ between configurations even without an '#if' (trial 28: a Release-only file was never formatted or checked).
    /// Up to the repository root (to the drive's without one). ponytail: the text of those files only, not other imports.
    /// </summary>
    public static bool CompileByConfiguration(string projectPath)
    {
        var top = RepositoryRoot(Path.GetDirectoryName(projectPath)!);
        var files = new List<string> { projectPath };
        for (var folder = Path.GetDirectoryName(projectPath); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            files.Add(Path.Combine(folder, "Directory.Build.props"));
            files.Add(Path.Combine(folder, "Directory.Build.targets"));
            if (top is not null && string.Equals(Path.TrimEndingDirectorySeparator(folder), Path.TrimEndingDirectorySeparator(top), StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return files.Any(f => File.Exists(f) && PicksCompileByConfiguration(File.ReadAllText(f)));
    }

    /// <summary>Whether MSBuild text has a Compile item, or an ItemGroup/When around one, with a '$(Configuration)' condition.</summary>
    public static bool PicksCompileByConfiguration(string text) =>
        Regex.IsMatch(text, @"<Compile\b[^>]*\bCondition\s*=\s*""[^""]*\$\(Configuration\)|<(?<tag>ItemGroup|When)\b[^>]*\bCondition\s*=\s*""[^""]*\$\(Configuration\)[^""]*""[^>]*>(?:(?!</\k<tag>>).)*?<Compile\b", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Whether an '#if' symbol is one the SDK defines for a target framework or platform (NET8_0_OR_GREATER, NETSTANDARD,
    /// NETFRAMEWORK, WINDOWS): the same in every configuration, so code that tests only those needs no run of its own per
    /// configuration (<see cref="ConfigurationRuns"/>).
    /// </summary>
    public static bool IsFrameworkSymbol(string symbol) =>
        Regex.IsMatch(symbol, @"^(?:NET|NET\d\w*|NETCOREAPP\w*|NETSTANDARD\w*|NETFRAMEWORK\w*|WINDOWS\w*|ANDROID\w*|IOS\w*|MACCATALYST\w*|MACOS\w*|TVOS\w*|WATCHOS\w*|BROWSER\w*|WASI\w*|TIZEN\w*)$");

    /// <summary>
    /// The configurations a solution builds (.sln: its SolutionConfigurationPlatforms; .slnx: its BuildType elements, else
    /// the default Debug and Release; a solution filter: its solution's), or null for a project or a file it can't read.
    /// </summary>
    public static List<string>? SolutionConfigurations(string workspacePath)
    {
        var path = IsSolutionFilter(workspacePath) ? ReadSolutionFilter(workspacePath).Solution : workspacePath;
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        // A platform other than Any CPU makes the pair a configuration of its own ('Release|x64'): a project's
        // '$(Configuration)|$(Platform)' conditions (PalCalc's DefineConstants) see it.
        static string Pair(string name, string platform) => IsAnyPlatform(platform) ? name : $"{name}|{platform}";
        var text = File.ReadAllText(path);
        if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var section = Regex.Match(text, @"GlobalSection\(SolutionConfigurationPlatforms\)[^\r\n]*(?<body>.*?)EndGlobalSection", RegexOptions.Singleline);
            return section.Success
                ? Regex.Matches(section.Groups["body"].Value, @"^\s*(?<name>[^|=\r\n]+?)\s*\|\s*(?<platform>[^=\r\n]*?)\s*=", RegexOptions.Multiline).Select(m => Pair(m.Groups["name"].Value, m.Groups["platform"].Value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : null;
        }

        if (path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            var types = Regex.Matches(text, @"<BuildType\s+Name\s*=\s*""(?<name>[^""]+)""").Select(m => m.Groups["name"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var platforms = Regex.Matches(text, @"<Platform\s+Name\s*=\s*""(?<name>[^""]+)""").Select(m => m.Groups["name"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return (types.Count > 0 ? types : ["Debug", "Release"]).SelectMany(t => platforms.Count > 0 ? platforms.Select(p => Pair(t, p)) : [t]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        return null;
    }

    /// <summary>A configuration as <see cref="SolutionConfigurations"/> names it ('Release', 'Release|x64'): its name and platform (null: the project's own).</summary>
    public static (string Configuration, string? Platform) SplitConfiguration(string configuration) =>
        configuration.IndexOf('|') is var at and >= 0 ? (configuration.Substring(0, at), configuration.Substring(at + 1)) : (configuration, null);

    /// <summary>
    /// A project's configurations besides its default (<paramref name="configuration"/>, Debug unless set): its Configurations
    /// property (the SDK's Debug;Release), only those the solution builds when it says (<paramref name="solution"/>), with the
    /// solution's platforms ('Release|x64', also 'Debug|x64' beside the default: a platform-only solution), at most four
    /// ('within reason': a configuration costs a 'dotnet format' run where the code differs). A solution platform the project's
    /// <paramref name="platforms"/> (its Platforms property, the SDK's AnyCPU) doesn't list builds it in its own platform: the
    /// solution maps it (Mediator's x86 and x64 build AnyCPU projects, which failed with Platform=x86).
    /// </summary>
    public static List<string> OtherConfigurations(string configurations, string configuration, IReadOnlyCollection<string>? solution, string platforms = "")
    {
        var own = configuration.Trim().Length > 0 ? configuration.Trim() : "Debug";
        var projectPlatforms = (platforms.Trim().Length > 0 ? platforms : "AnyCPU").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string Mapped(string c) => SplitConfiguration(c) is (var name, { } platform) && !projectPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase) ? name : c;
        return (configurations.Trim().Length > 0 ? configurations : "Debug;Release")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(c => solution is null ? [c] : solution.Where(s => SplitConfiguration(s).Configuration.Equals(c, StringComparison.OrdinalIgnoreCase)).Select(Mapped))
            .Where(c => !c.Equals(own, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => SplitConfiguration(c).Platform is not null)
            .Take(4)
            .ToList();
    }

    /// <summary>
    /// The runs for the configurations besides each project's default (owner's decision 2026-10-16, "Configurations like
    /// frameworks": format only ran Debug, so '#if !DEBUG' code was never fixed or checked while CI built Release with
    /// warnings as errors). For each configuration and each of <paramref name="runs"/> (the frameworks before
    /// <see cref="MergeFrameworks"/>, or one run without a framework), the projects whose code parses differently there than
    /// in all their default runs and in the configuration runs chosen before: <paramref name="key"/> (project, framework,
    /// configuration, "" the default) says how its files parse, null when unknown (no run). <paramref name="configurations"/>
    /// gives each project's other configurations, none for a project whose code tests only framework symbols.
    /// </summary>
    public static List<(string Framework, string Configuration, List<string> Projects)> ConfigurationRuns(
        IReadOnlyList<(string Framework, List<string> Projects)> runs,
        Func<string, IReadOnlyList<string>> configurations,
        Func<string, string, string, string?> key)
    {
        var covered = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> Covered(string project) => covered.TryGetValue(project, out var known)
            ? known
            : covered[project] = runs.Where(r => r.Projects.Contains(project, StringComparer.OrdinalIgnoreCase)).Select(r => key(project, r.Framework, string.Empty)).OfType<string>().ToHashSet(StringComparer.Ordinal);

        var result = new List<(string, string, List<string>)>();
        var all = runs.SelectMany(r => r.Projects).Distinct(StringComparer.OrdinalIgnoreCase).SelectMany(configurations).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var configuration in all)
        {
            foreach (var (framework, projects) in runs)
            {
                var selected = projects.Where(p => configurations(p).Contains(configuration, StringComparer.OrdinalIgnoreCase) && key(p, framework, configuration) is { } k && Covered(p).Add(k)).ToList();
                if (selected.Count > 0)
                {
                    result.Add((framework, configuration, selected));
                }
            }
        }

        return result;
    }

    /// <summary>What format says about the runs <see cref="ConfigurationRuns"/> added (configuration, projects), or null for none.</summary>
    public static string? ConfigurationsLine(IReadOnlyList<(string Framework, string Configuration, List<string> Projects)> runs)
    {
        if (runs.Count == 0)
        {
            return null;
        }

        var parts = runs.GroupBy(r => r.Configuration, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.SelectMany(r => r.Projects).Select(p => Path.GetFileNameWithoutExtension(p)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList() is var names
                ? $"{g.Key} ({string.Join(", ", names.Take(4))}{(names.Count > 4 ? $" and {names.Count - 4} more" : string.Empty)})"
                : string.Empty);
        return $"{ConfigurationsHeader} runs for {string.Join(", ", parts)} too: their '#if' code (DEBUG, RELEASE, ...) compiles differently there, and the build of every configuration gets the same fixes and checks.";
    }

    /// <summary>
    /// The identifiers a C# file's '#if' and '#elif' lines test (comments aside), once per file and process: what
    /// <see cref="CompileView.Key"/> compares. None for a package's file (<see cref="IsPackageFile"/>), which 'format' never
    /// changes: its '#if DEBUG' (Nullable's, PolyShim's) made runs for Release and parts for shared files, and only where the
    /// restore had put the package's files into the build (trial 27).
    /// </summary>
    public static string[] TestedSymbols(string file) => TestedSymbolsCache.GetOrAdd(Path.GetFullPath(file), path => File.Exists(path) && !IsPackageFile(path) ? TestedIn(File.ReadAllText(path)) : []);

    /// <summary>Whether a file is in a NuGet package's folder (any packages folder: a folder above it has the package's '.nupkg.metadata').</summary>
    public static bool IsPackageFile(string file)
    {
        for (var folder = Path.GetDirectoryName(Path.GetFullPath(file)); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            if (PackageFolders.GetOrAdd(folder, f => File.Exists(Path.Combine(f, ".nupkg.metadata"))))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The identifiers the '#if' and '#elif' lines of a C# text test (<see cref="TestedSymbols"/>).</summary>
    public static string[] TestedIn(string text) =>
        Regex.Matches(text, @"^[ \t]*#[ \t]*(?:el)?if\b(?<condition>[^\r\n]*)", RegexOptions.Multiline)
            .SelectMany(m => Regex.Matches(Regex.Replace(m.Groups["condition"].Value, "//.*$", string.Empty), @"[A-Za-z_]\w*").Select(i => i.Value))
            .Where(i => i is not ("true" or "false"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// The C# files under the root (relative, '/'-separated) the SDK's formatter breaks: a multi-line string holds a line
    /// starting with '#' ('#pragma warning disable' in a source generator's test input) inside an '#if' branch that some
    /// build leaves out. There the lexer reads that line as a directive, and the formatter moves directives to column 0, so
    /// the build that compiles the branch gets a broken string (CommunityToolkit: 19 CS8999 errors; a Roslyn bug).
    /// </summary>
    public static List<string> DirectiveStringFiles(string root) =>
        StyleCopSetup.EnumerateFiles(root)
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .Where(f => DirectiveStringOffsets(File.ReadAllText(f)).Count > 0)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Where a C# text has a line in a multi-line string that another set of '#if' symbols reads as a directive
    /// (<see cref="DirectiveStringFiles"/>): the offsets of those lines' '#'. A line's '#' counts when one parse has it
    /// starting a line of a multi-line string and another reads a directive there (any directive: a string's '#if DEBUG'
    /// too). The parses cover every branch: each combination of the symbols the file tests (trial 23: a string in an
    /// '#elif' branch, or under 'NET8_0_OR_GREATER &amp;&amp; !NET10_0_OR_GREATER', is compiled by neither "no symbol" nor
    /// "every symbol", which were the only two parses before).
    /// </summary>
    public static List<int> DirectiveStringOffsets(string text)
    {
        if (!text.Contains("#if", StringComparison.Ordinal)
            || !(text.Contains("\"\"\"", StringComparison.Ordinal) || text.Contains("@\"", StringComparison.Ordinal) || text.Contains("@$\"", StringComparison.Ordinal) || text.Contains("$@\"", StringComparison.Ordinal)))
        {
            return [];
        }

        // ponytail: every subset up to 8 symbols (256 parses of a rare file); beyond that each symbol alone and all but
        // each one, which misses a branch that needs two symbols set and two unset. Evaluate the conditions if that matters.
        var tested = TestedIn(text);
        var sets = tested.Length <= 8
            ? Enumerable.Range(0, 1 << tested.Length).Select(mask => tested.Where((_, i) => (mask & (1 << i)) != 0).ToArray())
            : new[] { Array.Empty<string>(), tested }.Concat(tested.Select(s => new[] { s })).Concat(tested.Select(s => tested.Where(o => o != s).ToArray()));
        var strings = new HashSet<int>();
        var directives = new HashSet<int>();
        foreach (var symbols in sets)
        {
            var root = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(text, new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview, preprocessorSymbols: symbols)).GetRoot();

            // Every '#' in a token: where another parse has a directive's '#', it starts a line, so the token is a multi-line
            // string. Not only after a line break in the token: in '$"""' + line break + '#if X', the break is the start token's
            // (trial 24: CS8999 after the first line moved).
            foreach (var token in root.DescendantTokens().Where(t => t.Text.Contains('#', StringComparison.Ordinal)))
            {
                for (var at = token.Text.IndexOf('#', StringComparison.Ordinal); at >= 0; at = token.Text.IndexOf('#', at + 1))
                {
                    strings.Add(token.SpanStart + at);
                }
            }

            directives.UnionWith(root.DescendantTrivia(descendIntoTrivia: true).Where(t => t.IsDirective).Select(t => text.IndexOf('#', t.Span.Start)));
        }

        return strings.Intersect(directives).Where(o => o >= 0).Order().ToList();
    }

    /// <summary>What format says about the files <see cref="DirectiveStringFiles"/> found (<see cref="DirectiveMask"/>).</summary>
    public static string DirectiveStringLine(IReadOnlyList<string> files) =>
        $"{ProtectedHeader} {string.Join(", ", files.Take(5))}{(files.Count > 5 ? $" and {files.Count - 5} more" : string.Empty)}: "
        + "a multi-line string there has lines starting with '#' inside an '#if' branch some build leaves out, which the SDK's formatter would move to column 0 "
        + "as if they were directives, breaking the string where the branch is compiled (CS8999, a Roslyn bug). Their '#' is swapped for another character while 'dotnet format' runs, then put back.";

    /// <summary>What format and verify say when <see cref="DirectiveMask.Damaged"/> finds files.</summary>
    public static string DamagedLine(IReadOnlyList<string> files) =>
        $"Stopped: {string.Join(", ", files.Take(5))}{(files.Count > 5 ? $" and {files.Count - 5} more" : string.Empty)} "
        + "have a line starting with a private-use character where a directive's '#' would be, the character 'format' swaps the '#' of protected lines for while it runs, "
        + "but no record says a run did (a run killed and its record lost, or an edit by hand). Put the '#' back, then run again.";

    /// <summary>
    /// <see cref="CompileSettings"/>'s output read, or null: the defined symbols, the language version (<see cref="EffectiveLangVersion"/>),
    /// the nullable context and the compiled files.
    /// </summary>
    public static CompileView? ReadView(string? json)
    {
        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            string Property(string name) => document.RootElement.TryGetProperty("Properties", out var properties) && properties.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
            var compile = document.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty("Compile", out var found)
                ? found.EnumerateArray().Select(c => c.GetProperty("FullPath").GetString()!).ToArray()
                : [];
            return new CompileView(
                Property("DefineConstants").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.Ordinal),
                EffectiveLangVersion(Property("LangVersion"), Property("NETCoreSdkVersion")),
                Property("Nullable").Trim().ToLowerInvariant(),
                compile);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The C# version a LangVersion value means: 'latest', 'latestMajor' and 'default' are the SDK's newest (its major
    /// version + 4: SDK 10 has C# 14), '12' is '12.0'; 'preview' and the rest stay. An unset LangVersion is already the
    /// framework's version after evaluation. Without it, 'latest' and '14.0' looked like different settings (b5a).
    /// </summary>
    public static string EffectiveLangVersion(string langVersion, string sdkVersion)
    {
        var value = langVersion.Trim().ToLowerInvariant();
        if (value is "latest" or "latestmajor" or "default" && int.TryParse(sdkVersion.Split('.')[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var sdk))
        {
            return $"{sdk + 4}.0";
        }

        return Regex.IsMatch(value, @"^\d+$") ? value + ".0" : value;
    }

    /// <summary>
    /// What format says about the frameworks it runs (<paramref name="all"/>) and those <see cref="MergeFrameworks"/> found
    /// compile the same code as another's (<paramref name="merged"/>: the runs left).
    /// </summary>
    public static string MultiTargetedLine(IReadOnlyList<(string Framework, List<string> Projects)> all, IReadOnlyList<(string Framework, List<string> Projects)> merged)
    {
        var line = $"{MultiTargetedHeader}one 'dotnet format' run per target framework ({string.Join(", ", merged.Select(p => p.Framework.Length > 0 ? p.Framework : NoFrameworkLabel))}).";
        var gone = all.Select(p => p.Framework).Except(merged.Select(p => p.Framework), StringComparer.OrdinalIgnoreCase).ToList();
        var fewer = all.Where(p => merged.Any(m => m.Framework == p.Framework && m.Projects.Count < p.Projects.Count)).Select(p => p.Framework).ToList();
        if (gone.Count > 0)
        {
            line += $" {string.Join(", ", gone)}: no run of {(gone.Count == 1 ? "its" : "their")} own, the code compiles the same as for another framework of the same kind (the same '#if' symbols tested, files and settings).";
        }

        if (fewer.Count > 0)
        {
            line += $" {string.Join(", ", fewer)}: only the projects whose code compiles differently there.";
        }

        return line;
    }

    /// <summary>
    /// What format says about the runs split because projects compile a file another project compiles with other '#if' symbols
    /// or settings: each framework's parts, by project name (the same parts said once), or null when no run was split.
    /// </summary>
    public static string? SharedFilesLine(IReadOnlyList<List<List<string>>> partitions)
    {
        static string Names(List<string> part) => part.Count > 4 ? $"{string.Join(", ", part.Take(4))} and {part.Count - 4} more" : string.Join(", ", part);
        var described = partitions.Select(parts => string.Join(" | ", parts.Select(Names))).Distinct(StringComparer.Ordinal).ToList();
        return described.Count == 0
            ? null
            : $"{SharedFilesHeader} these projects compile the same files with other '#if' symbols or settings, so each group gets a run of its own (loaded together, 'dotnet format' crashes: {LinkedMergeIssue}): {string.Join("; ", described)}.";
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

    /// <summary>
    /// The C# projects the workspace doesn't list but its projects reference, directly or through each other (trial 29: Moq's
    /// Issue1648Harness isn't in Moq.sln, Moq.Tests references it, and the build compiled and reported it while no run
    /// formatted it), each given to ONE run that loads it (several runs may check at once): the first default-configuration
    /// run that reaches it and has its framework, else the run without a framework. A multi-targeted one, or one no such run
    /// loads, goes to <paramref name="leftOut"/>: formatting it in a framework's run would merge its copies (conflict markers).
    /// <paramref name="known"/>: the workspace's projects by full path; <paramref name="read"/> evaluates any other.
    /// </summary>
    public static Dictionary<int, List<string>> OutsideProjectRuns(
        IReadOnlyList<(string Framework, string Configuration, IReadOnlyList<string> Projects)> runs,
        IReadOnlyDictionary<string, (string[] Frameworks, string[] References)> known,
        Func<string, (string[] Frameworks, string[] References)> read,
        string root,
        out List<string> leftOut)
    {
        var outside = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>(known.Values.SelectMany(p => p.References));
        while (pending.Count > 0)
        {
            var reference = pending.Dequeue();
            if (known.ContainsKey(reference) || outside.ContainsKey(reference) || !reference.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || Path.GetRelativePath(root, reference).StartsWith("..", StringComparison.Ordinal) || !File.Exists(reference))
            {
                continue;
            }

            outside[reference] = read(reference);
            foreach (var next in outside[reference].References)
            {
                pending.Enqueue(next);
            }
        }

        // Which projects each one reaches through its references.
        var reach = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> Reach(string project)
        {
            if (reach.TryGetValue(project, out var found))
            {
                return found;
            }

            var result = reach[project] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var references = known.TryGetValue(project, out var listed) ? listed.References : outside.TryGetValue(project, out var other) ? other.References : [];
            foreach (var reference in references)
            {
                result.Add(reference);
                result.UnionWith(Reach(reference));
            }

            return result;
        }

        var assigned = new Dictionary<int, List<string>>();
        leftOut = [];
        foreach (var (project, (frameworks, _)) in outside.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            var loading = Enumerable.Range(0, runs.Count)
                .Where(i => runs[i].Configuration.Length == 0 && runs[i].Projects.Any(p => Reach(p).Contains(project)))
                .ToList();
            var own = frameworks.Length == 1 ? NormalizeFramework(frameworks[0]) : null;
            var index = own is null ? -1 : loading.Where(i => runs[i].Framework == own).Append(-1).First();
            index = index >= 0 || own is null ? index : loading.Where(i => runs[i].Framework.Length == 0).Append(-1).First();
            if (index < 0)
            {
                leftOut.Add(project);
                continue;
            }

            (assigned.TryGetValue(index, out var list) ? list : assigned[index] = []).Add(project);
        }

        return assigned;
    }

    /// <summary>
    /// The folder a run on <paramref name="workspace"/> (a solution, filter, Traversal project or project) works from: the
    /// deepest one holding it and every project it lists. A project is its own folder (what it references gets no run).
    /// </summary>
    public static string WorkingRoot(string workspace)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(workspace))!;
        if (workspace.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return folder;
        }

        // ponytail: the project paths are read from the text (quoted '*.csproj'), not evaluated; that's every format the
        // workspace can have (.sln, .slnx, a Traversal project's Include="...") and a filter's JSON.
        var (solution, projects) = IsSolutionFilter(workspace)
            ? (SolutionFilterError(workspace) is null ? ReadSolutionFilter(workspace) : (null, []))
            : (workspace, Regex.Matches(File.ReadAllText(workspace), "\"([^\"]+\\.csproj)\"", RegexOptions.IgnoreCase).Select(m => m.Groups[1].Value).ToArray());
        var listedFrom = Path.GetDirectoryName(Path.GetFullPath(solution ?? workspace))!;
        foreach (var project in projects)
        {
            var projectFolder = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(listedFrom, project.Replace('\\', Path.DirectorySeparatorChar))))!;
            while (Path.GetRelativePath(folder, projectFolder) is var relative && (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
                && Path.GetDirectoryName(folder) is { } parent)
            {
                folder = parent;
            }
        }

        return folder;
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
    /// '..\Orleans.CodeGenerator.Shared\*.cs' into three projects. Also its restore output (project.assets.json, "" when not asked)
    /// and the project references it takes as analyzers (in-solution source generators, <see cref="CopyAnalyzerOutputs"/>).
    /// </summary>
    public static (string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers) ParseProject(string msbuildJson, string? projectDirectory = null)
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

        // Project references whose output is an analyzer (an in-solution source generator: 'OutputItemType="Analyzer"').
        var analyzers = json.RootElement.TryGetProperty("Items", out var all) && all.TryGetProperty("ProjectReference", out var projectReferences)
            ? projectReferences.EnumerateArray()
                .Where(r => r.TryGetProperty("OutputItemType", out var type) && string.Equals(type.GetString(), "Analyzer", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.GetProperty("FullPath").GetString()!)
                .ToArray()
            : Array.Empty<string>();
        return (frameworks, FullPaths("ProjectReference"), linked, FullPaths("Compile"), assets, analyzers);
    }

    /// <summary>
    /// The files a project compiles for a run's framework: the outer evaluation's (<paramref name="outer"/>), and a
    /// multi-targeted project's for that framework (<paramref name="framework"/>, null when unknown or single-targeted).
    /// </summary>
    public static string[] FrameworkCompile(string[] outer, string[]? framework) =>
        framework is null ? outer : outer.Union(framework, StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>
    /// The files a project compiles from outside its folder for a run: the outer evaluation's (<paramref name="linked"/>) and
    /// those of <paramref name="compile"/> (<see cref="FrameworkCompile"/>) outside it: refit links src/Polyfills/*.cs for net4*
    /// only, which the outer evaluation doesn't have, so they were never formatted while the build failed on them.
    /// </summary>
    public static List<string> Linked(string projectPath, IEnumerable<string> linked, IEnumerable<string> compile)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(projectPath))! + Path.DirectorySeparatorChar;
        return linked.Union(compile.Where(f => !Path.GetFullPath(f).StartsWith(folder, StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase).ToList();
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
            .SelectMany(f => RepositoryNames.Lines(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f).GetRoot(), f).ToList())
            .ToList();

        // Visual Basic files' names too: format leaves them alone, so a C# member they use keeps its name. Not as the file's
        // own path: a VB project a C# project references is loaded, and its files would count as seen by the run (c13: the
        // rename went through and was put back in the .vb file, which then didn't build).
        // ponytail: as written there; VB ignores case, so 'Base_Rate' for a C# 'base_rate' isn't matched.
        var visualBasic = StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
            .Select(f => RepositoryNames.IdentifierLine(f + " (Visual Basic, left alone)", Regex.Matches(File.ReadAllText(f), @"[A-Za-z_]\w*").Select(m => m.Value)));
        File.WriteAllLines(file, lines.Concat(visualBasic));
    }

    /// <summary>
    /// The Visual Basic projects a run would load (file names): the ones the solution (or filter) lists, and those the C#
    /// projects reference (<paramref name="references"/>, full paths).
    /// </summary>
    public static List<string> VisualBasicProjects(string workspacePath, IEnumerable<string> references)
    {
        var listed = workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || !File.Exists(workspacePath)
            ? Enumerable.Empty<string>()
            : Regex.Matches(File.ReadAllText(workspacePath), @"[^""\\/]+\.vbproj\b", RegexOptions.IgnoreCase).Select(m => m.Value.Trim());
        return listed.Concat(references.Where(r => r.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)).Select(Path.GetFileName).OfType<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    /// <summary>
    /// How to format the projects in <see cref="NotBuiltFolders"/> (<paramref name="folders"/>) on their own: a build of one
    /// still runs StyleBro when a Directory.Build.props above it references it (AngleSharp's build/_build.csproj kept ~25 warnings).
    /// </summary>
    /// <summary>
    /// The workload packs a project's evaluation (<see cref="ProjectQuery"/>) says aren't installed: the SDK's
    /// MissingWorkloadPack items, which make 'dotnet restore' and the build fail with NETSDK1147. Empty when none are missing.
    /// </summary>
    public static string[] MissingWorkloadPacks(string msbuildJson)
    {
        using var json = JsonDocument.Parse(msbuildJson);
        return json.RootElement.TryGetProperty("Items", out var items) && items.TryGetProperty("MissingWorkloadPack", out var packs)
            ? packs.EnumerateArray().Select(p => p.GetProperty("Identity").GetString()!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : [];
    }

    /// <summary>The note for the projects <see cref="MissingWorkloadPacks"/> leaves out.</summary>
    public static string MissingWorkloadsLine(IReadOnlyList<string> projects, IReadOnlyList<string> packs) =>
        $"{UnformattedHeader} {string.Join(", ", projects)}: {(projects.Count == 1 ? "it needs" : "they need")} workloads this machine doesn't have ({string.Join(", ", packs.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))}), so 'dotnet restore' fails on {(projects.Count == 1 ? "it" : "them")} (NETSDK1147) and 'format' leaves {(projects.Count == 1 ? "it" : "them")} alone. Run 'dotnet workload restore' and then 'bro format' again to format {(projects.Count == 1 ? "it" : "them")} too.";

    public static string NotBuiltHint(IReadOnlyList<string> folders, string root)
    {
        var projects = folders.SelectMany(f => Directory.EnumerateFiles(Path.Combine(root, f), "*.csproj"))
            .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return projects.Count == 0
            ? string.Empty
            : $" Where a Directory.Build.props above brings StyleBro into {(projects.Count == 1 ? "its build" : "their builds")}, format {(projects.Count == 1 ? "it" : "each")} on {(projects.Count == 1 ? "its" : "their")} own: {string.Join(", ", projects.Select(p => $"'bro format {p}'"))}.";
    }

    /// <summary>Each project of a solution (or the project itself) with its evaluated target frameworks, for init's count.</summary>
    public static IEnumerable<(string FullPath, string[] Frameworks)> EvaluatedProjects(string workspacePath) =>
        ReadProjects(workspacePath).Values.Select(p => (p.FullPath, p.Frameworks));

    /// <summary>
    /// The folders (relative to <paramref name="root"/>, '/'-separated with a trailing '/', as '--exclude' takes them) of the
    /// projects a .sln lists but no solution configuration builds (no '.Build.0' line for the project: Bogus's Builder), or a
    /// .slnx with '&lt;Build Project="false" /&gt;', when no other of its <paramref name="projects"/> (full paths) is in that
    /// folder. Other workspaces: none.
    /// </summary>
    public static List<string> NotBuiltFolders(string solutionPath, IReadOnlyList<string> projects, string root)
    {
        var slnx = solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
        if ((!slnx && !solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)) || !File.Exists(solutionPath))
        {
            return [];
        }

        // A .slnx says it with '<Build Project="false" />' (without 'Solution': for every configuration; trial 27, io-abstractions'
        // Pipeline/Build.csproj: "no StyleBro.Analyzers in its restore output").
        var text = File.ReadAllText(solutionPath);
        var notBuilt = slnx
            ? Regex.Matches(text, @"<Project\s+Path\s*=\s*""(?<path>[^""]+\.csproj)""[^>]*(?<!/)>(?<body>(?:(?!</Project>).)*)</Project>", RegexOptions.Singleline)
                .Where(m => Regex.IsMatch(m.Groups["body"].Value, @"<Build\s+Project\s*=\s*""false""\s*/>", RegexOptions.IgnoreCase))
                .Select(m => m.Groups["path"].Value)
            : Regex.Matches(text, @"^Project\(""[^""]*""\)\s*=\s*""[^""]*""\s*,\s*""(?<path>[^""]+\.csproj)""\s*,\s*""(?<guid>\{[^}]+\})""", RegexOptions.Multiline)
                .Where(m => Regex.Escape(m.Groups["guid"].Value) is var guid
                    && Regex.IsMatch(text, guid + @"\.[^\r\n]*\.ActiveCfg\s*=", RegexOptions.IgnoreCase) && !Regex.IsMatch(text, guid + @"\.[^\r\n]*\.Build\.0\s*=", RegexOptions.IgnoreCase))
                .Select(m => m.Groups["path"].Value);
        var folders = new List<string>();
        foreach (var path in notBuilt)
        {
            var full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionPath)!, path.Replace('\\', Path.DirectorySeparatorChar)));
            var folder = Path.GetDirectoryName(full)!;
            var relative = Path.GetRelativePath(root, folder).Replace('\\', '/');
            if (relative != "." && !relative.StartsWith("..", StringComparison.Ordinal)
                && !projects.Any(p => !string.Equals(p, full, StringComparison.OrdinalIgnoreCase) && p.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                folders.Add(relative + "/");
            }
        }

        return folders.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// What to say after runs that changed <paramref name="changed"/> (full paths): the generated files checked in next to
    /// them (<see cref="CheckedInGenerated"/>), and that CSharpier comes next where it formats the repository (Altinn: 12
    /// files failed 'csharpier check' after format).
    /// </summary>
    public static List<string> AfterNotes(string root, IReadOnlyCollection<string> changed, int csharpierFormatted = 0)
    {
        var notes = new List<string>();
        if (changed.Count == 0)
        {
            return notes;
        }

        var generated = CheckedInGenerated(root, changed);
        notes.AddRange(generated.Take(10).Select(f => $"{f} is generated and checked in: rebuild and commit it."));
        if (generated.Count > 10)
        {
            notes.Add($"... and {generated.Count - 10} more generated files checked in: rebuild and commit them.");
        }

        if (CheckedInGenerated(root, changed, otherProjects: true) is { Count: > 0 } others)
        {
            notes.Add($"Other projects have generated files checked in ({string.Join(", ", others.Take(3))}{(others.Count > 3 ? $" and {others.Count - 3} more" : string.Empty)}): a build may regenerate them from the changed code too, so rebuild and check.");
        }

        if (csharpierFormatted > 0)
        {
            notes.Add($"CSharpier formats this repository: 'format' ran it on the {csharpierFormatted} C# file{(csharpierFormatted == 1 ? string.Empty : "s")} it changed, so it has the last word on their layout.");
        }
        else if (Conventions.CSharpier(root) is { } evidence)
        {
            notes.Add($"CSharpier formats this repository ({evidence}): run it now (e.g. 'dotnet csharpier format .'), it has the last word on layout.");
        }

        return notes;
    }

    /// <summary>
    /// The generated files git tracks (relative to <paramref name="root"/>, '/'-separated) in the projects of the changed files:
    /// '*.g.cs', '*.g.i.cs', '*.Designer.cs', a T4 template's output (a '.tt' of the same name next to it), or a file with an
    /// '&lt;auto-generated' header. A build regenerates them from the code format changed (MoreLINQ's Extensions.g.cs from the
    /// doc comments), and a CI's 'git diff --exit-code' failed. Format leaves them alone; the note says to rebuild.
    /// </summary>
    /// <remarks>
    /// <paramref name="otherProjects"/>: those in the other projects instead (gencheck2: Lib2's build regenerates Docs2.g.cs from
    /// Lib's sources, which no project reference shows), for a note that they may change too.
    /// </remarks>
    public static List<string> CheckedInGenerated(string root, IEnumerable<string> changed, bool otherProjects = false)
    {
        var repository = RepositoryRoot(root);
        if (repository is null || PreviewCommand.Git(repository, "ls-files", "-z", "--", "*.cs") is not (0, var list))
        {
            return [];
        }

        var projects = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? ProjectOf(string file)
        {
            var folder = Path.GetDirectoryName(file)!;
            return projects.TryGetValue(folder, out var known) ? known : projects[folder] = NearestProject(folder, repository);
        }

        var changedSet = new HashSet<string>(changed.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        var changedProjects = new HashSet<string>(changedSet.Select(ProjectOf).OfType<string>(), StringComparer.OrdinalIgnoreCase);

        // A file its evaluated project doesn't compile isn't rebuilt here either (Splat's Resource.designer.cs: Android only,
        // a framework this machine didn't build). ponytail: the outer and first framework's Compile items (ReadProjects).
        Dictionary<string, HashSet<string>> compiled;
        lock (ProjectCache)
        {
            compiled = ProjectCache.Values.SelectMany(d => d.Values).Where(p => p.Compile.Length > 0)
                .GroupBy(p => Path.GetFullPath(p.FullPath), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => new HashSet<string>(g.SelectMany(p => p.Compile).Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }

        bool NotCompiled(string file, string project) => compiled.TryGetValue(Path.GetFullPath(project), out var files) && !files.Contains(file);

        // The MSBuild files and the build scripts (MoreLINQ's build.cmd writes Extensions.g.cs; a CI workflow may too).
        string[] scripts = [".cmd", ".bat", ".sh", ".ps1", ".yml", ".yaml"];
        var build = string.Join("\n", StyleCopSetup.EnumerateFiles(repository).Where(f => StyleCopSetup.IsMSBuild(f) || scripts.Any(s => f.EndsWith(s, StringComparison.OrdinalIgnoreCase))).Select(File.ReadAllText));
        return list.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => Path.GetFullPath(Path.Combine(repository, f)))
            .Where(f => !changedSet.Contains(f) && File.Exists(f) && ProjectOf(f) is { } project && changedProjects.Contains(project) != otherProjects && !NotCompiled(f, project) && IsGenerated(f, build))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Whether a C# file is generated by the build (see <see cref="CheckedInGenerated"/>), given the text of the repository's
    /// MSBuild files (<paramref name="build"/>): not a Visual Studio custom tool's output ('&lt;LastGenOutput&gt;', a designer),
    /// a T4 template's only when the build runs templates (TransformOnBuild, TextTransform; CommunityToolkit's Guard.*.g.cs
    /// come from Visual Studio), and a '.g.cs' or a file with just an '&lt;auto-generated' header only when a build file or script names it (kafkaflow's
    /// samples come from avrogen, which nothing runs).
    /// </summary>
    public static bool IsGenerated(string file, string build = "")
    {
        var name = Path.GetFileName(file);

        // A resource or WinForms designer's file is written by the designer, never by a build (SSH.NET's Resources.Designer.cs).
        if ((name.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) && DesignerWritten(file))
            || Regex.IsMatch(build, $@"<LastGenOutput>\s*{Regex.Escape(name)}\s*<", RegexOptions.IgnoreCase))
        {
            return false;
        }

        var folder = Path.GetDirectoryName(file)!;
        var stems = new[] { name.Split('.')[0], Path.GetFileNameWithoutExtension(name), name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ? name[..^".g.cs".Length] : null }.OfType<string>();

        // A template's own output only (trial 29, Moq: 'ReturnsExtensions.tt' writes 'ReturnsExtensions.Generated.cs', and the
        // 'ReturnsExtensions.cs' beside it is hand-written).
        if (stems.Any(s => File.Exists(Path.Combine(folder, s + ".tt")) && string.Equals(Migration.TextTemplateOutput(Path.Combine(folder, s + ".tt")), file, StringComparison.OrdinalIgnoreCase))
            || stems.Any(s => File.Exists(Path.Combine(folder, s + ".ttinclude"))))
        {
            return Regex.IsMatch(build, @"TransformOnBuild\s*>\s*true|TextTransform|dotnet[- ]t4", RegexOptions.IgnoreCase);
        }

        if (name.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // A source generator's output the build writes into the repository (EmitCompilerGeneratedFiles, a
        // CompilerGeneratedFilesOutputPath there): in that folder, the build rewrites it.
        if (Regex.IsMatch(build, @"<EmitCompilerGeneratedFiles>\s*true\s*<", RegexOptions.IgnoreCase))
        {
            var emitted = Regex.Matches(build, "<CompilerGeneratedFilesOutputPath>(?<path>[^<]*)<", RegexOptions.IgnoreCase)
                .Select(m => Regex.Replace(m.Groups["path"].Value, @"\$\([^)]*\)", string.Empty).Trim().TrimEnd('/', '\\'))
                .Select(p => p[(p.LastIndexOfAny(['/', '\\']) + 1)..])
                .Where(p => p.Length > 0)
                .DefaultIfEmpty("generated");
            var inRepository = RepositoryRoot(folder) is { } repository ? Path.GetRelativePath(repository, file) : name;
            if (inRepository.Split(Path.DirectorySeparatorChar, '/').SkipLast(1).Any(s => emitted.Contains(s, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        // A '.g.cs' or a file with an '<auto-generated' header only when a build step names it: MimeKit's *.g.cs come from a
        // tool in tools/ someone runs by hand (its csproj names them in Compile items only), trial 25.
        // ponytail: "names it" is any line of a build file or script with the name that isn't an item's (a target's output, an Exec).
        return (name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || File.ReadLines(file).Take(10).Any(l => l.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase)))
            && build.Split('\n').Any(l => l.Contains(name, StringComparison.OrdinalIgnoreCase) && !Regex.IsMatch(l, @"<(Compile|None|Content|EmbeddedResource|DependentUpon)\b", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// The files of the finding lines in a check's output, relative to <paramref name="root"/> ('/'-separated, for
    /// '--include'), or null when there are none or one isn't below the root.
    /// </summary>
    public static List<string>? FindingFiles(IEnumerable<string> lines, string root)
    {
        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Where(IsFindingLine))
        {
            var path = Regex.Match(line, @"^\s*(?<path>.+?)\(\d+,\d+\):").Groups["path"].Value;
            var relative = Path.IsPathRooted(path) ? Path.GetRelativePath(root, path) : path;
            if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
            {
                return null;
            }

            files.Add(relative.Replace('\\', '/'));
        }

        return files.Count == 0 ? null : files.ToList();
    }

    /// <summary>
    /// Records the findings a clean 'format' run on <paramref name="root"/> kept, for a '--verify-no-changes' on the same
    /// state (<see cref="StateKey"/>): it needn't format a copy to find them (in the temporary folder; a courtesy, never an error).
    /// When that run's check of every plan entry found nothing but them (<paramref name="verdict"/>: the lines verify ends with
    /// then, the generator <paramref name="builds"/> it makes after it, and the line it says about them, <paramref name="generatorLine"/>),
    /// verify answers from the record (<see cref="VerifyFromRecord"/>).
    /// </summary>
    public static void SaveKept(string root, IEnumerable<string> args, IEnumerable<KeptFinding> kept, IReadOnlyCollection<string>? verdict = null, IReadOnlyCollection<(string Project, string Configuration)>? builds = null, string? generatorLine = null)
    {
        try
        {
            var file = KeptRecord(root);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var checks = verdict is null
                ? []
                : verdict.Select(v => VerdictMark + v)
                    .Concat((builds ?? []).Select(b => $"{BuildMark}{b.Project}\t{b.Configuration}"))
                    .Concat(generatorLine is null || builds is not { Count: > 0 } ? [] : [GeneratorMark + generatorLine])
                    .Prepend(CheckedMark);
            File.WriteAllLines(file, kept.Select(k => k.ToString()).Concat(checks).Prepend(StateKey(root, args)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Puts the findings a clean run kept into the baseline the build reads (owner's choice, trial 23: with warnings as
    /// errors, jint's 227 kept findings failed the build 369 times after a "clean" format): <see cref="BaselineFile"/>, with
    /// its other entries kept unless the line they fingerprint is gone. What to say about it, or null when nothing changed
    /// (or the projects turn the baseline off). The build and '--verify-no-changes' skip them then; new violations still fail.
    /// </summary>
    public static string? BaselineKept(string root, IReadOnlyCollection<KeptFinding> kept, string? newAt = null)
    {
        // Each project's own (trial 27: <StyleBroBaseline> in src/Directory.Build.props named eng/'s, and the root's was written).
        var projects = ProjectBaselines(root, newAt);
        if (projects.Count == 0)
        {
            return BaselineFile(root, newAt) is { } file ? BaselineKeptInto(root, file, kept) : null;
        }

        var lines = BaselineGroups(projects, kept.Select(k => (Finding: k, k.Path)), BaselineFile(root, newAt))
            .Select(g => BaselineKeptInto(root, g.File, g.Items))
            .OfType<string>()
            .ToList();
        if (kept.Count > 0 && DifferentBaselinesLine(root, projects) is { } different)
        {
            lines.Insert(0, different);
        }

        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    /// <summary>
    /// The baseline file each project under <paramref name="root"/> that a run evaluated reads (full path to it, null when it's
    /// off or can't be resolved, with the files the project compiles): its evaluated &lt;StyleBroBaseline&gt;, which the package's
    /// targets resolve as the build does, else <see cref="BaselineFile"/>.
    /// </summary>
    public static Dictionary<string, (string? File, bool Off, HashSet<string> Compile)> ProjectBaselines(string root, string? newAt = null)
    {
        var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        List<(string FullPath, string[] Compile)> projects;
        lock (ProjectCache)
        {
            projects = ProjectCache.Values.SelectMany(d => d.Values).Select(p => (FullPath: Path.GetFullPath(p.FullPath), p.Compile))
                .Where(p => p.FullPath.StartsWith(top, StringComparison.OrdinalIgnoreCase))
                .DistinctBy(p => p.FullPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        string? fallback = null;
        var fallbackOff = false;
        var fell = false;
        return projects.ToDictionary(
            p => p.FullPath,
            p =>
            {
                var (known, file) = BaselineOf(EvaluatedProperty(p.FullPath, "StyleBroBaseline"), Path.GetDirectoryName(p.FullPath)!);
                if (!known && !fell)
                {
                    (fell, fallback, fallbackOff) = (true, BaselineFile(root, newAt), NamedBaseline(root).Off);
                }

                return (known ? file : fallback, known ? file is null : fallbackOff, p.Compile.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase));
            },
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A project's evaluated &lt;StyleBroBaseline&gt; (<paramref name="evaluated"/>) as a file: (false, null) when it's empty (no
    /// package targets, no baseline found), (true, null) for 'none', else the full path (relative to the project, like MSBuild).
    /// </summary>
    public static (bool Known, string? File) BaselineOf(string evaluated, string projectDirectory) => evaluated.Trim() switch
    {
        "" => (false, null),
        var none when none.Equals("none", StringComparison.OrdinalIgnoreCase) => (true, null),
        var value => (true, Path.GetFullPath(Path.Combine(projectDirectory, value.Replace('\\', Path.DirectorySeparatorChar)))),
    };

    /// <summary>
    /// <paramref name="items"/> (each with its file) grouped by the baseline the projects compiling the file read (<see cref="ProjectBaselines"/>;
    /// <paramref name="fallback"/> for a file none compiles), every project's baseline listed even without items; not the ones that are off.
    /// </summary>
    public static List<(string File, List<T> Items)> BaselineGroups<T>(IReadOnlyDictionary<string, (string? File, bool Off, HashSet<string> Compile)> projects, IEnumerable<(T Item, string Path)> items, string? fallback)
    {
        var groups = projects.Values.Select(p => p.File).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(f => f, _ => new List<T>(), StringComparer.OrdinalIgnoreCase);
        foreach (var (item, path) in items)
        {
            foreach (var file in BaselinesOf(projects, path, fallback))
            {
                (groups.TryGetValue(file, out var list) ? list : groups[file] = []).Add(item);
            }
        }

        return groups.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Value)).ToList();
    }

    /// <summary>The baselines a file's findings go into: those of the projects compiling it (<see cref="ProjectBaselines"/>), else <paramref name="fallback"/>.</summary>
    public static List<string> BaselinesOf(IReadOnlyDictionary<string, (string? File, bool Off, HashSet<string> Compile)> projects, string path, string? fallback)
    {
        var full = Path.GetFullPath(path);
        var owners = projects.Values.Where(p => p.Compile.Contains(full)).ToList();
        return (owners.Count > 0 ? owners.Select(p => p.File) : [fallback]).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>What to say when the projects read different baselines (<see cref="ProjectBaselines"/>), or null.</summary>
    public static string? DifferentBaselinesLine(string root, IReadOnlyDictionary<string, (string? File, bool Off, HashSet<string> Compile)> projects)
    {
        var files = projects.GroupBy(p => p.Value.File ?? string.Empty, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();
        return files.Count < 2
            ? null
            : "Baseline: the projects read different baselines ("
                + string.Join("; ", files.Select(g => $"{(g.Key.Length == 0 ? "none" : Relative(root, g.Key))}: {string.Join(", ", g.Select(p => Path.GetFileNameWithoutExtension(p.Key)).Order(StringComparer.OrdinalIgnoreCase).Take(4))}{(g.Count() > 4 ? $" and {g.Count() - 4} more" : string.Empty)}"))
                + "): each gets the findings in the files its projects compile.";
    }

    /// <summary>
    /// The baseline the projects under <paramref name="root"/> read: the file a Directory.Build.props above names with
    /// &lt;StyleBroBaseline&gt;, else the nearest stylebro.baseline at or above the root (as the package's targets look for
    /// it), else a new one at <paramref name="newAt"/> (<see cref="NewBaselineFolder"/> by default; --files gives the
    /// repository's, where a whole run puts it); null when the props turn it off ('none') or name it with other MSBuild properties.
    /// </summary>
    public static string? BaselineFile(string root, string? newAt = null) => NamedBaseline(root) is var (named, value, _) && named
        ? value
        : FoundBaseline(root, newAt);

    /// <summary>
    /// Where a new baseline goes for a run on <paramref name="root"/>: the folder that holds the projects of the solution it
    /// finds there (<see cref="WorkingRoot"/>), as for a run on that solution, else the root. Trial 28, Jackett: the preview
    /// (on the repository) wrote stylebro.baseline, 'format src/Jackett.sln' src/stylebro.baseline.
    /// </summary>
    public static string NewBaselineFolder(string root) =>
        Directory.Exists(root) && BaselineCommand.FindWorkspace(root) is { } workspace && File.Exists(Path.Combine(root, workspace))
            ? WorkingRoot(Path.Combine(root, workspace))
            : root;

    /// <summary>
    /// What to say when findings are kept and the props turn the baseline off (&lt;StyleBroBaseline&gt;none): nothing hides
    /// them from the build, which fails on them with warnings as errors, while 'format' and its check call them clean (trial 24).
    /// </summary>
    public static string? BaselineOffLine(string root, int kept) =>
        kept > 0 && (ProjectBaselines(root) is { Count: > 0 } projects ? projects.Values.Any(p => p.Off) : NamedBaseline(root).Off)
            ? $"Baseline off: <StyleBroBaseline>none</StyleBroBaseline> in Directory.Build.props, so the {(kept == 1 ? "kept finding isn't" : $"{kept} kept findings aren't")} baselined: "
                + $"the build still reports {(kept == 1 ? "it" : "them")} (an error with warnings as errors). Suppress {(kept == 1 ? "it" : "them")}, or remove that setting so 'format' baselines {(kept == 1 ? "it" : "them")} (https://bisforboman.github.io/stylebro/baseline/)."
            : null;

    /// <summary>The findings <see cref="SaveKept"/> recorded for this state of the repository and these options, else null.</summary>
    public static List<KeptFinding>? LoadKept(string root, IEnumerable<string> args)
    {
        try
        {
            var lines = File.Exists(KeptRecord(root)) ? File.ReadAllLines(KeptRecord(root)) : [];
            return lines.Length > 0 && lines[0] == StateKey(root, args) ? lines.Skip(1).Select(KeptFinding.Parse).OfType<KeptFinding>().ToList() : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// What the findings a run keeps depend on: every C# file, project, solution and settings file of the repository (its
    /// content), the options (not those that only say how to run: --verify-no-changes, --no-restore, --restore, --timings,
    /// --report), this tool's version and the SDK's and StyleBro.Analyzers' (<see cref="Versions"/>).
    /// </summary>
    public static string StateKey(string root, IEnumerable<string> args)
    {
        var source = RepositoryRoot(root) ?? root;
        string[] settings = [".cs", ".csproj", ".props", ".targets", ".editorconfig", ".globalconfig", ".sln", ".slnx", ".slnf", ".baseline", ".json", ".config"];
        var files = StyleCopSetup.EnumerateFiles(source)
            .Where(f => settings.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            .AsParallel()
            .Select(f => Path.GetRelativePath(source, f).Replace('\\', '/') + "|" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f))))
            .Order(StringComparer.Ordinal);
        var options = new List<string>();
        var list = args.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == "--report")
            {
                i++;
            }
            else if (list[i] is not ("--verify-no-changes" or "--no-restore" or RestoreOption or TimingsOption or CheckFirstOption or OnceOption))
            {
                options.Add(i == 0 && !list[i].StartsWith("-", StringComparison.Ordinal) ? Path.GetFullPath(list[i]) : list[i]);
            }
        }

        // And the SDK's and StyleBro.Analyzers' versions (owner's decision 2026-10-17: verify answers from the record).
        var version = typeof(FormatCommand).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false).OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion;
        var text = string.Join("\n", files.Append(string.Join(" ", options)).Append(version ?? string.Empty).Append(Versions(root)));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>The rules of the finding lines in a check's output (not the whitespace pass's), in order.</summary>
    public static List<string> FindingIds(IEnumerable<string> lines) =>
        lines.Where(l => IsFindingLine(l) && !IsWhitespaceLine(l))
            .Select(l => Regex.Match(l, @"\(\d+,\d+\): \w+ (?<id>\w+):").Groups["id"].Value)
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>The StyleBro rules of WarningsNotAsErrors values (upper case, sorted, once each).</summary>
    public static List<string> NotAsErrors(IEnumerable<string> values) =>
        values.SelectMany(v => Regex.Matches(v, @"\bBRO\d+\b", RegexOptions.IgnoreCase).Select(m => m.Value.ToUpperInvariant())).Distinct().Order(StringComparer.Ordinal).ToList();

    /// <summary>What format says about the StyleBro rules in WarningsNotAsErrors (<see cref="NotAsErrors"/>).</summary>
    public static string NotAsErrorsLine(IReadOnlyList<string> ids) =>
        $"{NotAsErrorsHeader} {string.Join(", ", ids)} {(ids.Count == 1 ? "is" : "are")} in <WarningsNotAsErrors>, which 'dotnet format' reads as \"skip\" while the build reports {(ids.Count == 1 ? "it" : "them")} as warnings: "
        + "'format' takes StyleBro's rules out of that list for its runs, so they're fixed and checked like the build reports them.";

    /// <summary>The deepest folder that holds all of <paramref name="folders"/> (full paths), or null when they're on different drives.</summary>
    public static string? CommonFolder(IEnumerable<string> folders)
    {
        string? common = null;
        foreach (var folder in folders.Select(f => Path.TrimEndingDirectorySeparator(Path.GetFullPath(f))))
        {
            common ??= folder;
            while (common is not null && !(folder.Equals(common, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(Path.TrimEndingDirectorySeparator(common) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                common = Path.GetDirectoryName(common);
            }
        }

        return common;
    }

    /// <summary>The MSBuild argument that sets <paramref name="platform"/>, none for null.</summary>
    internal static string[] PlatformProperty(string? platform) => platform is null ? [] : [$"-property:Platform={platform}"];

    /// <summary>Sets a configuration (<see cref="SplitConfiguration"/>) the way a run sees it: like TargetFramework, as environment variables, so the project file's own conditions see them.</summary>
    internal static void SetConfiguration(IDictionary<string, string?> environment, string configuration)
    {
        var (name, platform) = SplitConfiguration(configuration);
        environment["Configuration"] = name;
        if (platform is not null)
        {
            environment["Platform"] = platform;
        }
    }

    /// <summary>
    /// The configurations the baseline checks besides the default (<see cref="ConfigurationRuns"/>): those where some project's
    /// code parses differently. None when MSBuild can't evaluate the workspace (the run reports that itself).
    /// </summary>
    internal static List<string> WorkspaceConfigurations(string workspacePath)
    {
        try
        {
            var projects = ReadProjects(Path.GetFullPath(workspacePath));
            var plan = Plan(projects.ToDictionary(p => p.Key, p => p.Value.Frameworks));
            var byFullPath = projects.Values.ToDictionary(p => p.FullPath, p => p.Frameworks, StringComparer.OrdinalIgnoreCase);
            return ConfigurationRuns(projects, plan.Count == 0 ? [(string.Empty, projects.Keys.ToList())] : plan, workspacePath, (p, f, c) => ViewOf(p, byFullPath[p], f, c))
                .Select(r => r.Configuration)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (ToolException)
        {
            return [];
        }
    }

    /// <summary>Whether the file at <paramref name="path"/> exists and has <paramref name="text"/>, line endings aside.</summary>
    internal static bool SameButEndings(string text, string path) =>
        File.Exists(path) && File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) == text.Replace("\r\n", "\n", StringComparison.Ordinal);

    /// <summary>
    /// --verify-no-changes: 'dotnet format --verify-no-changes' fails on every finding it would hand to a fix, also those a
    /// StyleBro fix leaves on purpose, so a loop "until clean" never ends. When it fails, this formats a temporary copy of
    /// the repository (the fixes only run when files are written) and decides by the copy: no file changed means only
    /// findings no fix changes are left, listed with the reasons the fixes recorded; exit 0. A changed file means work is
    /// left: 'dotnet format''s output and exit code. The repository itself is never touched.
    /// </summary>
    internal static int Verify(string[] args, string root, Action<string> log, Func<string[], Action<string>?, int> runOnce)
    {
        // Nothing changed since a clean 'format' checked every run (owner's decision 2026-10-17): its check stands.
        if (VerifyFromRecord(args, root, log) is { } fromRecord)
        {
            return fromRecord;
        }

        var lines = new List<string>();

        // The progress of the runs at once is shown as it comes (splat: nothing for 48 minutes before), and what the check
        // is before it (the header came after the progress).
        var code = runOnce(args, line =>
        {
            if (line.StartsWith(ProgressPrefix, StringComparison.Ordinal) || Headers.Any(h => line.StartsWith(h, StringComparison.Ordinal)))
            {
                log(line);
            }
            else
            {
                lines.Add(line);
            }
        });

        // What the whitespace pass leaves and the build's IDE0055 reports: 'format' fixes it (DocIndentation). Only where the
        // build reports it (documentation not generated).
        if ((code == 0 || code == NotCleanExitCode) && DocIndentation.FindingLines(DocIndentationFindings(root, args, reported: true)) is { Count: > 0 } docLines)
        {
            lines.AddRange(docLines);
            code = NotCleanExitCode;
        }

        if (code == 0)
        {
            lines.ForEach(log);

            if (Unverified(lines))
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
        var verifyTemp = Path.Combine(Path.GetTempPath(), $"stylebro-verify-{Guid.NewGuid():N}");
        PreviewCommand.MarkCopy(verifyTemp);
        var copy = verifyTemp;
        var keptFile = verifyTemp + ".kept.tsv";
        var copyFailures = new SortedSet<string>(StringComparer.Ordinal);
        var putBack = new List<string>();
        (int Code, List<KeptFinding> Kept, List<string> Changed) FormatCopy()
        {
            // Files the projects compile from outside the repository (a sibling one) go next to the copy too.
            var outside = OutsideCompile(source);
            copy = PreviewCommand.CopyLocation(verifyTemp, source, outside);
            PreviewCommand.Copy(source, copy);
            PreviewCommand.CopyOutside(source, copy, verifyTemp, outside);

            // A git repository, like the source: a restore that installs git hooks fails without one (Mapperly's
            // 'dotnet husky install', common next to CSharpier), and the copy's run then fails although nothing is left.
            if (Directory.Exists(Path.Combine(source, ".git")) || File.Exists(Path.Combine(source, ".git")))
            {
                PreviewCommand.Git(copy, "init", "-q");
            }

            CopyAnalyzerOutputs(args.Length > 0 && !args[0].StartsWith("-", StringComparison.Ordinal) ? Path.GetFullPath(args[0]) : root, source, copy);
            var copyArgs = args.Where(a => a != "--verify-no-changes").ToList();

            // Only the files with findings: whether their fixes change anything (renames reach other files from there). A
            // copy formatted whole took splat 41 minutes for 9 kept findings in 4 files. With --files too: the named files
            // without findings needn't run (trial 25: three frameworks formatted in the copy for two files of one).
            if (FindingFiles(lines, root) is { Count: > 0 } findingFiles)
            {
                if (OptionValues(copyArgs, "--include") is { } named)
                {
                    copyArgs.RemoveRange(copyArgs.IndexOf("--include"), named.Count + 1);
                }

                copyArgs.Add("--include");
                copyArgs.AddRange(findingFiles);

                // And only their rules: a pass none of them is in (the style pass for StyleBro's) isn't made.
                if (FindingIds(lines) is { Count: > 0 } ids && !copyArgs.Contains("--diagnostics") && !copyArgs.Contains("--all"))
                {
                    copyArgs.Add("--diagnostics");
                    copyArgs.AddRange(ids);
                }
            }

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

            var copyRoot = Path.GetFullPath(Path.Combine(copy, Path.GetRelativePath(source, root)));
            using (namesInCopy)
            using (new KeptVariable(keptFile))
            {
                copyCode = runOnce(copyArgs.ToArray(), line =>
                {
                    if (FixFailure(line) is { } failure)
                    {
                        copyFailures.Add(failure);
                    }

                    if (line.StartsWith("== ", StringComparison.Ordinal))
                    {
                        log($"Formatting a copy: {line[3..]}");
                    }
                });
                kept = ReadKept(keptFile, copy, source);
            }

            // As 'format' does after each run (DocIndentation).
            if (copyCode == 0)
            {
                DocIndentation.FixFiles(DocIndentationFindings(copyRoot, copyArgs.Skip(1).ToList()));
            }

            // CSharpier on the files the copy's run changed, as 'format' runs it: what it puts back isn't left to change.
            var fixedState = Snapshot(copy);
            if (copyCode == 0 && CSharpierStep.Find(copyRoot) is { } csharpier && CSharpierStep.InScope(csharpier, copyRoot, ChangedFiles(before, fixedState)) is { Count: > 0 } pretty)
            {
                CSharpierStep.Run(csharpier, "format", pretty);
                putBack.AddRange(CSharpierStep.PutBack(before, fixedState, Snapshot(copy)).Select(f => Path.GetRelativePath(copy, f).Replace('\\', '/')));
            }

            // Not a file whose only change is a line ending (a mixed file the whitespace pass wrote this OS's ending into):
            // like the whitespace findings below, it's no reason to fail. Unless end_of_line asks for the new ending (trial 28:
            // 'end_of_line = crlf', LF files, "Clean" while 'format' converted them).
            var changedInCopy = ChangedFiles(before, Snapshot(copy));
            var endOfLine = EndOfLineSet(copy, changedInCopy);
            var changedFiles = changedInCopy
                .Where(f => !File.Exists(f) || endOfLine.Contains(f) || !SameButEndings(File.ReadAllText(f), Path.Combine(source, Path.GetRelativePath(copy, f))))
                .ToList();
            return (copyCode, kept, changedFiles.Select(f => Path.Combine(source, Path.GetRelativePath(copy, f))).ToList());
        }

        // The findings the last clean 'format' kept on this very state of the repository (SaveKept): known with their
        // reasons, so there's nothing to format a copy for (splat: 41 minutes for 9 such findings).
        var findings = lines.Where(IsFindingLine).ToList();
        var recorded = LoadKept(root, args) is { } known && findings.Count > 0 && findings.All(l => !IsWhitespaceLine(l) && AsKept(l, known) is not null)
            ? findings.Select(l => AsKept(l, known)!).GroupBy(k => (k.Path.ToUpperInvariant(), k.Line, k.Column, k.Id)).Select(g => g.First()).OrderBy(k => k.Path, StringComparer.OrdinalIgnoreCase).ThenBy(k => k.Line).ThenBy(k => k.Column).ToList()
            : null;
        try
        {
            (int, List<KeptFinding>, List<string>) copied;
            using (PhaseTimes.Measure(recorded is null ? "verify: format a copy" : "verify: kept findings from the record"))
            {
                copied = recorded is null ? FormatCopy() : (0, recorded, new List<string>());
            }

            var (copyCode, kept, changedFiles) = copied;
            var changed = changedFiles.Count;
            JsonReport.AddRun(1, changedFiles, null);
            if (putBack.Count > 0 && changed == 0 && copyCode == 0)
            {
                lines.ForEach(log);
                log(CSharpierStep.PutBackLine(putBack, FindingIds(lines.Where(l => FindingPath(l) is { } path && putBack.Contains(Path.GetRelativePath(source, path).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)))));
                return NotCleanExitCode;
            }

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
                    ? $"Not clean: formatting would change {changed} file{(changed == 1 ? string.Empty : "s")}. Run 'bro format'."
                    : "Not clean: formatting a copy of the repository failed (see above).");
                return code;
            }

            JsonReport.AddKept(kept);

            // Whitespace findings in a copy that formats clean only swapped line endings (KeepingLineEndings).
            var endingsOnly = lines.Count(IsWhitespaceLine);
            lines.Where(l => !IsKeptLine(l, keys) && !IsWhitespaceLine(l)).ToList().ForEach(log);
            if (endingsOnly > 0)
            {
                log($"{endingsOnly} whitespace finding{(endingsOnly == 1 ? " is" : "s are")} only this OS's line ending where a file uses the other one: ignored, 'format' keeps each file's own ending (set end_of_line to choose one).");
            }

            // Findings no fix changes and no fix keeps on purpose: the build still reports them (AngleSharp's IDE0049 in a
            // using alias, which the SDK's fix can't rewrite: "Clean" with three errors above it).
            var unfixed = lines.Where(l => IsFindingLine(l) && !IsKeptLine(l, keys) && !IsWhitespaceLine(l)).Select(l => l.Trim()).Distinct(StringComparer.Ordinal).Count();
            KeptSummary(kept, root, clean: unfixed == 0 && copyFailures.Count == 0).ForEach(log);
            if (BaselineOffLine(root, kept.Count) is { } off)
            {
                log(off);
            }

            // A fix that threw in the copy changed nothing there, which isn't "no fix changes" (akka: BRO1309's fix failed).
            if (copyFailures.Count > 0)
            {
                log(FixFailuresLine(copyFailures));
                return NotCleanExitCode;
            }

            if (unfixed > 0)
            {
                log($"Not clean: {unfixed} finding{(unfixed == 1 ? string.Empty : "s")} above no fix changes (the build reports {(unfixed == 1 ? "it" : "them")}): fix {(unfixed == 1 ? "it" : "them")} by hand, or suppress or baseline {(unfixed == 1 ? "it" : "them")}.");
                return NotCleanExitCode;
            }

            if (Unverified(lines))
            {
                log(NotVerified);
                return 1;
            }

            if (kept.Count == 0)
            {
                log("Clean: formatting would change nothing.");
            }

            return 0;
        }
        finally
        {
            File.Delete(keptFile);

            // The copy too (trial 23: hundreds were left in the temp folder).
            if (Directory.Exists(verifyTemp))
            {
                PreviewCommand.Delete(verifyTemp);
            }
            else
            {
                File.Delete(PreviewCommand.MarkerOf(verifyTemp));
            }
        }
    }

    /// <summary>
    /// Copies the build output of the projects a workspace (<paramref name="path"/>: a solution, project or folder in
    /// <paramref name="source"/>) references as analyzers ('OutputItemType="Analyzer"': Mapperly's in-solution source
    /// generator) into <paramref name="copy"/>. Git ignores it, so a copy of the tracked files has none, the generator doesn't
    /// run there, and the renames its generated code keeps back go through: verify failed where 'format' is clean. The
    /// repository's own output, not a build: a generator 'format' can't run in place doesn't run in the copy either.
    /// </summary>
    internal static void CopyAnalyzerOutputs(string path, string source, string copy)
    {
        // Only where an MSBuild file names one (the copy holds just the tracked files): evaluating every project costs ~1 s each.
        var workspace = File.Exists(path) ? path : BaselineCommand.FindWorkspace(path) is { } found ? Path.Combine(path, found) : null;
        if (workspace is null || !Directory.EnumerateFiles(copy, "*", SearchOption.AllDirectories).Where(StyleCopSetup.IsMSBuild).Any(f => File.ReadAllText(f).Contains("OutputItemType", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var generators = AnalyzerProjects(workspace);
        var sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source)) + Path.DirectorySeparatorChar;

        // Each configuration's output: the runs of other configurations load the generator built in theirs (BuildGenerators).
        var configurations = (SolutionConfigurations(workspace) ?? ["Debug", "Release"]).Prepend(string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var (project, configuration) in generators.SelectMany(g => configurations.Select(c => (g, c))))
        {
            // A multi-targeted project's outer TargetDir has no framework folder: every framework's output goes along.
            string output;
            try
            {
                var (name, platform) = SplitConfiguration(configuration);
                string[] chosen = configuration.Length == 0 ? [] : [$"-property:Configuration={name}", .. PlatformProperty(platform)];
                output = Capture(Path.GetDirectoryName(project)!, ["msbuild", project, "-getProperty:TargetDir", .. chosen, "-nologo"]).Trim();
            }
            catch (ToolException)
            {
                // The copy's run reports a project MSBuild can't evaluate itself.
                continue;
            }

            if (output.Length == 0 || !Directory.Exists(output) || !Path.GetFullPath(output).StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories))
            {
                var to = Path.Combine(copy, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                File.Copy(file, to, overwrite: true);

                // Newer than the copied sources (written just now), so the copy's run finds it built (BuildGenerators).
                File.SetLastWriteTimeUtc(to, DateTime.UtcNow);
            }
        }
    }

    /// <summary>
    /// Builds the projects the code takes as analyzers ('OutputItemType="Analyzer"': in-solution source generators) when
    /// their output is missing, once per process, saying so: without it 'dotnet format' runs
    /// without the generator, its generated code is missing, and a rename of a member that code uses broke the build
    /// (trial 23: CS0103 after a "clean" format and verify, owner's choice "Build generators first"). A failed build is said
    /// and the runs go on (as before: without the generated code). <paramref name="configuration"/>: the configuration of the
    /// runs that load it ("": the default; <see cref="SplitConfiguration"/>), whose output those runs use.
    /// </summary>
    internal static void BuildGenerators(IEnumerable<string> generators, string root, Action<string> log, string configuration = "")
    {
        // Whether each is built: one 'dotnet msbuild' each, all at once.
        var stale = generators.Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(g => File.Exists(g) && !BuiltGenerators.ContainsKey(g + "|" + configuration))
            .AsParallel()
            .AsOrdered()
            .Select(g => (Generator: g, Built: IsBuilt(g, configuration)))
            .ToList()
            .Where(g => !g.Built || !BuiltGenerators.TryAdd(g.Generator + "|" + configuration, true))
            .Select(g => g.Generator)
            .ToList();
        if (stale.Count == 0)
        {
            return;
        }

        var (name, platform) = SplitConfiguration(configuration);
        var where = configuration.Length == 0 ? "in the default configuration, the one 'dotnet format' loads" : $"in {configuration}, for the runs of that configuration";
        log($"{GeneratorsHeader} building {string.Join(", ", stale.Select(Path.GetFileNameWithoutExtension))} first ({where}): the code uses {(stale.Count == 1 ? "it" : "them")} as a source generator, and without {(stale.Count == 1 ? "its" : "their")} output the generated code would be missing from the checks.");
        foreach (var generator in stale)
        {
            var output = new List<string>();

            // Without analyzers and warnings as errors: the generator's own code isn't formatted yet (jint: its StyleBro findings
            // failed the build under TreatWarningsAsErrors), and only its output matters here.
            string[] chosen = configuration.Length == 0 ? [] : ["-c", name, .. PlatformProperty(platform)];
            if (Dotnet(root, null, ["build", generator, .. chosen, "-nologo", "-v", "q", "-p:RunAnalyzers=false", "-p:TreatWarningsAsErrors=false", .. RestoreOptions], output.Add) == 0)
            {
                BuiltGenerators.TryAdd(generator + "|" + configuration, true);
            }
            else
            {
                output.Where(l => l.Contains(" error ", StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).Take(5).ToList().ForEach(log);
                log($"{GeneratorsHeader} building {Path.GetFileName(generator)} failed: formatting goes on without its generated code, so a rename it uses may break the build. Build it, then run again.");
            }
        }
    }

    /// <summary>
    /// The projects a workspace's C# projects take as analyzers (full paths of those that exist), or none when MSBuild can't
    /// evaluate it (the run reports that itself).
    /// </summary>
    internal static string[] AnalyzerProjects(string workspacePath)
    {
        try
        {
            return ReadProjects(Path.GetFullPath(workspacePath)).Values.SelectMany(p => p.Analyzers).Distinct(StringComparer.OrdinalIgnoreCase).Where(File.Exists).ToArray();
        }
        catch (ToolException)
        {
            return [];
        }
    }

    /// <summary>Whether a project's output (TargetPath) in a configuration ("": its default) exists.</summary>
    internal static bool IsBuilt(string project, string configuration = "")
    {
        string target;
        try
        {
            var (name, platform) = SplitConfiguration(configuration);
            string[] chosen = configuration.Length == 0 ? [] : [$"-property:Configuration={name}", .. PlatformProperty(platform)];
            target = Capture(Path.GetDirectoryName(project)!, ["msbuild", project, "-getProperty:TargetPath", .. chosen, "-nologo"]).Trim();
        }
        catch (ToolException)
        {
            return false;
        }

        // Built at all, not up to date: format's own fixes change the generator's code, and rebuilding it for the next run
        // changed that run's results (Mediator: a third run in the real-world check).
        return target.Length > 0 && File.Exists(target);
    }

    /// <summary>
    /// A project's evaluation ('dotnet msbuild -getProperty/-getItem' JSON, <see cref="ProjectQuery"/>), once per process: the
    /// migration's check for StyleCop (<see cref="StyleCopSetup.EvaluateStyleCop"/>) and format's project list share it.
    /// Throws <see cref="ToolException"/> when MSBuild can't evaluate the project.
    /// </summary>
    internal static string EvaluateProject(string fullPath) =>
        Evaluations.GetOrAdd(fullPath, p => new Lazy<string>(() => Capture(Path.GetDirectoryName(p)!, ["msbuild", p, .. ProjectQuery]))).Value;

    /// <summary>
    /// What makes a project parse a file its way for one target framework (<see cref="Separate"/>, <see cref="MergeFrameworks"/>):
    /// its '#if' symbols (the SDK's implicit ones, NET8_0_OR_GREATER, included), language version, nullable context and
    /// the files it compiles, evaluated for that framework (the outer evaluation of a multi-targeted project has none of the
    /// framework's own, e.g. splat's IS_SHARED_NET), once per process; null when MSBuild can't evaluate it (<see cref="ReadView"/>).
    /// </summary>
    internal static string? CompileSettings(string fullPath, string framework, string configuration = "")
    {
        // The SDK adds the framework's own symbols (NET8_0_OR_GREATER) in a target: run it, so frameworks can be compared too.
        // A configuration (ConfigurationRuns) is set as the runs set it; "" is the project's own default.
        try
        {
            return Evaluations.GetOrAdd($"{fullPath}|{framework}|{configuration}", _ => new Lazy<string>(() => Capture(
                Path.GetDirectoryName(fullPath)!,
                [
                    "msbuild",
                    fullPath,
                    $"-property:TargetFramework={framework}",
                    .. configuration.Length > 0 ? [$"-property:Configuration={SplitConfiguration(configuration).Configuration}", .. PlatformProperty(SplitConfiguration(configuration).Platform)] : Array.Empty<string>(),
                    "-target:AddImplicitDefineConstants",
                    "-getProperty:DefineConstants",
                    "-getProperty:LangVersion",
                    "-getProperty:Nullable",
                    "-getProperty:NETCoreSdkVersion",
                    "-getItem:Compile",
                    "-nologo",
                ]))).Value;
        }
        catch (ToolException)
        {
            return null;
        }
    }

    internal static string Capture(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;

        // stdout on a thread of its own: ReadToEndAsync needs a free thread-pool thread, and init's evaluations (nested
        // parallel queries, each waiting here) took them all: 6-7 minutes of starvation for ~40 evaluations (trial 24).
        var output = string.Empty;
        var reader = ReadOnThread(process.StandardOutput, text => output = text);
        var errors = process.StandardError.ReadToEnd();
        reader.Join();
        process.WaitForExit();

        // A project MSBuild can't evaluate prints its error instead of JSON (GameDemo: "NuGet.Config is not valid XML"; it
        // was a JsonReaderException with a stack trace before).
        if (process.ExitCode != 0)
        {
            throw new ToolException($"'dotnet {string.Join(" ", arguments.Where(a => a != "-nologo"))}' failed (exit code {process.ExitCode}):{Environment.NewLine}{string.Join(Environment.NewLine, (output + errors).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).Select(l => "  " + l.TrimStart()))}");
        }

        return output;
    }

    /// <summary>
    /// Reads a process's stream to its end on a thread of its own (not the thread pool's, see <see cref="Capture"/>): the
    /// whole text, or (<paramref name="line"/>) line by line.
    /// </summary>
    internal static System.Threading.Thread ReadOnThread(StreamReader stream, Action<string>? text = null, Action<string>? line = null)
    {
        var thread = new System.Threading.Thread(() =>
        {
            if (line is null)
            {
                // Read even when nobody wants the text: 'text?.Invoke(stream.ReadToEnd())' skips the read when text is null,
                // and the child then blocks on a full pipe (trial 26: 'baseline' hung for hours on NLog's whitespace report).
                var all = stream.ReadToEnd();
                text?.Invoke(all);
                return;
            }

            for (var next = stream.ReadLine(); next is not null; next = stream.ReadLine())
            {
                line(next);
            }
        })
        { IsBackground = true };
        thread.Start();
        return thread;
    }

    /// <summary>A property of a project's evaluation (<see cref="EvaluateProject"/>), or "" when it isn't set or MSBuild can't evaluate it.</summary>
    internal static string EvaluatedProperty(string fullPath, string name)
    {
        try
        {
            using var json = JsonDocument.Parse(EvaluateProject(fullPath));
            return json.RootElement.TryGetProperty("Properties", out var properties) && properties.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
        }
        catch (Exception e) when (e is ToolException or JsonException)
        {
            return string.Empty;
        }
    }

    /// <summary>The git repository that holds the folder (the nearest folder above with a .git), or null.</summary>
    internal static string? RepositoryRoot(string folder)
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

    /// <summary>
    /// Deletes the temporary solution filters a killed run left next to the solution ('.stylebro-format-0.slnf', an
    /// untracked file in the repository, trial 23): those written before this process started.
    /// </summary>
    internal static void DeleteLeftoverFilters(string folder)
    {
        using var current = Process.GetCurrentProcess();
        var started = current.StartTime.ToUniversalTime();
        foreach (var file in new[] { ".stylebro-format-*.slnf", ".stylebro-restore-*.slnf", ".stylebro-files-*.slnx" }.SelectMany(p => Directory.EnumerateFiles(folder, p)))
        {
            if (File.GetLastWriteTimeUtc(file) < started)
            {
                File.Delete(file);
            }
        }
    }

    /// <summary>
    /// Deletes what a killed run left in the temporary folder (trial 25: 'stylebro-format-*.targets', 'stylebro-names-*.tsv'):
    /// the files and folders of format's runs written before <paramref name="before"/> (a day ago: a run in progress, even
    /// a long one, keeps its own).
    /// </summary>
    internal static void DeleteStaleTemp(string folder, DateTime before)
    {
        try
        {
            foreach (var file in new[] { "stylebro-format-*.targets", "stylebro-names-*.tsv", "stylebro-kept-*.tsv", "stylebro-verify-*.kept.tsv" }.SelectMany(p => Directory.EnumerateFiles(folder, p)))
            {
                if (File.GetLastWriteTimeUtc(file) < before)
                {
                    File.Delete(file);
                }
            }

            foreach (var directory in new[] { "stylebro-format-report-*", "stylebro-format-json-*", "stylebro-verify-*" }.SelectMany(p => Directory.EnumerateDirectories(folder, p)))
            {
                // A verify copy only when this tool marked it (PreviewCommand.MarkCopy): another program's alike-named folder stays.
                if (Directory.GetLastWriteTimeUtc(directory) < before && (!Path.GetFileName(directory).StartsWith("stylebro-verify-", StringComparison.Ordinal) || PreviewCommand.IsOwnCopy(directory)))
                {
                    PreviewCommand.Delete(directory);
                }
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Someone else's or in use: left alone.
        }
    }

    /// <summary>Runs dotnet in <paramref name="directory"/>, its output (stdout and stderr) line by line to <paramref name="output"/>.</summary>
    internal static int RunDotnet(string directory, IEnumerable<string> arguments, Action<string> output) => Dotnet(directory, null, arguments, output);

    /// <summary>
    /// The lines of the candidate C# files under <paramref name="root"/> (<see cref="IfBodies.IsCandidate"/>) whose if/else body
    /// changes between the <paramref name="runs"/> that compile them: <paramref name="compile"/> gives a run's project (full
    /// path), the files it compiles and its defined symbols (null when unknown: that run isn't counted).
    /// </summary>
    internal static Dictionary<string, (string Text, List<int> Lines)> IfBodyLines(
        string root,
        IEnumerable<(string Framework, string Configuration, List<string> Projects)> runs,
        Func<string, string, string, (string FullPath, string[] Compile, HashSet<string>? Defines)> compile)
    {
        var candidates = StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .AsParallel()
            .Where(f => IfBodies.IsCandidate(File.ReadAllText(f)))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (candidates.Count == 0)
        {
            return [];
        }

        var sets = runs.SelectMany(r => r.Projects.Select(p => (Project: p, r.Framework, r.Configuration)))
            .Distinct()
            .AsParallel()
            .Select(x => compile(x.Project, x.Framework, x.Configuration))
            .Where(c => c.Defines is not null)
            .SelectMany(c => c.Compile.Select(Path.GetFullPath).Where(candidates.Contains).Select(f => (File: f, Set: (IReadOnlyCollection<string>)TestedSymbols(f).Where(c.Defines!.Contains).ToList())))
            .ToList()
            .GroupBy(x => x.File, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Set).ToList(), StringComparer.OrdinalIgnoreCase);
        return IfBodies.Find(sets);
    }

    /// <summary>
    /// The baseline a Directory.Build.props at or above <paramref name="root"/> names with &lt;StyleBroBaseline&gt; (Named: true),
    /// null as its value when that turns it off ('none') or uses other MSBuild properties; Named false when none names one.
    /// </summary>
    internal static (bool Named, string? Value, bool Off) NamedBaseline(string root)
    {
        for (var folder = Path.GetFullPath(root); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            var props = Path.Combine(folder, "Directory.Build.props");
            if (File.Exists(props) && Regex.Match(StyleCopSetup.WithoutComments(File.ReadAllText(props)), @"<StyleBroBaseline\b[^>]*>(?<value>[^<]*)<") is { Success: true } named)
            {
                // MSBuild reads '\' as a folder separator on every OS ('eng\stylebro.baseline' on Linux too).
                var value = named.Groups["value"].Value.Trim().Replace("$(MSBuildThisFileDirectory)", folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase).Replace('\\', Path.DirectorySeparatorChar);
                var off = value.Equals("none", StringComparison.OrdinalIgnoreCase);
                return (true, off || value.Contains("$(", StringComparison.Ordinal) ? null : Path.GetFullPath(Path.Combine(folder, value)), off);
            }
        }

        return (false, null, false);
    }

    /// <summary><see cref="Run"/> without --timings.</summary>
    private static int RunMeasured(string[] args, Action<string>? output, Func<string[], Action<string>?, int>? runOnce)
    {
        Action<string> log = output ?? Console.WriteLine;
        ResetExplained();
        DeleteStaleTemp(Path.GetTempPath(), DateTime.UtcNow.AddDays(-1));

        runOnce = KeepingLineEndings(runOnce ?? RunOnce);

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
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            (output ?? Console.Error.WriteLine)($"Not found: {path}");
            return 1;
        }

        // A solution in a folder of its own (solutions/App.slnx listing ../src/...): the runs work from the folder that holds
        // all its projects, else '--include ../src/Lib/' matched nothing and the changed files weren't looked for there.
        var root = File.Exists(path) ? WorkingRoot(path) : path;
        if (JsonReport.Current is { } json && json["path"] is null)
        {
            json["path"] = root;
        }

        // Rules a bulk severity hides from 'dotnet format' but not from the build: no run is clean then. --verify-no-changes
        // checks CSharpier's layout too where it formats the repository (owner's decision 2026-10-16).
        // CSharpier's check comes after StyleBro's (trial 26: its "checking..." line came before the header), so the verdict that
        // says clean waits for it.
        var verify = args.Contains("--verify-no-changes");
        Unchecked.TryRemove(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)), out _);
        var hidden = HiddenLine(root);

        // The verdict and everything after it (the kept findings it lists, the baseline line) wait as one block.
        var verdicts = new List<string>();
        Action<string> runLog = hidden is null && !verify ? log : line =>
        {
            if (verdicts.Count > 0 || line.StartsWith("Clean", StringComparison.Ordinal))
            {
                verdicts.Add(line);
            }
            else
            {
                log(hidden is null ? line : line.Replace(" changed, clean.", " changed.", StringComparison.Ordinal));
            }
        };
        var exit = verify ? Verify(args, root, runLog, runOnce) : Repeat(args, root, runLog, output, runOnce, once ? 1 : MaxRuns);
        var buts = new[] { hidden, verify ? UncheckedLine(root, log) : null, verify ? CSharpierStep.CheckLine(root, log: log) : null }.OfType<string>().ToList();
        verdicts.Select((line, i) => i == 0 && buts.Count > 0 ? "Formatting is clean" + line.Substring("Clean".Length) + " But:" : hidden is null ? line : line.Replace(" changed, clean.", " changed.", StringComparison.Ordinal)).ToList().ForEach(log);
        foreach (var line in buts)
        {
            log(line);
            exit = exit == 0 ? NotCleanExitCode : exit;
        }

        JsonReport.Set("clean", exit == 0);
        return exit;
    }

    /// <summary>Whether 'dotnet format' leaves a file alone as generated code: its name ('.g.cs', '.designer.cs', ...) or an '&lt;auto-generated' header.</summary>
    private static bool IsGeneratedCode(string file) =>
        Regex.IsMatch(Path.GetFileName(file), @"\.(?:g|g\.i|designer|generated)\.cs$|^TemporaryGeneratedFile_", RegexOptions.IgnoreCase)
            || File.ReadLines(file).Take(10).Any(l => l.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether a solution platform builds projects in their own platform (Any CPU, Mixed Platforms).</summary>
    private static bool IsAnyPlatform(string platform) =>
        platform.Replace(" ", string.Empty, StringComparison.Ordinal) is var p && (p.Length == 0 || p.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase) || p.Equals("MixedPlatforms", StringComparison.OrdinalIgnoreCase));

    /// <summary>Puts <paramref name="kept"/> into the baseline <paramref name="file"/> (<see cref="BaselineKept"/>).</summary>
    private static string? BaselineKeptInto(string root, string file, IReadOnlyCollection<KeptFinding> kept)
    {
        var exists = File.Exists(file);
        if (kept.Count == 0 && !exists)
        {
            return null;
        }

        var folder = Path.GetDirectoryName(file)!;
        var lines = new Dictionary<string, HashSet<string>?>(StringComparer.OrdinalIgnoreCase);
        HashSet<string>? Fingerprints(string path) => lines.TryGetValue(path, out var known)
            ? known
            : lines[path] = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(Analyzers.Baseline.Baseline.Fingerprint).ToHashSet(StringComparer.Ordinal) : null;

        var counts = new Dictionary<Analyzers.Baseline.Baseline.Key, int>();
        var (removed, removedFindings) = (0, 0);
        foreach (var (key, count) in exists ? Analyzers.Baseline.Baseline.Parse(File.ReadAllText(file)).Entries : new Dictionary<Analyzers.Baseline.Baseline.Key, int>())
        {
            if (Fingerprints(Path.Combine(folder, key.Path))?.Contains(key.Fingerprint) == true)
            {
                counts[key] = count;
            }
            else
            {
                removed++;
                removedFindings += count;
            }
        }

        var added = 0;
        foreach (var group in kept.Select(k => (Finding: k, Relative: Analyzers.Baseline.Baseline.RelativePath(folder, Path.GetFullPath(k.Path))))
            .Where(k => k.Relative is not null && File.Exists(k.Finding.Path))
            .GroupBy(k => new Analyzers.Baseline.Baseline.Key(k.Finding.Id, k.Relative!, Analyzers.Baseline.Baseline.Fingerprint(File.ReadLines(k.Finding.Path).ElementAtOrDefault(k.Finding.Line - 1) ?? string.Empty))))
        {
            var had = counts.TryGetValue(group.Key, out var count) ? count : 0;
            if (group.Count() > had)
            {
                added += group.Count() - had;
                counts[group.Key] = group.Count();
            }
        }

        if (added == 0 && removed == 0)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(file)!); // a baseline <StyleBroBaseline> names in a folder not made yet
        File.WriteAllText(file, new Analyzers.Baseline.Baseline(counts).Render());
        var name = Relative(root, file);
        var parts = new List<string>();
        if (added > 0)
        {
            parts.Add($"added {added} kept finding{(added == 1 ? string.Empty : "s")}, so the build and 'format --verify-no-changes' skip {(added == 1 ? "it" : "them")} (new violations still fail; delete an entry to see it again)");
        }

        if (removed > 0)
        {
            parts.Add($"removed {removed} entr{(removed == 1 ? "y" : "ies")} whose line is gone{(removedFindings == removed ? string.Empty : $" ({removedFindings} findings)")}");
        }

        return $"Baseline: {name}: {string.Join("; ", parts)} (https://bisforboman.github.io/stylebro/baseline/).";
    }

    /// <summary>The nearest stylebro.baseline at or above <paramref name="root"/>, else a new one in <see cref="NewBaselineFolder"/>.</summary>
    private static string FoundBaseline(string root, string? newAt = null)
    {
        var at = Path.GetFullPath(newAt ?? NewBaselineFolder(root));
        string? found = null;
        for (var folder = Path.GetFullPath(root); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            found ??= File.Exists(Path.Combine(folder, Analyzers.Baseline.Baseline.FileName)) ? Path.Combine(folder, Analyzers.Baseline.Baseline.FileName) : null;
        }

        return found ?? Path.Combine(at, Analyzers.Baseline.Baseline.FileName);
    }

    /// <summary>Where <see cref="SaveKept"/> records a repository's kept findings.</summary>
    private static string KeptRecord(string root) =>
        Path.Combine(Path.GetTempPath(), "stylebro-kept", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)).ToUpperInvariant())))[..16] + ".tsv");

    /// <summary>
    /// Whether an 'X.Designer.cs' belongs to a designer: an 'X.resx' next to it (resource designer), or an 'X.cs' next to it and
    /// an InitializeComponent in it (WinForms/WPF form), or an EF Core migration's (a [Migration] attribute, or an auto-generated file in a Migrations folder).
    /// </summary>
    private static bool DesignerWritten(string file)
    {
        var stem = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileName(file)[..^".designer.cs".Length]);
        var text = File.ReadAllText(file);

        // An EF Core migration's designer: 'dotnet ef migrations add' writes it once, no build does (trial 24, Dawnshard).
        // A settings designer: Visual Studio writes Settings.Designer.cs from Settings.settings (Flow Launcher, trial 25).
        return File.Exists(stem + ".resx")
            || File.Exists(stem + ".settings")
            || (File.Exists(stem + ".cs") && text.Contains("InitializeComponent", StringComparison.Ordinal))
            || Regex.IsMatch(text, @"\[\s*(?:Microsoft\.EntityFrameworkCore\.Migrations\.)?Migration(?:Attribute)?\s*\(|Microsoft\.EntityFrameworkCore\.Migrations")
            || (Path.GetFileName(Path.GetDirectoryName(file)) is { } folder && folder.Equals("Migrations", StringComparison.OrdinalIgnoreCase) && text.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase));
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

        // Files of several projects: one run that loads them all (a temporary solution of those projects at their common folder),
        // not a restore and runs per project one after another (trial 25: two files in two of 11 projects, 97 s; the whole
        // repository took 70 s).
        string? together = null;
        if (!hasPath && groups.Count > 1 && CommonFolder(groups.Select(g => g.Folder)) is { } common)
        {
            var workspaces = groups.Select(g => g.Workspace).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (workspaces.Count > 1)
            {
                DeleteLeftoverFilters(common);
                together = Path.Combine(common, $".stylebro-files-{Guid.NewGuid():N}.slnx");
                File.WriteAllText(together, SolutionText(common, workspaces));
            }

            groups = [(together ?? workspaces[0], common, groups.SelectMany(g => g.Include.Select(i => Path.GetRelativePath(common, Path.Combine(g.Folder, i)).Replace('\\', '/'))).Distinct(StringComparer.OrdinalIgnoreCase).ToList())];
        }

        try
        {
            return RunGroups(groups, rest, hasPath, log, output, runOnce, maxRuns);
        }
        finally
        {
            if (together is not null)
            {
                File.Delete(together);
            }
        }
    }

    /// <summary>The runs of <see cref="RunFiles"/>, one per workspace.</summary>
    private static int RunGroups(List<(string Workspace, string Folder, List<string> Include)> groups, string[] rest, bool hasPath, Action<string> log, Action<string>? output, Func<string[], Action<string>?, int> runOnce, int maxRuns)
    {
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
            var code = arguments.Contains("--verify-no-changes") ? Verify(arguments, root, log, runOnce) : Repeat(arguments, root, log, output, runOnce, maxRuns, RepositoryRoot(root) ?? root);
            if (arguments.Contains("--verify-no-changes") && CSharpierStep.CheckLine(root, include.Select(f => Path.GetFullPath(Path.Combine(root, f))), log) is { } csharpierLine)
            {
                log(csharpierLine);
                code = code == 0 ? NotCleanExitCode : code;
            }

            exit = exit == 0 ? code : exit;
        }

        JsonReport.Set("clean", exit == 0);
        return exit;
    }

    /// <summary>
    /// Runs 'dotnet format' until a run changes no file (at most <paramref name="maxRuns"/>), then lists the kept findings.
    /// One run (--once) lists them after it, whether it changed files or not, and exits 0 when it succeeded.
    /// </summary>
    private static int Repeat(string[] args, string root, Action<string> log, Action<string>? output, Func<string[], Action<string>?, int> runOnce, int maxRuns = MaxRuns, string? shown = null)
    {
        // The paths it lists are relative to this (--files: the repository's root, not the files' project folder).
        shown ??= root;
        var keptFile = Path.Combine(Path.GetTempPath(), $"stylebro-kept-{Guid.NewGuid():N}.tsv");
        using var kept = new KeptVariable(keptFile);
        var allChanged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allFailures = new SortedSet<string>(StringComparer.Ordinal);

        // CSharpier, where the repository formats with it: on the files each run changed (owner's decision 2026-10-16).
        var csharpier = CSharpierStep.Find(root);

        // What CSharpier would change before the runs: its folders where that is nothing get formatted whole after a run, one
        // options lookup each instead of one per changed file (Speckle: 242 files, ~20 s named one by one).
        // Not for named files (--files): a few files named cost less than the check.
        var unformatted = csharpier is null || args.Contains("--include") ? null : CSharpierStep.Unformatted(csharpier, root);
        var formatted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var run = 1; ; run++)
        {
            File.Delete(keptFile);
            var before = Directory.Exists(root) ? Snapshot(root) : new Dictionary<string, string>();
            var beforeText = Directory.Exists(root) ? Snapshot(root, endingsAside: true) : new Dictionary<string, string>();

            // --json: each run's changes per rule come from its own report.
            var report = JsonReport.Current is null ? null : Path.Combine(Path.GetTempPath(), $"stylebro-format-json-{Guid.NewGuid():N}");
            var runArgs = report is null ? args : args.Concat(new[] { "--report", report }).ToArray();

            // What it fixes and the framework list are said once, not every run. A fix that failed leaves its findings: no
            // run is clean then (akka: BRO1309's fix threw in every run, and the last said "clean").
            var failures = new SortedSet<string>(StringComparer.Ordinal);
            var incomplete = false;
            var runPhase = PhaseTimes.Measure($"run {run}");
            var code = runOnce(runArgs, line =>
            {
                if (FixFailure(line) is { } failure)
                {
                    failures.Add(failure);
                }

                incomplete |= line.StartsWith(IncompleteMarker, StringComparison.Ordinal) || line.StartsWith(PartlyIncompleteMarker, StringComparison.Ordinal);

                if (run == 1)
                {
                    (output ?? Console.WriteLine)(line);
                }
                else if (!Headers.Any(h => line.StartsWith(h, StringComparison.Ordinal)))
                {
                    log(line);
                }
            });
            runPhase.Dispose();

            // The run checked every plan entry, clean apart from kept findings, and loaded every project: verify's checks find the
            // same on this state (VerifyFromRecord).
            var checkedClean = TakeCheckedClean(root) && !incomplete;

            // Usings the run's fixes left unused ('Int32' -> 'int' keeps 'using System;') go in the same run, where IDE0005 is
            // enforced: otherwise they took a run of their own, and a third run said clean (trial 21).
            if (code == 0 && !args.Contains("--diagnostics") && !args.Contains("--all") && !MissingCompileRoots.ContainsKey(root) && EnforcesUnusedUsings(root) && ChangedFiles(before, Snapshot(root)).Count > 0)
            {
                var usingsOutput = new List<string>();
                using (PhaseTimes.Measure($"run {run}: unused usings (IDE0005)"))
                {
                    code = runOnce([.. args, "--no-restore", "--diagnostics", UnusedUsings], usingsOutput.Add);
                }

                if (code != 0)
                {
                    usingsOutput.ForEach(log);
                }

                failures.UnionWith(usingsOutput.Select(FixFailure).OfType<string>());
            }

            allFailures.UnionWith(failures);

            // Documentation comment lines the whitespace pass leaves and the build's IDE0055 reports (DocIndentation).
            if (code == 0)
            {
                DocIndentation.FixFiles(DocIndentationFindings(root, args));
            }

            var fixedState = Snapshot(root);
            var changed = ChangedFiles(before, fixedState);
            var putBack = new List<string>();
            var putBackFiles = new List<string>();

            // And the files whose line endings aren't end_of_line's, which CSharpier's check fails on (trial 26).
            var endings = csharpier is null || code != 0 ? [] : CSharpierStep.WrongEndings(root, CSharpierStep.InScope(csharpier, root, fixedState.Keys)).Except(changed, StringComparer.OrdinalIgnoreCase).ToList();
            if (code == 0 && csharpier is not null && CSharpierStep.InScope(csharpier, root, changed.Concat(endings)) is { Count: > 0 } pretty)
            {
                static string Files(int count) => $"{count} C# file{(count == 1 ? string.Empty : "s")}";
                var ran = pretty.Count - endings.Count;
                log($"CSharpier: formatting {string.Join(" and ", new[] { ran > 0 ? $"the {Files(ran)} run {run} changed" : null, endings.Count > 0 ? $"{Files(endings.Count)} whose line endings aren't end_of_line's" : null }.OfType<string>())}...");
                (int, List<string>) csharpierRun;
                using (PhaseTimes.Measure($"run {run}: CSharpier"))
                {
                    csharpierRun = CSharpierStep.Run(csharpier, "format", CSharpierStep.Arguments(csharpier, root, pretty, unformatted));
                }

                var (csharpierCode, csharpierOutput) = csharpierRun;
                if (csharpierCode != 0)
                {
                    csharpierOutput.ForEach(log);
                    log($"CSharpier failed (exit code {csharpierCode}): the files 'format' changed may not be laid out as CSharpier wants. Run 'dotnet csharpier format' on them.");
                    code = csharpierCode;
                }

                formatted.UnionWith(pretty);
                var after = Snapshot(root);
                putBackFiles = CSharpierStep.PutBack(before, fixedState, after);
                putBack = putBackFiles.Select(f => Path.GetRelativePath(shown, f).Replace('\\', '/')).ToList();
                changed = ChangedFiles(before, after);
            }

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
                    ListFiles(shown, changed, int.MaxValue).ForEach(log);
                }

                return code;
            }

            // A run that changed only line endings or byte order marks (end_of_line, charset) is the last: no fix has anything new
            // to do (bencodex: a second run checked every framework again for 28 s to say clean).
            var endingsOnly = changed.Count > 0 && failures.Count == 0 && putBack.Count == 0
                && changed.All(f => File.Exists(f) && beforeText.TryGetValue(f, out var was) && was == Hash(File.ReadAllBytes(f), endingsAside: true));

            // Not "clean" when CSharpier put back what the fixes changed (trial 26: "Run 1: 0 files changed, clean." then "Not clean").
            log($"Run {run}: {changed.Count} file{(changed.Count == 1 ? string.Empty : "s")} changed{(endingsOnly ? ", line endings or byte order marks only (no run needed after it), clean:" : changed.Count > 0 ? ":" : failures.Count > 0 ? ", but code fixes failed (below)." : putBack.Count > 0 ? ", but CSharpier put back what the fixes changed (below)." : ", clean.")}");
            ListFiles(shown, changed, 10).ForEach(log);
            allChanged.UnionWith(changed);
            if (changed.Count == 0 || endingsOnly || maxRuns == 1)
            {
                var keptFindings = ReadKept(keptFile, root, root);
                var settled = (changed.Count == 0 || endingsOnly) && failures.Count == 0;
                var baselined = settled ? BaselineKept(root, keptFindings, shown) : null;
                if (settled)
                {
                    // The run's check reported the kept findings (and nothing else), so verify's does, and ends with these lines.
                    // Not after a run that changed files (endings only): its check was of the files before.
                    SaveKept(root, args, keptFindings, checkedClean && changed.Count == 0 ? CleanVerdict(root, keptFindings) : null, UncheckedByFormat.TryRemove(Key(root), out var builds) ? builds : null, GeneratorLines.TryRemove(Key(root), out var generatorLine) ? generatorLine : null);
                }

                JsonReport.AddKept(keptFindings);
                KeptSummary(keptFindings, shown, clean: settled).ForEach(log);
                if (baselined is not null)
                {
                    log(baselined);
                }

                if (BaselineOffLine(root, keptFindings.Count) is { } off)
                {
                    log(off);
                }

                AfterNotes(root, allChanged, formatted.Count).ForEach(log);
                if (allFailures.Count > 0)
                {
                    log(FixFailuresLine(allFailures));
                    return NotCleanExitCode;
                }

                if (putBack.Count > 0)
                {
                    // Which rules' fixes CSharpier undoes: a check of those files (they're as before the fixes again).
                    var check = new List<string>();
                    runOnce([.. WithInclude(NoRestore(args).Where(a => a != CheckFirstOption && a != "--verify-no-changes"), putBackFiles.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))), "--verify-no-changes"], check.Add);
                    log(CSharpierStep.PutBackLine(putBack, FindingIds(check.Where(l => FindingPath(l) is { } path && putBackFiles.Contains(path, StringComparer.OrdinalIgnoreCase)))));
                    return NotCleanExitCode;
                }

                return 0;
            }

            if (run == maxRuns)
            {
                // The kept findings too: with TreatWarningsAsErrors they fail the build whether or not the rest settles.
                var keptFindings = ReadKept(keptFile, root, root);
                JsonReport.AddKept(keptFindings);
                KeptSummary(keptFindings, shown, clean: false).ForEach(log);
                AfterNotes(root, allChanged, formatted.Count).ForEach(log);
                if (allFailures.Count > 0)
                {
                    log(FixFailuresLine(allFailures));
                }

                log($"Not clean: still changing after {MaxRuns} runs. Run 'bro format' again, and if it keeps changing the same lines, please report it.");
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
        var usageError = false;
        var nuGetErrors = new List<string>();
        string? restoreWarning = null;
        void Record(string line)
        {
            restoreFailed |= IsRestoreFailure(line);
            crashed |= IsCrash(line);
            usageError |= line.StartsWith("Unrecognized command or argument", StringComparison.Ordinal);
            if (line.Contains("error NU", StringComparison.Ordinal))
            {
                nuGetErrors.Add(line);
            }

            if (SkippedProject(line) is { } skippedName)
            {
                var (project, framework) = SkippedFor(skippedName, current);
                (skipped.TryGetValue(project, out var set) ? set : skipped[project] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)).Add(framework);
            }

            if (UnsupportedProject(line) is { } other)
            {
                unsupported.Add(other);
            }
        }

        // A failed fix is said once per document (akka: 120 times in one run): once is enough.
        var failuresShown = new HashSet<string>(StringComparer.Ordinal);
        var findingsShown = 0;
        var findingsIgnored = 0;
        void Watch(string line)
        {
            if (Ignored(line))
            {
                findingsIgnored++;
                return;
            }

            findingsShown += IsFindingLine(line) ? 1 : 0;
            Record(line);
            if ((FixFailure(line) is not { } failure || failuresShown.Add(failure)) && Explain(line, current, restoreWarning) is { } shownLine)
            {
                log(shownLine);
            }
        }

        // Like 'dotnet format': an optional folder, solution or project first, then options, which pass through (not format's own).
        args = args.Where(a => a != TimingsOption).ToArray();
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
            error(BaselineCommand.NoWorkspace(root, "'bro format <file>'"));
            return 1;
        }

        // A '#' swapped for another character with no record of it (DirectiveMask): formatting would only make it worse.
        if (DirectiveMask.Damaged(root) is { Count: > 0 } damaged)
        {
            error(DamagedLine(damaged));
            return 1;
        }

        // Another run's swapped files (its 'dotnet format' may outlive a killed 'format'): they come back when it ends.
        if (DirectiveMask.Busy(root) is { Count: > 0 } busy)
        {
            error($"Stopped: another 'bro format' (or the 'dotnet format' it started, still running after it was stopped) has swapped the '#' of protected lines in {string.Join(", ", busy.Take(5))}{(busy.Count > 5 ? $" and {busy.Count - 5} more" : string.Empty)}. Wait until it ends (the next command puts them back), then run again.");
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
        else if (File.Exists(path))
        {
            root = WorkingRoot(workspacePath); // like Run's: the folder that holds all the projects
        }

        // By default only StyleBro's rules and the built-in ones init/migrate turn on: plain 'dotnet format' also applies
        // every other analyzer's fixes and compiler fixes (CS8618's 'required', a Sonar fix removing 'init;': build
        // errors in Kavita). Whitespace formatting still runs. --all keeps them.
        if (!passThrough.Remove("--all") && !passThrough.Contains("--diagnostics"))
        {
            var ids = Diagnostics(root);
            log(RulesLine(passThrough.Contains("--verify-no-changes"), ids.Count, ids.Contains(Analyzers.Baseline.BaselineSuppressor.XunitIds[0])));
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

        // Files whose usings the team's ordering rule accepts and its own sort key doesn't (Splat: StyleSharp's SST1210): 'dotnet
        // format' sorts them whenever the key is set and can't skip only that, so they're left out.
        if (Migration.KeptUsingsFiles(root) is { Count: > 0 } keptUsings)
        {
            log($"{UnformattedHeader} {string.Join(", ", keptUsings)}: their usings follow the ordering rule your team enforces, not the sort key in .editorconfig, which 'dotnet format' would apply (the stylebro-migrate block names them).");
            Exclude(keptUsings);
        }

        // Files a T4 template writes (owner's choice 2026-10-17, trial 28: ZLogger): the template writes them again, so they
        // are generated code; the package's build targets hide StyleBro's findings there too (GeneratedFileSuppressor).
        if (Migration.TextTemplateOutputs(root) is { Count: > 0 } templates)
        {
            Exclude(templates);
        }

        // The SDK's formatter adds a line break to a file with old Mac line endings on every run: it never settles.
        if (OldMacLineEndings(root) is { Count: > 0 } oldMac)
        {
            log($"{OldMacHeader} {string.Join(", ", oldMac)}: old Mac line endings (a CR alone). 'dotnet format' (a .NET SDK bug) adds a line break to such a file on every run, so it never settles. Convert them to LF or CRLF, then run again.");
            Exclude(oldMac);
        }

        // 'dotnet format' runs its whitespace pass whatever IDE0055's severity says: where the settings turn IDE0055 off,
        // only the style and analyzer passes run (LiteBus: aligned switch arms flattened, '(T)x' became '(T) x').
        // Where it's off only in some folders (CSharpier formats one), the whitespace pass runs on its own and leaves them out.
        var whitespaceExclusions = WhitespaceExclusions(root);
        if (whitespaceExclusions is { Count: 0 })
        {
            log($"{NoWhitespaceHeader} IDE0055 is off in the settings, so no whitespace formatting: style and analyzer fixes only.");
        }
        else if (whitespaceExclusions is not null && NotExcluded(whitespaceExclusions, excluded) is { Count: > 0 } shownExclusions)
        {
            // Not the folders format leaves alone whole (trial 26: "Leaving alone: Benchmarks/", then "style and analyzer fixes
            // still run there").
            log(NoWhitespaceLine(root, shownExclusions));
        }

        // A CR alone in a string: the whitespace and style passes write end_of_line's line break there (trial 24).
        var styleExclusions = LoneCrStringFiles(root);
        if (styleExclusions.Count > 0)
        {
            log(LoneCrStringLine(styleExclusions));
            if (whitespaceExclusions is not { Count: 0 })
            {
                whitespaceExclusions = [.. whitespaceExclusions ?? [], .. styleExclusions.Where(f => whitespaceExclusions?.Any(e => Covers(e, f)) != true)];
            }
        }

        var passes = Passes(whitespaceExclusions);

        // Lines the SDK's formatter would break (a Roslyn bug) are protected while the runs format.
        var directiveFiles = whitespaceExclusions is { Count: 0 } ? [] : DirectiveStringFiles(root);
        if (directiveFiles.Count > 0)
        {
            log(DirectiveStringLine(directiveFiles));
        }

        using var directiveMask = new DirectiveMask(root, directiveFiles);

        int Format(string target, List<string> options, Func<IEnumerable<string>, int> run, string[]? runPasses = null)
        {
            // Each pass would write its own format-report.json over the last one: each gets a folder, merged after.
            runPasses = Relevant(runPasses ?? passes, OptionValues(options, "--diagnostics"));
            var at = options.IndexOf("--report");
            var report = runPasses.Length > 1 && at >= 0 && at + 1 < options.Count ? options[at + 1] : null;
            var folders = new List<string>();
            var exit = 0;
            foreach (var pass in runPasses)
            {
                var passOptions = options.ToList();
                if (report is not null)
                {
                    folders.Add(Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}"));
                    passOptions[at + 1] = folders[^1];
                }

                if (pass == "whitespace")
                {
                    passOptions = WhitespaceOptions(passOptions, whitespaceExclusions!);
                }
                else if (pass == "style" && styleExclusions.Count > 0)
                {
                    passOptions = Excluding(passOptions, styleExclusions);
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
        var forceRestore = passThrough.Remove(RestoreOption);
        var checkFirst = passThrough.Remove(CheckFirstOption);
        var checkedKey = Key(root);
        CheckedClean.TryRemove(checkedKey, out _);
        UncheckedByFormat.TryRemove(checkedKey, out _);
        GeneratorLines.TryRemove(checkedKey, out _);
        string[] restoreOptions = passThrough.Remove(UnlockedRestoreOption) ? [.. RestoreOptions, "-p:RestoreLockedMode=false"] : RestoreOptions;

        // The findings the fixes keep, for a check after this run (Remember): the caller's file, else one of its own.
        var keptFile = Environment.GetEnvironmentVariable(KeptFinding.Variable);
        using var ownKept = string.IsNullOrEmpty(keptFile) && !passThrough.Contains("--verify-no-changes")
            ? new KeptVariable(keptFile = Path.Combine(Path.GetTempPath(), $"stylebro-kept-{Guid.NewGuid():N}.tsv"))
            : null;

        // A solution filter's per-framework filters and restore filter name its solution.
        var solutionPath = IsSolutionFilter(workspacePath) ? ReadSolutionFilter(workspacePath).Solution! : workspacePath;
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        var isSolution = !workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
        DeleteLeftoverFilters(solutionDirectory);
        Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers)> projects;
        using (PhaseTimes.Measure("plan: projects (solution list, evaluations)"))
        {
            projects = ReadProjects(workspacePath);
        }

        if (projects.Count == 0)
        {
            error($"No C# project in {Path.GetFileName(workspacePath)}, so there's nothing to format. Name a solution or project with C# projects.");
            return 1;
        }

        // Files a project compiles that aren't there (a sibling repository not checked out, the preview's copy without it): the
        // code using them doesn't compile, and IDE0005 would remove the usings it needs.
        var missing = projects.Values.SelectMany(p => p.Compile).Where(f => !File.Exists(f)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
        {
            log(MissingCompileLine(root, missing));
            MissingCompileRoots[root] = true;
            passThrough = WithoutUnusedUsings(passThrough);
        }

        // Visual Basic is left alone (owner's choice, trial 23): the runs load the C# projects only (a solution filter, or what
        // they reference), format their files only, and a .vb file a fix still reaches is put back. Before, the plain run
        // reformatted and renamed in a .vb file without listing it, and verify reported VB findings as StyleBro's.
        var visualBasic = VisualBasicProjects(workspacePath, projects.Values.SelectMany(p => p.References));
        if (visualBasic.Count > 0)
        {
            log($"{VisualBasicHeader} {string.Join(", ", visualBasic.Take(3))}{(visualBasic.Count > 3 ? $" and {visualBasic.Count - 3} more" : string.Empty)}: Visual Basic projects are left alone, 'format' runs on the C# projects only.");
            Exclude(["**/*.vb"]);
        }

        using var visualBasicFiles = visualBasic.Count > 0 ? new UntouchedFiles(root, ".vb", log) : null;

        // Files the projects compile from outside the repository ('..\..\outside\Shared.cs' linked in): someone else's code, like
        // a submodule's (owner's decision 2026-10-16). They were rewritten without being listed; now they're put back after the
        // runs, and their findings don't count.
        var outside = OutsideRepository(projects.Values.SelectMany(p => p.Compile), RepositoryRoot(root) ?? root);
        if (outside.Count > 0)
        {
            log(OutsideRepositoryLine(root, outside));
        }

        // Quietly: the whitespace pass touches them on every run, and the line above already says they're left alone.
        using var outsideFiles = outside.Count > 0 ? new UntouchedFiles(outside, null, log) : null;
        var outsideSet = outside.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Nor do their '#if's or a package's files plan runs (trial 27): format leaves them alone. ponytail: for this process.
        outside.ForEach(f => TestedSymbolsCache[Path.GetFullPath(f)] = []);
        bool Planned(string file) => !outsideSet.Contains(file) && !IsPackageFile(file);
        bool Ignored(string line) => outsideSet.Count > 0 && IsFindingLine(line) && FindingPath(line) is { } findingPath && outsideSet.Contains(findingPath);

        // Projects that need a workload this machine doesn't have (trial 30, owner's choice 2026-10-18: AvaloniaVisualBasic6's
        // Android, iOS and Browser heads): 'dotnet restore' failed on them (NETSDK1147) and stopped the whole run, though the code
        // they share built. They're left alone, like a build of the other projects leaves them; the rest is formatted.
        var needWorkloads = projects
            .Select(p => (p.Key, p.Value.FullPath, Packs: MissingWorkloadPacks(EvaluateProject(p.Value.FullPath))))
            .Where(p => p.Packs.Length > 0)
            .ToList();
        if (needWorkloads.Count > 0)
        {
            var folders = needWorkloads
                .Select(p => Path.GetRelativePath(root, Path.GetDirectoryName(p.FullPath)!).Replace('\\', '/'))
                .Where(f => f != "." && !f.StartsWith("..", StringComparison.Ordinal))
                .Select(f => f + "/")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
            log(MissingWorkloadsLine(needWorkloads.Select(p => Path.GetRelativePath(root, p.FullPath).Replace('\\', '/')).ToList(), needWorkloads.SelectMany(p => p.Packs).ToList()));
            projects = new(projects, projects.Comparer); // the list is cached for the process
            needWorkloads.ForEach(p => projects.Remove(p.Key));
            if (projects.Count == 0)
            {
                return Done(1);
            }

            if (folders.Count > 0)
            {
                Exclude(folders);
                excluded.AddRange(folders);
            }
        }

        // Projects the solution lists but no configuration builds: the restore skips them, so StyleBro wouldn't run there, and
        // only the whitespace pass and the SDK's rules would (Bogus's Nuke builder: its usings were sorted, and the run warned
        // "Without StyleBro"). They're left alone, like the build leaves them.
        if (NotBuiltFolders(solutionPath, projects.Values.Select(p => p.FullPath).ToList(), root) is { Count: > 0 } notBuilt)
        {
            log($"{NotBuiltHeader} {string.Join(", ", notBuilt)}: no configuration of {Path.GetFileName(solutionPath)} builds {(notBuilt.Count == 1 ? "the project there" : "their projects")}, so 'dotnet restore' skips {(notBuilt.Count == 1 ? "it" : "them")} and 'format' leaves {(notBuilt.Count == 1 ? "it" : "them")} alone.{NotBuiltHint(notBuilt, root)}");
            Exclude(notBuilt);
            excluded.AddRange(notBuilt);
        }

        // One restore before the runs, which then all get --no-restore. Old-style projects are left out: 'dotnet restore'
        // rewrote their restore output (xbox-live-developer-tools: the next Visual Studio build failed, "doesn't list 'win'
        // as a RuntimeIdentifier") and can't restore packages.config at all. Skipping the restore for the whole repository
        // instead left the SDK-style projects without the just-added StyleBro.Analyzers (actions/runner: "clean").
        // Not when the restore output is current (owner's decision 2026-10-17): newer than every input, listing StyleBro.Analyzers
        // (RestoreIsCurrent); --restore restores anyway. A no-op 'dotnet restore' still took 2-18 s (PalCalc 17.5 s).
        var restored = RestoredProjects(projects.Values.Select(p => (p.FullPath, p.AssetsFile)));
        var restoreCurrent = SkipsRestore(noRestore, forceRestore, () => RestoreIsCurrent(root, restored));
        if (restoreCurrent)
        {
            PhaseTimes.Measure("restore: skipped, up to date").Dispose();
            restoreWarning = SkippedRestoreWarning(root, restored);
        }

        if (!noRestore && !restoreCurrent)
        {
            using var restorePhase = PhaseTimes.Measure("restore");
            var oldStyle = projects.Values.Select(p => p.FullPath).Where(IsOldStyle).ToList();
            var restoreTarget = workspacePath;
            string? restoreFilter = null;
            if (oldStyle.Count > 0 || needWorkloads.Count > 0)
            {
                if (oldStyle.Count > 0)
                {
                    log(OldStyleNote(oldStyle.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).Order(StringComparer.OrdinalIgnoreCase).ToList(), oldStyle.Any(UsesPackagesConfig)));
                }

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
                var restore = restoreTarget is null ? 0 : Dotnet(root, null, new[] { "restore", restoreTarget }.Concat(restoreOptions), restoreOutput.Add);
                if (restore != 0)
                {
                    restoreOutput.ForEach(Watch);
                    restoreFailed = true;
                    return Done(restore);
                }

                restoreWarning = RestoreWarning(root, restoreOutput);
                SaveRestoreStamp(root, restored.Select(p => p.AssetsFile), restoreWarning);
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
        using (PhaseTimes.Measure("generators: build check"))
        {
            BuildGenerators(projects.Values.SelectMany(p => p.Analyzers), root, log);
        }

        // StyleBro rules a project lists in WarningsNotAsErrors: 'dotnet format' skips them, the build reports them as warnings
        // (trial 24: "clean", then BRO1001 and BRO1505 in the build). The runs take them out of the list.
        var notAsErrors = NotAsErrors(projects.Values.Select(p => EvaluatedProperty(p.FullPath, "WarningsNotAsErrors")));
        if (notAsErrors.Count > 0)
        {
            log(NotAsErrorsLine(notAsErrors));
        }

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

        var allFrameworks = Plan(projects.ToDictionary(p => p.Key, p => p.Value.Frameworks));
        var byFullPath = projects.ToDictionary(p => p.Value.FullPath, p => p.Key, StringComparer.OrdinalIgnoreCase);
        string? Spelling(string fullPath, string framework) => projects[byFullPath[fullPath]].Frameworks is var own && framework.Length == 0
            ? (own.Length == 1 ? own[0] : null)
            : own.FirstOrDefault(f => NormalizeFramework(f) == framework);

        // The projects that share files, and the multi-targeted ones: their settings for each run's framework, evaluated at
        // once (one 'dotnet msbuild' each), together with the configurations' (ConfigurationRuns: one wave, not two).
        var planPhase = PhaseTimes.Measure("plan: frameworks and configurations (evaluations)");
        var sharing = SharingProjects(projects.Values.Select(p => (p.FullPath, p.Compile.Where(Planned).ToArray())), byName: false).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var multiTargeted = projects.Values.Where(p => p.Frameworks.Length > 1).Select(p => p.FullPath).ToList();
        var frameworkViews = allFrameworks.Select(r => r.Framework).DefaultIfEmpty(string.Empty)
            .SelectMany(f => sharing.Union(multiTargeted, StringComparer.OrdinalIgnoreCase).Select(s => (Project: s, Framework: f, Configuration: string.Empty)))
            .ToList();
        var views = new System.Collections.Concurrent.ConcurrentDictionary<(string, string, string), CompileView?>();
        CompileView? View(string fullPath, string framework, string configuration = "") => views.GetOrAdd(
            (fullPath, framework, configuration),
            _ => byFullPath.ContainsKey(fullPath) && Spelling(fullPath, framework) is { } spelling ? ReadView(CompileSettings(fullPath, spelling, configuration)) : null);

        // The configurations' evaluations (and the frameworks' above), all at once.
        var frameworkRuns = allFrameworks.Count == 0 ? new List<(string Framework, List<string> Projects)> { (string.Empty, projects.Keys.ToList()) } : allFrameworks;
        var withGenerator = new List<string>();
        var configurationRuns = ConfigurationRuns(projects, frameworkRuns, workspacePath, View, withGenerator, frameworkViews);

        // A framework whose code compiles the same as another's in a project (CommunityToolkit: no '#if' tests a NET9_0 or
        // NET10_0 symbol, so net8.0, net9.0 and net10.0 parse every file the same) doesn't need a run of its own there.
        string? MergeKey(string key, string framework) =>
            projects[key].Frameworks.Length <= 1 ? framework : View(projects[key].FullPath, framework) is { } view ? FrameworkFamily(framework) + "|" + view.Key(view.Compile) : null;
        var frameworkPlan = MergeFrameworks(allFrameworks, MergeKey);

        // The files a project compiles for a run's framework: a multi-targeted project's outer evaluation misses those only some
        // frameworks compile (refit: src/Polyfills/*.cs linked for net4* only were never formatted, and verify said clean).
        string[] CompileFor(string fullPath, string framework, string configuration = "") =>
            byFullPath.TryGetValue(fullPath, out var key)
                ? FrameworkCompile(projects[key].Compile, projects[key].Frameworks.Length > 1 || configuration.Length > 0 ? View(fullPath, framework, configuration)?.Compile : null).Where(Planned).ToArray()
                : [];
        IEnumerable<string> LinkedFor(string key, string framework, string configuration) => Linked(projects[key].FullPath, projects[key].Linked, CompileFor(projects[key].FullPath, framework, configuration));

        // "" for a run without a framework of its own (no project is multi-targeted).
        var runs = (allFrameworks.Count == 0 ? frameworkRuns : frameworkPlan).Select(r => (r.Framework, Configuration: string.Empty, r.Projects))
            .Concat(configurationRuns)
            .ToList();
        List<List<string>> Apart(string framework, string configuration, List<string> selected) => Separate(
                selected.Select(p => projects[p].FullPath),
                r => byFullPath.TryGetValue(r, out var key) ? projects[key].References : Array.Empty<string>(),
                c => CompileFor(c, framework, configuration),
                (s, file) => View(s, framework, configuration)?.Key([file]))
            .Select(g => g.Select(f => byFullPath[f]).ToList())
            .ToList();

        // How the projects the configuration runs load (and their references) compile there: Separate asks for each, which
        // were evaluated one at a time before (PalCalc: 11 of them, ~3 s each).
        RunViews(configurationRuns, r => byFullPath.TryGetValue(r, out var key) ? projects[key].References : Array.Empty<string>())
            .Where(v => byFullPath.ContainsKey(v.Project))
            .AsParallel()
            .ForAll(v => View(v.Project, v.Framework, v.Configuration));

        string RunLabel(string framework, string configuration) => string.Join(", ", new[] { framework.Length == 0 && allFrameworks.Count > 0 ? NoFrameworkLabel : framework, configuration }.Where(l => l.Length > 0));
        var plan = runs
            .SelectMany(p => Apart(p.Framework, p.Configuration, p.Projects) is var parts && parts.Count > 1
                ? parts.Select((part, i) => (p.Framework, p.Configuration, Projects: part, Label: $"{(RunLabel(p.Framework, p.Configuration) is { Length: > 0 } label ? label + ", " : string.Empty)}part {i + 1} of {parts.Count}", Parts: i == 0 ? parts : null))
                : new[] { (p.Framework, p.Configuration, p.Projects, Label: RunLabel(p.Framework, p.Configuration), Parts: (List<List<string>>?)null) })
            .ToList();
        planPhase.Dispose();

        // Lines '#if' makes an if/else body in some runs and not in others (owner's decision 2026-10-16): each run indented them
        // its own way, undoing the run before (NLog never converged). They keep their indentation, and the whitespace findings
        // there don't count.
        var ifBodies = plan.Count > 1 ? IfBodyLines(root, plan.Select(p => (p.Framework, p.Configuration, p.Projects)), (key, framework, configuration) =>
            (projects[key].FullPath, CompileFor(projects[key].FullPath, framework, configuration), View(projects[key].FullPath, framework, configuration)?.Defines)) : [];
        if (ifBodies.Count > 0)
        {
            log(IfBodies.Line(IfBodiesHeader, root, ifBodies));
        }

        using var keptIndentation = new IfBodies.Kept(ifBodies);

        // One run for everything: plain, unless it's the run after one that changed files, which checks first (and then formats
        // only the files with findings).
        // The generators the other configurations' runs load, built in those configurations first (owner's decision 2026-10-17).
        foreach (var configuration in configurationRuns.Select(r => r.Configuration).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            using var generatorPhase = PhaseTimes.Measure($"generators: build check ({configuration})");
            BuildGenerators(configurationRuns.Where(r => r.Configuration.Equals(configuration, StringComparison.OrdinalIgnoreCase)).SelectMany(r => r.Projects).SelectMany(p => projects[p].Analyzers), root, log, configuration);
        }

        if (withGenerator.Count > 0)
        {
            // --verify-no-changes still builds them in the others afterwards (UncheckedLine): verify said clean while that build
            // failed, and the build is the check of the generated code's configuration.
            // A 'format' run keeps them for its record, which verify answers from (VerifyFromRecord).
            var verifying = passThrough.Contains("--verify-no-changes");
            var solutionConfigurations = SolutionConfigurations(workspacePath);
            var notChecked = withGenerator.SelectMany(k => OtherConfigurations(EvaluatedProperty(projects[k].FullPath, "Configurations"), EvaluatedProperty(projects[k].FullPath, "Configuration"), solutionConfigurations, EvaluatedProperty(projects[k].FullPath, "Platforms"))
                .Select(c => (projects[k].FullPath, c))).ToList();
            if (verifying)
            {
                log(GeneratorConfigurationsLine(withGenerator.Select(k => Path.GetFileNameWithoutExtension(k)).ToList()));
                RecordUnchecked(root, notChecked);
            }
            else
            {
                UncheckedByFormat[checkedKey] = notChecked;
                GeneratorLines[checkedKey] = GeneratorConfigurationsLine(withGenerator.Select(k => Path.GetFileNameWithoutExtension(k)).ToList());
            }
        }

        var whole = plan.Count <= 1 && allFrameworks.Count == 0 && visualBasic.Count == 0 && needWorkloads.Count == 0;
        var select = Path.Combine(Path.GetTempPath(), $"stylebro-format-{Guid.NewGuid():N}.targets");
        File.WriteAllText(select, SelectFrameworkTargets);
        if (whole && !(checkFirst && !passThrough.Contains("--report")))
        {
            try
            {
                var plain = notAsErrors.Count > 0 ? new RunEnvironment(string.Empty, select, string.Empty, string.Empty, string.Empty, null, Warnings: true) : null;
                int single;
                using (PhaseTimes.Measure(passThrough.Contains("--verify-no-changes") ? "check: all projects" : "fix: all projects"))
                {
                    single = Format(workspacePath, passThrough, a => Dotnet(root, plain, a, Watch));
                }

                if (single == NotCleanExitCode && passThrough.Contains("--verify-no-changes") && findingsIgnored > 0 && findingsShown == 0)
                {
                    single = 0; // only findings in files format leaves alone
                }

                Incomplete(skipped, new Dictionary<string, List<string>>(), unsupported).ForEach(log);
                Crashed(projects.Keys);
                return Done(single);
            }
            finally
            {
                File.Delete(select);
            }
        }

        if (allFrameworks.Count > 0)
        {
            log(MultiTargetedLine(allFrameworks, frameworkPlan));
        }

        if (ConfigurationsLine(configurationRuns) is { } configurationsLine)
        {
            log(configurationsLine);
        }

        // Which projects each framework's parts hold: the ones that share files (else the part's own), by name.
        List<string> Names(List<string> part) => (part.Where(x => sharing.Contains(projects[x].FullPath)).ToList() is { Count: > 0 } own ? own : part)
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var shared = plan.Where(p => p.Parts is not null).Select(p => p.Parts!.Select(Names).ToList()).ToList();
        if (SharedFilesLine(shared) is { } sharedLine)
        {
            log(sharedLine);
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
            if (IsNewLine(shown, line) && Explain(line, current, restoreWarning) is { } shownLine)
            {
                log(shownLine);
            }
        }

        // A run loads only the projects that target its framework: the renaming fixes check the names of all the C# files.
        // --files names the whole repository's already (the root here is the files' project). One run for everything loads them
        // all, like the plain run: no names.
        var givenNames = Environment.GetEnvironmentVariable(RepositoryNames.Variable);
        var names = !string.IsNullOrEmpty(givenNames) ? givenNames! : whole ? string.Empty : Path.Combine(Path.GetTempPath(), $"stylebro-names-{Guid.NewGuid():N}.tsv");
        if (string.IsNullOrEmpty(givenNames) && !whole)
        {
            WriteRepositoryNames(root, names);
        }

        // Each run's own report folder, in the plan's order (the runs may run at once).
        reportFolders.AddRange(plan.Select(_ => Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}")).Where(_ => report is not null));

        // Each run formats its own projects' files ('--include'); named files only in the runs whose projects have them.
        // Projects the workspace doesn't list but its projects reference: the run that loads them formats them too.
        // ponytail: not in the configuration runs' plan; such a project's '#if DEBUG' code is formatted in the default one only.
        var outsideRuns = OutsideProjectRuns(
            plan.Select(p => (p.Framework, p.Configuration, (IReadOnlyList<string>)p.Projects.Select(x => projects[x].FullPath).ToList())).ToList(),
            projects.Values.ToDictionary(p => p.FullPath, p => (p.Frameworks, p.References), StringComparer.OrdinalIgnoreCase),
            p =>
            {
                var (frameworks, references, _, _, _, _) = ParseProject(EvaluateProject(p), Path.GetDirectoryName(p));
                return (frameworks, references);
            },
            root,
            out var outsideLeft);
        if (outsideLeft.Count > 0)
        {
            log($"{UnformattedHeader} {string.Join(", ", outsideLeft.Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')))}: projects the build compiles that {Path.GetFileName(workspacePath)} doesn't list, and no run of one of their frameworks loads (a multi-targeted one would get its copies merged). Add them to the solution, or run 'bro format' on each.");
        }

        var requested = OptionValues(passThrough, "--include");
        var includes = plan.Select((p, i) => RunInclude(requested, Include(root, solutionDirectory, p.Projects.Concat(outsideRuns.TryGetValue(i, out var extra) ? extra : []), p.Projects.SelectMany(x => LinkedFor(x, p.Framework, p.Configuration))).ToList())).ToList();
        if (requested is not null && requested.Any(r => !includes.Any(i => i.Contains(r))))
        {
            // A named file no run's projects have (or a path that isn't one): every run gets them, as named.
            includes = plan.Select(_ => requested).ToList();
        }

        var toRun = Enumerable.Range(0, plan.Count).Where(i => includes[i].Count > 0).ToList();

        // Several runs check at once first, the first run too: then only the runs with findings format, on those files (trial
        // 24: Nerdbank.Streams' five frameworks took 355 s for a run that changed nothing, one 'dotnet format' after another).
        checkFirst |= toRun.Count > 1;
        int RunFramework(int index, bool verify, Action<string> output, IReadOnlyList<string>? only = null)
        {
            var (framework, configuration, selected, _, _) = plan[index];
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

                if (requested is not null)
                {
                    options.RemoveRange(options.IndexOf("--include"), requested.Count + 1);
                }

                var include = only ?? includes[index];
                options.Add("--include");
                options.AddRange(include);

                // A run without a framework of its own still gets the repository's names (it loads only some projects).
                var byPath = projects.Values.ToDictionary(p => p.FullPath, p => (p.Frameworks, p.References), StringComparer.OrdinalIgnoreCase);
                var keep = framework.Length == 0 ? new HashSet<string>() : Keep(byPath, framework);
                var (alias, aliases) = framework.Length == 0 ? (string.Empty, string.Empty) : Aliases(byPath, keep, framework);
                var other = framework.Length == 0 ? new HashSet<string>() : Other(byPath, keep, framework);
                var environment = new RunEnvironment(alias, select, PathList(keep), PathList(other), aliases, names, notAsErrors.Count > 0, configuration);
                return Format(filter ?? workspacePath, options, a => Dotnet(root, environment, a, output), Passes(whitespaceExclusions, include));
            }
            finally
            {
                if (filter is not null)
                {
                    File.Delete(filter);
                }
            }
        }

        // A run without a framework or configuration of its own is named by what it loads ("all projects" read as one, trial 25).
        string RunName(int index) => plan[index].Label.Length > 0 ? plan[index].Label
            : configurationRuns.Count > 0 ? "default configuration"
            : $"{plan[index].Projects.Count} project{(plan[index].Projects.Count == 1 ? string.Empty : "s")}";

        var exit = 0;
        void RunOrReplay(int index, (int Code, List<string> Lines)? ran, IReadOnlyList<string>? only = null)
        {
            // A replayed check with nothing to say isn't announced (splat's second run listed all 12 frameworks' runs).
            if (ran is null || ran.Value.Lines.Any(IsFindingLine))
            {
                log($"== {RunName(index)} ({plan[index].Projects.Count} project(s){(only is null ? string.Empty : $", {only.Count} file{(only.Count == 1 ? string.Empty : "s")} with findings")})");
            }

            current = plan[index].Framework;
            ran?.Lines.ForEach(Show);
            int code;
            using (ran is null ? PhaseTimes.Measure($"fix: {RunName(index)}") : null)
            {
                code = ran?.Code ?? RunFramework(index, verify: false, Show, only);
            }

            Crashed(plan[index].Projects);
            exit = exit == 0 ? code : exit;
        }

        try
        {
            // Runs that write nothing can run at once (one 'dotnet format' per framework), their output shown afterwards in
            // the plan's order: '--verify-no-changes', and the check before a run that likely changes nothing (after a run
            // that changed files). When every framework's check is clean (which '--verify-no-changes' isn't for findings a
            // fix keeps either), that is what the runs one after another would find; otherwise they run as usual.
            // A check that finds only the findings the last run's fixes kept (on purpose, with a reason) is clean too: they
            // stay whatever runs (splat: 9 kept findings made the check fail, and a second full run changed nothing, 43 minutes).
            // At most one run per processor: each 'dotnet format' loads a workspace (splat: ~100 processes at once before).
            var verifying = passThrough.Contains("--verify-no-changes");
            IReadOnlyCollection<KeptFinding> known = verifying ? [] : Remembered(root);
            var done = 0;
            var atOnce = verifying || (checkFirst && report is null)
                ? toRun.AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Max(1, Math.Min(toRun.Count, Environment.ProcessorCount)))
                    .Select(i =>
                    {
                        var output = new List<string>();
                        int checkCode;
                        using (PhaseTimes.Measure($"check: {RunName(i)}"))
                        {
                            checkCode = RunFramework(i, verify: true, output.Add);
                        }

                        var (code, lines) = IfBodies.Without(checkCode, output, l => Ignored(l) || IfBodies.IsKeptFinding(l, ifBodies));
                        lock (shown)
                        {
                            log($"{ProgressPrefix}{RunName(i)} (run {++done} of {toRun.Count})");
                        }

                        return (Index: i, Code: code, Lines: lines);
                    })
                    .ToDictionary(c => c.Index, c => (c.Code, c.Lines))
                : null;
            var replay = atOnce is not null && (verifying || IsCleanCheck(atOnce.Values.Select(c => (c.Code, (IReadOnlyList<string>)c.Lines)), known));
            var keptNow = new List<KeptFinding>();

            // Otherwise each run decides by its own check (CommunityToolkit: 2 of 21 checks found something, and all 21 ran
            // again): a clean one is replayed, one with findings formats only their files. Unless a run before it changed
            // files it formats too (a file two runs share): its check is out of date then, and it runs as usual.
            var touched = new List<string>();
            var state = atOnce is not null && !replay ? Snapshot(root) : null;
            List<string> Touched(int index) => touched.Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).Where(f => includes[index].Any(i => Covers(i, f))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            void Replay(int index)
            {
                // The kept findings at their lines now, listed after the run (the replay doesn't print them as findings).
                var lines = atOnce![index].Lines;
                keptNow.AddRange(lines.Select(l => AsKept(l, known)).OfType<KeptFinding>());
                RunOrReplay(index, (atOnce[index].Code == NotCleanExitCode ? 0 : atOnce[index].Code, lines.Where(l => AsKept(l, known) is null).ToList()));
            }

            foreach (var i in toRun)
            {
                if (replay && !verifying)
                {
                    Replay(i);
                }
                else if (replay || atOnce is null)
                {
                    RunOrReplay(i, replay ? atOnce![i] : null);
                }
                else if (AfterCheck(Touched(i), atOnce[i].Code, atOnce[i].Lines, known, root) is var (replayed, only) && replayed)
                {
                    Replay(i);
                }
                else
                {
                    RunOrReplay(i, null, only);
                    var now = Snapshot(root);
                    touched.AddRange(ChangedFiles(state!, now));
                    state = now;
                }

                // An option 'dotnet format' doesn't take fails every run the same way (CommunityToolkit: its usage text 21 times).
                if (usageError)
                {
                    break;
                }
            }

            // Every run's check was clean (not --verify-no-changes): what verify's checks would find on this state, which a clean
            // end of 'format' records for it (VerifyFromRecord).
            if (replay && !verifying && !usageError)
            {
                CheckedClean[checkedKey] = true;
            }

            // A run 'dotnet format' skipped a project in stands in for the frameworks merged into it: another of them runs
            // instead (trial 28, EpubReader: netstandard2.0 merged into netstandard1.3, which didn't load, so neither was checked).
            var tried = new HashSet<(string, string)>();
            Dictionary<string, IReadOnlyCollection<string>> SkippedByKey() => projects.Keys
                .Where(k => skipped.ContainsKey(Path.GetFileNameWithoutExtension(k)))
                .ToDictionary(k => k, k => (IReadOnlyCollection<string>)skipped[Path.GetFileNameWithoutExtension(k)], StringComparer.OrdinalIgnoreCase);
            for (var standIns = StandIns(allFrameworks, frameworkPlan, SkippedByKey(), MergeKey, tried); standIns.Count > 0 && !usageError; standIns = StandIns(allFrameworks, frameworkPlan, SkippedByKey(), MergeKey, tried))
            {
                foreach (var group in standIns.GroupBy(s => s.Framework, StringComparer.OrdinalIgnoreCase))
                {
                    var selected = group.Select(s => s.Project).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
                    plan.Add((group.Key, string.Empty, selected, $"{group.Key}, instead of {string.Join(", ", group.Select(s => s.Instead).Distinct(StringComparer.OrdinalIgnoreCase))}", null));
                    includes.Add(RunInclude(requested, Include(root, solutionDirectory, selected, selected.SelectMany(x => LinkedFor(x, group.Key, string.Empty))).ToList()));
                    if (report is not null)
                    {
                        reportFolders.Add(Path.Combine(Path.GetTempPath(), $"stylebro-format-report-{Guid.NewGuid():N}"));
                    }

                    frameworkPlan.Add((group.Key, selected));
                    RunOrReplay(plan.Count - 1, null);
                }

                // The skipped framework's code is covered once its stand-in loaded.
                foreach (var (framework, project, instead) in standIns)
                {
                    if (skipped.TryGetValue(Path.GetFileNameWithoutExtension(project), out var set) && !set.Contains(framework))
                    {
                        set.Remove(instead);
                        if (set.Count == 0)
                        {
                            skipped.Remove(Path.GetFileNameWithoutExtension(project));
                        }
                    }
                }
            }

            if (keptFile is not null && !verifying)
            {
                if (keptNow.Count > 0)
                {
                    File.AppendAllLines(keptFile, keptNow.Select(k => k.ToString()));
                }

                Remember(root, ReadKept(keptFile, root, root));
            }
        }
        finally
        {
            File.Delete(select);
            if (string.IsNullOrEmpty(givenNames) && names.Length > 0)
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
    private static Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers)> ReadProjects(string workspacePath)
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
        (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers) Evaluate(string p)
        {
            var fullPath = Path.GetFullPath(Path.Combine(directory, p.Replace('\\', Path.DirectorySeparatorChar)));
            var (frameworks, references, linked, compile, assets, analyzers) = ParseProject(EvaluateProject(fullPath), Path.GetDirectoryName(fullPath));

            // A multi-targeted project's outer evaluation has only its explicit Compile items, not the SDK's default
            // ones (kubernetes-client's KubernetesClient: none, so its files didn't look shared with KubernetesClient.Aot's).
            // ponytail: the first framework's files only; a file only another framework compiles isn't seen as shared. The
            // evaluation is CompileSettings', which the runs' plan reuses (one 'dotnet msbuild' less per multi-targeted project).
            // One framework in <TargetFrameworks> is an outer evaluation too (MimeKit's Benchmarks: no Compile items, so its
            // '#if DEBUG' wasn't seen and the Release run left it out, trial 25).
            if (frameworks.Length > 1 || (frameworks.Length == 1 && EvaluatedProperty(fullPath, "TargetFrameworks").Trim().Length > 0))
            {
                compile = compile.Union(ReadView(CompileSettings(fullPath, frameworks[0]))?.Compile ?? [], StringComparer.OrdinalIgnoreCase).ToArray();
            }

            return (fullPath, frameworks, references, linked, compile, assets, analyzers);
        }

        Dictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers)> read;
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

    /// <summary>
    /// The runs for other configurations of a workspace's projects (<see cref="ConfigurationRuns"/>): only projects whose files
    /// test a symbol that isn't a framework's (<see cref="IsFrameworkSymbol"/>) are evaluated, for each framework and
    /// configuration at once, and projects whose project file picks Compile items by configuration (<see cref="CompileByConfiguration"/>).
    /// <paramref name="view"/>: how a project (full path) compiles for a framework and configuration. A project that takes an
    /// in-solution source generator gets those runs too (owner's decision 2026-10-17, "Fix it in a Release run"): format builds
    /// the generator in each configuration first (<paramref name="withGenerator"/> names them, for verify's build check).
    /// </summary>
    private static List<(string Framework, string Configuration, List<string> Projects)> ConfigurationRuns(
        IReadOnlyDictionary<string, (string FullPath, string[] Frameworks, string[] References, string[] Linked, string[] Compile, string AssetsFile, string[] Analyzers)> projects,
        IReadOnlyList<(string Framework, List<string> Projects)> runs,
        string workspacePath,
        Func<string, string, string, CompileView?> view,
        List<string>? withGenerator = null,
        IEnumerable<(string FullPath, string Framework, string Configuration)>? alsoEvaluate = null)
    {
        var solution = SolutionConfigurations(workspacePath);
        var tests = projects.AsParallel()
            .Where(p => p.Value.Compile.Any(f => TestedSymbols(f).Any(s => !IsFrameworkSymbol(s))) || CompileByConfiguration(p.Value.FullPath))
            .ToList();
        withGenerator?.AddRange(tests.Where(p => p.Value.Analyzers.Length > 0).Select(p => p.Key).OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase));
        var others = tests.AsParallel()
            .Select(p => (p.Key, Others: OtherConfigurations(EvaluatedProperty(p.Value.FullPath, "Configurations"), EvaluatedProperty(p.Value.FullPath, "Configuration"), solution, EvaluatedProperty(p.Value.FullPath, "Platforms"))))
            .Where(p => p.Others.Count > 0)
            .ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Others, StringComparer.OrdinalIgnoreCase);

        // One 'dotnet msbuild' per project, framework and configuration (the default's too), all at once, with the caller's.
        runs.SelectMany(r => r.Projects.Where(others.ContainsKey).SelectMany(p => others[p].Prepend(string.Empty).Select(c => (projects[p].FullPath, r.Framework, Configuration: c))))
            .Concat(alsoEvaluate ?? [])
            .Distinct()
            .AsParallel()
            .ForAll(x => view(x.FullPath, x.Framework, x.Configuration));
        if (others.Count == 0)
        {
            return [];
        }

        string? Key(string project, string framework, string configuration) =>
            view(projects[project].FullPath, framework, configuration) is { } compiled ? FrameworkFamily(framework) + "|" + compiled.Key(compiled.Compile) : null;
        return ConfigurationRuns(runs, p => others.TryGetValue(p, out var own) ? own : [], Key);
    }

    /// <summary>How a project compiles for a run's framework ("": its only one) and configuration ("": its default), or null.</summary>
    private static CompileView? ViewOf(string fullPath, string[] frameworks, string framework, string configuration) =>
        (framework.Length == 0 ? (frameworks.Length == 1 ? frameworks[0] : null) : frameworks.FirstOrDefault(f => NormalizeFramework(f) == framework)) is { } spelling
            ? ReadView(CompileSettings(fullPath, spelling, configuration))
            : null;

    /// <summary>The output of a build of one project in one configuration (<see cref="UncheckedLine"/>).</summary>
    private static (int ExitCode, IReadOnlyList<string> Output) BuildOutput(string project, string configuration)
    {
        var output = new List<string>();
        var (name, platform) = SplitConfiguration(configuration);
        var exitCode = Dotnet(Path.GetDirectoryName(project)!, null, ["build", project, "-c", name, .. PlatformProperty(platform), "--no-restore", "--no-incremental", "-nologo"], output.Add);
        return (exitCode, output);
    }

    /// <summary>The output of one build of several projects in one configuration (<see cref="TogetherProject"/>, <see cref="UncheckedLine"/>).</summary>
    private static (int ExitCode, IReadOnlyList<string> Output) BuildTogether(IReadOnlyList<string> projects, string configuration)
    {
        var output = new List<string>();
        var (name, platform) = SplitConfiguration(configuration);
        var file = Path.Combine(Path.GetTempPath(), $"stylebro-check-{Guid.NewGuid():N}.proj");
        try
        {
            File.WriteAllText(file, TogetherProject(projects));
            var exitCode = Dotnet(Path.GetDirectoryName(projects[0])!, null, ["build", file, "-c", name, .. PlatformProperty(platform), "--no-restore", "--no-incremental", "-nologo"], output.Add);
            return (exitCode, output);
        }
        finally
        {
            File.Delete(file);
        }
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
            if (f.Names is not null)
            {
                start.Environment[RepositoryNames.Variable] = f.Names;
            }

            if (f.Warnings)
            {
                start.Environment["StyleBroFormatWarnings"] = "true";
            }

            // Like TargetFramework: the project file's own '$(Configuration)' conditions (DefineConstants) see it.
            if (f.Configuration.Length > 0)
            {
                SetConfiguration(start.Environment, f.Configuration);
            }
        }

        using var process = new Process { StartInfo = start };
        process.Start();
        DirectiveMask.Active?.Track(process);
        if (start.ArgumentList.FirstOrDefault() == "format")
        {
            ChildProcesses.Track(process);
        }

        // Read on threads of their own, not the thread pool's (the checks run at once wait here: see Capture).
        if (output is not null)
        {
            var gate = new object();
            void Forward(string line)
            {
                lock (gate)
                {
                    output(line);
                }
            }

            var readers = new[] { ReadOnThread(process.StandardOutput, line: Forward), ReadOnThread(process.StandardError, line: Forward) };
            foreach (var reader in readers)
            {
                reader.Join();
            }
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>Full paths as the hook matches them: '|A|B|', upper-case; empty for none.</summary>
    private static string PathList(IReadOnlyCollection<string> paths) => paths.Count == 0 ? string.Empty : "|" + string.Join("|", paths.Select(k => k.ToUpperInvariant())) + "|";

    /// <summary>How a project compiles for one framework (<see cref="ReadView"/>): defined symbols, language version, nullable context, files.</summary>
    public sealed record CompileView(HashSet<string> Defines, string LangVersion, string Nullable, string[] Compile)
    {
        /// <summary>
        /// What decides how <paramref name="files"/> parse: each file with the symbols its '#if's test that are defined, the
        /// language version and nullable context. Two views with the same key parse those files the same. The symbols come
        /// from the file's own path: upper-cased, it doesn't exist on Linux, and every file tested nothing there.
        /// </summary>
        public string Key(IEnumerable<string> files) =>
            string.Join(";", files.Select(Path.GetFullPath).DistinctBy(f => f.ToUpperInvariant(), StringComparer.Ordinal).OrderBy(f => f.ToUpperInvariant(), StringComparer.Ordinal)
                .Select(f => f.ToUpperInvariant() + ":" + string.Join(",", TestedSymbols(f).Where(Defines.Contains))))
            + "|" + LangVersion + "|" + Nullable;
    }

    /// <summary>
    /// Puts the '#' of the lines <see cref="DirectiveStringOffsets"/> finds out of the formatter's reach while 'dotnet format'
    /// runs: each becomes a character the file doesn't have (a private-use one), which no parse reads as a directive and
    /// the formatter leaves alone (string content, or text of a branch left out), and comes back when this is disposed,
    /// also on Ctrl+C. Leaving the files out of the whitespace pass instead took two more 'dotnet format' runs (style and
    /// analyzers, whose IDE0055 formats too) for each run that has them: CommunityToolkit 8 to 11.5 minutes. Before it
    /// swaps anything it writes a record (<see cref="RecordFolder"/>), deleted once the files are back: a process killed
    /// in between leaves it, and the next bro command puts the '#' back (<see cref="Recover"/>).
    /// </summary>
    internal sealed class DirectiveMask : IDisposable
    {
        // A swapped '#' as the guard and the recovery recognize it: a private-use character where a directive's '#' would be.
        private static readonly Regex MaskedLine = new(@"(?m)^[ \t]*[-][ \t]*(?:pragma|nullable|region|endregion|define|undef|error|warning|line|if|elif|else|endif)\b", RegexOptions.Compiled);

        private readonly List<(string Path, char Mark)> masked = [];
        private readonly string? record;

        public DirectiveMask(string root, IEnumerable<string> files, string? recordFolder = null)
        {
            var plan = new List<(string Path, string Text, List<int> Offsets, char Mark)>();
            foreach (var file in files.Select(f => Path.GetFullPath(Path.Combine(root, f))))
            {
                if (Read(file) is { } text && DirectiveStringOffsets(text) is { Count: > 0 } offsets
                    && Enumerable.Range(0xE000, 0x1900).Select(c => (char)c).FirstOrDefault(c => !text.Contains(c, StringComparison.Ordinal)) is > '\0' and var mark)
                {
                    plan.Add((file, text, offsets, mark));
                }
            }

            if (plan.Count == 0)
            {
                return;
            }

            // The record first: a kill right after the first swap must find it.
            using var current = Process.GetCurrentProcess();
            var folder = recordFolder ?? RecordFolder;
            Directory.CreateDirectory(folder);
            record = Path.Combine(folder, $"{Hash(root)}-{Environment.ProcessId}.json");
            File.WriteAllText(record, JsonSerializer.Serialize(new MaskRecord(root, Environment.ProcessId, current.StartTime.ToUniversalTime().Ticks, plan.Select(p => new MaskedFile(p.Path, p.Mark, p.Offsets.Count)).ToList())));
            foreach (var (file, text, offsets, mark) in plan)
            {
                var chars = text.ToCharArray();
                offsets.ForEach(o => chars[o] = mark);
                Write(file, new string(chars));
                masked.Add((file, mark));
            }

            Console.CancelKeyPress += OnCancel;
            AppDomain.CurrentDomain.ProcessExit += OnExit;
            Active = this;
        }

        /// <summary>Gets the mask of the run in progress, whose record <see cref="Dotnet"/> adds the 'dotnet format' processes to.</summary>
        public static DirectiveMask? Active { get; private set; }

        /// <summary>Where the records of swapped files are: '%TEMP%/stylebro-mask'.</summary>
        public static string RecordFolder => Path.Combine(Path.GetTempPath(), "stylebro-mask");

        /// <summary>
        /// Puts the '#' back in the files a killed run left swapped (a record whose process is gone), where each recorded
        /// file still has exactly its swapped characters at directive positions; anything else is said and left alone.
        /// The record is deleted either way. Says what it did through <paramref name="log"/>.
        /// </summary>
        public static void Recover(Action<string> log, string? recordFolder = null)
        {
            var folder = recordFolder ?? RecordFolder;
            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
            {
                if (ReadRecord(file) is not { } leftover || IsRunning(leftover))
                {
                    continue;
                }

                foreach (var (path, mark, count) in leftover.Files)
                {
                    var found = Read(path) is { } text ? Masked(text, (char)mark) : null;
                    if (found is { Count: 0 })
                    {
                        continue;
                    }

                    if (found?.Count == count)
                    {
                        var chars = Read(path)!.ToCharArray();
                        found.ForEach(o => chars[o] = '#');
                        Write(path, new string(chars));
                        log($"Restored: {path}: {count} '#' an interrupted 'bro format' had swapped.");
                    }
                    else
                    {
                        log($"Not restored: {path}: an interrupted 'bro format' swapped the '#' of {count} line(s) for U+{mark:X4}, and the file has changed since. Put the '#' back by hand.");
                    }
                }

                File.Delete(file);
            }
        }

        /// <summary>
        /// The files under <paramref name="root"/> (relative) another process's running format has swapped: a 'dotnet format'
        /// still writes them (also one a killed 'format' left running), so they're neither restored nor formatted now.
        /// </summary>
        public static List<string> Busy(string root, string? recordFolder = null)
        {
            var folder = recordFolder ?? RecordFolder;
            var top = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
            return Directory.Exists(folder)
                ? Directory.EnumerateFiles(folder, "*.json").Select(ReadRecord)
                    .Where(r => r is not null && r.Pid != Environment.ProcessId && IsRunning(r))
                    .SelectMany(r => r!.Files.Select(f => f.Path))
                    .Where(p => p.StartsWith(top, StringComparison.OrdinalIgnoreCase))
                    .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.Ordinal)
                    .ToList()
                : [];
        }

        /// <summary>
        /// The C# files under <paramref name="root"/> with a swapped '#' no running 'format' recorded: a damaged file
        /// (a record lost after a kill, or a hand edit) that formatting would only make worse.
        /// </summary>
        public static List<string> Damaged(string root, string? recordFolder = null)
        {
            var folder = recordFolder ?? RecordFolder;
            var live = Directory.Exists(folder)
                ? Directory.EnumerateFiles(folder, "*.json").Select(ReadRecord).Where(r => r is not null && IsRunning(r)).SelectMany(r => r!.Files.Select(f => f.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : [];
            return StyleCopSetup.EnumerateFiles(root)
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !live.Contains(Path.GetFullPath(f)))
                .AsParallel()
                .Where(f => Read(f) is { } text && text.Any(c => c is >= '' and <= '') && MaskedLine.IsMatch(text))
                .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
                .Order(StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Adds a process this run started to the record: a 'dotnet format' left running after this process was killed
        /// still writes the swapped files, so <see cref="Recover"/> waits for it too (trial 23: it put the '#' back, then the
        /// orphan wrote the swapped text again, and the next command stopped on a "damaged" file).
        /// </summary>
        public void Track(Process process)
        {
            lock (masked)
            {
                if (record is null || masked.Count == 0 || ReadRecord(record) is not { } current)
                {
                    return;
                }

                try
                {
                    var child = new MaskProcess(process.Id, process.StartTime.ToUniversalTime().Ticks);
                    File.WriteAllText(record, JsonSerializer.Serialize(current with { Children = [.. current.Children ?? [], child] }));
                }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
                {
                    // Already gone: nothing left to wait for.
                }
            }
        }

        public void Dispose()
        {
            Console.CancelKeyPress -= OnCancel;
            AppDomain.CurrentDomain.ProcessExit -= OnExit;
            if (Active == this)
            {
                Active = null;
            }

            lock (masked)
            {
                foreach (var (file, mark) in masked)
                {
                    if (Read(file) is { } text && text.Contains(mark, StringComparison.Ordinal))
                    {
                        Write(file, text.Replace(mark, '#'));
                    }
                }

                masked.Clear();
                if (record is not null)
                {
                    File.Delete(record);
                }
            }
        }

        // The offsets of mark in text, or null when one isn't where a directive's '#' would be.
        private static List<int>? Masked(string text, char mark)
        {
            var offsets = new List<int>();
            for (var i = text.IndexOf(mark, StringComparison.Ordinal); i >= 0; i = text.IndexOf(mark, i + 1))
            {
                var start = text.LastIndexOf('\n', Math.Max(i - 1, 0)) + 1;
                if (i > 0 && text[start..i].Any(c => c is not (' ' or '\t')))
                {
                    return null;
                }

                offsets.Add(i);
            }

            return offsets;
        }

        // Whether the record's process, or a 'dotnet format' it started, still runs.
        private static bool IsRunning(MaskRecord record) =>
            IsRunning(record.Pid, record.Started) || (record.Children ?? []).Any(c => IsRunning(c.Pid, c.Started));

        // Whether a process still runs (same id and start time; an unreadable one counts as running).
        private static bool IsRunning(int pid, long started)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                return process.StartTime.ToUniversalTime().Ticks == started;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
            {
                return true;
            }
        }

        private static MaskRecord? ReadRecord(string file)
        {
            try
            {
                return JsonSerializer.Deserialize<MaskRecord>(File.ReadAllText(file));
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static string Hash(string root) =>
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(root).ToUpperInvariant())))[..16];

        // UTF-8 only: other encodings are left alone. A BOM is read as a character, so writing the text back keeps it.
        // ponytail: a UTF-16 file isn't protected.
        private static string? Read(string file)
        {
            try
            {
                return File.Exists(file) ? new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(File.ReadAllBytes(file)) : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void Write(string file, string text) => File.WriteAllText(file, text, new System.Text.UTF8Encoding(false));

        private void OnCancel(object? sender, ConsoleCancelEventArgs e) => Dispose();

        private void OnExit(object? sender, EventArgs e) => Dispose();

        /// <summary>What <see cref="DirectiveMask"/> swapped, while it runs: the process (id, start time in UTC ticks) and the files.</summary>
        internal sealed record MaskRecord(string Root, int Pid, long Started, List<MaskedFile> Files, List<MaskProcess>? Children = null);

        /// <summary>A process the run started (<see cref="Track"/>): its id and start time in UTC ticks.</summary>
        internal sealed record MaskProcess(int Pid, long Started);

        /// <summary>A swapped file: its full path, the character (as a number) and how many '#' it replaced.</summary>
        internal sealed record MaskedFile(string Path, int Mark, int Count);
    }

    /// <summary>
    /// The files of one extension under a root, as they are when this is made, put back when it's disposed if a run changed
    /// them (said through the log): '.vb' files, which format leaves alone even where a fix reaches them.
    /// </summary>
    internal sealed class UntouchedFiles : IDisposable
    {
        private readonly Dictionary<string, byte[]> files;
        private readonly Action<string> log;
        private readonly string? what;

        public UntouchedFiles(string root, string extension, Action<string> log)
            : this(StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(extension, StringComparison.OrdinalIgnoreCase)), "Visual Basic", log)
        {
        }

        /// <summary>Keeps <paramref name="files"/> as they are; <paramref name="what"/> names them in what's said when one is put back (null: nothing is said).</summary>
        public UntouchedFiles(IEnumerable<string> files, string? what, Action<string> log)
        {
            this.log = log;
            this.what = what;
            this.files = files.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(f => f, File.ReadAllBytes, StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            foreach (var (file, bytes) in files.Where(f => !File.Exists(f.Key) || !File.ReadAllBytes(f.Key).AsSpan().SequenceEqual(f.Value)))
            {
                File.WriteAllBytes(file, bytes);
                if (what is not null)
                {
                    log($"Put back: {file}: a fix changed it, and 'format' leaves {what} alone. Check that the build still passes.");
                }
            }
        }
    }

    /// <summary>What a 'dotnet format' run for one framework gets in its environment (see the remarks on the class).</summary>
    private sealed record RunEnvironment(string Name, string SelectTargets, string Keep, string Other, string Aliases, string? Names, bool Warnings = false, string Configuration = "");

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
