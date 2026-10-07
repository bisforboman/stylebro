using System.Collections.Generic;
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
    /// The symbol <paramref name="replacement"/> binds to in place of <paramref name="original"/>, with the whole enclosing
    /// statement, initializer or expression body bound again (a position-based lookup misses parameters and locals in
    /// some places). Null when it can't be checked.
    /// </summary>
    public static ISymbol? SymbolAfterReplacing(SemanticModel model, ExpressionSyntax original, ExpressionSyntax replacement, CancellationToken cancellationToken)
    {
        var annotation = new SyntaxAnnotation();
        replacement = replacement.WithAdditionalAnnotations(annotation);
        return GetContainer(original) is { } container
            && Speculate(model, container, container.ReplaceNode(original, replacement)) is { } speculative
            && speculative.Container.GetAnnotatedNodes(annotation).FirstOrDefault() is { } bound
            ? speculative.Model.GetSymbolInfo(bound, cancellationToken).Symbol
            : null;
    }

    /// <summary>
    /// The node <see cref="BindsTheSame"/> and <see cref="SymbolAfterReplacing"/> bind again: the nearest enclosing
    /// statement (not a block), initializer (not a local's, its statement is used) or expression body. Null if none.
    /// </summary>
    public static SyntaxNode? GetContainer(SyntaxNode node) =>
        node.Ancestors().FirstOrDefault(a => a is (StatementSyntax and not BlockSyntax)
            or EqualsValueClauseSyntax { Parent: not VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax } }
            or ArrowExpressionClauseSyntax);

    /// <summary>
    /// Whether <paramref name="replacementText"/> in place of <paramref name="original"/> converts to the same type and,
    /// as an argument, calls the same method. False when it can't be checked (no statement, initializer or expression
    /// body around it) or doesn't parse.
    /// </summary>
    public static bool BindsTheSame(SemanticModel model, ExpressionSyntax original, string replacementText, CancellationToken cancellationToken) =>
        BindTheSame(model, new[] { original }, replacementText, cancellationToken);

    /// <summary>
    /// <see cref="BindsTheSame"/> with every one of <paramref name="originals"/> replaced at once (all in the
    /// <see cref="GetContainer">container</see> of the first): each must still convert to the same type and, as an
    /// argument, call the same method.
    /// </summary>
    public static bool BindTheSame(SemanticModel model, IReadOnlyList<ExpressionSyntax> originals, string replacementText, CancellationToken cancellationToken)
    {
        var replacement = SyntaxFactory.ParseExpression(replacementText);
        if (replacement.ContainsDiagnostics || replacement.FullSpan.Length != replacementText.Length
            || GetContainer(originals[0]) is not { } container)
        {
            return false;
        }

        var annotations = originals.ToDictionary(o => o, _ => new SyntaxAnnotation());
        if (Speculate(model, container, container.ReplaceNodes(originals, (o, _) => replacement.WithAdditionalAnnotations(annotations[o]))) is not { } speculative)
        {
            return false;
        }

        foreach (var original in originals)
        {
            if (speculative.Container.GetAnnotatedNodes(annotations[original]).FirstOrDefault() is not ExpressionSyntax bound)
            {
                return false;
            }

            var before = model.GetTypeInfo(original, cancellationToken).ConvertedType;
            var after = speculative.Model.GetTypeInfo(bound, cancellationToken).ConvertedType;
            if (before is null || after is null || before.TypeKind == TypeKind.Error || !SymbolEqualityComparer.Default.Equals(before, after))
            {
                return false;
            }

            if (original.Parent is ArgumentSyntax { Parent.Parent: { } call })
            {
                var callAfter = bound.Parent?.Parent?.Parent;
                var symbolBefore = model.GetSymbolInfo(call, cancellationToken).Symbol;
                if (symbolBefore is null || callAfter is null
                    || !SymbolEqualityComparer.Default.Equals(symbolBefore, speculative.Model.GetSymbolInfo(callAfter, cancellationToken).Symbol))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static (SemanticModel Model, SyntaxNode Container)? Speculate(SemanticModel model, SyntaxNode original, SyntaxNode replaced)
    {
        SemanticModel? speculative = null;
        switch (original)
        {
            case StatementSyntax statement:
                model.TryGetSpeculativeSemanticModel(statement.SpanStart, (StatementSyntax)replaced, out speculative);
                break;

            case EqualsValueClauseSyntax clause:
                model.TryGetSpeculativeSemanticModel(clause.SpanStart, (EqualsValueClauseSyntax)replaced, out speculative);
                break;

            case ArrowExpressionClauseSyntax arrow:
                model.TryGetSpeculativeSemanticModel(arrow.SpanStart, (ArrowExpressionClauseSyntax)replaced, out speculative);
                break;
        }

        return speculative is null ? null : (speculative, replaced);
    }
}
