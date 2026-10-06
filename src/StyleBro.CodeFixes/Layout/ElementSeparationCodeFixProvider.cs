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

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.ElementsSeparatedByBlankLine);

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
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        var trailingComma = SingleLineBlocks.WantsTrailingComma(document.Project.CompilationOptions, root.SyntaxTree, cancellationToken);
        var reported = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var changes = new List<TextChange>();
        var autoAccessorLines = Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken) ? options : null;
        foreach (var (previous, current) in ElementSeparation.GetViolations(root, text, autoAccessorLines))
        {
            if (!reported.Contains(current.FullSpan.Start))
            {
                continue;
            }

            // Members of a type (namespace, accessor list) written on one line: the same expansion BRO1509's fix makes,
            // which separates them as BRO1505 wants. Splitting just this gap would leave the braces on the line and
            // stop BRO1509 from seeing the type, so the result would depend on which fix 'dotnet format' runs first.
            if (current.Parent is { } container && SingleLineBlocks.GetChanges(container, text, options, trailingComma) is { } expansion)
            {
                changes.AddRange(expansion);
            }
            else if (ElementSeparation.GetChange(previous, current, text) is { } change)
            {
                changes.Add(change);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
