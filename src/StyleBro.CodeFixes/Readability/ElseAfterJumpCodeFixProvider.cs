using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1143. All the diagnostics' 'if' statements of a document are fixed together, so 'else' bodies nested in
/// each other move out in one pass.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ElseAfterJumpCodeFixProvider))]
public sealed class ElseAfterJumpCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.ElseAfterJump);

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
                    "Remove 'else'",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(ElseAfterJumpCodeFixProvider)),
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
        var compilationOptions = document.Project.CompilationOptions;
        var tree = root.SyntaxTree;
        bool IsOn(string id) => Severities.IsOn(compilationOptions, tree, id, cancellationToken);
        var candidates = diagnostics
            .Select(d => root.FindToken(d.Location.SourceSpan.Start).Parent?.Parent as IfStatementSyntax)
            .Where(node => node is not null && ElseAfterJump.IsCandidate(node, text, options, IsOn))
            .Distinct()
            .ToList();

        // Braces a brace rule wants on an 'if' branch come first, then the 'else' goes from the wrapped text.
        var branches = candidates.Select(c => ElseAfterJump.GetBranchToWrap(c!, text, options, IsOn)).OfType<StatementSyntax>().ToList();
        if (branches.Count > 0 && Braces.GetChanges(branches, text, options) is { } braces)
        {
            var sorted = braces.OrderBy(c => c.Span.Start).ToList();
            text = text.WithChanges(sorted);
            var wrapped = CSharpSyntaxTree.ParseText(text, (CSharpParseOptions)tree.Options, cancellationToken: cancellationToken).GetRoot(cancellationToken);
            candidates = candidates
                .Select(c => wrapped.FindToken(c!.SpanStart + sorted.Where(b => b.Span.End <= c.SpanStart).Sum(b => b.NewText!.Length - b.Span.Length)).Parent as IfStatementSyntax)
                .Where(node => node is not null && ElseAfterJump.IsCandidate(node, text, options, IsOn))
                .ToList();
        }

        var changes = ElseAfterJump.GetChanges(candidates!, text, Indentation.GetUnit(options), IsOn);
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
