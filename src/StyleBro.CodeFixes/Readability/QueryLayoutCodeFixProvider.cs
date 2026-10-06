using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>Fix for BRO1127-BRO1130: removes blank lines between clauses or puts a clause on its own line.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QueryLayoutCodeFixProvider))]
public sealed class QueryLayoutCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        DiagnosticIds.QueryClauseBlankLine, DiagnosticIds.QueryClausesOnSeparateLines, DiagnosticIds.QueryClauseAfterMultiLineClause, DiagnosticIds.MultiLineQueryClause);

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
                    diagnostic.Id == DiagnosticIds.QueryClauseBlankLine ? "Remove the blank lines" : "Put the clause on its own line",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(QueryLayoutCodeFixProvider) + diagnostic.Id),
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
        var changes = new List<TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (token.Parent?.AncestorsAndSelf().OfType<QueryExpressionSyntax>().FirstOrDefault() is not { } query)
            {
                continue;
            }

            // The innermost query that reports this rule on this keyword (a clause may contain another query).
            for (var current = query; current is not null; current = current.Ancestors().OfType<QueryExpressionSyntax>().FirstOrDefault())
            {
                var findings = QueryLayout.GetFindings(current, text, options).Where(f => f.Id == diagnostic.Id && f.Token == token).ToList();
                if (findings.Count > 0)
                {
                    changes.AddRange(findings.Select(f => f.Change));
                    break;
                }
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
