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
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Layout;

/// <summary>Fix for BRO1511-BRO1513: removes or adds the blank lines around documentation and before 'while'.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentationBlankLinesCodeFixProvider))]
public sealed class DocumentationBlankLinesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.BlankLineAfterDocumentation, DiagnosticIds.BlankLineBeforeWhile, DiagnosticIds.BlankLineBeforeDocumentation);

    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Fix the blank lines",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(DocumentationBlankLinesCodeFixProvider) + diagnostic.Id),
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
        var changes = new List<TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            var start = diagnostic.Location.SourceSpan.Start;
            var token = root.FindToken(start);
            if (diagnostic.Id == DiagnosticIds.BlankLineBeforeWhile)
            {
                if (token.Parent is DoStatementSyntax @do && DocumentationBlankLines.GetBlankLinesBeforeWhile(@do, text) is { } blankWhile)
                {
                    changes.Add(new TextChange(blankWhile, string.Empty));
                }

                continue;
            }

            if (token.Parent?.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault() is not { } member)
            {
                continue;
            }

            if (diagnostic.Id == DiagnosticIds.BlankLineAfterDocumentation && DocumentationBlankLines.GetBlankLinesAfterDocumentation(member, text) is { } blank && blank.First.Start == start)
            {
                changes.AddRange(blank.Changes);
            }
            else if (diagnostic.Id == DiagnosticIds.BlankLineBeforeDocumentation && DocumentationBlankLines.GetMissingBlankLine(member, text) is { } position)
            {
                var line = text.Lines.GetLineFromPosition(position);
                var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
                changes.Add(new TextChange(new TextSpan(position, 0), lineBreak.Length > 0 ? lineBreak : "\n"));
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}