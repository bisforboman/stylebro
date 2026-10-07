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
using StyleBro.Analyzers.Spacing;

namespace StyleBro.CodeFixes.Spacing;

/// <summary>Fix for BRO1006: removes the space after '#'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DirectiveSpacingCodeFixProvider))]
public sealed class DirectiveSpacingCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.DirectiveSpacing);

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
                    "Remove the space after '#'",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(DirectiveSpacingCodeFixProvider)),
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
        var starts = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var changes = DirectiveSpacingAnalyzer.GetSpaces(root)
            .Where(s => starts.Contains(s.Keyword.SpanStart))
            .Select(s => new TextChange(s.Space, string.Empty))
            .ToList();
        return document.WithText(text.WithChanges(changes));
    }
}
