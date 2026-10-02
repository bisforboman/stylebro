using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Maintainability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Maintainability;

/// <summary>
/// Fix for BRO1401: inserts ',' after the last item. With nested initializers the inner comma goes before the inner
/// '}' and the outer one after it, so Fix All's insertions never collide.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TrailingCommaCodeFixProvider))]
public sealed class TrailingCommaCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add trailing comma";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.TrailingComma);

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
                    Title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(TrailingCommaCodeFixProvider)),
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
        if (root is null)
        {
            return document;
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var insertions = new Dictionary<int, TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            // The diagnostic is on the last item; its parent is the list.
            var item = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var list = item.Parent;
            while (list is not null && list.Span == item.Span)
            {
                list = list.Parent;
            }

            if (list is not null && TrailingCommas.GetLastItemWithoutComma(list, text) is { } last)
            {
                insertions[last.Span.End] = new TextChange(new TextSpan(last.Span.End, 0), TrailingCommas.GetInsertion(last, text));
            }
        }

        return document.WithText(text.WithChanges(insertions.Values));
    }
}
