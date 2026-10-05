using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
/// Fix for BRO1503 (deletes the blank lines below the brace) and BRO1504 (inserts a line break at the start of the
/// comment's line). Both are whole-line edits, so Fix All can apply all of them at once.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BlankLineAfterCodeFixProvider))]
public sealed class BlankLineAfterCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.BlankLineAfterOpenBrace, DiagnosticIds.BlankLineBeforeComment);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var title = diagnostic.Id == DiagnosticIds.BlankLineAfterOpenBrace ? "Remove blank line" : "Add blank line";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(BlankLineAfterCodeFixProvider) + diagnostic.Id),
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
        var insertions = new Dictionary<int, TextChange>();
        var exempt = BlankLines.GetExemptPrefixes(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        foreach (var diagnostic in diagnostics)
        {
            var start = diagnostic.Location.SourceSpan.Start;
            if (diagnostic.Id == DiagnosticIds.BlankLineAfterOpenBrace)
            {
                blankLines.AddRange(BlankLines.GetBlankLinesBelow(root.FindToken(start), text));
            }
            else if (BlankLines.NeedsBlankLineAbove(root.FindTrivia(start), text, id => Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, id, cancellationToken), exempt))
            {
                var line = text.Lines.GetLineFromPosition(start);
                var above = text.Lines[line.LineNumber - 1];
                var lineBreak = text.ToString(TextSpan.FromBounds(above.End, above.EndIncludingLineBreak));
                insertions[line.Start] = new TextChange(new TextSpan(line.Start, 0), lineBreak.Length > 0 ? lineBreak : "\n");
            }
        }

        return document.WithText(text.WithChanges(BlankLines.GetChanges(blankLines).Concat(insertions.Values)));
    }
}
