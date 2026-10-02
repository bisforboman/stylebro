using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Baseline;

/// <summary>
/// A baseline: existing violations that aren't reported, so a team can turn rules on and fail only on new violations.
/// The file (<c>stylebro.baseline</c>, found like .editorconfig) has one entry per line, tab-separated: rule id, file
/// path relative to the baseline, a fingerprint of the reported line's text, and how many violations of that rule
/// that line has. The fingerprint ignores the line's position and its indentation, so edits elsewhere in the file don't
/// matter; changing the line itself makes its violations new.
/// </summary>
internal sealed class Baseline
{
    public const string FileName = "stylebro.baseline";

    public const string Header =
        "# StyleBro baseline: existing violations that aren't reported. Regenerate with 'stylebro-migrate baseline'.\n"
        + "# Rule\tFile\tLine fingerprint\tCount\n";

    private readonly Dictionary<Key, int> counts;

    public Baseline(Dictionary<Key, int> counts)
    {
        this.counts = counts;
    }

    public int Count => counts.Count;

    public IReadOnlyDictionary<Key, int> Entries => counts;

    /// <summary>An entry's identity: rule, path ('/'-separated, relative to the baseline's folder), fingerprint.</summary>
    public readonly struct Key : IEquatable<Key>
    {
        public Key(string id, string path, string fingerprint)
        {
            Id = id;
            Path = path;
            Fingerprint = fingerprint;
        }

        public string Id { get; }

        public string Path { get; }

        public string Fingerprint { get; }

        public bool Equals(Key other) =>
            string.Equals(Id, other.Id, StringComparison.Ordinal)
            && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Fingerprint, other.Fingerprint, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is Key other && Equals(other);

        public override int GetHashCode() =>
            (StringComparer.Ordinal.GetHashCode(Id) * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(Path)) * 31
            + StringComparer.Ordinal.GetHashCode(Fingerprint);
    }

    public static Baseline Parse(string text)
    {
        var counts = new Dictionary<Key, int>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length == 4 && int.TryParse(parts[3], out var count) && count > 0)
            {
                var key = new Key(parts[0], NormalizePath(parts[1]), parts[2]);
                counts[key] = counts.TryGetValue(key, out var existing) ? existing + count : count;
            }
        }

        return new Baseline(counts);
    }

    /// <summary>The file's text: the header, then the entries sorted so regenerating gives small diffs.</summary>
    public string Render()
    {
        var builder = new StringBuilder(Header);
        foreach (var entry in counts.OrderBy(e => e.Key.Path, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Key.Id, StringComparer.Ordinal)
            .ThenBy(e => e.Key.Fingerprint, StringComparer.Ordinal))
        {
            builder.Append(entry.Key.Id).Append('\t').Append(entry.Key.Path).Append('\t').Append(entry.Key.Fingerprint).Append('\t')
                .Append(entry.Value).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>How many violations of this rule on this line the baseline covers (0 when none).</summary>
    public int Allowed(Key key) => counts.TryGetValue(key, out var count) ? count : 0;

    /// <summary>
    /// The fingerprint of a line: 64-bit FNV-1a over the line's text without leading and trailing whitespace, as 16 hex
    /// digits. Deterministic across processes and platforms (string.GetHashCode isn't).
    /// </summary>
    public static string Fingerprint(string lineText)
    {
        var text = lineText.Trim();
        var hash = 14695981039346656037UL;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 1099511628211UL;
        }

        return hash.ToString("x16");
    }

    /// <summary>The fingerprint of the line a position is on.</summary>
    public static string Fingerprint(SourceText text, int position) => Fingerprint(text.ToString(text.Lines.GetLineFromPosition(position).Span));

    /// <summary>A file's path relative to the baseline's folder, '/'-separated; null when it's outside that folder.</summary>
    public static string? RelativePath(string baselineDirectory, string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.Path.IsPathRooted(filePath))
        {
            return null;
        }

        var root = NormalizePath(baselineDirectory).TrimEnd('/') + "/";
        var file = NormalizePath(filePath);
        return file.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? file.Substring(root.Length) : null;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');
}
