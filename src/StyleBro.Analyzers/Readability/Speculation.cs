using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>Binds a replacement expression in place of the original to check that it means the same.</summary>
internal static class Speculation
{
    /// <summary>
    /// Whether <paramref name="replacementText"/> in place of <paramref name="original"/> converts to the same type and,
    /// as an argument, calls the same method. False when it can't be checked (no statement, initializer or expression
    /// body around it) or doesn't parse.
    /// </summary>
    /// <summary>
    /// The symbol <paramref name="replacement"/> binds to in place of <paramref name="original"/>, with the whole enclosing
    /// statement, initializer or expression body bound again (a position-based lookup misses parameters and locals in
    /// some places). Null when it can't be checked.
    /// </summary>
    public static ISymbol? SymbolAfterReplacing(SemanticModel model, ExpressionSyntax original, ExpressionSyntax replacement, CancellationToken cancellationToken)
    {
        var annotation = new SyntaxAnnotation();
        replacement = replacement.WithAdditionalAnnotations(annotation);
        foreach (var ancestor in original.Ancestors())
        {
            SemanticModel? speculative = null;
            SyntaxNode? container = null;
            switch (ancestor)
            {
                case StatementSyntax statement when statement is not BlockSyntax:
                    container = statement.ReplaceNode(original, replacement);
                    model.TryGetSpeculativeSemanticModel(statement.SpanStart, (StatementSyntax)container, out speculative);
                    break;

                case EqualsValueClauseSyntax clause when clause.Parent is not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax }:
                    container = clause.ReplaceNode(original, replacement);
                    model.TryGetSpeculativeSemanticModel(clause.SpanStart, (EqualsValueClauseSyntax)container, out speculative);
                    break;

                case ArrowExpressionClauseSyntax arrow:
                    container = arrow.ReplaceNode(original, replacement);
                    model.TryGetSpeculativeSemanticModel(arrow.SpanStart, (ArrowExpressionClauseSyntax)container, out speculative);
                    break;

                default:
                    continue;
            }

            return speculative is not null && container?.GetAnnotatedNodes(annotation).FirstOrDefault() is { } bound
                ? speculative.GetSymbolInfo(bound, cancellationToken).Symbol
                : null;
        }

        return null;
    }

    public static bool BindsTheSame(SemanticModel model, ExpressionSyntax original, string replacementText, CancellationToken cancellationToken)
    {
        var replacement = SyntaxFactory.ParseExpression(replacementText);
        if (replacement.ContainsDiagnostics || replacement.FullSpan.Length != replacementText.Length)
        {
            return false;
        }

        var annotation = new SyntaxAnnotation();
        replacement = replacement.WithAdditionalAnnotations(annotation);
        SemanticModel? speculative = null;
        SyntaxNode? container = null;
        foreach (var ancestor in original.Ancestors())
        {
            switch (ancestor)
            {
                case StatementSyntax statement when statement is not BlockSyntax:
                    container = statement.ReplaceNode(original, replacement);
                    model.TryGetSpeculativeSemanticModel(statement.SpanStart, (StatementSyntax)container, out speculative);
                    break;

                case EqualsValueClauseSyntax clause when clause.Parent is not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax }:
                    container = clause.ReplaceNode(original, replacement);
                    model.TryGetSpeculativeSemanticModel(clause.SpanStart, (EqualsValueClauseSyntax)container, out speculative);
                    break;

                case ArrowExpressionClauseSyntax arrow:
                    container = arrow.ReplaceNode(original, replacement);
                    model.TryGetSpeculativeSemanticModel(arrow.SpanStart, (ArrowExpressionClauseSyntax)container, out speculative);
                    break;

                default:
                    continue;
            }

            break;
        }

        if (speculative is null || container?.GetAnnotatedNodes(annotation).FirstOrDefault() is not ExpressionSyntax bound)
        {
            return false;
        }

        var before = model.GetTypeInfo(original, cancellationToken).ConvertedType;
        var after = speculative.GetTypeInfo(bound, cancellationToken).ConvertedType;
        if (before is null || after is null || before.TypeKind == TypeKind.Error || !SymbolEqualityComparer.Default.Equals(before, after))
        {
            return false;
        }

        if (original.Parent is ArgumentSyntax { Parent.Parent: { } call })
        {
            var callAfter = bound.Parent?.Parent?.Parent;
            var symbolBefore = model.GetSymbolInfo(call, cancellationToken).Symbol;
            return symbolBefore is not null && callAfter is not null
                && SymbolEqualityComparer.Default.Equals(symbolBefore, speculative.GetSymbolInfo(callAfter, cancellationToken).Symbol);
        }

        return true;
    }
}
