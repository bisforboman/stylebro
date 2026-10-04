using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.CodeFixes.Layout;

/// <summary>Fix for BRO1514-BRO1516: puts the statement in braces.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BracesCodeFixProvider))]
public sealed class BracesCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.BracesOmitted, DiagnosticIds.BracesMultiLine, DiagnosticIds.BracesConsistent);

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
                    "Add braces",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(BracesCodeFixProvider)),
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
        var statements = new List<StatementSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            if (root.FindNode(span).AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault(s => s.Span == span) is { } statement)
            {
                statements.Add(statement);
            }
        }

        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        bool IsOn(string id) => Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, id, cancellationToken);

        // An if/else chain gets all its braces at once: braces on one clause turn the others into BRO1516's findings,
        // which 'dotnet format' may have fixed already in this run (it fixes one id at a time, in no fixed order).
        foreach (var chain in statements.Select(Braces.GetChain).OfType<IfStatementSyntax>().Distinct().ToList())
        {
            statements.AddRange(Braces.GetFindings(chain, text, IsOn, Braces.AllowConsecutiveUsings(options), Braces.GetPreference(options))
                .Select(f => f.Child)
                .Where(c => Braces.GetChanges(new[] { c }, text, options) is not null));
        }

        return Braces.GetChanges(statements, text, options, IsOn) is { } changes
            ? document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)))
            : document;
    }
}
