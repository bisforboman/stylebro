using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1501 and BRO1502, used by both the analyzer and the code fix: the blank lines directly above a
/// token, and the edits that remove them.
/// </summary>
internal static class BlankLines
{
    /// <summary>BRO1504's option: comma-separated prefixes of comment text that needs no blank line above (default none).</summary>
    public const string ExemptPrefixesKey = "stylebro_comment_blank_line_exempt_prefixes";

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

    /// <summary>
    /// The blank lines directly below an opening brace that ends its line (BRO1503). Only whole lines up to the next
    /// token's code count, so text inside a string literal is never touched. A brace followed by a comment on its own
    /// line is skipped.
    /// </summary>
    public static IReadOnlyList<TextLine> GetBlankLinesBelow(SyntaxToken openBrace, SourceText text)
    {
        var next = openBrace.GetNextToken(includeZeroWidth: true);
        if (!openBrace.IsKind(SyntaxKind.OpenBraceToken) || next.IsKind(SyntaxKind.None))
        {
            return [];
        }

        var braceLine = text.Lines.GetLineFromPosition(openBrace.SpanStart);
        if (!IsBlank(text, TextSpan.FromBounds(openBrace.Span.End, braceLine.End)))
        {
            return [];
        }

        var lines = new List<TextLine>();
        for (var number = braceLine.LineNumber + 1; number < text.Lines.Count; number++)
        {
            var line = text.Lines[number];
            if (line.End > next.SpanStart || !IsBlank(text, line.Span))
            {
                break;
            }

            lines.Add(line);
        }

        return lines;
    }

    /// <summary>The prefixes in <see cref="ExemptPrefixesKey"/>, trimmed, without empty entries.</summary>
    public static IReadOnlyList<string> GetExemptPrefixes(AnalyzerConfigOptions options) =>
        options.TryGetValue(ExemptPrefixesKey, out var value)
            ? value.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray()
            : [];

    /// <summary>
    /// Whether a '//' comment needs a blank line above it (BRO1504), like StyleCop's SA1515: the comment starts its
    /// line and the line above is code. Not when the line above is blank, a comment or a directive; not directly after
    /// an opening brace, '=>' or a 'case'/'default' label; and not for '///' and '////' (commented-out code). Not for a comment
    /// BRO1132 or BRO1134 moves into a block when that rule is on (<paramref name="isOn"/>): the blank line would stay
    /// behind. Not for a comment whose text starts with one of <paramref name="exemptPrefixes"/> (tool markers such as
    /// '// ReSharper disable once ...', from <see cref="ExemptPrefixesKey"/>). Not for a comment below code
    /// (<see cref="IsCommentBelowCode"/>).
    /// </summary>
    public static bool NeedsBlankLineAbove(SyntaxTrivia comment, SourceText text, Func<string, bool> isOn, IReadOnlyList<string> exemptPrefixes)
    {
        if (!comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || comment.ToString().StartsWith("///", StringComparison.Ordinal))
        {
            return false;
        }

        if (exemptPrefixes.Count > 0)
        {
            var commentText = comment.ToString().Substring(2).TrimStart();
            if (exemptPrefixes.Any(p => commentText.StartsWith(p, StringComparison.Ordinal)))
            {
                return false;
            }
        }

        var line = text.Lines.GetLineFromPosition(comment.SpanStart);
        if (line.LineNumber == 0 || !IsBlank(text, TextSpan.FromBounds(line.Start, comment.SpanStart)))
        {
            return false;
        }

        var above = text.ToString(text.Lines[line.LineNumber - 1].Span).Trim();
        if (above.Length == 0 || above.StartsWith("//", StringComparison.Ordinal) || above.StartsWith("/*", StringComparison.Ordinal)
            || above.StartsWith("*", StringComparison.Ordinal) || above.StartsWith("#", StringComparison.Ordinal))
        {
            return false;
        }

        // The token before the comment: an opening brace (or a collection expression's '[', like StyleCop's #3766), the
        // colon of a switch label, or '=>' (a switch arm, lambda or expression body, StyleCop's #3392) keeps the comment
        // attached.
        var previous = comment.Token.SpanStart >= comment.Span.End ? comment.Token.GetPreviousToken() : comment.Token;
        return !previous.IsKind(SyntaxKind.OpenBraceToken)
            && !previous.IsKind(SyntaxKind.EqualsGreaterThanToken)
            && !(previous.IsKind(SyntaxKind.OpenBracketToken) && previous.Parent.IsKind(SyntaxKind.CollectionExpression))
            && !(previous.IsKind(SyntaxKind.ColonToken) && previous.Parent is SwitchLabelSyntax)
            && !(Readability.EmbeddedComments.GetMovingRule(comment, text) is { } rule && isOn(rule))
            && !IsCommentBelowCode(comment, text);
    }

