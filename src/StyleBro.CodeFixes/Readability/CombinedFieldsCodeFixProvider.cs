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
using StyleBro.Analyzers.Layout;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1114 and BRO1142: one declaration per field (each with the attributes, modifiers, type and documentation)
/// or local.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CombinedFieldsCodeFixProvider))]
public sealed class CombinedFieldsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.CombinedFields, DiagnosticIds.CombinedLocals);

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
                    "Declare each variable separately",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(CombinedFieldsCodeFixProvider)),
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
        var allowAdjacent = ElementSeparation.AllowsAdjacentSingleLineMembers(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        foreach (var diagnostic in diagnostics)
        {
            var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.AncestorsAndSelf()
                .FirstOrDefault(n => n is BaseFieldDeclarationSyntax or LocalDeclarationStatementSyntax);
            if ((declaration is BaseFieldDeclarationSyntax field ? CombinedFields.GetChange(field, text, allowAdjacent)
                : declaration is LocalDeclarationStatementSyntax local ? CombinedFields.GetChange(local, text) : null) is { } change)
            {
                changes.Add(change);
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
