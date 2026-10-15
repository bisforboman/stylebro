namespace StyleBro.Analyzers.Spacing;

/// <summary>Shared logic for BRO1002, used by both the analyzer and the code fix.</summary>
internal static class CommentSpacing
{
    /// <summary>
    /// The corrected text of a '//' comment, or null when it's fine. Matches StyleCop's SA1005: comments that are
    /// empty or already start with a space are fine, and so are '///...' (including '////' commented-out code) and
    /// '//--' separators. Also fine: the 'dotnet new' template markers '//-:' and '//+:' (StyleCop #2689: a space breaks
    /// them). The fix inserts a space and keeps tabs after '//' (commented-out code indented with tabs keeps its
    /// layout: '// ' still ends before the first tab stop; trial 23); a comment with nothing but whitespace becomes '//'.
    /// </summary>
    public static string? GetFixedText(string comment)
    {
        if (comment.Length <= 2
            || comment[2] == ' '
            || comment[2] == '/'
            || comment.StartsWith("//--", System.StringComparison.Ordinal)
            || comment.StartsWith("//-:", System.StringComparison.Ordinal)
            || comment.StartsWith("//+:", System.StringComparison.Ordinal))
        {
            return null;
        }

        var rest = comment.Substring(2);
        return rest.TrimStart(' ', '\t').Length == 0 ? "//" : "// " + rest;
    }
}