    /// <summary>
    /// Whether the '//' comment is in a run of comment lines directly below a line that ends a statement or a member
    /// with ';' (not a using directive or a file-scoped namespace), with a blank line after the run: it describes the code above it (eShop: a field, '// note on UTF8 here:
    /// ...', a blank line, a method). StyleCop's SA1515 and SA1512 want the blank line moved above the comment, which
    /// attaches it to the code below; BRO1504 and BRO1506 leave it, and BRO1001 doesn't move the members on either side.
    /// </summary>
    public static bool IsCommentBelowCode(SyntaxTrivia comment, SourceText text)
    {
        if (!comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || comment.ToString().StartsWith("///", StringComparison.Ordinal))
        {
            return false;
        }

        var line = text.Lines.GetLineFromPosition(comment.SpanStart);
        if (!IsBlank(text, TextSpan.FromBounds(line.Start, comment.SpanStart)))
        {
            return false;
        }

        bool IsComment(int number) => text.ToString(text.Lines[number].Span).TrimStart().StartsWith("//", StringComparison.Ordinal);
        var first = line.LineNumber;
        while (first > 0 && IsComment(first - 1))
        {
            first--;
        }

        var last = line.LineNumber;
        while (last + 1 < text.Lines.Count && IsComment(last + 1))
        {
            last++;
        }

        // '#region'/'#endregion' lines below the run don't count: BRO1112/BRO1113 may remove them in the same run, and
        // the answer mustn't change then.
        var below = last + 1;
        while (below < text.Lines.Count && text.ToString(text.Lines[below].Span).TrimStart() is var content
            && (content.StartsWith("#region", StringComparison.Ordinal) || content.StartsWith("#endregion", StringComparison.Ordinal)))
        {
            below++;
        }

        // The ';' before the comment, on the line right above the run; and a blank line (not the end of the file) below it,
        // which isn't one before a '}' (BRO1518 removes those, and the comment would then want BRO1504's line above it).
        var previous = comment.Token.SpanStart >= comment.Span.End ? comment.Token.GetPreviousToken() : comment.Token;
        return first > 0
            && previous.IsKind(SyntaxKind.SemicolonToken)
            && (previous.Parent is StatementSyntax || previous.Parent is MemberDeclarationSyntax and not BaseNamespaceDeclarationSyntax)
            && text.Lines.GetLineFromPosition(previous.Span.End).LineNumber == first - 1
            && below < text.Lines.Count
            && text.Lines[below].End < text.Length
            && IsBlank(text, text.Lines[below].Span)
            && !comment.Token.IsKind(SyntaxKind.CloseBraceToken);
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
    /// between the two braces. Unlike SA1509, not the brace of an initializer's element (<c>{ "ssh", 22 }</c> in a
    /// dictionary initializer, an inner array's <c>{ 1, 2 }</c>): a blank line there groups entries (StyleCop #2832).
    /// </summary>
    public static bool IsCheckedOpenBrace(SyntaxToken token)
    {
        return token.IsKind(SyntaxKind.OpenBraceToken) && !token.GetPreviousToken().IsKind(SyntaxKind.CloseBraceToken)
            && !(token.Parent is InitializerExpressionSyntax { Parent: InitializerExpressionSyntax });
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
