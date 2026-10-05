using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        bool IsOn(string id) => Severities.IsOn(compilationOptions, root.SyntaxTree, id, cancellationToken);
        var candidates = diagnostics
            .Select(d => root.FindToken(d.Location.SourceSpan.Start).Parent?.Parent as IfStatementSyntax)
            .Where(node => node is not null && ElseAfterJump.IsCandidate(node, text, options, IsOn))
            .Distinct()
            .ToList();
        var changes = ElseAfterJump.GetChanges(candidates!, text, Indentation.GetUnit(options), IsOn);
        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
