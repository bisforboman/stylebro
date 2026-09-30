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
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1107 (moves the first item to the line after the parenthesis) and BRO1108 (puts every item on its own
/// line, starting on the line after the parenthesis). BRO1107's edit is the first of BRO1108's edits for the same list,
/// so the two never disagree. Lists don't share gaps, so Fix All applies all edits at once.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParameterLayoutCodeFixProvider))]
public sealed class ParameterLayoutCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.SplitParametersStartOnNewLine, DiagnosticIds.ParametersOnSameOrSeparateLines);

    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

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
        var changes = new Dictionary<int, TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            var item = root.FindNode(diagnostic.Location.SourceSpan);
            var list = item.Parent;
            if (list is null)
            {
                continue;
            }

            if (diagnostic.Id == DiagnosticIds.SplitParametersStartOnNewLine)
            {
                if (ParameterLayout.GetFirstItemToMove(list, text) is not null)
                {
                    var first = ParameterLayout.GetChanges(list, text, indentUnit).First();
                    changes[first.Span.Start] = first;
                }
            }
            else if (ParameterLayout.GetFirstMisplacedItem(list, text) is not null)
            {
                foreach (var change in ParameterLayout.GetChanges(list, text, indentUnit))
                {
                    changes[change.Span.Start] = change;
                }
            }
        }

        return document.WithText(text.WithChanges(changes.Values));
    }
}
