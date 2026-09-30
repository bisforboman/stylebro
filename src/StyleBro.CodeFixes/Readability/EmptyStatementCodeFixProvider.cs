using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1101: removes the reported semicolons. Fix All removes every one in a document in a single set of
/// text changes, computed per line by <see cref="EmptyStatements.GetChanges"/>, so it matches the single fix.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyStatementCodeFixProvider))]
public sealed class EmptyStatementCodeFixProvider : CodeFixProvider
{
    private const string Title = "Remove empty statement";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.EmptyStatement);

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
                    equivalenceKey: nameof(EmptyStatementCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixDocumentAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = EmptyStatements.GetChanges(text, diagnostics.Select(d => d.Location.SourceSpan));
        return document.WithText(text.WithChanges(changes));
    }
}
