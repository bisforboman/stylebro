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
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1107 (moves the first item to the line after the parenthesis) and BRO1108 (puts every item on its own
/// line, starting on the line after the parenthesis). BRO1107's edit is the first of BRO1108's edits for the same list,
/// so the two never disagree. Lists don't share gaps, so Fix All applies all edits at once.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParameterLayoutCodeFixProvider))]
public sealed class ParameterLayoutCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.SplitParametersStartOnNewLine, DiagnosticIds.ParametersOnSameOrSeparateLines);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var title = diagnostic.Id == DiagnosticIds.SplitParametersStartOnNewLine
                ? "Move the first item to the next line"
                : "Put each item on its own line";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(ParameterLayoutCodeFixProvider) + diagnostic.Id),
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

        // One entry per list: its span, the position of its '(' and whether every item moves (BRO1108) or only the first.
        var remaining = diagnostics
            .Select(d => (Diagnostic: d, List: root.FindNode(d.Location.SourceSpan).Parent))
            .Where(f => f.List is not null)
            .GroupBy(f => f.List!)
            .Select(g => (Span: g.Key.Span, Open: ParameterLayout.GetList(g.Key).Open.SpanStart, All: g.Any(f => f.Diagnostic.Id != DiagnosticIds.SplitParametersStartOnNewLine)))
            .ToList();

        // A list inside a moved item moves with it, so nested lists are fixed in rounds, outer lists first, each round
        // on the text the previous one left: the same result as fixing them one by one.
        var rounds = new List<List<TextChange>>();
        var current = root;
        var currentText = text;
        while (remaining.Count > 0)
        {
            var round = remaining.Where(l => !remaining.Any(o => o != l && o.Span.Contains(l.Span))).ToList();
            remaining = remaining.Except(round).ToList();
            var changes = new Dictionary<int, TextChange>();
            foreach (var list in round)
            {
                var open = rounds.Aggregate(list.Open, Map);
                if (current.FindToken(open) is var token && token.SpanStart == open && token.Parent is { } node
                    && ParameterLayout.GetList(node).Open == token
                    && (list.All
                        ? ParameterLayout.GetFirstMisplacedItem(node, currentText) ?? ParameterLayout.GetFirstItemToMove(node, currentText)
                        : ParameterLayout.GetFirstItemToMove(node, currentText)) is not null)
                {
                    foreach (var change in ParameterLayout.GetChanges(node, currentText, indentUnit, firstOnly: !list.All))
                    {
                        changes[change.Span.Start] = change;
                    }
                }
            }

            var ordered = changes.Values.OrderBy(c => c.Span.Start).ToList();
            rounds.Add(ordered);
            currentText = currentText.WithChanges(ordered);
            if (remaining.Count > 0)
            {
                current = await root.SyntaxTree.WithChangedText(currentText).GetRootAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return document.WithText(currentText);
    }

    /// <summary>Where a position (between edits, never inside one) is after a round of edits.</summary>
    private static int Map(int position, List<TextChange> changes) =>
        position + changes.Where(c => c.Span.End <= position).Sum(c => c.NewText!.Length - c.Span.Length);
}
