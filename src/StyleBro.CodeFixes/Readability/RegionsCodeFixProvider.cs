using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Layout;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Ordering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes.Readability;

/// <summary>
/// Fix for BRO1112 and BRO1113: removes the '#region' and '#endregion' lines. All regions of a document are removed in
/// one text edit set, so neighboring regions share their blank-line cleanup.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RegionsCodeFixProvider))]
public sealed class RegionsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.NoRegions, DiagnosticIds.NoRegionsInCodeElements);

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
                    "Remove the region",
                    ct => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), ct),
                    equivalenceKey: nameof(RegionsCodeFixProvider) + diagnostic.Id),
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
        var starts = new HashSet<int>(diagnostics.Select(d => d.Location.SourceSpan.Start));
        var directives = new List<DirectiveTriviaSyntax>();
        foreach (var (region, endRegion, _) in Regions.GetRegions(root))
        {
            if (starts.Contains(region.SpanStart))
            {
                directives.Add(region);
                directives.Add(endRegion);
            }
        }

        var changes = Regions.GetChanges(directives, text).ToList();
        var removed = document.WithText(text.WithChanges(changes));
        bool IsOn(string id) => Severities.IsOn(document.Project.CompilationOptions, root.SyntaxTree, id, cancellationToken);
        var regionsRemoved = removed;
        var regionChanges = changes.ToList();

        // A '//' comment right above removed lines can end up with a blank line below it, which BRO1506 removes: now,
        // rather than in another run (Newtonsoft.Json's samples: '// output' + '#endregion' + a blank line).
        if (IsOn(DiagnosticIds.BlankLineAfterComment))
        {
            var removedText = await removed.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var removedRoot = await removed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var blanks = new List<TextChange>();
            foreach (var change in changes)
            {
                // Where the removed lines were, in the text without them (the changes are deletions).
                var position = change.Span.Start - changes.Where(c => c.Span.Start < change.Span.Start).Sum(c => c.Span.Length);
                if (position <= 0 || removedRoot is null)
                {
                    continue;
                }

                var line = removedText.Lines.GetLineFromPosition(position - 1);
                var comment = removedRoot.FindTrivia(line.Start + line.ToString().Length - line.ToString().TrimStart().Length);
                foreach (var blank in TrailingBlankLines.GetBlankLinesAfterComment(comment, removedText))
                {
                    blanks.Add(new TextChange(TextSpan.FromBounds(ToOriginal(blank.Start), ToOriginal(blank.EndIncludingLineBreak)), string.Empty));
                }
            }

            if (blanks.Count > 0)
            {
                changes.AddRange(blanks);
                removed = document.WithText(text.WithChanges(changes));
            }
        }

        // A '//' comment right below removed lines can end up below code, where BRO1504 wants a blank line above it:
        // now, rather than in another run (Newtonsoft.Json's Friend.cs: 'using ...;' + '#region License' + the header).
        // Judged on the text without the region lines only: the blank lines BRO1506 removes are below comments.
        if (IsOn(DiagnosticIds.BlankLineBeforeComment))
        {
            var removedText = await regionsRemoved.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var removedRoot = await regionsRemoved.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var insertions = new List<TextChange>();
            foreach (var change in regionChanges)
            {
                var position = change.Span.Start - regionChanges.Where(c => c.Span.Start < change.Span.Start).Sum(c => c.Span.Length);
                if (position <= 0 || position >= removedText.Length || removedRoot is null)
                {
                    continue;
                }

                var line = removedText.Lines.GetLineFromPosition(position);
                var comment = removedRoot.FindTrivia(line.Start + line.ToString().Length - line.ToString().TrimStart().Length);
                if (BlankLines.NeedsBlankLineAbove(comment, removedText, IsOn))
                {
                    var above = removedText.Lines[line.LineNumber - 1];
                    var lineBreak = removedText.ToString(TextSpan.FromBounds(above.End, above.EndIncludingLineBreak));
                    insertions.Add(new TextChange(new TextSpan(change.Span.End, 0), lineBreak.Length > 0 ? lineBreak : "\n"));
                }
            }

            if (insertions.Count > 0)
            {
                changes.AddRange(insertions);
                removed = document.WithText(text.WithChanges(changes));
            }
        }

        if (!IsOn(DiagnosticIds.MemberOrdering))
        {
            return removed;
        }

        // BRO1001 sorts within each region; without them it sorts across, now rather than in another run. Each sorted
        // container replaces its span in the original text, so the edits still merge with other copies' (multi-targeting).
        var sorts = await MemberOrderingCodeFixProvider.GetSortChangesAsync(removed, cancellationToken).ConfigureAwait(false);
        var edits = new List<TextChange>();
        foreach (var sort in sorts)
        {
            // Removed lines hold no tokens, so they're inside a container's span or outside it, never across its edge.
            edits.Add(new TextChange(TextSpan.FromBounds(ToOriginal(sort.Span.Start), ToOriginal(sort.Span.End)), sort.NewText!));
        }

        // Against the sorts only (ToList): a lazy Where saw the changes it had already added, and the BRO1504 insertion
        // at the end of a removed run counted as inside that run.
        edits.AddRange(changes.Where(c => !edits.Any(e => e.Span.Contains(c.Span))).ToList());
        return document.WithText(text.WithChanges(edits));

        // A position in the text without the region lines (only deletions), in the original text.
        int ToOriginal(int position)
        {
            var shift = 0;
            foreach (var change in changes.OrderBy(c => c.Span.Start))
            {
                if (change.Span.Start - shift > position)
                {
                    break;
                }

                shift += change.Span.Length - change.NewText!.Length;
            }

            return position + shift;
        }
    }
}
