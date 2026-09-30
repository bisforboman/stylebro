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
/// Fix for BRO1505: adds the blank line before each reported element. The edits are insertions at line starts (or a
/// replaced run of spaces), one per element, so Fix All applies them all at once.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ElementSeparationCodeFixProvider))]
public sealed class ElementSeparationCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add blank line";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.ElementsSeparatedByBlankLine);

    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(ElementSeparationCodeFixProvider)),
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
        var reported = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var changes = new Dictionary<int, TextChange>();
        foreach (var (previous, current) in ElementSeparation.GetViolations(root, text))
        {
            if (reported.Contains(current.FullSpan.Start) && ElementSeparation.GetChange(previous, current, text) is { } change)
            {
                changes[change.Span.Start] = change;
            }
        }

        return document.WithText(text.WithChanges(changes.Values));
    }
}
