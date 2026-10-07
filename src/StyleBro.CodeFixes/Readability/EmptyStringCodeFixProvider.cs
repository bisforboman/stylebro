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

/// <summary>
/// Fix for BRO1106: replaces "" with 'string.Empty', or 'string.Empty' with "" in the literal style. The replaced
/// expressions never overlap, so Fix All is one set of edits.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyStringCodeFixProvider))]
public sealed class EmptyStringCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use the configured empty string";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.EmptyString);

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
                    equivalenceKey: nameof(EmptyStringCodeFixProvider)),
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
        var literalStyle = EmptyStrings.PrefersLiteral(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var model = literalStyle ? await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) : null;
        var changes = new List<TextChange>();
        foreach (var span in diagnostics.Select(d => d.Location.SourceSpan).Distinct())
        {
            var node = root.FindNode(span, getInnermostNodeForTie: true);
            if (!literalStyle && node is LiteralExpressionSyntax literal && EmptyStrings.ShouldReplace(literal))
            {
                changes.Add(new TextChange(literal.Span, "string.Empty"));
            }
            else if (model is not null
                && node is MemberAccessExpressionSyntax access
                && EmptyStrings.ShouldReplaceWithLiteral(access, model, cancellationToken))
            {
                changes.Add(new TextChange(access.Span, "\"\""));
            }
        }

        return document.WithText(text.WithChanges(changes));
    }
}
