using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace StyleBro.CodeFixes.Naming;

/// <summary>
/// The words of a solution's snapshot and approval files (Verify's '*.verified.*' and '*.received.*', ApprovalTests'
/// '*.approved.*'): they hold the names a serializer wrote, so they're treated like strings (owner's choice 2026-10-15).
/// A data member whose name is there keeps it: renaming broke 2 of 15 xunit.v3 + Verify tests in trial 23. Read from the
/// solution's additional files and from disk, under each project's repository (its git root, else its folder), so the
/// tests' snapshots count while a 'stylebro-migrate format' run loads only the library.
/// </summary>
internal static class SnapshotFiles
{
    private static readonly Regex Word = new(@"[\p{L}_][\p{L}\p{Nd}_]*", RegexOptions.CultureInvariant);

    private static readonly string[] Markers = { ".verified.", ".approved.", ".received." };

    private static readonly string[] SkippedFolders = { "bin", "obj", ".git", ".vs", "node_modules" };

    // The disk scan per folder, reused for five minutes: 'dotnet format' builds a new solution for every rule it fixes.
    private static readonly Dictionary<string, (DateTime At, Dictionary<string, string> Words)> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the file name marks a snapshot or approval file ('Tests.Summary.verified.txt').</summary>
    public static bool IsSnapshotFile(string path)
    {
        var name = Path.GetFileName(path);
        return Markers.Any(m => name.IndexOf(m, StringComparison.OrdinalIgnoreCase) > 0);
    }

    /// <summary>Every word of the solution's snapshot files, with where it first is ('path(line)').</summary>
    public static Dictionary<string, string> GetWords(Solution solution)
    {
        var words = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var document in solution.Projects.SelectMany(p => p.AdditionalDocuments))
        {
            if (document.FilePath is { } path && IsSnapshotFile(path) && document.GetTextAsync().GetAwaiter().GetResult() is { } text)
            {
                Add(words, path, text.ToString());
            }
        }

        foreach (var root in GetRoots(solution))
        {
            foreach (var pair in Scan(root))
            {
                if (!words.ContainsKey(pair.Key))
                {
                    words[pair.Key] = pair.Value;
                }
            }
        }

        return words;
    }

    /// <summary>The folders to scan: each project's git root, else its folder; none inside another.</summary>
    private static List<string> GetRoots(Solution solution)
    {
        var roots = new List<string>();
        foreach (var folder in solution.Projects.Select(p => Path.GetDirectoryName(p.FilePath ?? string.Empty)).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var root = GitRoot(folder!) ?? folder!;
            if (!roots.Any(r => IsInside(root, r)))
            {
                roots.RemoveAll(r => IsInside(r, root));
                roots.Add(root);
            }
        }

        return roots;

        static bool IsInside(string path, string folder) =>
            path.Equals(folder, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

#pragma warning disable RS1035 // A code fix (not an analyzer) reads the repository's snapshot files, like the renamer's other guards.
    private static string? GitRoot(string folder)
    {
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }
        }

        return null;
    }

    private static Dictionary<string, string> Scan(string root)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(root, out var cached) && DateTime.UtcNow - cached.At < TimeSpan.FromMinutes(5))
            {
                return cached.Words;
            }
        }

        var words = new Dictionary<string, string>(StringComparer.Ordinal);
        var folders = new Stack<string>();
        folders.Push(root);
        while (folders.Count > 0)
        {
            var folder = folders.Pop();
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    // Binary snapshots (images) and huge ones don't hold member names worth reading.
                    if (IsSnapshotFile(file) && new FileInfo(file).Length < 1 << 20 && File.ReadAllText(file) is var text && text.IndexOf('\0') < 0)
                    {
                        Add(words, file, text);
                    }
                }

                foreach (var sub in Directory.EnumerateDirectories(folder))
                {
                    if (!SkippedFolders.Contains(Path.GetFileName(sub), StringComparer.OrdinalIgnoreCase))
                    {
                        folders.Push(sub);
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A folder that can't be read has no snapshots we could honour.
            }
        }

        lock (Cache)
        {
            Cache[root] = (DateTime.UtcNow, words);
        }

        return words;
    }
#pragma warning restore RS1035

    private static void Add(Dictionary<string, string> words, string path, string text)
    {
        var line = 1;
        var counted = 0;
        foreach (Match match in Word.Matches(text))
        {
            for (; counted < match.Index; counted++)
            {
                line += text[counted] == '\n' ? 1 : 0;
            }

            if (!words.ContainsKey(match.Value))
            {
                words[match.Value] = $"{path}({line})";
            }
        }
    }
}
