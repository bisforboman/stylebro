using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Readability;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1102: splits each reported attribute list. Attribute lists never overlap, so Fix All applies all
/// replacements in a document as one set of text changes.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CombinedAttributesCodeFixProvider))]
public sealed class CombinedAttributesCodeFixProvider : CodeFixProvider
{
    private const string Title = "Put each attribute in its own brackets";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.CombinedAttributes);

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
                    equivalenceKey: nameof(CombinedAttributesCodeFixProvider)),
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
        var lists = new HashSet<AttributeListSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var list = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AttributeListSyntax>();
            if (list is not null && CombinedAttributes.IsSplittable(list))
            {
                lists.Add(list);
            }
        }

        return document.WithText(text.WithChanges(lists.Select(l => CombinedAttributes.GetChange(l, text))));
    }
}
