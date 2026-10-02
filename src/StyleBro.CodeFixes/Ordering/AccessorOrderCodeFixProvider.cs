using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Ordering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Ordering;

/// <summary>Fix for BRO1003/BRO1004: swaps the two accessors with their comments (text edits, so Fix All agrees).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AccessorOrderCodeFixProvider))]
public sealed class AccessorOrderCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.PropertyAccessorOrder, DiagnosticIds.EventAccessorOrder);

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
                    "Swap the accessors",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(AccessorOrderCodeFixProvider)),
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
            if (root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.AncestorsAndSelf().OfType<AccessorListSyntax>().FirstOrDefault() is { } list
                && AccessorOrder.GetFinding(list, text) is not null
                && AccessorOrder.GetSwap(list, text) is { } swap)
            {
                changes.Add(swap.A);
                changes.Add(swap.B);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
