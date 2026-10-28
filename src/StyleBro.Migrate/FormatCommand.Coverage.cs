using System;
using System.Collections.Generic;
using System.Linq;

namespace StyleBro.Migrate;

/// <summary>
/// What format and its check didn't see the way the build sees it (owner's decision 2026-10-20, "Close the gap first"): a
/// project or framework that didn't load, a workload skip, an unlisted multi-targeted project, files a pass leaves out, ids
/// whose fixes can't run. Each gap is a <see cref="NotCheckedPrefix"/> line a run prints; check and format collect them,
/// list them at the end and say <see cref="NotVerifiedLine"/> with <see cref="NotVerifiedExitCode"/> instead of "Clean".
/// Skips the build also makes (submodules, Visual Basic, generated files, files outside the repository, projects no
/// configuration builds, IDE0055 off) aren't gaps. A held-back SDK rule (<see cref="HeldBackGap"/>) is listed like a gap
/// but is a note: on its own it leaves the verdict and the exit code alone (owner's decision 2026-10-22, "Note it, don't
/// fail"; SDK 10.0.1xx held IDE0049 back in every multi-targeted repository, so nothing could end clean there).
/// </summary>
internal static partial class FormatCommand
{
    /// <summary>The exit code when nothing was left to change in what format and the check saw, but part of the build wasn't checked.</summary>
    public const int NotVerifiedExitCode = 3;

    /// <summary>The start of a line that names one part of the build a run didn't check, and why (<see cref="Gap"/>).</summary>
    public const string NotCheckedPrefix = "Not checked:";

    /// <summary>
    /// Why the IDE fixes <see cref="HeldBackIds"/> leaves out aren't checked (their gap line, <see cref="HeldBackGap"/>); it
    /// tells that line from the gaps that count (<see cref="Unverified"/>).
    /// </summary>
    public const string HeldBackReason = "this SDK's 'dotnet format' ignores the multi-target guard, so these fixes are held back and not checked (a note: a newer .NET SDK checks them; the verdict doesn't count them)";

    /// <summary>The start of <see cref="NotVerifiedLine"/>.</summary>
    public const string NotVerifiedMarker = "Not verified:";

    /// <summary>How a gap line that is only a note ends (<see cref="Unverified"/>).</summary>
    private const string NoteEnd = "the verdict doesn't count them)";

    /// <summary>How <see cref="NotVerifiedLine"/> ends: what to do about the parts it names.</summary>
    private const string BuildMayReport = "The build may still report findings there: fix what's named, or build it (bro check --build).";

    /// <summary>A gap line: <paramref name="what"/> wasn't checked, <paramref name="why"/>.</summary>
    public static string Gap(string what, string why) => $"{NotCheckedPrefix} {what}: {why}";

    /// <summary>The first few of <paramref name="items"/> and how many more, for a gap line.</summary>
    public static string ShortList(IReadOnlyCollection<string> items, int shown = 3) =>
        string.Join(", ", items.Take(shown)) + (items.Count > shown ? $" and {items.Count - shown} more" : string.Empty);

    /// <summary>The gap line for the SDK rules format holds back (<see cref="HeldBackIds"/>): listed, but not counted (<see cref="Unverified"/>).</summary>
    public static string HeldBackGap(IReadOnlyList<string> ids) => Gap(string.Join(", ", ids), HeldBackReason);

    /// <summary>
    /// The gap line for xunit.analyzers' fixes, which have no Fix All from 1.24.0 on (<see cref="XunitWithoutFixAll"/>): a note
    /// like <see cref="HeldBackGap"/> (owner's decision 2026-10-22, trial 44: the build still reports their warnings, and the
    /// verdict depended on whether the restore output was there before the run).
    /// </summary>
    public static string XunitGap(string version) =>
        Gap("xunit.analyzers' rules", $"xunit.analyzers {version}'s fixes have no Fix All, so 'dotnet format' can't apply or check them (a note: the build still reports their warnings; {NoteEnd}");

    /// <summary>The gaps that make the verdict "Not verified": all but the notes (<see cref="HeldBackGap"/>, <see cref="XunitGap"/>).</summary>
    public static List<string> Unverified(IEnumerable<string> gaps) => gaps.Where(g => !g.EndsWith(NoteEnd, StringComparison.Ordinal)).ToList();

    /// <summary>Whether a gap line is the held-back SDK rules' note (<see cref="HeldBackGap"/>).</summary>
    public static bool IsHeldBackGap(string line) => IsGapLine(line) && line.EndsWith(HeldBackReason, StringComparison.Ordinal);

    /// <summary>Whether a run's line names a gap (<see cref="Gap"/>).</summary>
    public static bool IsGapLine(string line) => line.StartsWith(NotCheckedPrefix, StringComparison.Ordinal);

    /// <summary>The gaps among a run's lines, each once, in their order.</summary>
    public static List<string> Gaps(IEnumerable<string> lines) => lines.Where(IsGapLine).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>The verdict when <paramref name="count"/> gaps are left (the lines above it).</summary>
    public static string NotVerifiedLine(int count) =>
        $"{NotVerifiedMarker} {count} part{(count == 1 ? " of the build wasn't" : "s of the build weren't")} checked (the '{NotCheckedPrefix}' line{(count == 1 ? string.Empty : "s")} above); the rest is clean. "
        + BuildMayReport;

