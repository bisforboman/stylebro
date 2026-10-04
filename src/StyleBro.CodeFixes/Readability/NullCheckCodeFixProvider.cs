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

/// <summary>
/// Fix for BRO1133. Each diagnostic's check is found again by its start (BRO1103 may have swapped 'null == x' to
/// 'x == null' at the same position) and checked again, so a check that no longer needs the fix is left alone.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCheckCodeFixProvider))]
public sealed class NullCheckCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.NullCheckStyle);

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
                    "Use the configured null check",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(NullCheckCodeFixProvider)),
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
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        var changes = new List<TextChange>();
        foreach (var start in diagnostics.Select(d => d.Location.SourceSpan.Start).Distinct())
        {
            if (start >= root.FullSpan.End)
            {
                continue;
            }

            var fix = root.FindToken(start).Parent?.AncestorsAndSelf().OfType<ExpressionSyntax>()
                .Where(e => e.SpanStart == start)
                .Select(e => NullChecks.GetFix(e, model, options, cancellationToken))
                .FirstOrDefault(f => f is not null);
            if (fix is { } found)
            {
                changes.AddRange(found.Changes);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
