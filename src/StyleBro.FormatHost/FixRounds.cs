using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Host;

/// <summary>
/// The rounds a fix run applies the ids in (phase 3): ids whose fixes don't meet share a round, one analysis for all of them,
/// and their Fix Alls are applied to the same solution, the edits merged per document (<see cref="MergeAsync"/>). The order
/// stays the host's (<see cref="FixOrder"/>) wherever two ids interact: an id's round comes after the round of every id before
/// it that it interacts with, so it sees their fixes as it did one id at a time. Renames get a round each, last.
/// </summary>
public static class FixRounds
{
    /// <summary>The rounds for <paramref name="ids"/> (any order, repeats ignored), each in ordinal order.</summary>
    public static List<List<string>> Of(IEnumerable<string> ids, FixInteractions interactions)
    {
        var ordered = FixOrder.Of(ids);
        var rounds = new List<List<string>>();
        var placed = new List<(string Id, int Round)>();
        foreach (var id in ordered.Where(i => !interactions.Renames(i)))
        {
            var round = placed.Where(p => interactions.Interact(p.Id, id)).Select(p => p.Round + 1).DefaultIfEmpty(0).Max();
            if (round == rounds.Count)
            {
                rounds.Add([]);
            }

            rounds[round].Add(id);
            placed.Add((id, round));
        }

        rounds.AddRange(ordered.Where(interactions.Renames).Select(id => new List<string> { id }));
        return rounds;
    }

    /// <summary>
    /// <paramref name="start"/> with the fixes of a round merged: each of <paramref name="fixes"/> is one id's Fix All applied
    /// to <paramref name="start"/>, taken in their order. A fix whose edits touch an edit taken before it (overlap or meet at a
    /// position), or that changes anything but the text of documents (adds or removes one, an additional file, an encoding),
    /// is left out and its id returned in <paramref name="deferred"/> order: it runs in a round of its own after this one.
    /// </summary>
    public static async Task<(Solution Merged, List<string> Deferred)> MergeAsync(Solution start, IReadOnlyList<(string Id, Solution Fixed)> fixes, CancellationToken cancellation)
    {
        var taken = new Dictionary<DocumentId, List<TextChange>>();
        var deferred = new List<string>();
        foreach (var (id, fixedSolution) in fixes)
        {
            var edits = await EditsAsync(start, fixedSolution, cancellation).ConfigureAwait(false);
            if (edits is null || edits.Any(e => taken.TryGetValue(e.Key, out var before) && Touch(before, e.Value)))
            {
                deferred.Add(id);
                continue;
            }

            foreach (var (document, changes) in edits)
            {
                if (!taken.TryGetValue(document, out var list))
                {
                    taken[document] = list = [];
                }

                list.AddRange(changes);
            }
        }

        var merged = start;
        foreach (var (document, changes) in taken)
        {
            var text = await start.GetDocument(document)!.GetTextAsync(cancellation).ConfigureAwait(false);
            merged = merged.WithDocumentText(document, text.WithChanges(changes.OrderBy(c => c.Span.Start)), PreservationMode.PreserveValue);
        }

        return (merged, deferred);
    }

    // The text edits that make 'after' of 'start', per document, or null when 'after' changes more than documents' text (or
    // its edits don't give its text back).
    private static async Task<Dictionary<DocumentId, List<TextChange>>?> EditsAsync(Solution start, Solution after, CancellationToken cancellation)
    {
        var changes = after.GetChanges(start);
        if (changes.GetAddedProjects().Any() || changes.GetRemovedProjects().Any())
        {
            return null;
        }

        var edits = new Dictionary<DocumentId, List<TextChange>>();
        foreach (var project in changes.GetProjectChanges())
        {
            if (project.GetAddedDocuments().Any() || project.GetRemovedDocuments().Any()
                || project.GetAddedAdditionalDocuments().Any() || project.GetRemovedAdditionalDocuments().Any() || project.GetChangedAdditionalDocuments().Any()
                || project.GetAddedAnalyzerConfigDocuments().Any() || project.GetRemovedAnalyzerConfigDocuments().Any() || project.GetChangedAnalyzerConfigDocuments().Any()
                || project.GetAddedProjectReferences().Any() || project.GetRemovedProjectReferences().Any()
                || project.GetAddedMetadataReferences().Any() || project.GetRemovedMetadataReferences().Any())
            {
                return null;
            }

            foreach (var id in project.GetChangedDocuments())
            {
                var oldDocument = start.GetDocument(id)!;
                var newDocument = after.GetDocument(id)!;
                var oldText = await oldDocument.GetTextAsync(cancellation).ConfigureAwait(false);
                var newText = await newDocument.GetTextAsync(cancellation).ConfigureAwait(false);
                if (!Equals(oldText.Encoding, newText.Encoding) || oldDocument.FilePath != newDocument.FilePath || oldDocument.Name != newDocument.Name)
                {
                    return null;
                }

                if (oldText.ContentEquals(newText))
                {
                    continue;
                }

                var documentEdits = (await newDocument.GetTextChangesAsync(oldDocument, cancellation).ConfigureAwait(false)).ToList();
                if (!oldText.WithChanges(documentEdits).ContentEquals(newText))
                {
                    return null;
                }

                edits[id] = documentEdits;
            }
        }

        return edits;
    }

    // Whether any of two documents' edit lists overlap or meet (an insertion at the end or start of the other's span too).
    private static bool Touch(List<TextChange> taken, List<TextChange> next) =>
        next.Any(n => taken.Any(t => n.Span.Start <= t.Span.End && t.Span.Start <= n.Span.End));
}
