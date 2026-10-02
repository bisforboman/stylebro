using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1131 (SA1100): 'base.' where 'this.' means the same member.</summary>
internal static class BaseCalls
{
    /// <summary>
    /// Like StyleCop: 'this' in place of 'base' binds to the same member (so the type has no override or hiding member
    /// of its own). Unlike StyleCop, a virtual member in a type that isn't sealed is skipped: 'this.M()' dispatches
    /// virtually, so a derived type's override would run instead of the base implementation.
    /// </summary>
    public static bool CanUseThis(BaseExpressionSyntax node, SemanticModel model, CancellationToken cancellationToken)
    {
        ExpressionSyntax speculative;
        switch (node.Parent)
        {
            case MemberAccessExpressionSyntax access when access.Expression == node:
                speculative = access.WithExpression(SyntaxFactory.ThisExpression());
                if (access.Parent is InvocationExpressionSyntax invocation && invocation.Expression == access)
                {
                    // Overload resolution needs the arguments.
                    speculative = invocation.WithExpression(speculative);
                }

                break;
            case ElementAccessExpressionSyntax element when element.Expression == node:
                speculative = element.WithExpression(SyntaxFactory.ThisExpression());
                break;
            default:
                return false;
        }

        var symbol = model.GetSymbolInfo(node.Parent, cancellationToken).Symbol;
        if (symbol is null
            || !SymbolEqualityComparer.Default.Equals(symbol, model.GetSpeculativeSymbolInfo(node.Parent.SpanStart, speculative, SpeculativeBindingOption.BindAsExpression).Symbol))
        {
            return false;
        }

        var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;
        return !dispatchesVirtually || model.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingType is { IsSealed: true };
    }
}
