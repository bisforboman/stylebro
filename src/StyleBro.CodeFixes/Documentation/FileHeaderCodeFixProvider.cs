using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Documentation;

namespace StyleBro.CodeFixes.Documentation;

/// <summary>Fix for BRO1615: writes the header, or rewrites only the copyright tag's lines.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FileHeaderCodeFixProvider))]
public sealed class FileHeaderCodeFixProvider : CodeFixProvider
{
    private const string Title = "Fix the file header";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticIds.FileHeader);

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
                    equivalenceKey: nameof(FileHeaderCodeFixProvider)),
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
        if (root is null || diagnostics.IsEmpty)
        {
            return document;
        }

        var options = FileHeaderOptions.Read(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return options is not null && FileHeaders.GetFinding(root, text, options) is { } finding
            ? document.WithText(text.WithChanges(finding.Change))
            : document;
    }
}
