using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// 'bro check --build' (owner's decision 2026-10-20): after the check, builds the workspace in every configuration of the
/// solution (and platform) with TreatWarningsAsErrors, as CI does, and compares with the untouched code: a copy of git HEAD
/// (the working tree is never touched). A finding of format's rules (<see cref="Diagnostics"/>) always fails; any other error
/// fails only when the build of the untouched code doesn't have it (without git, always). Every false "Clean" of trials 1-35
/// was caught only by such a build.
/// </summary>
internal static partial class FormatCommand
{
    /// <summary>The option that builds every configuration after the check (<see cref="BuildCheckLine"/>).</summary>
    public const string BuildOption = "--build";

    /// <summary>The environment variable Microsoft.Common.targets imports a file from (MSBuild reads environment variables as properties).</summary>
    private const string HookVariable = "CustomAfterMicrosoftCommonTargets";

    private static readonly Regex BuildLine = new(@"^\s*(?<path>.+?)(?:\((?<line>\d+),(?<column>\d+)\))? ?: (?<kind>warning|error) (?<id>[A-Za-z]+\d+): (?<message>.*?)(?: \[(?<project>[^\]]*)\])?\s*$");

    // Compiler errors without a place in the code that the compiler reports when it emits, after the analyzers ran (trial 49,
    // superpower: CS8002, a strong-named assembly referencing an unsigned one, with warnings as errors; a planted finding in
    // that project was reported). Every other error without a place stops the build before the compiler (restore, MSBuild,
    // command line: CS0006, CS2001).
    private static readonly HashSet<string> EmitErrors = new(StringComparer.Ordinal) { "CS8002" };

    private static readonly Regex OutputLine = new(@"^\s*(?<name>\S.*?) -> (?:/|[A-Za-z]:\\)");

    /// <summary>The errors and warnings of a build's <paramref name="output"/> (each once: the summary repeats them), paths relative to <paramref name="root"/>.</summary>
    public static List<BuildMessage> BuildMessages(IEnumerable<string> output, string root) =>
        output.Select(l => BuildLine.Match(l))
            .Where(m => m.Success)
            .Select(m =>
            {
                var path = Relative(root, m.Groups["path"].Value.Trim());
                var project = m.Groups["project"].Value;
                var framework = project.IndexOf("::TargetFramework=", StringComparison.Ordinal) is var at and >= 0 ? project.Substring(at + "::TargetFramework=".Length) : string.Empty;
                var where = m.Groups["line"].Success ? $"{path}({m.Groups["line"].Value},{m.Groups["column"].Value})" : path;
                var shown = $"  {where}: {m.Groups["id"].Value}: {m.Groups["message"].Value}{(framework.Length > 0 ? $" ({framework})" : string.Empty)}";
                var projectFile = framework.Length > 0 ? project.Substring(0, project.IndexOf("::", StringComparison.Ordinal)) : project;
                return (Line: m.Value.Trim(), Message: new BuildMessage($"{path.ToUpperInvariant()}|{framework}|{m.Groups["id"].Value}|{m.Groups["message"].Value}", m.Groups["id"].Value, m.Groups["kind"].Value == "error", shown, m.Groups["line"].Success, Path.GetFileNameWithoutExtension(projectFile), path));
            })
            .DistinctBy(x => x.Line, StringComparer.Ordinal)
            .Select(x => x.Message)
            .ToList();

