using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Migrate;

/// <summary>
/// The lines after a documentation comment's first line that 'dotnet format whitespace' leaves alone while the build's IDE0055
/// reports them (owner's decision 2026-10-17, "fix it ourselves + report upstream"): its other '///' lines and the line of the
/// code it documents, indented to the first line's column with other characters ('\t\t' under eight spaces, tab_width 4).
/// The formatter compares columns there, IDE0055 the text, so verify said clean while the build failed (PalCalc). format
/// writes those lines' indentation as the settings ask (indent_style, tab_width); nothing else on them changes.
/// </summary>
internal static class DocIndentation
{
    /// <summary>
    /// The lines (0-based) of <paramref name="text"/> to give the indentation of their documentation comment's first line,
    /// written with tabs (<paramref name="useTabs"/>: as many as fit, then spaces) or spaces, a tab <paramref name="tabWidth"/> wide.
    /// Only where that first line is written that way (else the formatter rewrites the whole comment) and the line is at its column.
    /// </summary>
    public static List<int> Lines(string text, bool useTabs, int tabWidth)
    {
        if (!text.Contains("///", StringComparison.Ordinal))
        {
            return [];
        }

        var source = SourceText.From(text);
        var lines = new SortedSet<int>();
        foreach (var trivia in CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)).GetRoot().DescendantTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                continue;
            }

            var first = source.Lines.GetLineFromPosition(trivia.FullSpan.Start);
            var indentation = text[first.Start..trivia.FullSpan.Start]; // its Span starts after the first "///"
            if (indentation.Trim().Length > 0 || Indentation(Column(indentation, tabWidth), useTabs, tabWidth) != indentation)
            {
                continue;
            }

            // Its other lines start with '///'; the last is the line of the token it documents, when that token starts it.
            var column = Column(indentation, tabWidth);
            var last = source.Lines.GetLineFromPosition(Math.Min(trivia.FullSpan.End, text.Length)).LineNumber;
            for (var number = first.LineNumber + 1; number <= last && number < source.Lines.Count; number++)
            {
                var line = source.Lines[number].ToString();
                var own = line[..(line.Length - line.TrimStart().Length)];
                var isComment = line.TrimStart().StartsWith("///", StringComparison.Ordinal);
                var isToken = number == last && source.Lines[number].Start + own.Length == trivia.Token.SpanStart;
                if ((isComment || isToken) && Column(own, tabWidth) == column && own != Indentation(column, useTabs, tabWidth))
                {
                    lines.Add(number);
                }
            }
        }

        return lines.ToList();
    }

    /// <summary><paramref name="text"/> with the indentation of <see cref="Lines"/> rewritten; the rest of each line and its line break stay.</summary>
    public static string Fix(string text, bool useTabs, int tabWidth)
    {
        var lines = Lines(text, useTabs, tabWidth);
        if (lines.Count == 0)
        {
            return text;
        }

        var source = SourceText.From(text);
        var result = new StringBuilder(text);
        foreach (var number in lines.OrderByDescending(l => l))
        {
            var line = source.Lines[number];
            var content = line.ToString();
            var own = content.Length - content.TrimStart().Length;
            result.Remove(line.Start, own).Insert(line.Start, Indentation(Column(content[..own], tabWidth), useTabs, tabWidth));
        }

        return result.ToString();
    }

    /// <summary>
    /// The files of <paramref name="files"/> (full paths) with such lines (1-based), each with the settings its .editorconfig
    /// gives it (indent_style, tab_width: 4 unless set). UTF-8 files only.
    /// </summary>
    public static List<(string File, List<int> Lines, bool UseTabs, int TabWidth)> Find(string root, IEnumerable<string> files)
    {
        var candidates = files.Where(f => Read(f) is { } text && text.Contains("///", StringComparison.Ordinal)).ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var set = FormatCommand.ConfigSet(root);
        return candidates.AsParallel()
            .Select(f =>
            {
                var options = set.GetOptionsForSourcePath(Path.GetFullPath(f)).AnalyzerOptions;
                var useTabs = options.TryGetValue("indent_style", out var style) && style.Trim().Equals("tab", StringComparison.OrdinalIgnoreCase);
                var tabWidth = options.TryGetValue("tab_width", out var width) && int.TryParse(width.Trim(), out var w) && w > 0 ? w : 4;
                return (File: f, Lines: Lines(Read(f) ?? string.Empty, useTabs, tabWidth).Select(l => l + 1).ToList(), UseTabs: useTabs, TabWidth: tabWidth);
            })
            .Where(f => f.Lines.Count > 0)
            .OrderBy(f => f.File, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Rewrites the lines <see cref="Find"/> found; the files it changed.</summary>
    public static List<string> FixFiles(IEnumerable<(string File, List<int> Lines, bool UseTabs, int TabWidth)> found)
    {
        var changed = new List<string>();
        foreach (var (file, _, useTabs, tabWidth) in found)
        {
            if (Read(file) is { } text && Fix(text, useTabs, tabWidth) is var fixedText && fixedText != text)
            {
                File.WriteAllText(file, fixedText, new UTF8Encoding(false));
                changed.Add(file);
            }
        }

        return changed;
    }

    /// <summary>A finding line per line <see cref="Find"/> found, like 'dotnet format --verify-no-changes' writes them (IDE0055, as the build reports it).</summary>
    public static List<string> FindingLines(IEnumerable<(string File, List<int> Lines, bool UseTabs, int TabWidth)> found) =>
        found.SelectMany(f => f.Lines.Select(l => $"{f.File}({l},1): warning IDE0055: Fix formatting: indent this line with {(f.UseTabs ? "tabs" : "spaces")}, like the first line of the documentation comment above it ('dotnet format' leaves it, the build reports it)."))
            .ToList();

    // How many columns the whitespace takes.
    private static int Column(string whitespace, int tabWidth) =>
        whitespace.Aggregate(0, (column, c) => c == '\t' ? column + tabWidth - (column % tabWidth) : column + 1);

    private static string Indentation(int column, bool useTabs, int tabWidth) =>
        useTabs ? new string('\t', column / tabWidth) + new string(' ', column % tabWidth) : new string(' ', column);

    // UTF-8 only (a BOM is read as a character, so writing the text back keeps it), like IfBodies.
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
}
