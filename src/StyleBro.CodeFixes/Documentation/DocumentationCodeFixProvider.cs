using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Documentation;
using StyleBro.Analyzers.Layout;
using StyleBro.Analyzers.Readability;

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
        var misplaced = DocumentationComments.GetMisplacedDocumentationComments(root.DescendantTrivia()).ToList();
        var missingPeriods = new HashSet<int>(DocumentationPeriods.GetMissingPeriods(
            root.DescendantTrivia(), document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree)));
        var fixedMembers = new HashSet<(SyntaxNode, ParameterDocumentation.TagKind)>();
        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree);
        var blankLineBefore = Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.BlankLineBeforeDocumentation, cancellationToken);
        var separate = Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.ElementsSeparatedByBlankLine, cancellationToken);
        var noBlankLineBelowComment = Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.BlankLineAfterComment, cancellationToken);
        var documented = new HashSet<SyntaxNode>(diagnostics.Where(d => d.Id == DiagnosticIds.InheritDocumentation)
            .Select(d => root.FindToken(d.Location.SourceSpan.Start).Parent?.AncestorsAndSelf().FirstOrDefault(n => n is MemberDeclarationSyntax))
            .Where(m => m is not null)!);
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            if (diagnostic.Id == DiagnosticIds.InheritDocumentation)
            {
                var member = root.FindToken(span.Start).Parent?.AncestorsAndSelf()
                    .FirstOrDefault(n => n is MemberDeclarationSyntax);
                if (member is not null && !DocumentationComments.HasDocumentation(member))
                {
                    changes.Add(DocumentationComments.GetInheritDocChange(member, text, options, blankLineBefore, noBlankLineBelowComment));

                    // Documented, a property needs a blank line before the property below it too (BRO1505), unless that
                    // one gets documented in this batch: its own edit brings the blank line (BRO1513).
                    if (separate && member is PropertyDeclarationSyntax property
                        && ElementSeparation.GetBlankLineBelowOnceDocumented(
                            property,
                            text,
                            Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, DiagnosticIds.AutoAccessorsOnOneLine, cancellationToken) ? options : null,
                            ElementSeparation.AllowsAdjacentSingleLineMembers(options)) is { } blankLine
                        && !(blankLineBefore && property.Parent is TypeDeclarationSyntax type && documented.Contains(type.Members[type.Members.IndexOf(property) + 1])))
                    {
                        changes.Add(blankLine);
                    }
                }
            }
            else if (diagnostic.Id is DiagnosticIds.PropertySummaryWording or DiagnosticIds.PropertySummaryRestrictedSetter)
            {
                if (root.FindToken(span.Start).Parent is PropertyDeclarationSyntax property
                    && PropertySummaries.GetFinding(property, text, await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false), PropertySummaries.FollowsStyleSharp(options)) is { } finding)
                {
                    changes.Add(new TextChange(finding.Replace, finding.NewText));
                }
            }
            else if (diagnostic.Id is DiagnosticIds.ConstructorSummary or DiagnosticIds.DestructorSummary)
            {
                if (root.FindToken(span.Start).Parent?.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault() is { } member
                    && ConstructorSummaries.GetFinding(member, text, PropertySummaries.FollowsStyleSharp(options)) is { } finding)
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
                if (ParameterDocumentation.GetEmptyRemarks(root.DescendantTrivia()).FirstOrDefault(r => DocumentationTags.GetStart(r) == span.Start) is { } remarks)
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
                if (DocumentationTags.GetPlaceholders(root.DescendantTrivia()).FirstOrDefault(p => p.StartTag.Span == span) is { } placeholder)
                {
                    changes.AddRange(DocumentationTags.GetUnwrap(placeholder));
                }
            }
            else if (diagnostic.Id == DiagnosticIds.DocumentationEndsWithPeriod)
            {
                if (missingPeriods.Contains(span.Start))
                {
                    changes.Add(DocumentationPeriods.GetPeriodChange(span.Start, text));
                }
            }
            else if (misplaced.FirstOrDefault(t => DocumentationComments.GetReportSpan(t, text) == span) is { } comment && comment != default)
            {
                var slashes = DocumentationComments.GetSlashChanges(comment, text).ToList();
                var edits = WithoutEmptyEnds(document, root, text, comment, slashes, options, cancellationToken);
                changes.AddRange(edits);
                if (GetBlankLineAbove(document, root, text, comment, edits, options, cancellationToken) is { } blankLine)
                {
                    changes.Add(blankLine);
                }
            }
        }

        return document.WithText(text.WithChanges(LinkedFileFixAllProvider.Merge(changes)));
    }

    /// <summary>
    /// BRO1602's edits plus BRO1120's removal of the empty '//' lines the conversion leaves at the ends of their comment
    /// group, when BRO1120 is on (trial 073, Ardalis.Extensions: a '///' block ending in '/// ' needed a second run). Judged
    /// by BRO1120's own logic on the text with the slashes replaced; only removals of whole lines of this comment, mapped
    /// back by line (the slash edits keep every line).
    /// </summary>
    private static List<TextChange> WithoutEmptyEnds(Document document, SyntaxNode root, SourceText text, SyntaxTrivia comment, List<TextChange> slashes, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        var tree = root.SyntaxTree;
        if (slashes.Count == 0 || !Severities.IsOn(document.Project.CompilationOptions, tree, DiagnosticIds.EmptyComment, cancellationToken))
        {
            return slashes;
        }

        var first = text.Lines.GetLineFromPosition(comment.FullSpan.Start).LineNumber;
        var last = text.Lines.GetLineFromPosition(comment.Span.End).LineNumber;
        var newText = text.WithChanges(slashes);
        var newRoot = CSharpSyntaxTree.ParseText(newText, (CSharpParseOptions)tree.Options, cancellationToken: cancellationToken).GetRoot(cancellationToken);
        var removed = new List<(int First, int Last)>();
        foreach (var (_, empty) in CommentText.GetEmptyComments(CommentText.GetComments(newRoot), newText, CommentText.HasHeaderTemplate(options)))
        {
            foreach (var change in CommentText.GetRemovals(empty, newText))
            {
                // Whole lines only ('x; //' keeps its code: a column there isn't the original's).
                var from = newText.Lines.GetLineFromPosition(change.Span.Start);
                var to = newText.Lines.GetLineFromPosition(change.Span.End);
                if (change.Span.Start == from.Start && change.Span.End == to.Start && to.LineNumber > from.LineNumber
                    && newText.Lines.GetLineFromPosition(empty[0].SpanStart).LineNumber is var line && line >= first && line <= last)
                {
                    removed.Add((from.LineNumber, to.LineNumber - 1));
                }
            }
        }

        if (removed.Count == 0)
        {
            return slashes;
        }

        bool IsRemoved(int line) => removed.Any(r => line >= r.First && line <= r.Last);
        return slashes.Where(c => !IsRemoved(text.Lines.GetLineFromPosition(c.Span.Start).LineNumber))
            .Concat(removed.Select(r => new TextChange(TextSpan.FromBounds(text.Lines[r.First].Start, text.Lines[r.Last].EndIncludingLineBreak), string.Empty)))
            .OrderBy(c => c.Span.Start)
            .ToList();
    }

    /// <summary>
    /// BRO1504's blank line above the comment once it's a '//' comment, when that rule is on and wants it there
    /// (<c>}</c> + <c>/// note</c> + <see langword="else"/>, akka.net): otherwise 'dotnet format' needs a second run. Judged on the text
    /// with the slashes replaced, so it's exactly BRO1504's answer.
    /// </summary>
    private static TextChange? GetBlankLineAbove(Document document, SyntaxNode root, SourceText text, SyntaxTrivia comment, List<TextChange> slashes, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        var tree = root.SyntaxTree;
        bool IsOn(string id) => Severities.IsOn(document.Project.CompilationOptions, tree, id, cancellationToken);
        if (slashes.Count == 0 || !IsOn(DiagnosticIds.BlankLineBeforeComment))
        {
            return null;
        }

        var start = comment.FullSpan.Start;
        var newText = text.WithChanges(slashes);
        var newRoot = CSharpSyntaxTree.ParseText(newText, (CSharpParseOptions)tree.Options, cancellationToken: cancellationToken).GetRoot(cancellationToken);
        var converted = newRoot.FindTrivia(start);
        return BlankLines.NeedsBlankLineAbove(converted, newText, IsOn, BlankLines.GetExemptPrefixes(options), SwitchSectionBlankLines.ReadMode(options))
            ? new TextChange(new TextSpan(text.Lines.GetLineFromPosition(start).Start, 0), SingleLineBlocks.LineBreak(text, start))
            : null;
    }
}
