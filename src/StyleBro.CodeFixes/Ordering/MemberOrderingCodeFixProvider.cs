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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Ordering;

/// <summary>
/// Fix for BRO1001. Uses a custom, document-based Fix All provider: every flagged type in a document is
/// sorted in one syntax rewrite (innermost types first), so nested types never produce conflicting edits
/// the way the default BatchFixer's text merging can. This is what <c>dotnet format</c> invokes.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MemberOrderingCodeFixProvider))]
public sealed class MemberOrderingCodeFixProvider : CodeFixProvider
{
    private const string Title = "Reorder members";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.MemberOrdering);

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
                    equivalenceKey: nameof(MemberOrderingCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The edits that sort every container of the document that BRO1001 reports: one per outermost container, replacing
    /// its span. For fixes that change what BRO1001 sees, so it doesn't need another 'dotnet format' run (BRO1112:
    /// removing '#region's lets it sort across them). Edits of a span rather than the whole text, so they still merge
    /// with the other copies' edits of a multi-targeted file.
    /// </summary>
    internal static async Task<List<TextChange>> GetSortChangesAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return new List<TextChange>();
        }

        var options = MemberOrderOptions.Read(
            document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var partialAccess = new Dictionary<SyntaxNode, MemberAccess?[]?>();
        foreach (var container in root.DescendantNodesAndSelf(n => MemberOrdering.GetMembers(n) is not null).Where(n => MemberOrdering.GetMembers(n) is not null))
        {
            var access = MemberOrdering.GetPartialAccess(container, model, cancellationToken);
            if (MemberOrdering.FindFirstViolation(container, options, access) is not null)
            {
                partialAccess[container] = access;
            }
        }

        var targets = new HashSet<SyntaxNode>(partialAccess.Keys);
        var rewriter = new SortingRewriter(targets, options, partialAccess);
        return targets.Where(t => !t.Ancestors().Any(targets.Contains))
            .Select(t => new TextChange(t.Span, rewriter.Visit(t)!.ToString()))
            .ToList();
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

        var options = MemberOrderOptions.Read(
            document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));

        var targets = new HashSet<SyntaxNode>();
        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var member = node.FirstAncestorOrSelf<MemberDeclarationSyntax>(m => m.Parent is { } parent && MemberOrdering.GetMembers(parent) is not null);
            if (member?.Parent is { } container)
            {
                targets.Add(container);
            }
        }

        if (targets.Count == 0)
        {
            return document;
        }

        // Partial types' accessibility comes from the semantic model, which knows the original nodes only: read it now.
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var partialAccess = new Dictionary<SyntaxNode, MemberAccess?[]?>();
        foreach (var target in targets)
        {
            partialAccess[target] = model is null ? null : MemberOrdering.GetPartialAccess(target, model, cancellationToken);
        }

        var newRoot = new SortingRewriter(targets, options, partialAccess).Visit(root)!;
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>Sorts the targeted types bottom-up, so nested types are sorted before their parents.</summary>
    private sealed class SortingRewriter : CSharpSyntaxRewriter
    {
        private readonly HashSet<SyntaxNode> targets;
        private readonly MemberOrderOptions options;
        private readonly Dictionary<SyntaxNode, MemberAccess?[]?> partialAccess;

        public SortingRewriter(HashSet<SyntaxNode> targets, MemberOrderOptions options, Dictionary<SyntaxNode, MemberAccess?[]?> partialAccess)
        {
            this.targets = targets;
            this.options = options;
            this.partialAccess = partialAccess;
        }

        // One override for every container, also C# 14 extension blocks, whose Visit method Roslyn 4.8 doesn't have.
        public override SyntaxNode? Visit(SyntaxNode? node) =>
            node is null ? null : SortIfTargeted(node, base.Visit(node));

        private SyntaxNode? SortIfTargeted(SyntaxNode original, SyntaxNode? visited)
        {
            // 'original' is the node from the unmodified tree, so reference equality with the targets holds.
            // Sorting nested types doesn't move this container's own members, so the indexes still match.
            return targets.Contains(original) && visited is not null
                ? MemberOrdering.Sort(visited, options, partialAccess[original])
                : visited;
        }
    }
}
