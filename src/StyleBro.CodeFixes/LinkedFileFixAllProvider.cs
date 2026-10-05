using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
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
    private readonly Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document>> fixDocument;

    private LinkedFileFixAllProvider(Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document>> fixDocument)
    {
        this.fixDocument = fixDocument;
    }

    public static FixAllProvider Create(Func<Document, ImmutableArray<Diagnostic>, CancellationToken, Task<Document>> fixDocument)
    {
        return new LinkedFileFixAllProvider(fixDocument);
    }

    /// <inheritdoc/>
    public override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
    {
        return new[] { FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution };
    }

    /// <inheritdoc/>
    public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        return Task.FromResult<CodeAction?>(CodeAction.Create(
            fixAllContext.CodeActionEquivalenceKey ?? "Fix all",
            ct => FixAllAsync(fixAllContext, ct),
            fixAllContext.CodeActionEquivalenceKey));
    }

    /// <summary>
    /// The lowest C# version among the document's copies: a file linked into several projects, or a project whose target
    /// frameworks get different default versions (net472: 7.3), is compiled with each, and a fix must compile in all of
    /// them (LibGit2Sharp: 'is not null' broke the net472 tests). Rules that need a newer version check it on the fix side.
    /// </summary>
    internal static LanguageVersion GetLowestLanguageVersion(Document document) =>
        document.GetLinkedDocumentIds().Add(document.Id)
            .Select(id => document.Project.Solution.GetDocument(id)?.Project.ParseOptions)
            .OfType<CSharpParseOptions>()
            .Select(o => o.LanguageVersion)
            .DefaultIfEmpty(LanguageVersion.Latest)
            .Min();

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
            else if (!(await document.GetTextAsync(cancellationToken).ConfigureAwait(false)).ContentEquals(group.Original))
            {
                // The copies of a multi-targeted file usually have the same text, but 'dotnet format' runs its whitespace
                // and code style fixes first and can leave them different. Edits made for this copy's text don't fit the
                // first copy's ("Changes must be within bounds" on Serilog), and every copy must end with the same text:
                // copies fixed differently, or one fixed and one not, are written as conflict markers by 'dotnet
                // format' (Newtonsoft.Json). So every copy gets the first copy's text with its fixes; what only this
                // copy's '#if' code needs (fixes, and earlier whitespace edits) comes back on the next run.
                continue;
            }

            // Exact changes for fixes that edit text (SourceText.WithChanges); a single whole-text change for fixes
            // that rewrite the syntax tree. Never a computed diff: two copies' diffs of the same result can be chunked
            // differently, and merging those duplicated members in a multi-targeted repo.
            var fixedDocument = await fixDocument(document, diagnostics, cancellationToken).ConfigureAwait(false);
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
