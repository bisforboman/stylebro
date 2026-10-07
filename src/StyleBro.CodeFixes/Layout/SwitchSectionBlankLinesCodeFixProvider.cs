using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;

namespace StyleBro.CodeFixes.Layout;

/// <summary>
/// Fix for BRO1526. The findings are computed again on the document as it is now and matched to the diagnostics by
/// position, so a diagnostic another fix has already dealt with changes nothing.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwitchSectionBlankLinesCodeFixProvider))]
public sealed class SwitchSectionBlankLinesCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.BlankLineBetweenSwitchSections);

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
                    "Fix the blank line between switch sections",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(SwitchSectionBlankLinesCodeFixProvider)),
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
        var mode = SwitchSectionBlankLines.ReadMode(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var options = document.Project.CompilationOptions;
        var wanted = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var changes = diagnostics
            .Select(d => root.FindToken(d.Location.SourceSpan.Start).Parent?.FirstAncestorOrSelf<SwitchStatementSyntax>())
            .Where(s => s is not null)
            .Distinct()
            .SelectMany(s => SwitchSectionBlankLines.GetFindings(s!, text, mode, id => Severities.IsOn(options, root.SyntaxTree, id, cancellationToken)))
            .Where(f => wanted.Contains(f.Location.Start))
            .Select(f => f.Change)
            .ToList();
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
