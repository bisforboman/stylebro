using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1132: moves every comment between a header and its '{' into the block (all of one block at once, so a
/// single fix and Fix All give the same edits).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmbeddedCommentCodeFixProvider))]
public sealed class EmbeddedCommentCodeFixProvider : CodeFixProvider
{
    private const string Title = "Move the comment into the block";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.EmbeddedComment);

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
                    equivalenceKey: nameof(EmbeddedCommentCodeFixProvider)),
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
        var indentUnit = Indentation.GetUnit(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var changes = diagnostics
            .Select(d => EmbeddedComments.GetOpenBrace(root.FindTrivia(d.Location.SourceSpan.Start)))
            .Distinct()
            .Select(brace => (Brace: brace, Comments: EmbeddedComments.GetComments(brace, text)))
            .Where(x => !x.Comments.IsEmpty)
            .SelectMany(x => EmbeddedComments.GetChanges(x.Brace, x.Comments, text, indentUnit));
        return document.WithText(text.WithChanges(changes));
    }
}
