using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Maintainability;

namespace StyleBro.CodeFixes.Maintainability;

/// <summary>Fix for BRO1405 and BRO1410: removes the parentheses.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParenthesesCodeFixProvider))]
public sealed class ParenthesesCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.UnnecessaryParentheses, DiagnosticIds.UnnecessaryPatternParentheses);

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
                    "Remove the parentheses",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(ParenthesesCodeFixProvider)),
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
        var tree = root.SyntaxTree;
        bool KeepPrecedence() =>
            Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.ConditionalPrecedence, cancellationToken)
            && Precedence.IsWanted(DiagnosticIds.ConditionalPrecedence, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree));
        var nodes = new List<SyntaxNode>();
        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if ((node is ParenthesizedExpressionSyntax expression && Parentheses.IsUnnecessary(expression, text))
                || (node is ParenthesizedPatternSyntax pattern && Parentheses.IsUnnecessary(pattern, text, KeepPrecedence)))
            {
                nodes.Add(node);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(Parentheses.GetChanges(nodes, text))));
    }
}
