using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1506 (StyleCop SA1512: no blank line after a single-line comment) and BRO1507 (SA1518: no
/// blank lines at the end of a file).
/// </summary>
internal static class TrailingBlankLines
{
    /// <summary>
    /// The blank lines directly below a '//' comment that starts its line, like SA1512. Not for the file header (the
    /// comments before the file's first code), '///' and '////' comments, or when the next non-blank line is another
    /// comment. The blank lines are whitespace inside trivia, never string contents.
    /// </summary>
    public static IReadOnlyList<TextLine> GetBlankLinesAfterComment(SyntaxTrivia comment, SourceText text)
    {
        if (!comment.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || comment.ToString().StartsWith("///", StringComparison.Ordinal)
            || IsFileHeader(comment))
        {
            return [];
        }

        var line = text.Lines.GetLineFromPosition(comment.SpanStart);
        if (!IsBlank(text, TextSpan.FromBounds(line.Start, comment.SpanStart)))
        {
            return [];
        }

        var lines = new List<TextLine>();
        for (var number = line.LineNumber + 1; number < text.Lines.Count; number++)
        {
            var next = text.Lines[number];
            if (!IsBlank(text, next.Span))
            {
                var content = text.ToString(next.Span).TrimStart();
                return content.StartsWith("//", StringComparison.Ordinal) || content.StartsWith("/*", StringComparison.Ordinal)
                    ? []
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
