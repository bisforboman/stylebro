using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Readability;

/// <summary>Fix for BRO1131: 'base' becomes 'this'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BaseCallsCodeFixProvider))]
public sealed class BaseCallsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.BaseCall);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use 'this' instead of 'base'",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(BaseCallsCodeFixProvider)),
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

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is BaseExpressionSyntax node
                && BaseCalls.CanUseThis(node, model, cancellationToken))
            {
                changes.Add(new TextChange(node.Token.Span, "this"));
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
