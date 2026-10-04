using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// BRO1616: the layout of a '&lt;summary&gt;'. With multi_line (the default) the tags are on lines of their own; with
/// single_line_when_fits a summary whose text is one line, fits within max_line_length and holds no block element is
/// written on one line, and any other one has its tags on lines of their own. The edits only rewrite the gap between
/// a tag and the text next to it, so the text, the lines in between and the tags themselves stay as they are.
/// </summary>
internal static class SummaryLayout
{
    /// <summary>The layout key: multi_line (default) or single_line_when_fits.</summary>
    public const string LayoutKey = "stylebro_summary_layout";

    /// <summary>The line length a one-line summary must fit in (the common .editorconfig key; no limit when unset).</summary>
    public const string MaxLineLengthKey = "max_line_length";

    /// <summary>Elements that make a summary more than a sentence: such a summary isn't joined onto one line.</summary>
    private static readonly HashSet<string> BlockElements = new() { "para", "code", "list" };

    /// <summary>Every summary in the tree whose layout is wrong: its start tag, whether it's joined, and the edits.</summary>
    public static IEnumerable<(XmlElementStartTagSyntax StartTag, bool Join, TextChange[] Changes)> GetFindings(SyntaxNode root, SourceText text, AnalyzerConfigOptions options)
    {
        var singleLine = options.TryGetValue(LayoutKey, out var layout) && layout.Split(':')[0].Trim().Equals("single_line_when_fits", StringComparison.OrdinalIgnoreCase);
        var maxLength = options.TryGetValue(MaxLineLengthKey, out var max) && int.TryParse(max.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var length) && length > 0
            ? length
            : int.MaxValue;
        foreach (var trivia in TreeWalk.Trivia(root))
        {
            // '/** */' comments too: their start tag's line doesn't begin with '///', so they're skipped below.
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (var summary in documentation.Content.OfType<XmlElementSyntax>())
            {
                if (summary.StartTag.Name.LocalName.ValueText == "summary" && GetFinding(summary, text, singleLine, maxLength) is { } finding)
                {
                    yield return (summary.StartTag, finding.Join, finding.Changes);
                }
            }
        }
    }

    private static (bool Join, TextChange[] Changes)? GetFinding(XmlElementSyntax summary, SourceText text, bool singleLine, int maxLength)
    {
        var start = summary.StartTag;
        var end = summary.EndTag;

        // A missing end tag has an empty name: like a misspelled one, it's an XML error the rule leaves alone.
        if (end.Name.LocalName.ValueText != "summary")
        {
            return null;
        }

        // The tags' lines hold nothing else: '///' before the start tag, nothing after the end tag.
        var startLine = text.Lines.GetLineFromPosition(start.SpanStart);
        var endLine = text.Lines.GetLineFromPosition(end.Span.End);
        var prefix = text.ToString(TextSpan.FromBounds(startLine.Start, start.SpanStart));
        if (prefix.Trim() != "///" || text.ToString(TextSpan.FromBounds(end.Span.End, endLine.End)).Trim().Length > 0)
        {
            return null;
        }

        var segments = GetTextSegments(text, start.Span.End, end.SpanStart, startLine.LineNumber, endLine.LineNumber);
        if (segments.Count == 0)
        {
            return null;
        }

        var first = segments[0];
        var last = segments[segments.Count - 1];
        if (startLine.LineNumber == endLine.LineNumber)
        {
            return singleLine ? null : (false, Split(text, start, end, startLine, prefix, first, last, true, true));
        }

        if (singleLine
            && segments.Count == 1
            && !summary.Content.SelectMany(c => c.DescendantNodesAndSelf()).Any(IsBlockElement)
            && (start.Span.End - startLine.Start) + first.Length + end.Span.Length <= maxLength)
        {
            return (true, [new TextChange(TextSpan.FromBounds(start.Span.End, first.Start), string.Empty), new TextChange(TextSpan.FromBounds(last.End, end.SpanStart), string.Empty)]);
        }

        var textAfterStart = first.Start < startLine.EndIncludingLineBreak;
        var textBeforeEnd = last.End > endLine.Start;
        return textAfterStart || textBeforeEnd ? (false, Split(text, start, end, startLine, prefix, first, last, textAfterStart, textBeforeEnd)) : null;
    }

    /// <summary>Puts a line break and the start line's '/// ' between a tag and the text next to it.</summary>
    private static TextChange[] Split(
        SourceText text,
        XmlElementStartTagSyntax start,
        XmlElementEndTagSyntax end,
        TextLine startLine,
        string prefix,
        TextSpan first,
        TextSpan last,
        bool afterStart,
        bool beforeEnd)
    {
        var lineBreak = text.ToString(TextSpan.FromBounds(startLine.End, startLine.EndIncludingLineBreak));
        var newLine = (lineBreak.Length > 0 ? lineBreak : "\n") + prefix.Substring(0, prefix.IndexOf("///", StringComparison.Ordinal)) + "/// ";
        var changes = new List<TextChange>();
        if (afterStart)
        {
            changes.Add(new TextChange(TextSpan.FromBounds(start.Span.End, first.Start), newLine));
        }

        if (beforeEnd)
        {
            changes.Add(new TextChange(TextSpan.FromBounds(last.End, end.SpanStart), newLine));
        }

        return changes.ToArray();
    }

    /// <summary>
    /// The text between the tags, one span per line that has any: without the '///' of a continuation line and without
    /// the spaces around it.
    /// </summary>
    private static List<TextSpan> GetTextSegments(SourceText text, int contentStart, int contentEnd, int firstLine, int lastLine)
    {
        var segments = new List<TextSpan>();
        for (var number = firstLine; number <= lastLine; number++)
        {
            var line = text.Lines[number];
            var segmentStart = Math.Max(line.Start, contentStart);
            var segmentEnd = Math.Min(line.End, contentEnd);
            if (line.Start >= contentStart)
            {
                segmentStart = SkipSpaces(text, segmentStart, segmentEnd);
                if (text.ToString(TextSpan.FromBounds(segmentStart, segmentEnd)).StartsWith("///", StringComparison.Ordinal))
                {
                    segmentStart += 3;
                }
            }

            segmentStart = SkipSpaces(text, segmentStart, segmentEnd);
            while (segmentEnd > segmentStart && text[segmentEnd - 1] is ' ' or '\t')
            {
                segmentEnd--;
            }

            if (segmentEnd > segmentStart)
            {
                segments.Add(TextSpan.FromBounds(segmentStart, segmentEnd));
            }
        }

        return segments;
    }

    private static int SkipSpaces(SourceText text, int position, int end)
    {
        while (position < end && text[position] is ' ' or '\t')
        {
            position++;
        }

        return position;
    }

    private static bool IsBlockElement(SyntaxNode node) => node switch
    {
        XmlElementSyntax element => BlockElements.Contains(element.StartTag.Name.LocalName.ValueText),
        XmlEmptyElementSyntax empty => BlockElements.Contains(empty.Name.LocalName.ValueText),
        _ => false,
    };
}
