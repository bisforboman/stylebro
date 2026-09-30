using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Spacing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Spacing;

/// <summary>Fix for BRO1002: rewrites each reported comment. Comments never overlap, so Fix All is one set of edits.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommentSpacingCodeFixProvider))]
public sealed class CommentSpacingCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add a space after '//'";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.CommentSpacing);

    public override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(async (fixAllContext, document, diagnostics) =>
            diagnostics.IsEmpty
                ? null
                : await FixDocumentAsync(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false));

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(CommentSpacingCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixDocumentAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>();
        foreach (var span in diagnostics.Select(d => d.Location.SourceSpan).Distinct())
        {
            if (CommentSpacing.GetFixedText(text.ToString(span)) is { } fixedText)
            {
                changes.Add(new TextChange(span, fixedText));
            }
        }

        return document.WithText(text.WithChanges(changes));
    }
}