    /// <summary>
    /// What a build in one configuration says: the findings of format's rules (<paramref name="ids"/>), errors the untouched
    /// code's build (<paramref name="untouched"/>, null: no such build) doesn't have, and the errors both have. Counted per key,
    /// so a message the untouched code has twice and the code three times is one new error. In the folders format leaves
    /// alone (<paramref name="leftAlone"/>, relative to the build's root with a trailing '/') format's rules are errors like
    /// any other: format fixed nothing there.
    /// </summary>
    public static (List<string> Findings, List<string> New, int Old) CompareBuild(IReadOnlyList<BuildMessage> current, IReadOnlyList<BuildMessage>? untouched, IReadOnlySet<string> ids, IReadOnlyList<string>? leftAlone = null)
    {
        bool Finding(BuildMessage m) => IsFinding(m, ids, leftAlone);
        var findings = current.Where(Finding).Select(m => m.Shown).Distinct(StringComparer.Ordinal).ToList();
        var before = (untouched ?? []).Where(m => m.Error && !Finding(m)).GroupBy(m => m.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var newErrors = new List<string>();
        var old = 0;
        foreach (var group in current.Where(m => m.Error && !Finding(m)).GroupBy(m => m.Key, StringComparer.Ordinal))
        {
            var had = before.TryGetValue(group.Key, out var count) ? count : 0;
            old += Math.Min(had, group.Count());
            newErrors.AddRange(group.Skip(had).Select(m => m.Shown));
        }

        return (findings, newErrors, old);
    }

    /// <summary>
    /// Whether a build message is a finding of format's rules: one of <paramref name="ids"/>, outside the folders format leaves
    /// alone (<see cref="Migration.UnformattedFolders"/>). Trial 58, LightInject: StyleCop ran only in the library, so format
    /// left the tests alone, and an xUnit2013 there that the untouched code has too made check --build "Not clean".
    /// </summary>
    public static bool IsFinding(BuildMessage message, IReadOnlySet<string> ids, IReadOnlyList<string>? leftAlone) =>
        ids.Contains(message.Id) && leftAlone?.Any(f => message.RelativePath.StartsWith(f, StringComparison.OrdinalIgnoreCase)) != true;

    /// <summary>The configurations --build builds: the solution's ('Release|x64' with its platform), else the project's Configurations (Debug and Release).</summary>
    public static List<string> BuildConfigurations(string workspacePath) =>
        SolutionConfigurations(workspacePath) is { Count: > 0 } solution
            ? solution
            : (EvaluatedProperty(workspacePath, "Configurations") is { Length: > 0 } own && workspacePath.EndsWith("proj", StringComparison.OrdinalIgnoreCase) ? own : "Debug;Release")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

    /// <summary>
    /// The files that make the working tree of <paramref name="repository"/> differ from HEAD: (tracked path, in HEAD), plus
    /// the untracked ones (not in HEAD). Null without git; empty when nothing differs.
    /// </summary>
    public static List<(string Path, bool InHead)>? ChangedSinceHead(string repository)
    {
        if (PreviewCommand.Git(repository, "rev-parse", "--verify", "-q", "HEAD") is not (0, _)
            || PreviewCommand.Git(repository, "diff", "--name-only", "-z", "HEAD") is not (0, var tracked)
            || PreviewCommand.Git(repository, "ls-files", "--others", "--exclude-standard", "-z") is not (0, var untracked))
        {
            return null;
        }

        return tracked.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => (p, PreviewCommand.Git(repository, "cat-file", "-e", $"HEAD:{p}").Code == 0))
            .Concat(untracked.Split('\0', StringSplitOptions.RemoveEmptyEntries).Select(p => (p, false)))
            .ToList();
    }

    /// <summary>Puts HEAD's version of <paramref name="changed"/> (<see cref="ChangedSinceHead"/>) into <paramref name="copy"/>, a copy of <paramref name="repository"/>'s working tree.</summary>
    public static void ToHead(string repository, string copy, IReadOnlyList<(string Path, bool InHead)> changed)
    {
        foreach (var (path, _) in changed)
        {
            File.Delete(Path.Combine(copy, path));
        }

        var inHead = changed.Where(c => c.InHead).Select(c => c.Path).ToList();
        if (inHead.Count == 0)
        {
            return;
        }

        var tar = Path.Combine(Path.GetTempPath(), $"stylebro-head-{Guid.NewGuid():N}.tar");
        try
        {
            // An archive, not 'git show': the bytes as committed (byte order marks, line endings as git checks them out).
            if (PreviewCommand.Git(repository, ["archive", "--format=tar", "-o", tar, "HEAD", "--", .. inHead]) is (0, _))
            {
                System.Formats.Tar.TarFile.ExtractToDirectory(tar, copy, overwriteFiles: true);
            }
        }
        finally
        {
            File.Delete(tar);
        }
    }

