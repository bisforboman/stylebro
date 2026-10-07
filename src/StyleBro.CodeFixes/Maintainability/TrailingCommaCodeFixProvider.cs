using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Maintainability;

namespace StyleBro.CodeFixes.Maintainability;

/// <summary>
/// Fix for BRO1401: inserts ',' after the last item, or with <c>stylebro_trailing_comma = omit</c> removes it. With
/// nested initializers the inner comma is before the inner '}' and the outer one after it, so Fix All's edits never collide.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TrailingCommaCodeFixProvider))]
public sealed class TrailingCommaCodeFixProvider : CodeFixProvider
{
    private const string Title = "Fix trailing comma";

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
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        var changes = new Dictionary<int, TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            // The diagnostic is on the last item (include) or the trailing comma (omit): the list is the nearest node
            // whose finding is at that place.
            foreach (var node in root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).AncestorsAndSelf())
            {
                if (TrailingCommas.GetFinding(node, text, () => options) is { } finding && finding.Location.SourceSpan == diagnostic.Location.SourceSpan)
                {
                    changes[finding.Change.Span.Start] = finding.Change;
                    break;
                }
            }
        }

        return document.WithText(text.WithChanges(changes.Values));
    }
}
