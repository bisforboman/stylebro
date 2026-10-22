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

    /// <summary>The start of <see cref="NotVerifiedLine"/>.</summary>
    public const string NotVerifiedMarker = "Not verified:";

    /// <summary>The reason of <see cref="HeldBackGap"/>'s line, which tells it from the gaps that count.</summary>
    public const string HeldBackWhy = "this SDK's 'dotnet format' ignores the multi-target guard, so these fixes are held back and not checked (a note: a newer .NET SDK checks them; the verdict doesn't count them)";

    /// <summary>A gap line: <paramref name="what"/> wasn't checked, <paramref name="why"/>.</summary>
    public static string Gap(string what, string why) => $"{NotCheckedPrefix} {what}: {why}";

    /// <summary>The first few of <paramref name="items"/> and how many more, for a gap line.</summary>
    public static string ShortList(IReadOnlyCollection<string> items, int shown = 3) =>
        string.Join(", ", items.Take(shown)) + (items.Count > shown ? $" and {items.Count - shown} more" : string.Empty);

    /// <summary>The gap line for the SDK rules format holds back (<see cref="HeldBackIds"/>): listed, but not counted (<see cref="Unverified"/>).</summary>
    public static string HeldBackGap(IReadOnlyList<string> ids) => Gap(string.Join(", ", ids), HeldBackWhy);

    /// <summary>The gaps that make the verdict "Not verified": all but the held-back SDK rules (<see cref="HeldBackGap"/>).</summary>
    public static List<string> Unverified(IEnumerable<string> gaps) => gaps.Where(g => !g.EndsWith(HeldBackWhy, StringComparison.Ordinal)).ToList();

    /// <summary>Whether a run's line names a gap (<see cref="Gap"/>).</summary>
    public static bool IsGapLine(string line) => line.StartsWith(NotCheckedPrefix, StringComparison.Ordinal);

    /// <summary>The gaps among a run's lines, each once, in their order.</summary>
    public static List<string> Gaps(IEnumerable<string> lines) => lines.Where(IsGapLine).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>The verdict when <paramref name="count"/> gaps are left (the lines above it).</summary>
    public static string NotVerifiedLine(int count) =>
        $"{NotVerifiedMarker} {count} part{(count == 1 ? " of the build wasn't" : "s of the build weren't")} checked (the '{NotCheckedPrefix}' line{(count == 1 ? string.Empty : "s")} above); the rest is clean. "
        + "The build may still report findings there: fix what's named, or build it (bro check --build).";

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
