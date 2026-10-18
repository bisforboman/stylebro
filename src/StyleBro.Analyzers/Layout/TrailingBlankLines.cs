using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1506 (StyleCop SA1512: no blank line after a single-line comment) and BRO1507 (SA1518: no
/// blank lines at the end of a file).
/// </summary>
internal static class TrailingBlankLines
{
    /// <summary>
    /// <c>stylebro_blank_line_below_standalone_comments</c>: <c>allow</c> (default) keeps the blank line below a standalone
    /// comment (<see cref="IsStandalone"/>), <c>remove</c> removes it like StyleCop's SA1512 (bro migrate writes it
    /// for StyleCop setups with SA1512 on).
    /// </summary>
    public const string StandaloneKey = "stylebro_blank_line_below_standalone_comments";

    /// <summary>Whether <see cref="StandaloneKey"/> is <c>remove</c>.</summary>
    public static bool RemovesStandalone(AnalyzerConfigOptions options) =>
        options.TryGetValue(StandaloneKey, out var value) && value.Trim().Equals("remove", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The blank lines directly below a '//' comment that starts its line, like SA1512. Not for the file header (the
    /// comments before the file's first code), '///' and '////' comments, when the next non-blank line is another
    /// comment, for a comment below code (<see cref="BlankLines.IsCommentBelowCode"/>), or, unless
    /// <paramref name="removeStandalone"/>, for a standalone comment (<see cref="IsStandalone"/>). The blank lines are
    /// whitespace inside trivia, never string contents.
    /// </summary>
    public static IReadOnlyList<TextLine> GetBlankLinesAfterComment(SyntaxTrivia comment, SourceText text, bool removeStandalone) =>
        GetCheckedBlankLines(comment, text, removeStandalone) ?? [];

    /// <summary>
    /// The same, but null for a comment BRO1506 doesn't check at all (init counts the checked ones, with blank lines below
    /// or without).
    /// </summary>
    public static IReadOnlyList<TextLine>? GetCheckedBlankLines(SyntaxTrivia comment, SourceText text, bool removeStandalone)
    {
        if (!comment.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || comment.ToString().StartsWith("///", StringComparison.Ordinal)
            || IsFileHeader(comment)
            || BlankLines.IsCommentBelowCode(comment, text)
            || (!removeStandalone && IsStandalone(comment, text)))
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(comment.SpanStart);
        if (!IsBlank(text, TextSpan.FromBounds(line.Start, comment.SpanStart)))
        {
            return null;
        }

        var lines = new List<TextLine>();
        for (var number = line.LineNumber + 1; number < text.Lines.Count; number++)
        {
            var next = text.Lines[number];
            if (!IsBlank(text, next.Span))
            {
                var content = text.ToString(next.Span).TrimStart();
                return content.StartsWith("//", StringComparison.Ordinal) || content.StartsWith("/*", StringComparison.Ordinal)
                    ? null
                    : lines;
            }

            // The line after the file's last line break isn't a blank line; spaces there are BRO1507's.
            if (next.End == text.Length)
            {
                break;
            }

            // The comment starts its line, so it sits in the next token's leading trivia, and every blank line before
            // the next non-blank line is part of that trivia too.
            lines.Add(next);
        }

        return lines;
    }

    /// <summary>
    /// Whether the '//' comment is in a run of comment lines with a blank line (or a '{' line) right above it and one right below it: a
    /// note or a section header that stands on its own (owner's choice, 2026-10-14; AzureEventGridSimulator: SA1512's fix
    /// glued notes about the code above and section headers onto unrelated code below). '#region'/'#endregion' lines next
    /// to the run don't count (BRO1112/BRO1113 may remove them in the same run). Not before a '}': those blank lines are
    /// BRO1518's, which leaves them to BRO1506 below a comment.
    /// </summary>
    public static bool IsStandalone(SyntaxTrivia comment, SourceText text)
    {
        if (comment.Token.IsKind(SyntaxKind.CloseBraceToken))
        {
            return false;
        }

        bool StartsWith(int number, string prefix) => text.ToString(text.Lines[number].Span).TrimStart().StartsWith(prefix, StringComparison.Ordinal);
        bool IsRegion(int number) => StartsWith(number, "#region") || StartsWith(number, "#endregion");
        var above = text.Lines.GetLineFromPosition(comment.SpanStart).LineNumber;
        var below = above;
        while (above > 0 && StartsWith(above - 1, "//"))
        {
            above--;
        }

        while (below + 1 < text.Lines.Count && StartsWith(below + 1, "//"))
        {
            below++;
        }

        do
        {
            above--;
        }
        while (above >= 0 && IsRegion(above));

        // Above: a blank line, or the '{' line the comments start a block after (a block's first section header is
        // standalone like the others, trial 27). Below: a region line there leaves no blank line below the comment for
        // BRO1506 anyway.
        below++;
        return above >= 0 && (IsBlank(text, text.Lines[above].Span) || text.ToString(text.Lines[above].Span).TrimEnd().EndsWith("{", StringComparison.Ordinal))
            && below < text.Lines.Count && text.Lines[below].End < text.Length && IsBlank(text, text.Lines[below].Span);
    }

    /// <summary>
    /// The text after the last line with content, when it holds more than that line's line break: extra blank lines,
    /// or lines of spaces. A file that ends without a line break, or with exactly one, is fine (StyleCop's default).
    /// </summary>
    public static TextSpan? GetExtraEnding(SourceText text)
    {
        var last = LastContentLine(text);
        if (last is null || last.Value.EndIncludingLineBreak >= text.Length)
        {
            return null;
        }

        return TextSpan.FromBounds(last.Value.EndIncludingLineBreak, text.Length);
    }

    /// <summary>Where BRO1507 is reported: the end of the file's last code or comment, like StyleCop.</summary>
    public static int GetEndOfContent(SourceText text)
    {
        return LastContentLine(text) is { } line ? line.Start + text.ToString(line.Span).TrimEnd().Length : 0;
    }

    private static TextLine? LastContentLine(SourceText text)
    {
        for (var number = text.Lines.Count - 1; number >= 0; number--)
        {
            if (!IsBlank(text, text.Lines[number].Span))
            {
                return text.Lines[number];
            }
        }

        return null;
    }

    private static bool IsFileHeader(SyntaxTrivia comment)
    {
        // A comment in the leading trivia of the file's first token, with only comments and whitespace before it.
        var root = comment.SyntaxTree?.GetRoot();
        if (root is null || comment.Token != root.GetFirstToken(includeZeroWidth: true))
        {
            return false;
        }

        foreach (var trivia in comment.Token.LeadingTrivia)
        {
            if (trivia == comment)
            {
                return true;
            }

            if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia)
                && !trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                return false;
            }
        }

        return false;
    }

    private static bool IsBlank(SourceText text, TextSpan span)
    {
        for (var i = span.Start; i < span.End; i++)
        {
            if (text[i] is not (' ' or '\t'))
            {
                return false;
            }
        }

        return true;
    }
}
