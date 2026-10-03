using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StyleBro.CodeFixes.Layout;

/// <summary>
/// Fix for BRO1517-BRO1519. The findings are computed again on the document as it is now and matched to the
/// diagnostics by rule and span, so a diagnostic another fix has already dealt with changes nothing.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BlankLineRunsCodeFixProvider))]
public sealed class BlankLineRunsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.MultipleBlankLines, DiagnosticIds.BlankLineBeforeCloseBrace, DiagnosticIds.BlankLineAfterCloseBrace);

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
                    diagnostic.Id == DiagnosticIds.BlankLineAfterCloseBrace ? "Add a blank line" : "Remove the blank lines",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(BlankLineRunsCodeFixProvider)),
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
        var wanted = new HashSet<(string, int, int)>(diagnostics.Select(d => (d.Id, d.Location.SourceSpan.Start, d.Location.SourceSpan.End)));
        var options = document.Project.CompilationOptions;
        var changes = BlankLineRuns.GetFindings(root, text, id => Severities.IsOn(options, root.SyntaxTree, id, cancellationToken))
            .Where(f => wanted.Contains((f.Id, f.Location.Start, f.Location.End)))
            .Select(f => f.Change)
            .ToList();
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
