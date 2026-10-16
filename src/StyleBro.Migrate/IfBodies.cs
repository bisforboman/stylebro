using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Migrate;

/// <summary>
/// Lines '#if' makes an if/else (or loop) body in one build and not in another (owner's decision 2026-10-16, "leave such
/// lines alone"): '#if NETFRAMEWORK' + 'if (x) a(); else' + '#endif' + 'b();' is indented as the else body where the
/// symbol is defined and as a statement of its own where it isn't, so format's per-framework and per-configuration runs
/// undid each other's indentation forever (NLog never converged). format leaves the indentation of these lines as it is.
/// </summary>
internal static class IfBodies
{
    // A statement owner ('if (x)', 'else', a loop) right before a directive line: where a body can change with '#if'. The
    // parses decide; this only keeps the other files from being parsed.
    private static readonly Regex Candidate = new(
        @"(?:\)|^[ \t]*(?:\}[ \t]*)?(?:else|do))[ \t]*(?://[^\r\n]*)?\r?\n[ \t]*#[ \t]*(?:if|elif|else|endif)\b",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // A whitespace finding that only rewrites a line break ("Replace 1 characters with '\r\n'"), which the whitespace pass
    // writes on the line above such a body when it re-indents the body.
    private static readonly Regex LineBreakOnly = new(@" WHITESPACE: Fix whitespace formatting\. Replace \d+ characters with '(?:(?:\\r)?\\n)+'\.", RegexOptions.Compiled);

    // A whitespace finding whose new text has a line break: its edit runs from the end of its line into the next.
    private static readonly Regex WritesLineBreak = new(@" WHITESPACE: Fix whitespace formatting\. (?:Replace \d+ characters with|Insert) '[^']*\\n", RegexOptions.Compiled);

    /// <summary>Whether a C# text may have such lines (<see cref="Lines"/> decides).</summary>
    public static bool IsCandidate(string text) => text.Contains("#if", StringComparison.Ordinal) && Candidate.IsMatch(text);

    /// <summary>
    /// The lines (0-based) of <paramref name="text"/> whose statement nesting differs between the parses with
    /// <paramref name="symbolSets"/> (each the defined symbols of one run that compiles the file): a line counts when its first
    /// token is code in two parses (not text of a branch left out) and is a body of a different number of statements in them.
    /// </summary>
    public static List<int> Lines(string text, IEnumerable<IReadOnlyCollection<string>> symbolSets)
    {
        var sets = symbolSets.Select(s => s.Order(StringComparer.Ordinal).ToArray()).DistinctBy(s => string.Join(";", s)).ToList();
        if (sets.Count < 2 || !IsCandidate(text))
        {
            return [];
        }

        var source = SourceText.From(text);
        var seen = new Dictionary<(int Line, int Offset), int>();
        var protectedLines = new SortedSet<int>();
        foreach (var symbols in sets)
        {
            var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: symbols)).GetRoot();
            var lastLine = -1;
            foreach (var token in root.DescendantTokens())
            {
                var line = source.Lines.GetLineFromPosition(token.SpanStart).LineNumber;
                if (line == lastLine || token.IsKind(SyntaxKind.EndOfFileToken))
                {
                    continue;
                }

                lastLine = line;
                var depth = Depth(token);
                if (!seen.TryGetValue((line, token.SpanStart), out var other))
                {
                    seen[(line, token.SpanStart)] = depth;
                }
                else if (other != depth)
                {
                    protectedLines.Add(line);
                }
            }
        }

