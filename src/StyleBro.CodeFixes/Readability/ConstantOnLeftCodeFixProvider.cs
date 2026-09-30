using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1103: swaps the operands. Comparisons can nest ('1 == (2 == x)'), so Fix All rewrites a document
/// bottom-up in one pass instead of merging text edits.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstantOnLeftCodeFixProvider))]
public sealed class ConstantOnLeftCodeFixProvider : CodeFixProvider
{
    private const string Title = "Put the constant on the right-hand side";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.ConstantOnLeft);

    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(ConstantOnLeftCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixDocumentAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return document;
        }

        var targets = new HashSet<BinaryExpressionSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is BinaryExpressionSyntax binary
                && ConstantComparisons.ShouldSwap(binary, model, cancellationToken))
            {
                targets.Add(binary);
            }
        }

        return targets.Count == 0 ? document : document.WithSyntaxRoot(new SwapRewriter(targets).Visit(root));
    }

    private sealed class SwapRewriter : CSharpSyntaxRewriter
    {
        private readonly HashSet<BinaryExpressionSyntax> _targets;

        public SwapRewriter(HashSet<BinaryExpressionSyntax> targets)
        {
            _targets = targets;
        }

        public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // 'node' is from the original tree, so it matches the targets; the operands are already rewritten.
            var visited = base.VisitBinaryExpression(node);
            return _targets.Contains(node) && visited is BinaryExpressionSyntax binary
                ? ConstantComparisons.Swap(binary)
                : visited;
        }
    }
}
