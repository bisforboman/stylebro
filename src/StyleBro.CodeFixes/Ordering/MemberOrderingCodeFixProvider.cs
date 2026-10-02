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

        var targets = new HashSet<TypeDeclarationSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var member = node.FirstAncestorOrSelf<MemberDeclarationSyntax>(m => m.Parent is TypeDeclarationSyntax);
            if (member?.Parent is TypeDeclarationSyntax type)
            {
                targets.Add(type);
            }
        }

        if (targets.Count == 0)
        {
            return document;
        }

        var newRoot = new SortingRewriter(targets, options).Visit(root);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>Sorts the targeted types bottom-up, so nested types are sorted before their parents.</summary>
    private sealed class SortingRewriter : CSharpSyntaxRewriter
    {
        private readonly HashSet<TypeDeclarationSyntax> targets;
        private readonly MemberOrderOptions options;

        public SortingRewriter(HashSet<TypeDeclarationSyntax> targets, MemberOrderOptions options)
        {
            this.targets = targets;
            this.options = options;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitClassDeclaration(node));

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitStructDeclaration(node));

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitInterfaceDeclaration(node));

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node) =>
            SortIfTargeted(node, base.VisitRecordDeclaration(node));

        private SyntaxNode? SortIfTargeted(TypeDeclarationSyntax original, SyntaxNode? visited)
        {
            // 'original' is the node from the unmodified tree, so reference equality with the targets holds.
            return targets.Contains(original) && visited is TypeDeclarationSyntax type
                ? MemberOrdering.Sort(type, options)
                : visited;
        }
    }
}