        return protectedLines.ToList();
    }

    /// <summary>
    /// How many statements a token is the body of (its own statement included), not counting blocks or a 'using' right
    /// inside another, which the formatter doesn't indent.
    /// </summary>
    public static int Depth(SyntaxToken token)
    {
        var depth = 0;
        for (var node = token.Parent; node is not null && node is not MemberDeclarationSyntax; node = node.Parent)
        {
            if (node is StatementSyntax and not BlockSyntax
                && (node.Parent is ElseClauseSyntax
                    || (node.Parent is StatementSyntax parent and not BlockSyntax and not LabeledStatementSyntax && !(node is UsingStatementSyntax && parent is UsingStatementSyntax))))
            {
                depth++;
            }
        }

        return depth;
    }

    /// <summary>
    /// <paramref name="after"/> with the protected <paramref name="lines"/> of <paramref name="before"/> given their indentation and
    /// line break back, and the line breaks of the lines from the code above each one down to it. A line is found again by its
    /// text without the indentation and the lines around it (<see cref="Locate"/>), else it's left as it is.
    /// </summary>
    public static string Keep(string before, IReadOnlyCollection<int> lines, string after)
    {
        var old = Split(before);
        var now = Split(after);
        var oldText = old.Select(l => l.Text.Trim()).ToArray();
        var nowText = now.Select(l => l.Text.Trim()).ToArray();
        var changed = false;
        foreach (var index in lines.Where(l => l < old.Count).SelectMany(l => Range(old, l).Select(i => (Line: i, Indent: i == l))).DistinctBy(x => x.Line))
        {
            if (Locate(oldText, index.Line, nowText) is not (>= 0 and var at))
            {
                continue;
            }

            var text = index.Indent ? Indentation(old[index.Line].Text) + now[at].Text.TrimStart() : now[at].Text;
            if (text != now[at].Text || old[index.Line].Break != now[at].Break)
            {
                now[at] = (text, old[index.Line].Break);
                changed = true;
            }
        }

        return changed ? string.Concat(now.Select(l => l.Text + l.Break)) : after;
    }

    /// <summary>
    /// Where line <paramref name="line"/> of <paramref name="old"/> (lines without indentation) is in <paramref name="now"/>, or -1:
    /// the one line with its text and the lines around it, as few of them as tell it apart (a fix elsewhere in the file can add
    /// or remove a '{' or 'else' line: counting such lines found the wrong one, NLog). With a change right next to it, the
    /// lines on its other side only.
    /// </summary>
    public static int Locate(string[] old, int line, string[] now)
    {
        const int MaxContext = 40;
        bool Same(int at, int from, int to)
        {
            for (var d = from; d <= to; d++)
            {
                var (o, n) = (line + d, at + d);
                var (oldIn, nowIn) = (o >= 0 && o < old.Length, n >= 0 && n < now.Length);
                if (oldIn != nowIn || (oldIn && old[o] != now[n]))
                {
                    return false;
                }
            }

            return true;
        }

        int Unique(int from, int to)
        {
            var found = -1;
            for (var at = 0; at < now.Length; at++)
            {
                if (now[at] == old[line] && Same(at, from, to))
                {
                    if (found >= 0)
                    {
                        return -2;
                    }

                    found = at;
                }
            }

            return found;
        }

        for (var k = 0; k <= MaxContext; k++)
        {
            switch (Unique(-k, k))
            {
                case >= 0 and var at:
                    return at;

                case -1:
                    for (var side = 1; side <= k; side++)
                    {
                        if (Unique(-side, 0) is >= 0 and var before)
                        {
                            return before;
                        }

                        if (Unique(0, side) is >= 0 and var after)
                        {
                            return after;
                        }
                    }

                    return -1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Whether a 'dotnet format --verify-no-changes' line is a whitespace finding <see cref="Keep"/> undoes: on a protected line
    /// (1-based in <paramref name="protectedLines"/>, by full path), or only a line break on the lines above one.
    /// </summary>
    public static bool IsKeptFinding(string line, IReadOnlyDictionary<string, (string Text, List<int> Lines)> protectedLines)
    {
        var match = Regex.Match(line, @"^\s*(?<path>.+?)\((?<line>\d+),\d+\): \w+ WHITESPACE:");
        if (!match.Success || !protectedLines.TryGetValue(Path.GetFullPath(match.Groups["path"].Value), out var file))
        {
            return false;
        }

        // A finding is reported where its edit starts: one that writes a line break starts at the end of the line before the
        // line it indents.
        var number = int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture) - 1;
        if (file.Lines.Contains(WritesLineBreak.IsMatch(line) ? number + 1 : number))
        {
            return true;
        }

        var old = Split(file.Text);
        return LineBreakOnly.IsMatch(line) && file.Lines.Any(l => l < old.Count && Range(old, l).Contains(number));
    }

    /// <summary>The protected lines of the C# files under <paramref name="root"/> that <paramref name="symbolSets"/> names (full path to its sets), with their text.</summary>
    public static Dictionary<string, (string Text, List<int> Lines)> Find(IReadOnlyDictionary<string, List<IReadOnlyCollection<string>>> symbolSets) =>
        symbolSets.AsParallel()
            .Select(f => (File: f.Key, Text: Read(f.Key), f.Value))
            .Where(f => f.Text is not null)
            .Select(f => (f.File, Text: f.Text!, Lines: Lines(f.Text!, f.Value)))
            .Where(f => f.Lines.Count > 0)
            .ToDictionary(f => f.File, f => (f.Text, f.Lines), StringComparer.OrdinalIgnoreCase);

    /// <summary>Gives the protected lines of each file their indentation back (<see cref="Keep"/>) after a run changed it.</summary>
    public static void Restore(IReadOnlyDictionary<string, (string Text, List<int> Lines)> protectedLines)
    {
        foreach (var (file, (text, lines)) in protectedLines)
        {
            if (Read(file) is { } now && now != text && Keep(text, lines, now) is var kept && kept != now)
            {
                File.WriteAllText(file, kept, new UTF8Encoding(false));
            }
        }
    }

    /// <summary>
    /// A run's check (<paramref name="code"/>, <paramref name="lines"/>) without the findings that don't count (<paramref name="ignored"/>:
    /// those <see cref="Keep"/> undoes, <see cref="IsKeptFinding"/>, and those in files format leaves alone): exit code 0 when
    /// they were its only findings.
    /// </summary>
    public static (int Code, List<string> Lines) Without(int code, List<string> lines, Func<string, bool> ignored)
    {
        var left = lines.Where(l => !ignored(l)).ToList();
        return (code == FormatCommand.NotCleanExitCode && left.Count < lines.Count && !left.Any(FormatCommand.IsFindingLine) ? 0 : code, left);
    }

    /// <summary>What format says about the protected lines (relative paths and 1-based lines).</summary>
    public static string Line(string header, string root, IReadOnlyDictionary<string, (string Text, List<int> Lines)> protectedLines)
    {
        // Each file once, its lines as ranges ('A.cs:173-177,190').
        static string Ranges(List<int> lines) => string.Join(",", lines.Select((l, i) => (Line: l + 1, Start: i == 0 || lines[i - 1] != l - 1))
            .Aggregate(new List<(int From, int To)>(), (ranges, l) =>
            {
                if (l.Start)
                {
                    ranges.Add((l.Line, l.Line));
                }
                else
                {
                    ranges[^1] = (ranges[^1].From, l.Line);
                }

                return ranges;
            })
            .Select(r => r.From == r.To ? $"{r.From}" : $"{r.From}-{r.To}"));
        var places = protectedLines.OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
            .Select(f => $"{Path.GetRelativePath(root, f.Key).Replace('\\', '/')}:{Ranges(f.Value.Lines)}")
            .ToList();
        return $"{header} {string.Join(", ", places.Take(5))}{(places.Count > 5 ? $" and {places.Count - 5} more files" : string.Empty)}: "
            + "'#if' decides whether these lines are an if/else (or loop) body, so each framework or configuration would indent them its own way. "
            + "Their indentation is left as it is.";
    }

    // The lines from the code line above a protected line down to it: past directive lines, blank lines and the '#if' blocks
    // they close (code some parses leave out), so the line every parse has above it.
    private static IEnumerable<int> Range(List<(string Text, string Break)> lines, int line)
    {
        var start = line - 1;
        for (var open = 0; start > 0; start--)
        {
            var text = lines[start].Text.TrimStart();
            if (!text.StartsWith('#'))
            {
                if (open == 0 && text.Length > 0)
                {
                    break;
                }

                continue;
            }

            var directive = text.TrimStart('#').TrimStart();
            open += directive.StartsWith("endif", StringComparison.Ordinal) ? 1 : directive.StartsWith("if", StringComparison.Ordinal) ? -1 : 0;
        }

        return Enumerable.Range(Math.Max(start, 0), line - Math.Max(start, 0) + 1);
    }

    private static string Indentation(string line) => line[..(line.Length - line.TrimStart().Length)];

    // Each line's text and its line break ("" for the last).
    private static List<(string Text, string Break)> Split(string text) =>
        Regex.Matches(text, @"(?<text>[^\r\n]*)(?<break>\r\n|\n|\r|$)")
            .Where(m => m.Length > 0 || m.Index < text.Length)
            .Select(m => (m.Groups["text"].Value, m.Groups["break"].Value))
            .ToList();

    // UTF-8 only (a BOM is read as a character, so writing the text back keeps it), like DirectiveMask.
    private static string? Read(string file)
    {
        try
        {
            return File.Exists(file) ? new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(File.ReadAllBytes(file)) : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>Gives the protected lines their indentation back when disposed (<see cref="Restore"/>): after a run's 'dotnet format' calls.</summary>
    internal sealed class Kept(IReadOnlyDictionary<string, (string Text, List<int> Lines)> protectedLines) : IDisposable
    {
        public void Dispose() => Restore(protectedLines);
    }
}
