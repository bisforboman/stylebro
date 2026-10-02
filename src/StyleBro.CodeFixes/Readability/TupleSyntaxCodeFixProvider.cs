using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

/// <summary>Fix for BRO1123: writes the tuple type or tuple literal.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TupleSyntaxCodeFixProvider))]
public sealed class TupleSyntaxCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.TupleSyntax);

    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use tuple syntax",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(TupleSyntaxCodeFixProvider)),
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
            // The diagnostic is on the type, the creation, or 'ValueTuple.Create' inside the invocation.
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: false);
            var finding = node.AncestorsAndSelf()
                .Select(n => n switch
                {
                    GenericNameSyntax name => TupleSyntax.GetType(name, model, cancellationToken),
                    ObjectCreationExpressionSyntax or InvocationExpressionSyntax => TupleSyntax.GetCreation((ExpressionSyntax)n, model, cancellationToken),
                    _ => null,
                })
                .Concat(node.DescendantNodes().OfType<GenericNameSyntax>().Select(n => TupleSyntax.GetType(n, model, cancellationToken)))
                .FirstOrDefault(f => f is not null && f.Location.SourceSpan == diagnostic.Location.SourceSpan);
            if (finding is not null)
            {
                changes.Add(finding.Change);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
