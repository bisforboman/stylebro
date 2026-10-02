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

/// <summary>Fix for BRO1005 (one space after '///') and BRO1120 (removes empty comments at the start or end of a comment).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommentTextCodeFixProvider))]
public sealed class CommentTextCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.DocumentationLineSpace, DiagnosticIds.EmptyComment);

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
                    "Fix the comment",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(CommentTextCodeFixProvider) + diagnostic.Id),
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
        var changes = new List<TextChange>();
        var spaces = CommentText.GetBadDocumentationSpaces(root, text).ToList();
        var empty = CommentText.GetEmptyComments(root, text).ToList();
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            if (diagnostic.Id == DiagnosticIds.DocumentationLineSpace && spaces.Contains(span))
            {
                changes.Add(new TextChange(span, " "));
            }
            else if (empty.FirstOrDefault(e => e.Reported.Span == span) is { Removed: { } removed })
            {
                changes.AddRange(removed.Select(c => CommentText.GetRemoval(c, text)));
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
