using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// Shared logic for BRO1003 (StyleCop SA1212: 'get' before 'set'/'init') and BRO1004 (SA1213: 'add' before 'remove').
/// Like StyleCop, auto-properties count too (<c>{ set; get; }</c>).
/// </summary>
internal static class AccessorOrder
{
    /// <summary>The accessor in the wrong place (the first) and its rule, or null when the order is right or the fix can't swap them.</summary>
    public static (AccessorDeclarationSyntax First, string Id)? GetFinding(AccessorListSyntax list, SourceText text)
    {
        if (list.Accessors.Count != 2 || list.ContainsDirectives || list.ContainsDiagnostics)
        {
            return null;
        }

        var (first, second) = (list.Accessors[0], list.Accessors[1]);
        var id = (first.Keyword.Kind(), second.Keyword.Kind()) switch
        {
            (SyntaxKind.SetKeyword or SyntaxKind.InitKeyword, SyntaxKind.GetKeyword) => DiagnosticIds.PropertyAccessorOrder,
            (SyntaxKind.RemoveKeyword, SyntaxKind.AddKeyword) => DiagnosticIds.EventAccessorOrder,
            _ => null,
        };

        return id is not null && GetSwap(list, text) is not null ? (first, id) : null;
    }

    /// <summary>
    /// The two edits that swap the accessors. On one line: the accessors' text. Each on its own lines: the accessor
    /// with the comments above it and a comment after it on its last line, so blank lines stay where they are. Null
    /// for any other layout.
    /// </summary>
    public static (TextChange A, TextChange B)? GetSwap(AccessorListSyntax list, SourceText text)
    {
        var (first, second) = (list.Accessors[0], list.Accessors[1]);
        if (Line(text, first.SpanStart) == Line(text, second.Span.End))
        {
            return (new TextChange(first.Span, text.ToString(second.Span)), new TextChange(second.Span, text.ToString(first.Span)));
        }

        if (GetSlot(first, text) is not { } a || GetSlot(second, text) is not { } b || a.End > b.Start)
        {
            return null;
        }

        return (new TextChange(a, text.ToString(b)), new TextChange(b, text.ToString(a)));
    }

    /// <summary>From the first line of the comments above the accessor to the end of its last line, or null when it shares a line with other code.</summary>
    private static TextSpan? GetSlot(AccessorDeclarationSyntax accessor, SourceText text)
    {
        var firstTrivia = accessor.GetLeadingTrivia()
            .FirstOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));
        var start = firstTrivia.RawKind != 0 ? firstTrivia.SpanStart : accessor.SpanStart;
        var startLine = text.Lines.GetLineFromPosition(start);
        var endLine = text.Lines.GetLineFromPosition(accessor.Span.End);
        var before = text.ToString(TextSpan.FromBounds(startLine.Start, start));
        var after = accessor.GetTrailingTrivia().Where(t => t.SpanStart < endLine.End);
        if (before.Trim().Length > 0
            || accessor.GetLastToken().GetNextToken().SpanStart < endLine.End
            || after.Any(t => t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
        {
            return null;
        }

        return TextSpan.FromBounds(startLine.Start, endLine.End);
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
