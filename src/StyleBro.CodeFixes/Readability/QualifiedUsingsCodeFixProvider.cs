using System.Collections.Generic;
using System.Collections.Immutable;
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

/// <summary>Fix for BRO1126: writes the fully qualified name (and sorts the usings when 'dotnet format' would).</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QualifiedUsingsCodeFixProvider))]
public sealed class QualifiedUsingsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.QualifiedUsing);

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
                    "Qualify the using directive",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(QualifiedUsingsCodeFixProvider)),
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

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var changes = new List<TextChange>();
        foreach (var diagnostic in diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan) is UsingDirectiveSyntax { Name: { } name } directive
                && QualifiedUsings.GetQualifiedName(directive, model, cancellationToken) is { } qualified)
            {
                changes.Add(new TextChange(name.Span, qualified));
            }
        }

        var fixedDocument = document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));

        // 'dotnet format' sorts usings (its style pass, before the analyzers) whenever a sort key is set: the qualified
        // name usually sorts elsewhere ('DevTools.Common' -> 'Microsoft.Xbox.Services.DevTools.Common' in xbox's AuthClient),
        // and the next run moved it. The same organizer, here, so one run converges.
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        return changes.Count > 0 && (options.TryGetValue("dotnet_sort_system_directives_first", out _) || options.TryGetValue("dotnet_separate_import_directive_groups", out _))
            ? await Microsoft.CodeAnalysis.Formatting.Formatter.OrganizeImportsAsync(fixedDocument, cancellationToken).ConfigureAwait(false)
            : fixedDocument;
    }
}