    /// <summary>
    /// <see cref="NotVerifiedLine"/> after the builds of 'check --build' (<paramref name="line"/>), which comes after them
    /// (owner's decision 2026-10-27; trial 57, Newtonsoft.Json.Bson: it came before six clean builds, said to run the command
    /// running, and the output ended with the last build's line): what the builds checked there and what they don't when they
    /// found nothing (<paramref name="buildsClean"/>), else without the hint to build.
    /// </summary>
    public static string AfterBuilds(string line, bool buildsClean) =>
        !line.EndsWith(BuildMayReport, StringComparison.Ordinal) ? line
            : buildsClean ? line.Substring(0, line.Length - BuildMayReport.Length)
                + "The builds above compiled that code with warnings as errors: StyleBro's rules at warning or error were checked there, "
                + "not the whitespace and the IDE fixes a build doesn't enforce (EnforceCodeStyleInBuild)."
            : line.Substring(0, line.Length - BuildMayReport.Length).TrimEnd();

    /// <summary>The gap lines for the projects 'dotnet format' skipped (<see cref="Incomplete"/>'s arguments).</summary>
    public static List<string> SkippedGaps(IReadOnlyDictionary<string, SortedSet<string>> skipped, IReadOnlyDictionary<string, List<string>> frameworks, IReadOnlyCollection<string>? unsupported = null)
    {
        var why = unsupported is { Count: > 0 }
            ? $"'dotnet format' couldn't load it (it references {string.Join(", ", unsupported.Order(StringComparer.OrdinalIgnoreCase))}, which 'dotnet format' can't load)"
            : "'dotnet format' couldn't load it (references didn't load)";
        return skipped
            .OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
            .Select(s => !frameworks.TryGetValue(s.Key, out var all) || all.All(s.Value.Contains)
                ? Gap(s.Key, why)
                : Gap($"{s.Key} ({string.Join(", ", s.Value.Where(f => f.Length > 0))})", why + ", so the code only that framework compiles"))
            .ToList();
    }

    /// <summary>
    /// The gap line for a project whose design-time build failed (<see cref="LoadFailure"/>) in the runs named by <paramref name="where"/>
    /// ('net10.0', 'net48, Release'; empty: the only run): 'dotnet format' loaded it without its compiler's command line, so
    /// none of its build settings (the preset, the .editorconfig, the '#if' symbols) applied and StyleBro's rules didn't run
    /// there (owner's decision 2026-10-27; trial 57, DapperAOT: Nerdbank.GitVersioning's negative build number in a shallow clone).
    /// </summary>
    public static string LoadFailureGap(string project, IReadOnlyCollection<string> where, string message) =>
        Gap(where.Count == 0 ? project : $"{project} ({string.Join("; ", where)})", $"MSBuild failed loading it ({message.TrimEnd('.')}), so 'dotnet format' had none of its build settings and StyleBro's rules didn't run there");

    /// <summary>
    /// The first error of a failed design-time build's <paramref name="output"/> ('error : build ('-2') must be a non-negative
    /// value. (Parameter 'build')'), without its code and project; null without one.
    /// </summary>
    public static string? FirstBuildError(string output) =>
        output.Split('\n').Select(l => System.Text.RegularExpressions.Regex.Match(l, @"(?:^|:\s)error(?: [A-Za-z]+\d+)?\s*:\s*(?<message>.+?)(?:\s+\[[^\]]*\])?\s*$"))
            .Where(m => m.Success)
            .Select(m => m.Groups["message"].Value.Trim())
            .FirstOrDefault(m => m.Length > 0);

    /// <summary>The gap lines for the projects that need workload packs this machine lacks (project path relative to the root).</summary>
    public static List<string> WorkloadGaps(IEnumerable<(string Project, string[] Packs)> projects) =>
        projects.Select(p => Gap(p.Project, $"it needs {string.Join(", ", p.Packs)}, which this machine doesn't have installed")).ToList();

    /// <summary>
    /// The verdict lines when <paramref name="gaps"/> are left: the list, then <see cref="NotVerifiedLine"/> when any of them
    /// counts (<see cref="Unverified"/>). None without gaps.
    /// </summary>
    public static List<string> GapVerdict(IReadOnlyList<string> gaps) =>
        Unverified(gaps) is { Count: > 0 } counted ? [.. gaps, NotVerifiedLine(counted.Count)] : [.. gaps];

    /// <summary>--json: the gaps as 'notChecked' (what, why), every time they're said.</summary>
    public static void ReportGaps(IEnumerable<string> gaps) =>
        JsonReport.Set("notChecked", new System.Text.Json.Nodes.JsonArray(gaps.Select(g => (System.Text.Json.Nodes.JsonNode?)g.Substring(NotCheckedPrefix.Length).Trim()).ToArray()));

    /// <summary>
    /// The exit code once <paramref name="gaps"/> are known: a clean <paramref name="code"/> becomes <see cref="NotVerifiedExitCode"/>
    /// with gaps that count (<see cref="Unverified"/>); findings and errors stay what they are.
    /// </summary>
    public static int WithGaps(int code, IReadOnlyCollection<string> gaps) => code == 0 && Unverified(gaps).Count > 0 ? NotVerifiedExitCode : code;
}
