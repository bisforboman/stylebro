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
/// Fix for BRO1506 (deletes the blank lines below the comment) and BRO1507 (removes everything after the last line's
/// line break). Both are deletions that never overlap, so Fix All applies them at once.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TrailingBlankLinesCodeFixProvider))]
public sealed class TrailingBlankLinesCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove blank lines";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.BlankLineAfterComment, DiagnosticIds.BlankLinesAtEndOfFile);

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
                    equivalenceKey: nameof(TrailingBlankLinesCodeFixProvider) + diagnostic.Id),
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
        var blankLines = new List<TextLine>();
        TextSpan? ending = null;
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Id == DiagnosticIds.BlankLineAfterComment)
            {
                blankLines.AddRange(TrailingBlankLines.GetBlankLinesAfterComment(root.FindTrivia(diagnostic.Location.SourceSpan.Start), text));
            }
            else
            {
                ending = TrailingBlankLines.GetExtraEnding(text);
            }
        }

        // Blank lines after a comment at the end of the file are part of the ending; removing the ending covers them.
        var changes = BlankLines.GetChanges(blankLines).Where(c => ending is not { } e || c.Span.End <= e.Start).ToList();
        if (ending is { } span)
        {
            changes.Add(new TextChange(span, string.Empty));
        }

        return document.WithText(text.WithChanges(changes));
    }
}
