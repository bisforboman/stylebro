using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1124 (StyleCop SA1142): <c>t.Name</c> instead of <c>t.Item1</c> when the element has a name.</summary>
internal static class TupleElementNames
{
    /// <summary>
    /// The element's name for an <c>ItemN</c> member access (<c>t.Item1</c>, <c>t?.Item1</c>), or null. Not inside
    /// <c>nameof</c>, where the name is the result.
    /// </summary>
    public static string? GetName(IdentifierNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!name.Identifier.ValueText.StartsWith("Item", StringComparison.Ordinal)
            || !((name.Parent is MemberAccessExpressionSyntax access && access.Name == name) || name.Parent is MemberBindingExpressionSyntax)
            || name.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => IsNameOf(i, model, cancellationToken)))
        {
            return null;
        }

        if (model.GetSymbolInfo(name, cancellationToken).Symbol is not IFieldSymbol { ContainingType.IsTupleType: true } field
            || !SymbolEqualityComparer.Default.Equals(field.CorrespondingTupleField, field))
        {
            return null;
        }

        return field.ContainingType.TupleElements
            .FirstOrDefault(e => SymbolEqualityComparer.Default.Equals(e.CorrespondingTupleField, field) && e.Name != field.Name)?.Name;
    }

    private static bool IsNameOf(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken) =>
        invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
        && model.GetSymbolInfo(invocation, cancellationToken).Symbol is null;
}
