using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Documentation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.CodeFixes.Documentation;

/// <summary>
/// Fix for BRO1617 (braces for a cref's type arguments), BRO1618 ('&lt;see langword&gt;') and BRO1619 (elements in
/// order). Text edits recomputed from the shared logic, so Fix All agrees.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentationStyleCodeFixProvider))]
public sealed class DocumentationStyleCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.GenericCrefBraces, DiagnosticIds.LangwordElement, DiagnosticIds.DocumentationElementOrder);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var title = diagnostic.Id switch
            {
                DiagnosticIds.GenericCrefBraces => "Use braces for the type arguments",
                DiagnosticIds.LangwordElement => "Use <see langword/>",
                _ => "Put the documentation elements in order",
            };
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(DocumentationStyleCodeFixProvider) + diagnostic.Id),
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
        var reported = diagnostics.Select(d => (d.Id, d.Location.SourceSpan)).ToImmutableHashSet();
        var changes = root.DescendantTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .SelectMany(d => DocumentationStyle.GetFindings(d, text))
            .Where(f => reported.Contains((f.Id, f.Span)))
            .SelectMany(f => f.Changes)
            .ToList();
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
