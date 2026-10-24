using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StyleBro.Host;

/// <summary>
/// Which of StyleBro's ids have fixes that meet (docs/proposals/format-speed.md, phase 3: merged fix rounds), as
/// <c>FixInteractionsTests</c> derives it from the fix-order cases (<c>FixOrderTests</c>: ids a case fixes together interact)
/// and writes it to FixInteractions.txt, embedded here: one line per id, the ids it interacts with after it, or "rename" for
/// a rule that renames (a rename's guards and references reach anything, so it interacts with every id). An id the list
/// doesn't name (another analyzer's, the SDK's) interacts with every id too.
/// </summary>
public sealed class FixInteractions
{
    /// <summary>What a line says of a rule that renames.</summary>
    public const string Rename = "rename";

    private static readonly Lazy<FixInteractions> Embedded = new(() =>
    {
        using var stream = typeof(FixInteractions).Assembly.GetManifestResourceStream("StyleBro.FormatHost.FixInteractions.txt")
            ?? throw new InvalidOperationException("FixInteractions.txt isn't embedded in StyleBro.FormatHost.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd().Split('\n'));
    });

    private readonly Dictionary<string, HashSet<string>> with;
    private readonly HashSet<string> renames;

    private FixInteractions(Dictionary<string, HashSet<string>> with, HashSet<string> renames)
    {
        this.with = with;
        this.renames = renames;
    }

    /// <summary>Gets the list embedded in the host.</summary>
    public static FixInteractions Default => Embedded.Value;

    /// <summary>Gets the ids the list names, in ordinal order.</summary>
    public IReadOnlyList<string> Ids => [.. this.with.Keys.Order(StringComparer.Ordinal)];

    /// <summary>Reads the list's lines ('#' starts a comment).</summary>
    public static FixInteractions Parse(IEnumerable<string> lines)
    {
        var with = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var renames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines.Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal)))
        {
            var words = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            var others = With(with, words[0]);
            foreach (var other in words.Skip(1))
            {
                if (other == Rename)
                {
                    renames.Add(words[0]);
                    continue;
                }

                // Both ways: the list writes each pair at both ids, a hand-made one may not.
                others.Add(other);
                With(with, other).Add(words[0]);
            }
        }

        return new FixInteractions(with, renames);
    }

    /// <summary>
    /// The list's text for <paramref name="ids"/> (every id once), <paramref name="cases"/> (each case's ids: they interact with
    /// each other) and <paramref name="renames"/>, with <paramref name="header"/> as its comment.
    /// </summary>
    public static string Format(IEnumerable<string> ids, IEnumerable<IReadOnlyCollection<string>> cases, IEnumerable<string> renames, IEnumerable<string> header)
    {
        var known = ids.ToHashSet(StringComparer.Ordinal);
        var renaming = renames.ToHashSet(StringComparer.Ordinal);
        var with = known.ToDictionary(id => id, _ => new SortedSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        foreach (var group in cases.Select(c => c.Where(known.Contains).Distinct(StringComparer.Ordinal).ToList()))
        {
            foreach (var id in group)
            {
                with[id].UnionWith(group.Where(other => other != id));
            }
        }

        var lines = header.Select(h => $"# {h}".TrimEnd()).ToList();
        lines.AddRange(known.Order(StringComparer.Ordinal).Select(id => string.Join(" ", new[] { id }.Concat(renaming.Contains(id) ? [Rename] : with[id]))));
        return string.Join("\n", lines) + "\n";
    }

    /// <summary>Whether the list names <paramref name="id"/>.</summary>
    public bool Knows(string id) => this.with.ContainsKey(id);

    /// <summary>Whether <paramref name="id"/>'s fix renames.</summary>
    public bool Renames(string id) => this.renames.Contains(id);

    /// <summary>
    /// Whether the fixes of <paramref name="a"/> and <paramref name="b"/> may meet: they share a fix-order case, one of them
    /// renames, or the list doesn't name one of them.
    /// </summary>
    public bool Interact(string a, string b) =>
        !this.Knows(a) || !this.Knows(b) || this.Renames(a) || this.Renames(b) || this.with[a].Contains(b);

    private static HashSet<string> With(Dictionary<string, HashSet<string>> with, string id)
    {
        if (!with.TryGetValue(id, out var set))
        {
            with[id] = set = new HashSet<string>(StringComparer.Ordinal);
        }

        return set;
    }
}
