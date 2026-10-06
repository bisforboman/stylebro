using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
        if (symbol is null)
        {
            return false;
        }

        var dispatchesVirtually = (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) && !symbol.IsSealed;
        return (!dispatchesVirtually || model.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingType is { IsSealed: true })
            && SymbolEqualityComparer.Default.Equals(symbol, model.GetSpeculativeSymbolInfo(node.Parent.SpanStart, speculative, SpeculativeBindingOption.BindAsExpression).Symbol);
    }

    /// <summary>
    /// The fix: 'this' in place of 'base' (StyleCop's), or, when the SDK's dotnet_style_qualification_for_* for the
    /// member's kind is false (the preset's choice), no qualifier at all where the plain name binds to the same member.
    /// Null when <see cref="CanUseThis"/> says no.
    /// </summary>
    public static TextChange? GetChange(BaseExpressionSyntax node, SemanticModel model, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        if (!CanUseThis(node, model, cancellationToken))
        {
            return null;
        }

        if (node.Parent is MemberAccessExpressionSyntax access
            && model.GetSymbolInfo(access, cancellationToken).Symbol is { } symbol
            && PrefersNoQualifier(symbol, options))
        {
            // The whole statement is bound again with the plain name: a parameter or local of that name would take it.
            ExpressionSyntax original = access;
            ExpressionSyntax plain = access.Name.WithTriviaFrom(access);
            if (access.Parent is InvocationExpressionSyntax invocation && invocation.Expression == access)
            {
                original = invocation;
                plain = invocation.WithExpression(plain);
            }

            if (SymbolEqualityComparer.Default.Equals(symbol, Speculation.SymbolAfterReplacing(model, original, plain, cancellationToken))
                && access.OperatorToken.LeadingTrivia.Concat(access.OperatorToken.TrailingTrivia).Concat(node.Token.TrailingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                return new TextChange(TextSpan.FromBounds(node.SpanStart, access.Name.SpanStart), string.Empty);
            }
        }

        return new TextChange(node.Token.Span, "this");
    }

    private static bool PrefersNoQualifier(ISymbol symbol, AnalyzerConfigOptions options)
    {
        var key = symbol switch
        {
            IMethodSymbol => "dotnet_style_qualification_for_method",
            IPropertySymbol => "dotnet_style_qualification_for_property",
            IFieldSymbol => "dotnet_style_qualification_for_field",
            IEventSymbol => "dotnet_style_qualification_for_event",
            _ => null,
        };
        return key is not null && options.TryGetValue(key, out var value) && value.Split(':')[0].Trim().Equals("false", System.StringComparison.OrdinalIgnoreCase);
    }
}
