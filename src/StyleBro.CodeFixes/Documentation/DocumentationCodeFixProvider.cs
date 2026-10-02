using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Documentation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Documentation;

/// <summary>Fix for BRO1601 (adds '/// &lt;inheritdoc/&gt;'), BRO1602 ('///' -> '//') and BRO1603 (adds the period). Text edits, so Fix All agrees.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentationCodeFixProvider))]
public sealed class DocumentationCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(
            DiagnosticIds.InheritDocumentation,
            DiagnosticIds.DocumentationSlashesInComment,
            DiagnosticIds.DocumentationEndsWithPeriod,
            DiagnosticIds.PropertySummaryWording,
            DiagnosticIds.PropertySummaryRestrictedSetter,
            DiagnosticIds.ConstructorSummary,
            DiagnosticIds.DestructorSummary,
            DiagnosticIds.VoidReturnDocumented,
            DiagnosticIds.PlaceholderElement,
            DiagnosticIds.EmptyRemarks,
            DiagnosticIds.ParameterTagsMatch,
            DiagnosticIds.ParameterTagHasName,
            DiagnosticIds.TypeParameterTagsMatch,
            DiagnosticIds.TypeParameterTagHasName);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() =>
        LinkedFileFixAllProvider.Create(FixDocumentAsync);

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var title = diagnostic.Id switch
            {
                DiagnosticIds.InheritDocumentation => "Add <inheritdoc/>",
                DiagnosticIds.DocumentationSlashesInComment => "Use '//' for the comment",
                DiagnosticIds.PropertySummaryWording or DiagnosticIds.PropertySummaryRestrictedSetter => "Begin the summary with the accessors' words",
                DiagnosticIds.ConstructorSummary or DiagnosticIds.DestructorSummary => "Begin the summary with the standard text",
                DiagnosticIds.VoidReturnDocumented => "Remove the <returns> documentation",
                DiagnosticIds.PlaceholderElement => "Remove the <placeholder> tags",
                DiagnosticIds.EmptyRemarks => "Remove the empty <remarks>",
                DiagnosticIds.ParameterTagsMatch => "Make the <param> tags match the parameters",
                DiagnosticIds.ParameterTagHasName => "Name the <param> tag",
                DiagnosticIds.TypeParameterTagsMatch => "Make the <typeparam> tags match the type parameters",
                DiagnosticIds.TypeParameterTagHasName => "Name the <typeparam> tag",
                _ => "Add a period",
            };
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(DocumentationCodeFixProvider) + diagnostic.Id),
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
        var misplaced = DocumentationComments.GetMisplacedDocumentationComments(root).ToList();
        var missingPeriods = new HashSet<int>(DocumentationPeriods.GetMissingPeriods(root));
        var fixedMembers = new HashSet<(SyntaxNode, ParameterDocumentation.TagKind)>();
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            if (diagnostic.Id == DiagnosticIds.InheritDocumentation)
            {
                var member = root.FindToken(span.Start).Parent?.AncestorsAndSelf()
                    .FirstOrDefault(n => n is MemberDeclarationSyntax);
                if (member is not null && !DocumentationComments.HasDocumentation(member))
                {
                    changes.Add(DocumentationComments.GetInheritDocChange(member, text));
                }
            }
            else if (diagnostic.Id is DiagnosticIds.PropertySummaryWording or DiagnosticIds.PropertySummaryRestrictedSetter)
            {
                if (root.FindToken(span.Start).Parent is PropertyDeclarationSyntax property
                    && PropertySummaries.GetFinding(property, text) is { } finding)
                {
                    changes.Add(new TextChange(finding.Replace, finding.NewText));
                }
            }
            else if (diagnostic.Id is DiagnosticIds.ConstructorSummary or DiagnosticIds.DestructorSummary)
            {
                if (root.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault() is { } member
                    && ConstructorSummaries.GetFinding(member, text) is { } finding)
                {
                    changes.Add(new TextChange(finding.Replace, finding.NewText));
                }
            }
            else if (diagnostic.Id == DiagnosticIds.VoidReturnDocumented)
            {
                var owner = root.FindToken(span.Start, findInsideTrivia: true).Parent?.AncestorsAndSelf()
                    .FirstOrDefault(n => n is MethodDeclarationSyntax or DelegateDeclarationSyntax)
                    ?? root.FindToken(span.Start).Parent?.AncestorsAndSelf().FirstOrDefault(n => n is MethodDeclarationSyntax or DelegateDeclarationSyntax);
                if (owner is not null
                    && DocumentationTags.GetVoidReturns(owner).FirstOrDefault(r => DocumentationTags.GetStart(r) == span.Start) is { } returns)
                {
                    changes.Add(DocumentationTags.GetRemoval(returns, text));
                }
            }
            else if (diagnostic.Id == DiagnosticIds.EmptyRemarks)
            {
                if (ParameterDocumentation.GetEmptyRemarks(root).FirstOrDefault(r => DocumentationTags.GetStart(r) == span.Start) is { } remarks)
                {
                    changes.Add(DocumentationTags.GetRemoval(remarks, text));
                }
            }
            else if (diagnostic.Id is DiagnosticIds.ParameterTagsMatch or DiagnosticIds.ParameterTagHasName
                or DiagnosticIds.TypeParameterTagsMatch or DiagnosticIds.TypeParameterTagHasName)
            {
                // One member can have several diagnostics (of up to two rules per tag kind); its edits are computed
                // together and applied once, so every rule's fix gives the same result.
                var kind = diagnostic.Id is DiagnosticIds.ParameterTagsMatch or DiagnosticIds.ParameterTagHasName
                    ? ParameterDocumentation.TagKind.Parameter
                    : ParameterDocumentation.TagKind.TypeParameter;
                var kinds = kind == ParameterDocumentation.TagKind.Parameter ? ParameterDocumentation.MemberKinds : ParameterDocumentation.TypeParameterMemberKinds;
                if (root.FindToken(span.Start, findInsideTrivia: true).Parent?.AncestorsAndSelf()
                        .FirstOrDefault(n => kinds.Any(k => n.IsKind(k))) is { } member
                    && fixedMembers.Add((member, kind))
                    && ParameterDocumentation.GetFinding(member, text, kind) is { } finding)
                {
                    changes.AddRange(finding.Changes);
                }
            }
            else if (diagnostic.Id == DiagnosticIds.PlaceholderElement)
            {
                if (DocumentationTags.GetPlaceholders(root).FirstOrDefault(p => p.StartTag.Span == span) is { } placeholder)
                {
                    changes.AddRange(DocumentationTags.GetUnwrap(placeholder));
                }
            }
            else if (diagnostic.Id == DiagnosticIds.DocumentationEndsWithPeriod)
            {
                if (missingPeriods.Contains(span.Start))
                {
                    changes.Add(new TextChange(new TextSpan(span.Start, 0), "."));
                }
            }
            else if (misplaced.FirstOrDefault(t => DocumentationComments.GetReportSpan(t, text) == span) is { } comment && comment != default)
            {
                changes.AddRange(DocumentationComments.GetSlashChanges(comment, text));
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }
}
