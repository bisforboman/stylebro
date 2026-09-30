using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1104: replaces 'new T()' with the replacement the analyzer computed. A parameterless creation can't
/// contain another one, so Fix All replaces all of them in a document at once.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DefaultValueConstructorCodeFixProvider))]
public sealed class DefaultValueConstructorCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use default value";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.DefaultValueConstructor);

    public override FixAllProvider GetFixAllProvider() =>
        FixAllProvider.Create(async (fixAllContext, document, diagnostics) =>
            diagnostics.IsEmpty
                ? null
                : await FixDocumentAsync(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false));

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(DefaultValueConstructorCodeFixProvider)),
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
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
        {
            return document;
        }

        var replacements = new Dictionary<SyntaxNode, string>();
        foreach (var diagnostic in diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is BaseObjectCreationExpressionSyntax creation
                && DefaultValueConstructors.GetReplacement(creation, model, cancellationToken) is { } replacement)
            {
                replacements[creation] = replacement;
            }
        }

        var newRoot = root.ReplaceNodes(
            replacements.Keys,
            (original, _) => SyntaxFactory.ParseExpression(replacements[original]).WithTriviaFrom(original));
        return document.WithSyntaxRoot(newRoot);
    }
}
