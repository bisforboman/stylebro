using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1109 (moves the opening token up to the name) and BRO1110 (moves the closing token up to the last item).
/// Both are text edits within one list, so single fixes and Fix All agree.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParenthesisPlacementCodeFixProvider))]
public sealed class ParenthesisPlacementCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.OpenParenthesisOnNameLine, DiagnosticIds.CloseParenthesisOnLastItemLine);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var title = diagnostic.Id == DiagnosticIds.OpenParenthesisOnNameLine
                ? "Move the opening parenthesis up"
                : "Move the closing parenthesis up";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(ParenthesisPlacementCodeFixProvider) + diagnostic.Id),
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
        foreach (var diagnostic in diagnostics)
        {
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            var list = token.Parent;
            if (list is null)
            {
                continue;
            }

            if (diagnostic.Id == DiagnosticIds.OpenParenthesisOnNameLine)
            {
                if (ParenthesisPlacement.GetMisplacedOpen(list, text) is { } open)
                {
                    changes.Add(ParenthesisPlacement.GetOpenChange(open, text));
                }
            }
            else if (ParenthesisPlacement.GetMisplacedClose(list, text) is { } close)
            {
                changes.Add(ParenthesisPlacement.GetCloseChange(close, text));
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