    /// <summary>
    /// Makes <paramref name="copy"/>, the copy of HEAD that check --build compares with, a git repository with
    /// <paramref name="repository"/>'s history (<see cref="PreviewCommand.BorrowHistory"/>: objects, HEAD, remotes, shallow
    /// boundary) and HEAD's tree in its index, so version tools build it as they build the repository. Without it
    /// (fixture matrix, git-height-shallow): in a shallow clone Nerdbank.GitVersioning fails every build of the repository,
    /// the copy without git built, and check --build said "Not clean" for errors the untouched code has too.
    /// </summary>
    public static void WithHistory(string copy, string repository)
    {
        if (PreviewCommand.Git(copy, "init", "-q") is not (0, _))
        {
            return;
        }

        PreviewCommand.BorrowHistory(copy, repository);
        PreviewCommand.Git(copy, "read-tree", "HEAD");
    }

    /// <summary>
    /// --build: builds <paramref name="workspacePath"/> (under <paramref name="root"/>) in each of its configurations and
    /// returns what makes it not clean ("Not clean: ...") or not verified (<see cref="NotVerifiedMarker"/>: builds that failed
    /// like the untouched code's), null for nothing. <paramref name="build"/> stands in for 'dotnet build' (folder,
    /// workspace, configuration) in tests; <paramref name="untouchedRoot"/> for the copy of HEAD (null: none, "": the same code).
    /// </summary>
    public static string? BuildCheckLine(string root, string workspacePath, Action<string> log, Func<string, string, string, (int ExitCode, IReadOnlyList<string> Output)>? build = null, Func<string?>? untouchedRoot = null, Func<IReadOnlyList<string>>? expected = null)
    {
        build ??= BuildWithWarningsAsErrors;
        List<string>? expectedProjects = null;
        var ids = Diagnostics(root).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var configurations = BuildConfigurations(workspacePath);
        var repository = RepositoryRoot(root);
        var top = repository ?? root;
        var relative = Path.GetRelativePath(top, workspacePath);
        var leftAlone = Migration.UnformattedFolders(root).Select(f => Path.GetRelativePath(top, Path.GetFullPath(Path.Combine(root, f))).Replace('\\', '/').TrimEnd('/') + "/").ToList();
        string? temp = null;
        string? untouched;
        if (untouchedRoot is not null)
        {
            untouched = untouchedRoot();
        }
        else if (repository is null || ChangedSinceHead(repository) is not { } changed)
        {
            untouched = null;
        }
        else if (changed.Count == 0)
        {
            untouched = string.Empty;
        }
        else
        {
            temp = Path.Combine(Path.GetTempPath(), $"stylebro-build-{Guid.NewGuid():N}");
            PreviewCommand.MarkCopy(temp);
            var outside = OutsideCompile(repository);
            untouched = PreviewCommand.CopyLocation(temp, repository, outside);
            PreviewCommand.Copy(repository, untouched);
            PreviewCommand.CopyOutside(repository, untouched, temp, outside);
            ToHead(repository, untouched, changed);
            WithHistory(untouched, repository);
        }

        log(untouched switch
        {
            null => $"Building {Path.GetFileName(workspacePath)} in {string.Join(", ", configurations)} with warnings as errors (no git: every error counts)...",
            "" => $"Building {Path.GetFileName(workspacePath)} in {string.Join(", ", configurations)} with warnings as errors (the code is HEAD's: errors other than format's rules are the code's own)...",
            _ => $"Building {Path.GetFileName(workspacePath)} in {string.Join(", ", configurations)} with warnings as errors, and a copy of HEAD to compare...",
        });

        var findings = 0;
        var failed = new List<string>();
        var bothFailed = new List<string>();
        var uncompiled = new List<string>();
        var notCompared = new List<string>();
        List<string> Expected(string configuration) => (expectedProjects ??= [.. expected?.Invoke() ?? BuiltProjects(workspacePath, root)]).Except(NotBuiltIn(workspacePath, configuration), StringComparer.OrdinalIgnoreCase).ToList();
        try
        {
            foreach (var configuration in configurations)
            {
                using var phase = PhaseTimes.Measure($"build: {configuration}");
                var (exitCode, output) = build(root, workspacePath, configuration);
                var current = BuildMessages(output, top);
                (int ExitCode, IReadOnlyList<string> Output)? before = untouched is { Length: > 0 } copy
                    ? WithHook(UntouchedHook(Path.GetDirectoryName(Path.GetFullPath(copy))!, ids, leftAlone.Select(f => Path.GetFullPath(Path.Combine(copy, f)))), () => build(Path.GetFullPath(Path.Combine(copy, Path.GetRelativePath(top, root))), Path.GetFullPath(Path.Combine(copy, relative)), configuration))
                    : null;
                var beforeMessages = before is { } b ? BuildMessages(b.Output, untouched!) : untouched == string.Empty ? current : null;

                // Errors in projects the untouched code's build compiled nothing for (a project they need failed there) have
                // nothing to compare with: not new, not verified (owner's decision 2026-10-27; trial 57, DapperAOT: after a
                // design-time load MSBuild compiled Dapper.AOT.Test.Integration without the reference that failed, and its 7
                // CS0234 errors made "Not clean" where the untouched build never compiled it). Findings of format's rules count.
                var unmatched = before is { ExitCode: not 0 } failedBefore
                    ? Uncompiled(failedBefore.Output, beforeMessages!, Expected(configuration)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                    : [];
                bool Unmatched(BuildMessage m) => m.Error && !IsFinding(m, ids, leftAlone) && unmatched.Contains(m.Project);
                var (found, newErrors, old) = CompareBuild(current.Where(m => !Unmatched(m)).ToList(), beforeMessages, ids, leftAlone);
                var notComparedHere = current.Where(Unmatched).Select(m => m.Project).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();

                // A failed build no line explains (a crash, an MSBuild error without a position) counts unless the untouched one failed too.
                var unexplained = exitCode != 0 && found.Count == 0 && newErrors.Count == 0 && (untouched is null || (before is { } u && u.ExitCode == 0));
                log($"{configuration}: {(found.Count == 0 && newErrors.Count == 0 && !unexplained ? "no findings, no new errors" : string.Join(", ", new[] { found.Count > 0 ? $"{found.Count} finding{(found.Count == 1 ? string.Empty : "s")} of format's rules" : null, newErrors.Count > 0 ? $"{newErrors.Count} new error{(newErrors.Count == 1 ? string.Empty : "s")}" : null, unexplained ? $"the build failed (exit code {exitCode})" : null }.OfType<string>()))}{(old > 0 ? $" ({old} error{(old == 1 ? string.Empty : "s")} the untouched code has too, not counted)" : string.Empty)}{(found.Count > 0 || newErrors.Count > 0 ? ":" : ".")}");
                found.Take(20).Concat(newErrors.Take(20)).ToList().ForEach(log);
                if (notComparedHere.Count > 0)
                {
                    notCompared.Add($"{configuration} ({ShortList(notComparedHere)})");
                    log($"  Not compared: {ShortList(notComparedHere, 10)} (the untouched code's build didn't compile {(notComparedHere.Count == 1 ? "it" : "them")}: a project {(notComparedHere.Count == 1 ? "it needs" : "they need")} failed there).");
                }

                findings += found.Count;
                if (newErrors.Count > 0 || unexplained)
                {
                    failed.Add(configuration);
                }
                else if (exitCode != 0 && found.Count == 0 && (current.Any(StopsCompiling) || !current.Any(m => m.Error)))
                {
                    // Failed like the untouched code's at the restore or in MSBuild (an error without a place in the code): nothing
                    // compiled (sqlite-net: NETSDK1147). An error in the code is the compiler's, which ran the analyzers there
                    // (Ulid's obsolete calls in its benchmarks, warnings as errors).
                    bothFailed.Add(configuration);
                    current.Where(StopsCompiling).Select(m => m.Shown).Distinct(StringComparer.Ordinal).Take(5).ToList().ForEach(log);
                }
                else if (exitCode != 0 && found.Count == 0 && Uncompiled(output, current, Expected(configuration)) is { Count: > 0 } skipped)
                {
                    // Failed with errors in the code the untouched code has too: the projects that need the failed ones didn't
                    // compile, so their analyzers never ran (owner's decision 2026-10-20: not verified, naming them).
                    uncompiled.Add($"{configuration} ({ShortList(skipped)})");
                    log($"  Didn't compile: {ShortList(skipped, 10)} (a project {(skipped.Count == 1 ? "it needs" : "they need")} failed).");
                }
            }
        }
        finally
        {
            if (temp is not null && Directory.Exists(temp))
            {
                PreviewCommand.Delete(temp);
            }
            else if (temp is not null)
            {
                File.Delete(PreviewCommand.MarkerOf(temp));
            }
        }

        var findingsPart = findings > 0 ? $"the builds report {findings} finding{(findings == 1 ? string.Empty : "s")} of format's rules (above): fix {(findings == 1 ? "it" : "them")} by hand, or suppress or baseline {(findings == 1 ? "it" : "them")}" : null;
        var failedPart = failed.Count > 0 ? $"the {string.Join(", ", failed)} build{(failed.Count == 1 ? string.Empty : "s")} {(untouched is null ? "failed" : "failed where the untouched code's didn't")} (above)" : null;
        var notComparedPart = notCompared.Count > 0
            ? $"the {string.Join(", ", notCompared)} build{(notCompared.Count == 1 ? string.Empty : "s")} compiled projects the untouched code's build didn't (a project they need failed there), so their errors have nothing to compare with (above)"
            : null;
        var notVerified = findingsPart is not null || failedPart is not null ? null
            : bothFailed.Count == 0 && uncompiled.Count > 0 ? $"{NotVerifiedMarker} the {string.Join(", ", uncompiled)} build{(uncompiled.Count == 1 ? string.Empty : "s")} didn't compile {(uncompiled.Count == 1 ? "those projects" : "the projects named")}: a project they need failed, as in the untouched code's build (above), so StyleBro's rules didn't run there. Fix those errors, then run again."
            : bothFailed.Count > 0 ? $"{NotVerifiedMarker} the {string.Join(", ", bothFailed)} build{(bothFailed.Count == 1 ? string.Empty : "s")} failed, as the untouched code's {(bothFailed.Count == 1 ? "does" : "do")} (above), so {(bothFailed.Count == 1 ? "it" : "they")} compiled nothing past {(bothFailed.Count == 1 ? "its" : "their")} errors: fix them (a missing workload, a failed restore), then run again."
            : null;
        return findingsPart is not null || failedPart is not null ? $"Not clean: {string.Join("; and ", new[] { findingsPart, failedPart }.OfType<string>())}."
            : notComparedPart is null ? notVerified
            : notVerified is null ? $"{NotVerifiedMarker} {notComparedPart}: fix the errors the untouched code has too, then run again."
            : $"{notVerified} Also, {notComparedPart}.";
    }

    /// <summary>
    /// The projects of <paramref name="expected"/> (names) a build's <paramref name="output"/> shows nothing compiled for: no
    /// 'Name -&gt; output' line and no compiler or analyzer message (MSBuild's and NuGet's don't count). In a failed build,
    /// those a failed project kept from compiling (trial 36: App referencing a Lib whose error the untouched code has too;
    /// MSBuild says nothing about App, and "Clean" stood there).
    /// </summary>
    public static List<string> Uncompiled(IReadOnlyList<string> output, IReadOnlyList<BuildMessage> messages, IEnumerable<string> expected)
    {
        var compiled = output.Select(l => OutputLine.Match(l)).Where(m => m.Success).Select(m => m.Groups["name"].Value)
            .Concat(messages.Where(m => m.Project.Length > 0 && !m.Id.StartsWith("MSB", StringComparison.Ordinal) && !m.Id.StartsWith("NETSDK", StringComparison.Ordinal) && !m.Id.StartsWith("NU", StringComparison.Ordinal)).Select(m => m.Project))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return expected.Where(p => !compiled.Contains(p)).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// The MSBuild file the HEAD copy's build imports (<see cref="HookVariable"/>): format's rules (<paramref name="ids"/>) stay
    /// warnings there. Trial 40, Blazored.Toast: HEAD had init's settings but not format's fixes, so the library failed on
    /// BRO findings and the projects referencing it never compiled in the copy; their own errors (CS0067, the same seven)
    /// then counted as new in the formatted build, and check --build said "Not clean". A hook already set is imported too.
    /// Not in the projects under <paramref name="leftAlone"/> (full paths): format fixed nothing there, so their errors are
    /// compared as they are (<see cref="IsFinding"/>).
    /// </summary>
    public static string UntouchedHook(string folder, IEnumerable<string> ids, IEnumerable<string>? leftAlone = null)
    {
        var path = Path.Combine(folder, "stylebro-untouched.targets");
        var chained = Environment.GetEnvironmentVariable(HookVariable);
        var import = string.IsNullOrEmpty(chained) ? string.Empty : $"  <Import Project=\"{System.Security.SecurityElement.Escape(chained)}\" Condition=\"Exists('{System.Security.SecurityElement.Escape(chained)}')\" />\n";
        var outside = string.Join(" and ", (leftAlone ?? []).Select(f => $"!$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)').StartsWith('{System.Security.SecurityElement.Escape(f.TrimEnd('/', '\\') + Path.DirectorySeparatorChar)}'))"));
        File.WriteAllText(path, $"<Project>\n{import}  <PropertyGroup{(outside.Length > 0 ? $" Condition=\"{outside}\"" : string.Empty)}>\n    <WarningsNotAsErrors>$(WarningsNotAsErrors);{string.Join(";", ids.Order(StringComparer.Ordinal))}</WarningsNotAsErrors>\n  </PropertyGroup>\n</Project>\n");
        return path;
    }

    /// <summary>
    /// The names of the .sln's C# projects <paramref name="configuration"/> ('Release', 'Release|x64') doesn't build: no
    /// '.Build.0' line for it (trial 40, a Blazored.Toast copy: a sample unchecked in Release was named as a project that didn't
    /// compile). Other workspaces: none.
    /// </summary>
    public static List<string> NotBuiltIn(string solutionPath, string configuration)
    {
        if (!solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || !File.Exists(solutionPath))
        {
            return [];
        }

        var (name, platform) = SplitConfiguration(configuration);
        var key = Regex.Escape($".{name}|{platform ?? "Any CPU"}.Build.0");
        var text = File.ReadAllText(solutionPath);
        return Regex.Matches(text, @"^Project\(""[^""]*""\)\s*=\s*""[^""]*""\s*,\s*""(?<path>[^""]+\.csproj)""\s*,\s*""(?<guid>\{[^}]+\})""", RegexOptions.Multiline)
            .Where(m => !Regex.IsMatch(text, Regex.Escape(m.Groups["guid"].Value) + key + @"\s*=", RegexOptions.IgnoreCase))
            .Select(m => Path.GetFileNameWithoutExtension(m.Groups["path"].Value.Replace('\\', '/')))
            .ToList();
    }

    /// <summary>
    /// The ids of <paramref name="gapLines"/> when every one of them is the IDE fixes this SDK's 'dotnet format' holds back
    /// (<see cref="HeldBackReason"/>), else null: any other gap is something the build doesn't check for format.
    /// </summary>
    public static List<string>? HeldBackGapIds(IReadOnlyList<string> gapLines)
    {
        var ids = new List<string>();
        foreach (var line in gapLines.Distinct(StringComparer.Ordinal))
        {
            var suffix = $": {HeldBackReason}";
            if (!line.StartsWith(NotCheckedPrefix, StringComparison.Ordinal) || !line.EndsWith(suffix, StringComparison.Ordinal))
            {
                return null;
            }

            ids.AddRange(line.Substring(NotCheckedPrefix.Length, line.Length - NotCheckedPrefix.Length - suffix.Length).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return ids.Count > 0 ? ids.Distinct(StringComparer.OrdinalIgnoreCase).ToList() : null;
    }

    /// <summary>
    /// Whether a build of <paramref name="workspacePath"/> reports <paramref name="ids"/> (IDE rules) everywhere: every project it
    /// builds sets EnforceCodeStyleInBuild, and every C# file under <paramref name="root"/> has each id at warning or error.
    /// </summary>
    public static bool BuildEnforces(string root, string workspacePath, IReadOnlyList<string> ids)
    {
        try
        {
            var projects = ReadProjects(workspacePath).Values.Select(p => p.FullPath).ToList();
            var notBuilt = NotBuiltFolders(workspacePath, projects, root).Select(f => Path.GetFullPath(Path.Combine(root, f))).ToList();
            var built = projects.Where(p => !notBuilt.Any(f => p.StartsWith(f, StringComparison.OrdinalIgnoreCase))).ToList();
            if (built.Count == 0 || !built.All(p => Capture(Path.GetDirectoryName(p)!, "msbuild", p, "-getProperty:EnforceCodeStyleInBuild", "-nologo").Trim().Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var set = ConfigSet(root, ConfigTexts(root));
            return StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .All(f => ids.All(id => Severity(set, Path.GetFullPath(f), id) is Microsoft.CodeAnalysis.ReportDiagnostic.Warn or Microsoft.CodeAnalysis.ReportDiagnostic.Error));
        }
        catch (Exception e) when (e is ToolException or IOException)
        {
            return false;
        }
    }

    /// <summary>One error or warning of a build: its key (path relative to the build's root, framework, id, message: no line, which a fix moves) and how it's shown.</summary>
    /// <summary>
    /// Whether a build error came before the compiler ran the analyzers: no place in the code (restore, MSBuild, the
    /// compiler's command line), except the errors the compiler reports when it emits (<see cref="EmitErrors"/>).
    /// </summary>
    public static bool StopsCompiling(BuildMessage message) => message.Error && !message.InSource && !EmitErrors.Contains(message.Id);

    /// <summary>Runs <paramref name="action"/> with <see cref="HookVariable"/> set to <paramref name="hook"/> (every MSBuild node inherits it).</summary>
    private static T WithHook<T>(string hook, Func<T> action)
    {
        var previous = Environment.GetEnvironmentVariable(HookVariable);
        Environment.SetEnvironmentVariable(HookVariable, hook);
        try
        {
            return action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(HookVariable, previous);
        }
    }

    /// <summary>A build of a workspace in one configuration ('Release|x64') with TreatWarningsAsErrors, as CI builds (restore included, NuGet's audit off).</summary>
    private static (int ExitCode, IReadOnlyList<string> Output) BuildWithWarningsAsErrors(string folder, string workspace, string configuration)
    {
        var output = new List<string>();
        var (name, platform) = SplitConfiguration(configuration);
        var exitCode = Dotnet(folder, null, ["build", workspace, "-c", name, .. PlatformProperty(platform), "-p:TreatWarningsAsErrors=true", "-p:NuGetAudit=false", "--no-incremental", "-nologo", "-nodeReuse:false", "-clp:NoSummary"], output.Add);
        return (exitCode, output);
    }

    /// <summary>The names of the workspace's C# projects a configuration builds (<see cref="NotBuiltFolders"/> left out), none when they can't be read.</summary>
    private static List<string> BuiltProjects(string workspacePath, string root)
    {
        try
        {
            var projects = ReadProjects(workspacePath).Values.Select(p => p.FullPath).ToList();
            var notBuilt = NotBuiltFolders(workspacePath, projects, root).Select(f => Path.GetFullPath(Path.Combine(root, f))).ToList();
            return projects.Where(p => !notBuilt.Any(f => p.StartsWith(f, StringComparison.OrdinalIgnoreCase))).Select(Path.GetFileNameWithoutExtension).OfType<string>().ToList();
        }
        catch (Exception e) when (e is ToolException or IOException)
        {
            return [];
        }
    }

    public sealed record BuildMessage(string Key, string Id, bool Error, string Shown, bool InSource = true, string Project = "", string RelativePath = "");
}
