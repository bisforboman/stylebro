using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1109 (StyleCop SA1110: the opening parenthesis or bracket is on the line of the name it belongs
/// to) and BRO1110 (SA1111: the closing parenthesis or bracket is on the line where the last item ends).
/// </summary>
internal static class ParenthesisPlacement
{
    /// <summary>
    /// BRO1109: the opening token when it starts a line after the name before it: 'Method' then '(int a)' on the next
    /// line. Only lists that belong to a name (declarations, calls, 'new', element access, attributes, constructor
    /// initializers), not a lambda's parameters. Null when it's fine or the gap has comments or directives.
    /// </summary>
    public static SyntaxToken? GetMisplacedOpen(SyntaxNode list, SourceText text)
    {
        var (open, _, _) = ParameterLayoutList(list);
        if (open.IsMissing || !BelongsToName(list))
        {
            return null;
        }

        var previous = open.GetPreviousToken();
        return Line(text, previous.Span.End) < Line(text, open.SpanStart) && IsPlainGap(previous, open) ? open : null;
    }

    /// <summary>
    /// The edit for BRO1109: the opening token moves to the end of the name's line. Whatever followed it on its own
    /// line stays on that line, which becomes the list's first line: 'Method' / '(int a, int b)' turns into
    /// 'Method(' / 'int a, int b)'. An empty list is simply joined: 'Parens()'.
    /// </summary>
    public static TextChange GetOpenChange(SyntaxToken open, SourceText text)
    {
        var previous = open.GetPreviousToken();
        var gap = text.ToString(TextSpan.FromBounds(previous.Span.End, open.SpanStart));
        var next = open.GetNextToken();
        var restOfLineIsEmpty = Line(text, next.SpanStart) > Line(text, open.SpanStart);
        var joinOnly = next.IsKind(SyntaxKind.CloseParenToken) || next.IsKind(SyntaxKind.CloseBracketToken) || restOfLineIsEmpty;

        // Replace '<gap>(' (and the spaces after it) with '(' plus the gap, which keeps the next item at its indentation.
        var end = restOfLineIsEmpty || joinOnly ? open.Span.End : next.SpanStart;
        return new TextChange(TextSpan.FromBounds(previous.Span.End, end), open.Text + (joinOnly ? string.Empty : gap));
    }

    /// <summary>
    /// BRO1110: the closing token when it isn't on the line where the last item ends. Null for empty lists (spacing,
    /// StyleCop's SA1009), and for gaps with anything but whitespace and one comment at the end of the last item's line
    /// (a comment there is only moved when nothing but whitespace follows the closing token on its line, since the
    /// code after it would otherwise end up behind the comment).
    /// </summary>
    public static SyntaxToken? GetMisplacedClose(SyntaxNode list, SourceText text)
    {
        var (_, items, close) = ParameterLayoutList(list);
        if (items.Count == 0 || close.IsMissing)
        {
            return null;
        }

        var last = items[items.Count - 1].GetLastToken();
        if (Line(text, last.Span.End) == Line(text, close.SpanStart) || GetTrailingComment(last, close, text) is not { } comment)
        {
            return null;
        }

        if (comment.Length > 0)
        {
            var closeLine = text.Lines.GetLineFromPosition(close.SpanStart);
            if (!string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(close.Span.End, closeLine.End))))
            {
                return null;
            }
        }

        return close;
    }

    /// <summary>
    /// The edit for BRO1110: the closing token moves right after the last item, and a comment at the end of the last
    /// item's line goes after it: 'int b // last' / ')' turns into 'int b) // last'.
    /// </summary>
    public static TextChange GetCloseChange(SyntaxToken close, SourceText text)
    {
        var last = close.GetPreviousToken();
        var comment = GetTrailingComment(last, close, text) ?? string.Empty;
        return new TextChange(TextSpan.FromBounds(last.Span.End, close.Span.End), close.Text + comment);
    }

    /// <summary>The list's opening token, items and closing token, for the list kinds <see cref="ParameterLayout"/> checks.</summary>
    public static (SyntaxToken Open, IReadOnlyList<SyntaxNode> Items, SyntaxToken Close) ParameterLayoutList(SyntaxNode list)
    {
        return list switch
        {
            BaseParameterListSyntax parameters => (parameters.GetFirstToken(), parameters.Parameters, parameters.GetLastToken()),
            BaseArgumentListSyntax arguments => (arguments.GetFirstToken(), arguments.Arguments, arguments.GetLastToken()),
            AttributeArgumentListSyntax attributeArguments => (attributeArguments.OpenParenToken, attributeArguments.Arguments, attributeArguments.CloseParenToken),
            _ => (default, [], default),
        };
    }

    private static bool BelongsToName(SyntaxNode list)
    {
        return list.Parent is BaseMethodDeclarationSyntax
            or DelegateDeclarationSyntax
            or LocalFunctionStatementSyntax
            or IndexerDeclarationSyntax
            or TypeDeclarationSyntax
            or InvocationExpressionSyntax
            or BaseObjectCreationExpressionSyntax
            or ElementAccessExpressionSyntax
            or AttributeSyntax
            or ConstructorInitializerSyntax
            or PrimaryConstructorBaseTypeSyntax;
    }

    /// <summary>Only whitespace and line breaks between two tokens.</summary>
    private static bool IsPlainGap(SyntaxToken before, SyntaxToken after)
    {
        return before.TrailingTrivia.Concat(after.LeadingTrivia)
            .All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));
    }

    /// <summary>
    /// The text to keep after the moved closing token: empty when the gap is only whitespace, the comment (with the
    /// spaces before it) when the last item's line ends with a single-line comment, or null when the gap holds
    /// anything else.
    /// </summary>
    private static string? GetTrailingComment(SyntaxToken last, SyntaxToken close, SourceText text)
    {
        var trivia = last.TrailingTrivia.Concat(close.LeadingTrivia).ToList();
        var comments = trivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
        if (comments.Count == 0)
        {
            return string.Empty;
        }

        var comment = comments[0];
        if (comments.Count > 1 || !comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || Line(text, comment.SpanStart) != Line(text, last.Span.End))
        {
            return null;
        }

        return text.ToString(TextSpan.FromBounds(last.Span.End, comment.Span.End));
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
