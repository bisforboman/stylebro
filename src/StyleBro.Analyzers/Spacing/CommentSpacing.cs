namespace StyleBro.Analyzers.Spacing;

/// <summary>Shared logic for BRO1002, used by both the analyzer and the code fix.</summary>
internal static class CommentSpacing
{
    /// <summary>
    /// The corrected text of a '//' comment, or null when it's fine. Matches StyleCop's SA1005: comments that are
    /// empty or already start with a space are fine, and so are '///...' (including '////' commented-out code) and
    /// '//--' separators. Also fine: the 'dotnet new' template markers '//-:' and '//+:' (StyleCop #2689: a space breaks
    /// them). Tabs and other whitespace after '//' become a single space; a comment with nothing but
    /// whitespace becomes '//'.
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

        var rest = comment.Substring(2).TrimStart(' ', '\t');
        return rest.Length == 0 ? "//" : "// " + rest;
    }
}
