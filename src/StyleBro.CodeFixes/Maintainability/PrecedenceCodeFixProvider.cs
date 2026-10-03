using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Maintainability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StyleBro.CodeFixes.Maintainability;

/// <summary>Fix for BRO1406/BRO1407: puts the operand in parentheses.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrecedenceCodeFixProvider))]
public sealed class PrecedenceCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.ArithmeticPrecedence, DiagnosticIds.ConditionalPrecedence);

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
                    "Add parentheses",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(PrecedenceCodeFixProvider)),
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
        var operands = new List<SyntaxNode>();
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            var operand = root.FindNode(span, getInnermostNodeForTie: true).AncestorsAndSelf().FirstOrDefault(n => n.Span == span && n.Parent is { } parent
                && Precedence.GetFindings(parent).Any(f => f.Operand == n));
            if (operand is not null)
            {
                operands.Add(operand);
            }
        }

        return document.WithText(text.WithChanges(Precedence.GetChanges(operands)));
    }
}
