using System;
using System.IO;
using System.Linq;

namespace StyleBro.Migrate;

/// <summary>
/// The line in the repository's .gitattributes that lets git merge stylebro.baseline (owner's choice 2026-11-08, backlog 085,
/// docs/proposals/agents-in-consumer-repos.md): 'bro format' and 'bro baseline' rewrite the file, so parallel branches edit
/// it. With 'merge=union' a local merge or rebase keeps both sides' lines instead of a conflict; a key on two lines takes the
/// larger count (Baseline.Parse), and the next write sorts the entries and drops those whose line is gone.
/// </summary>
internal static class GitAttributes
{
    public const string BaselineLine = Analyzers.Baseline.Baseline.FileName + " merge=union";

    private const string Comment = "# StyleBro: git keeps both branches' baseline entries when it merges them (https://bisforboman.github.io/stylebro/baseline/#merging-branches)";

    /// <summary>
    /// Adds <see cref="BaselineLine"/> to the .gitattributes at the root of the git repository holding <paramref name="baseline"/>
    /// (one line covers every stylebro.baseline: a pattern without '/' matches the name in any folder). Nothing outside a
    /// repository, or when a line there already names the baseline (the team chose its attributes). What to say, or null.
    /// </summary>
    public static string? AddBaselineLine(string root, string baseline)
    {
        if (FormatCommand.RepositoryRoot(Path.GetDirectoryName(Path.GetFullPath(baseline))!) is not { } repository)
        {
            return null;
        }

        var file = Path.Combine(repository, ".gitattributes");
        if (Migration.ReadOrNull(file) is { } text && NamesBaseline(text))
        {
            return null;
        }

        PreviewCommand.EditFile(file, Append);
        var relative = Path.GetRelativePath(Path.GetFullPath(root), file);
        var name = (relative.StartsWith("..", StringComparison.Ordinal) ? file : relative).Replace('\\', '/');
        return $"Baseline: {name}: added '{BaselineLine}', so git keeps both branches' entries when it merges them (https://bisforboman.github.io/stylebro/baseline/#merging-branches).";
    }

    /// <summary>Whether a line of the attributes names the baseline: its pattern ends with 'stylebro.baseline' ('eng/stylebro.baseline', '**/stylebro.baseline').</summary>
    public static bool NamesBaseline(string text) =>
        text.Split('\n').Select(l => l.Trim()).Any(l => l.Length > 0 && l[0] != '#'
            && l.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0].EndsWith(Analyzers.Baseline.Baseline.FileName, StringComparison.OrdinalIgnoreCase));

    /// <summary>The attributes with the comment and <see cref="BaselineLine"/> at the end (null: no file yet).</summary>
    public static string Append(string? text) =>
        (string.IsNullOrEmpty(text) ? string.Empty : text.EndsWith('\n') ? text : text + "\n") + Comment + "\n" + BaselineLine + "\n";
}
