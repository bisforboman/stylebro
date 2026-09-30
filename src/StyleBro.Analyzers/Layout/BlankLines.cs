using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1501 and BRO1502, used by both the analyzer and the code fix: the blank lines directly above a
/// token, and the edits that remove them.
/// </summary>
internal static class BlankLines
{
    /// <summary>
    /// The blank lines directly above <paramref name="token"/>, when it starts its line. Only whole lines inside the
    /// token's leading trivia count, so text inside a (raw) string literal is never touched. The scan stops at the
    /// first line that isn't blank, such as a comment or a directive, like StyleCop's SA1509/SA1510.
    /// </summary>
    public static IReadOnlyList<TextLine> GetBlankLinesAbove(SyntaxToken token, SourceText text)
    {
        if (!token.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia))
        {
            return [];
        }

        var tokenLine = text.Lines.GetLineFromPosition(token.SpanStart);
        if (!IsBlank(text, TextSpan.FromBounds(tokenLine.Start, token.SpanStart)))
        {
            return [];
        }

        var triviaStart = token.FullSpan.Start;
        var lines = new List<TextLine>();
        for (var number = tokenLine.LineNumber - 1; number >= 0; number--)
        {
            var line = text.Lines[number];
            if (line.Start < triviaStart || !IsBlank(text, line.Span))
            {
                break;
            }

            lines.Add(line);
        }

        return lines;
    }

    /// <summary>Deletes the given lines. Duplicates are ignored, so Fix All can pass overlapping sets.</summary>
    public static IEnumerable<TextChange> GetChanges(IEnumerable<TextLine> lines)
    {
        return lines
            .GroupBy(line => line.LineNumber)
            .Select(group => new TextChange(group.First().SpanIncludingLineBreak, string.Empty));
    }

    /// <summary>
    /// Whether BRO1501 applies to the token: an opening brace, except one whose previous token is a closing brace, such
    /// as a block following another block. That exception matches StyleCop's SA1509, including when a comment sits
    /// between the two braces.
    /// </summary>
    public static bool IsCheckedOpenBrace(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.OpenBraceToken) && !token.GetPreviousToken().IsKind(SyntaxKind.CloseBraceToken);
    }

    /// <summary>Whether the token is 'else', 'catch' or 'finally' continuing an if or try statement (BRO1502).</summary>
    public static bool IsChainedBlockKeyword(SyntaxToken token)
    {
        return token.Kind() switch
        {
            SyntaxKind.ElseKeyword => token.Parent.IsKind(SyntaxKind.ElseClause),
            SyntaxKind.CatchKeyword => token.Parent.IsKind(SyntaxKind.CatchClause),
            SyntaxKind.FinallyKeyword => token.Parent.IsKind(SyntaxKind.FinallyClause),
            _ => false,
        };
    }

    private static bool IsBlank(SourceText text, TextSpan span)
    {
        for (var i = span.Start; i < span.End; i++)
        {
            if (text[i] != ' ' && text[i] != '\t')
            {
                return false;
            }
        }

        return true;
    }
}
