using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;

namespace StyleBro.CodeFixes.Layout;

/// <summary>
/// Fix for BRO1501 and BRO1502: deletes the blank lines directly above the reported token. Fix All deletes the
/// lines for every diagnostic in a document at once; whole-line deletions can't conflict.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BlankLineBeforeCodeFixProvider))]
public sealed class BlankLineBeforeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove blank line";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.BlankLineBeforeOpenBrace, DiagnosticIds.BlankLineBeforeChainedBlock);

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
                    equivalenceKey: nameof(BlankLineBeforeCodeFixProvider)),
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
        var lines = diagnostics.SelectMany(d => BlankLines.GetBlankLinesAbove(root.FindToken(d.Location.SourceSpan.Start), text));
        return document.WithText(text.WithChanges(BlankLines.GetChanges(lines)));
    }
}
