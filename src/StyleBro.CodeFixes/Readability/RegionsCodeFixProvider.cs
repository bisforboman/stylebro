using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Ordering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1112 and BRO1113: removes the '#region' and '#endregion' lines. All regions of a document are removed in
/// one text edit set, so neighboring regions share their blank-line cleanup.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RegionsCodeFixProvider))]
public sealed class RegionsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.NoRegions, DiagnosticIds.NoRegionsInCodeElements);

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
                    "Remove the region",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(RegionsCodeFixProvider) + diagnostic.Id),
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
        var starts = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var directives = new List<DirectiveTriviaSyntax>();
        foreach (var (region, endRegion, _) in Regions.GetRegions(root))
        {
            if (starts.Contains(region.SpanStart))
            {
                directives.Add(region);
                directives.Add(endRegion);
            }
        }

        var fixedDocument = document.WithText(text.WithChanges(Regions.GetChanges(directives, text)));

        // BRO1001 sorts within each region; without them it sorts across, now rather than in another run.
        return Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.MemberOrdering, cancellationToken)
            ? await MemberOrderingCodeFixProvider.SortAllAsync(fixedDocument, cancellationToken).ConfigureAwait(false)
            : fixedDocument;
    }
}
