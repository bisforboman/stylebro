using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>Shared logic for BRO1401, used by both the analyzer and the code fix.</summary>
internal static class TrailingCommas
{
    /// <summary>
    /// The last item of a multi-line list that has no trailing comma, or null. Like StyleCop's SA1413, the lists are
    /// array, object, collection and 'with' initializers, anonymous objects, enums, switch expressions and (like
    /// StyleCop master) property patterns; a list is multi-line when its braces are on different lines. The comma goes
    /// right after the last item's code, so before a trailing comment ('2 // last' becomes '2, // last').
    /// Lists with a preprocessor directive between their braces are skipped: which item is last can then depend on
    /// the build configuration, and in a multi-targeted project each target framework would want a different edit to
    /// the same file, which 'dotnet format' merges into conflict markers.
    /// </summary>
    public static SyntaxNode? GetLastItemWithoutComma(SyntaxNode node, SourceText text)
    {
        var list = GetList(node);
        if (list is not { } l
            || l.Last is null
            || l.SeparatorCount == l.Count
            || l.OpenBrace.IsMissing
            || l.CloseBrace.IsMissing
            || node.ContainsDiagnostics
            || text.Lines.GetLineFromPosition(l.OpenBrace.SpanStart).LineNumber == text.Lines.GetLineFromPosition(l.CloseBrace.SpanStart).LineNumber
            || HasDirectiveBetween(node, l.OpenBrace, l.CloseBrace))
        {
            return null;
        }

        return l.Last;
    }

    /// <summary>
    /// The text to insert after the last item: ',' or, when code follows directly ('"x"}'), ', ' so the fix doesn't
    /// leave a comma without the space after it that the formatter and StyleCop's SA1001 expect.
    /// </summary>
    public static string GetInsertion(SyntaxNode lastItem, SourceText text)
    {
        var end = lastItem.Span.End;
        return end < text.Length && text[end] is not (' ' or '\t' or '\r' or '\n') ? ", " : ",";
    }

    private static bool HasDirectiveBetween(SyntaxNode node, SyntaxToken open, SyntaxToken close)
    {
        var inside = TextSpan.FromBounds(open.Span.End, close.SpanStart);
        foreach (var trivia in node.DescendantTrivia(inside))
        {
            if (trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            {
                return true;
            }
        }

        return false;
    }

    private static (int Count, int SeparatorCount, SyntaxNode? Last, SyntaxToken OpenBrace, SyntaxToken CloseBrace)? GetList(SyntaxNode node)
    {
        return node switch
        {
            InitializerExpressionSyntax initializer when initializer.Kind() is SyntaxKind.ArrayInitializerExpression
                or SyntaxKind.ObjectInitializerExpression
                or SyntaxKind.CollectionInitializerExpression
                or SyntaxKind.WithInitializerExpression
                => Describe(initializer.Expressions, initializer.OpenBraceToken, initializer.CloseBraceToken),
            AnonymousObjectCreationExpressionSyntax anonymous
                => Describe(anonymous.Initializers, anonymous.OpenBraceToken, anonymous.CloseBraceToken),
            EnumDeclarationSyntax enumDeclaration
                => Describe(enumDeclaration.Members, enumDeclaration.OpenBraceToken, enumDeclaration.CloseBraceToken),
            SwitchExpressionSyntax switchExpression
                => Describe(switchExpression.Arms, switchExpression.OpenBraceToken, switchExpression.CloseBraceToken),
            PropertyPatternClauseSyntax propertyPattern
                => Describe(propertyPattern.Subpatterns, propertyPattern.OpenBraceToken, propertyPattern.CloseBraceToken),
            _ => null,
        };
    }

    private static (int, int, SyntaxNode?, SyntaxToken, SyntaxToken) Describe<T>(SeparatedSyntaxList<T> items, SyntaxToken open, SyntaxToken close)
        where T : SyntaxNode
    {
        return (items.Count, items.SeparatorCount, items.Count > 0 ? items[items.Count - 1] : null, open, close);
    }
}
