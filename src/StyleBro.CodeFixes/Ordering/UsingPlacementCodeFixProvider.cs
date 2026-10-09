using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Ordering;

namespace StyleBro.CodeFixes.Ordering;

/// <summary>
/// Fix for BRO1008: moves every using of the file. In a multi-targeted project each target framework's copy of the file
/// must agree (a name can bind differently where a framework lacks a type); otherwise nothing changes.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingPlacementCodeFixProvider))]
public sealed class UsingPlacementCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.UsingPlacement);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(
            CodeAction.Create(
                "Move the using directives",
                ct => FixDocumentAsync(context.Document, context.Diagnostics, ct),
                equivalenceKey: nameof(UsingPlacementCodeFixProvider)),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    private static async Task<Document> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var original = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        SourceText? fixedText = null;
        foreach (var id in document.GetLinkedDocumentIds().Insert(0, document.Id))
        {
            // A copy whose text 'dotnet format' already changed differently is left to the next run, like in LinkedFileFixAllProvider.
            if (document.Project.Solution.GetDocument(id) is not { } copy
                || !(await copy.GetTextAsync(cancellationToken).ConfigureAwait(false)).ContentEquals(original))
            {
                continue;
            }

            if (await copy.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
            {
                return document;
            }

            var options = copy.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(model.SyntaxTree);
            var changes = UsingPlacement.GetChanges(model, UsingPlacement.GetMode(options), Indentation.GetUnit(options), cancellationToken);
            var copyText = changes is null ? null : original.WithChanges(changes);
            if (copyText is null || (fixedText is not null && !fixedText.ContentEquals(copyText)))
            {
                return document;
            }

            fixedText = copyText;
        }

        return fixedText is null ? document : document.WithText(fixedText);
    }
}
