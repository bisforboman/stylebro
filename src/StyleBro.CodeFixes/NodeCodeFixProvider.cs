using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;

namespace StyleBro.CodeFixes;

/// <summary>
/// A fix that finds the reported node again (the nearest <typeparamref name="TNode"/> around the diagnostic's span) and
/// asks the rule's shared logic for its edits, so analyzer and fix can't disagree. A node that no longer needs the fix
/// (another rule changed it) gets none.
/// </summary>
public abstract class NodeCodeFixProvider<TNode> : CodeFixProvider
    where TNode : SyntaxNode
{
    private readonly string id;
    private readonly string title;

    private protected NodeCodeFixProvider(string id, string title)
    {
        this.id = id;
        this.title = title;
    }

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(this.id);

    /// <summary>Gets a value indicating whether <see cref="GetChanges"/> needs the semantic model.</summary>
    private protected virtual bool NeedsSemanticModel => false;

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(this.FixDocumentAsync);

    /// <inheritdoc/>
    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    this.title,
                    ct => this.FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: this.GetType().Name),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The rule's edits for the node, or nothing when it no longer needs the fix. <paramref name="isOn"/> says whether
    /// another rule is on for the file.
    /// </summary>
    private protected abstract IEnumerable<TextChange>? GetChanges(TNode node, SourceText text, SemanticModel? model, AnalyzerConfigOptions options, Func<string, bool> isOn, CancellationToken cancellationToken);

    private async Task<Document> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var model = this.NeedsSemanticModel ? await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) : null;
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        bool IsOn(string id) => Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, id, cancellationToken);
        var changes = new List<TextChange>();
        foreach (var span in diagnostics.Select(d => d.Location.SourceSpan).Distinct())
        {
            if (span.End <= root.FullSpan.End
                && root.FindNode(span, getInnermostNodeForTie: true).AncestorsAndSelf().OfType<TNode>().FirstOrDefault() is { } node
                && this.GetChanges(node, text, model, options, IsOn, cancellationToken) is { } found)
            {
                changes.AddRange(found);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
