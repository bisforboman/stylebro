using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.CodeFixes;

/// <summary>
/// A document-based Fix All provider that handles linked files. In a multi-targeted project every target framework
/// has its own copy of each file, and '#if' regions can make the copies want different edits. Fixing each copy
/// separately leaves the merge to the host, and 'dotnet format' then writes conflict markers into the source
/// (seen on Newtonsoft.Json). Here the edits of all copies of a file are merged first (identical edits once,
/// conflicting ones dropped for the next run), and every copy gets the same merged text. Fixes that rewrite the
/// syntax tree count as one whole-text edit: identical results merge, otherwise the first copy's result wins.
/// </summary>
internal sealed class LinkedFileFixAllProvider : FixAllProvider
{
    private readonly Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document>> _fixDocument;

    private LinkedFileFixAllProvider(Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document>> fixDocument)
    {
        _fixDocument = fixDocument;
    }

    public static FixAllProvider Create(Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document>> fixDocument)
    {
        return new LinkedFileFixAllProvider(fixDocument);
    }

    public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
    {
        return new[] { FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution };
    }

    public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        return Task.FromResult<CodeAction?>(CodeAction.Create(
            fixAllContext.CodeActionEquivalenceKey ?? "Fix all",
            ct => FixAllAsync(fixAllContext, ct),
            fixAllContext.CodeActionEquivalenceKey));
    }

    private async Task<Solution> FixAllAsync(FixAllContext context, CancellationToken cancellationToken)
    {
        IEnumerable<Document> documents = context.Scope switch
        {
            FixAllScope.Project => context.Project.Documents,
            FixAllScope.Solution => context.Solution.Projects.SelectMany(p => p.Documents),
            _ => context.Document is null ? Array.Empty<Document>() : new[] { context.Document },
        };

        // One group per physical file: a document and its linked copies in other projects.
        var groups = new Dictionary<DocumentId, FileGroup>();
        var groupOf = new Dictionary<DocumentId, FileGroup>();
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var diagnostics = await context.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            if (diagnostics.IsEmpty)
            {
                continue;
            }

            if (!groupOf.TryGetValue(document.Id, out var group))
            {
                var ids = document.GetLinkedDocumentIds().Add(document.Id);
                group = new FileGroup(ids, await document.GetTextAsync(cancellationToken).ConfigureAwait(false));
                groups[document.Id] = group;
                foreach (var id in ids)
                {
                    groupOf[id] = group;
                }
            }

            // Exact changes for fixes that edit text (SourceText.WithChanges); a single whole-text change for fixes
            // that rewrite the syntax tree. Never a computed diff: two copies' diffs of the same result can be chunked
            // differently, and merging those duplicated members in a multi-targeted repo.
            var fixedDocument = await _fixDocument(document, diagnostics, cancellationToken).ConfigureAwait(false);
            var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var fixedText = await fixedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            group.Changes.AddRange(fixedText.GetTextChanges(originalText));
        }

        var solution = context.Solution;
        foreach (var group in groups.Values)
        {
            var merged = group.Original.WithChanges(Merge(group.Changes));
            foreach (var id in group.Ids)
            {
                solution = solution.WithDocumentText(id, merged);
            }
        }

        return solution;
    }

    /// <summary>Identical changes are kept once; a change that conflicts with an accepted one is dropped.</summary>
    internal static List<TextChange> Merge(List<TextChange> changes)
    {
        var accepted = new List<TextChange>();
        foreach (var change in changes.OrderBy(c => c.Span.Start).ThenBy(c => c.Span.End))
        {
            if (!accepted.Any(a => a.Span == change.Span && a.NewText == change.NewText)
                && !accepted.Any(a => Conflicts(a.Span, change.Span)))
            {
                accepted.Add(change);
            }
        }

        return accepted;
    }

    private static bool Conflicts(TextSpan a, TextSpan b)
    {
        if (a.IsEmpty || b.IsEmpty)
        {
            // An insertion conflicts with another insertion at the same position (order would be arbitrary) and with
            // a replacement that covers its position.
            var (insertion, other) = a.IsEmpty ? (a, b) : (b, a);
            return other.IsEmpty
                ? insertion.Start == other.Start
                : insertion.Start > other.Start && insertion.Start < other.End;
        }

        return a.OverlapsWith(b);
    }

    private sealed class FileGroup
    {
        public FileGroup(ImmutableArray<DocumentId> ids, SourceText original)
        {
            Ids = ids;
            Original = original;
        }

        public ImmutableArray<DocumentId> Ids { get; }

        public SourceText Original { get; }

        public List<TextChange> Changes { get; } = new();
    }
}
