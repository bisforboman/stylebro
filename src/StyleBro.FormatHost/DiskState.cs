using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Host;

/// <summary>
/// Keeps a loaded workspace's documents as the files on disk are. Between two runs of the host, bro and the fixes change files
/// on disk (CSharpier, kept indentation, a lone CR converted, the last plan entry's fixes), and the next run must see them, as a
/// new 'dotnet format' process reads them: <see cref="SyncAsync"/> reads back each file whose size or write time changed, for
/// every document of that path (all framework copies of a multi-targeted file get the same text, which is what each
/// framework's 'dotnet format' run read from disk). <see cref="Save"/> writes a fix run's changed documents the way
/// MSBuildWorkspace writes them for 'dotnet format'.
/// </summary>
public sealed class DiskState
{
    private readonly Dictionary<string, (long Length, DateTime Written)> stamps = new(StringComparer.Ordinal);

    /// <summary>
    /// Writes <paramref name="text"/> as MSBuildWorkspace saves a document: in the text's encoding, else the file's own, else
    /// UTF-8 without a byte order mark.
    /// </summary>
    public static void Write(string path, SourceText text)
    {
        var encoding = text.Encoding;
        if (encoding is null && File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            encoding = SourceText.From(stream).Encoding;
        }

        using var writer = new StreamWriter(path, append: false, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        text.Write(writer);
    }

    /// <summary>Remembers the files of <paramref name="solution"/>'s documents as they are now (right after loading it).</summary>
    public void Remember(Solution solution)
    {
        foreach (var path in Paths(solution))
        {
            this.Stamp(path);
        }
    }

    /// <summary>
    /// <paramref name="solution"/> with the text of each file that changed on disk since it was remembered, and how many files
    /// that was. Documents whose file didn't change keep their text (and their projects their compilations).
    /// </summary>
    public async Task<(Solution Solution, int Changed)> SyncAsync(Solution solution, CancellationToken cancellation)
    {
        var changed = 0;
        foreach (var group in solution.Projects.SelectMany(p => p.Documents).Where(d => d.FilePath is not null).GroupBy(d => d.FilePath!, StringComparer.Ordinal))
        {
            var path = group.Key;
            if (!File.Exists(path) || (this.stamps.TryGetValue(path, out var stamp) && stamp == Read(path)))
            {
                continue;
            }

            // Read like the workspace loaded it.
            var first = group.First();
            var loaded = solution.WithDocumentTextLoader(first.Id, new FileTextLoader(path, defaultEncoding: null), PreservationMode.PreserveValue);
            var text = await loaded.GetDocument(first.Id)!.GetTextAsync(cancellation).ConfigureAwait(false);
            var before = await first.GetTextAsync(cancellation).ConfigureAwait(false);
            this.Stamp(path);
            if (text.ContentEquals(before) && Equals(text.Encoding, before.Encoding))
            {
                continue;
            }

            changed++;
            foreach (var document in group)
            {
                solution = solution.WithDocumentText(document.Id, text, PreservationMode.PreserveValue);
            }
        }

        return (solution, changed);
    }

    /// <summary>
    /// Writes the documents <paramref name="after"/> changed from <paramref name="before"/> (each file once) and returns
    /// <paramref name="current"/> with the new text in every document of those files, and the files written.
    /// </summary>
    public async Task<(Solution Solution, List<string> Written)> SaveAsync(Solution current, Solution before, Solution after, CancellationToken cancellation)
    {
        var written = new List<string>();
        foreach (var change in after.GetChanges(before).GetProjectChanges())
        {
            foreach (var id in change.GetChangedDocuments())
            {
                var document = after.GetDocument(id)!;
                if (document.FilePath is not { } path || written.Contains(path, StringComparer.Ordinal))
                {
                    continue;
                }

                var text = await document.GetTextAsync(cancellation).ConfigureAwait(false);
                var old = await before.GetDocument(id)!.GetTextAsync(cancellation).ConfigureAwait(false);
                if (text.ContentEquals(old) && Equals(text.Encoding, old.Encoding))
                {
                    continue;
                }

                Write(path, text);
                written.Add(path);
                this.Stamp(path);
                foreach (var copy in current.GetDocumentIdsWithFilePath(path))
                {
                    current = current.WithDocumentText(copy, text, PreservationMode.PreserveValue);
                }
            }
        }

        return (current, written);
    }

    private static IEnumerable<string> Paths(Solution solution) =>
        solution.Projects.SelectMany(p => p.Documents).Select(d => d.FilePath).OfType<string>().Distinct(StringComparer.Ordinal);

    private static (long Length, DateTime Written) Read(string path)
    {
        var info = new FileInfo(path);
        return (info.Length, info.LastWriteTimeUtc);
    }

    private void Stamp(string path)
    {
        if (File.Exists(path))
        {
            this.stamps[path] = Read(path);
        }
    }
}
