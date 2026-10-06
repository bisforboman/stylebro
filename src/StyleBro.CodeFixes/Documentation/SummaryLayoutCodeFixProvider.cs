using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Documentation;

namespace StyleBro.CodeFixes.Documentation;

/// <summary>Fix for BRO1616: rewrites the gaps between the summary's tags and its text. Text edits, so Fix All agrees.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SummaryLayoutCodeFixProvider))]
public sealed class SummaryLayoutCodeFixProvider : CodeFixProvider
{
    private const string Title = "Fix the summary's layout";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.SummaryLayout);

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
                    equivalenceKey: nameof(SummaryLayoutCodeFixProvider)),
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
        var reported = diagnostics.Select(d => d.Location.SourceSpan).ToImmutableHashSet();
        var changes = SummaryLayout.GetFindings(root, text, options)
            .Where(f => reported.Contains(f.StartTag.Span))
            .SelectMany(f => f.Changes)
            .ToList();
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
