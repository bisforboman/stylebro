using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Ordering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        var newRoot = new SortingRewriter(targets, options, partialAccess).Visit(root);
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

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitClassDeclaration(node));

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitStructDeclaration(node));

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitInterfaceDeclaration(node));

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitRecordDeclaration(node));

        public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitNamespaceDeclaration(node));

        public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitFileScopedNamespaceDeclaration(node));

        public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node) =>
            SortIfTargeted(node, base.VisitCompilationUnit(node));

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
