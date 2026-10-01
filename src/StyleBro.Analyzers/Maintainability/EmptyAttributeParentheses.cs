using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>Shared logic for BRO1402 (StyleCop SA1411: <c>[Obsolete]</c> instead of <c>[Obsolete()]</c>).</summary>
internal static class EmptyAttributeParentheses
{
    /// <summary>
    /// The edit that removes the empty argument list (and any space before it), or null when the attribute has
    /// arguments or a comment sits in or before the parentheses.
    /// </summary>
    public static TextChange? GetChange(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList is not { Arguments.Count: 0 } list || list.ContainsDirectives
            || attribute.Name.GetTrailingTrivia().Concat(list.DescendantTrivia())
                .Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        return new TextChange(TextSpan.FromBounds(attribute.Name.Span.End, list.Span.End), string.Empty);
    }
}
