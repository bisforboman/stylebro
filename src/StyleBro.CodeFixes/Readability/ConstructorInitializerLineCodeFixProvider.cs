using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1105: moves the initializer to its own line, indented with the .editorconfig indentation settings.
/// Each constructor has at most one initializer, so Fix All is one set of non-overlapping edits.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorInitializerLineCodeFixProvider))]
public sealed class ConstructorInitializerLineCodeFixProvider : CodeFixProvider
{
    private const string Title = "Put the constructor initializer on its own line";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.ConstructorInitializerLine);

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
                    equivalenceKey: nameof(ConstructorInitializerLineCodeFixProvider)),
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
        var indentUnit = Indentation.GetUnit(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var changes = new Dictionary<ConstructorInitializerSyntax, TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            var initializer = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent as ConstructorInitializerSyntax;
            if (initializer is not null && ConstructorInitializers.ShouldMove(initializer, text))
            {
                changes[initializer] = ConstructorInitializers.GetChange(initializer, text, indentUnit);
            }
        }

        return document.WithText(text.WithChanges(changes.Values));
    }
}
