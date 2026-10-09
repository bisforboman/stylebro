using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;
using StyleBro.Analyzers.Ordering;

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

        // The layout pre-step's edits are inside the containers, so each outermost one still replaces its original span.
        var outermost = partialAccess.Keys.Where(t => !t.Ancestors().Any(partialAccess.ContainsKey)).ToList();
        var (laidOut, newRoot, moved) = await ApplyLayoutAsync(document, root, new HashSet<SyntaxNode>(partialAccess.Keys), cancellationToken).ConfigureAwait(false);
        var movedAccess = moved.ToDictionary(p => p.Value, p => partialAccess[p.Key]);
        var rewriter = new SortingRewriter(new HashSet<SyntaxNode>(movedAccess.Keys), options, movedAccess, GetAutoAccessorLines(laidOut, newRoot.SyntaxTree, cancellationToken), AllowsAdjacentSingleLine(laidOut, newRoot.SyntaxTree));
        return outermost.Where(moved.ContainsKey)
            .Select(t => new TextChange(t.Span, rewriter.Visit(moved[t])!.ToString()))
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

        var (laidOut, laidOutRoot, moved) = await ApplyLayoutAsync(document, root, targets, cancellationToken).ConfigureAwait(false);
        (document, root) = (laidOut, laidOutRoot);
        partialAccess = moved.ToDictionary(p => p.Value, p => partialAccess[p.Key]);
        targets = new HashSet<SyntaxNode>(moved.Values);

        var newRoot = new SortingRewriter(targets, options, partialAccess, GetAutoAccessorLines(document, root.SyntaxTree, cancellationToken), AllowsAdjacentSingleLine(document, root.SyntaxTree)).Visit(root)!;
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Applies <see cref="GetLayoutChanges"/>: the new document and root, and where each container is in it (a container
    /// that can't be found again is left out).
    /// </summary>
    private static async Task<(Document Document, SyntaxNode Root, Dictionary<SyntaxNode, SyntaxNode> Moved)> ApplyLayoutAsync(Document document, SyntaxNode root, HashSet<SyntaxNode> targets, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var layout = LinkedFileFixAllProvider.Merge(GetLayoutChanges(document, root, text, targets, cancellationToken));
        var moved = targets.ToDictionary(t => t, t => t);
        if (layout.Count == 0)
        {
            return (document, root, moved);
        }

        // The containers again, in the new text: each starts where it did, shifted by the edits before it.
        document = document.WithText(text.WithChanges(layout));
        var newRoot = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
        moved.Clear();
        foreach (var target in targets)
        {
            var start = target.SpanStart + layout.Where(c => c.Span.End <= target.SpanStart).Sum(c => c.NewText!.Length - c.Span.Length);
            if (newRoot.FindToken(start).Parent?.AncestorsAndSelf().FirstOrDefault(n => n.SpanStart == start && n.RawKind == target.RawKind) is { } found)
            {
                moved[target] = found;
            }
        }

        return (document, newRoot, moved);
    }

    /// <summary>
    /// What BRO1509 and BRO1505 do to the containers, made before the sort so it starts from the same text in every fix
    /// order: blank lines stay with their slot, so sorting before or after those fixes gave different text. A container on
    /// one line is expanded like BRO1509's fix does (also BRO1505's, when it reports members there); otherwise BRO1505's
    /// missing blank lines between its members are added.
    /// </summary>
    private static List<TextChange> GetLayoutChanges(Document document, SyntaxNode root, SourceText text, HashSet<SyntaxNode> targets, CancellationToken cancellationToken)
    {
        bool IsOn(string id) => Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, id, cancellationToken);
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        var violations = IsOn(DiagnosticIds.ElementsSeparatedByBlankLine)
            ? ElementSeparation.GetViolations(root, text, GetAutoAccessorLines(document, root.SyntaxTree, cancellationToken), ElementSeparation.AllowsAdjacentSingleLineMembers(options)).ToList()
            : [];
        var trailingComma = SingleLineBlocks.WantsTrailingComma(document.Project.CompilationOptions, root.SyntaxTree, cancellationToken);
        var changes = new List<TextChange>();
        foreach (var target in targets)
        {
            var own = violations.Where(v => v.Current.Parent == target).ToList();
            if ((own.Count > 0 || IsOn(DiagnosticIds.SingleLineElement)) && SingleLineBlocks.GetChanges(target, text, options, trailingComma, IsOn) is { } expansion)
            {
                changes.AddRange(expansion);
            }
            else
            {
                changes.AddRange(own.Select(v => ElementSeparation.GetChange(v.Previous, v.Current, text)).OfType<TextChange>());
            }
        }

        return changes;
    }

    /// <summary>The file's options when BRO1527 is on: its fix puts multi-line auto-properties on one line, so the sort judges them so.</summary>
    private static AnalyzerConfigOptions? GetAutoAccessorLines(Document document, SyntaxTree tree, CancellationToken cancellationToken) =>
        Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken)
            ? document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree)
            : null;

    /// <summary>BRO1505's stylebro_allow_adjacent_single_line_members: the sort adds a blank line only where BRO1505 wants one.</summary>
    private static bool AllowsAdjacentSingleLine(Document document, SyntaxTree tree) =>
        StyleBro.Analyzers.Layout.ElementSeparation.AllowsAdjacentSingleLineMembers(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree));

    /// <summary>Sorts the targeted types bottom-up, so nested types are sorted before their parents.</summary>
    private sealed class SortingRewriter : CSharpSyntaxRewriter
    {
        private readonly HashSet<SyntaxNode> targets;
        private readonly MemberOrderOptions options;
        private readonly Dictionary<SyntaxNode, MemberAccess?[]?> partialAccess;
        private readonly AnalyzerConfigOptions? autoAccessorLines;
        private readonly bool allowAdjacentSingleLine;

        public SortingRewriter(HashSet<SyntaxNode> targets, MemberOrderOptions options, Dictionary<SyntaxNode, MemberAccess?[]?> partialAccess, AnalyzerConfigOptions? autoAccessorLines, bool allowAdjacentSingleLine)
        {
            this.allowAdjacentSingleLine = allowAdjacentSingleLine;
            this.targets = targets;
            this.options = options;
            this.partialAccess = partialAccess;
            this.autoAccessorLines = autoAccessorLines;
        }

        // One override for every container, also C# 14 extension blocks, whose Visit method Roslyn 4.8 doesn't have.
        public override SyntaxNode? Visit(SyntaxNode? node) =>
            node is null ? null : SortIfTargeted(node, base.Visit(node));

        private SyntaxNode? SortIfTargeted(SyntaxNode original, SyntaxNode? visited)
        {
            // 'original' is the node from the unmodified tree, so reference equality with the targets holds.
            // Sorting nested types doesn't move this container's own members, so the indexes still match.
            return targets.Contains(original) && visited is not null
                ? MemberOrdering.Sort(visited, options, partialAccess[original], autoAccessorLines, allowAdjacentSingleLine)
                : visited;
        }
    }
}
