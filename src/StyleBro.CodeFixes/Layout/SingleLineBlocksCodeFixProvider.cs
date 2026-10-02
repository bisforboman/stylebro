using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Layout;

/// <summary>
/// Fix for BRO1508/BRO1509: puts the braces and each item between them on their own lines (text edits in the gaps, so
/// nested blocks don't overlap and Fix All agrees with the single fix).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleLineBlocksCodeFixProvider))]
public sealed class SingleLineBlocksCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.SingleLineStatementBlock, DiagnosticIds.SingleLineElement);

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
                    "Put it on separate lines",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(SingleLineBlocksCodeFixProvider)),
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
        var trailingComma = SingleLineBlocks.WantsTrailingComma(document.Project.CompilationOptions, root.SyntaxTree, cancellationToken);
        var changes = new List<TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            // The diagnostic is on the opening brace; the node is the one whose braces those are.
            var brace = root.FindToken(diagnostic.Location.SourceSpan.Start);
            for (var node = brace.Parent; node is not null; node = node.Parent)
            {
                if (SingleLineBlocks.GetBraces(node) is { } braces && braces.Open == brace)
                {
                    changes.AddRange(SingleLineBlocks.GetChanges(node, text, options, trailingComma) ?? []);
                    break;
                }
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
