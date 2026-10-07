using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;

namespace StyleBro.CodeFixes.Layout;

/// <summary>
/// Fix for BRO1525. The findings are computed again on the document as it is now and matched to the diagnostics by
/// position, so a diagnostic another fix has already dealt with changes nothing.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AttributeBlankLinesCodeFixProvider))]
public sealed class AttributeBlankLinesCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.BlankLineAfterAttributes);

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
                    "Remove the blank line",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(AttributeBlankLinesCodeFixProvider)),
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
        var wanted = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var changes = AttributeBlankLines.GetFindings(root, text)
            .Where(span => wanted.Contains(span.Start))
            .Select(span => new TextChange(span, string.Empty))
            .ToList();
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
