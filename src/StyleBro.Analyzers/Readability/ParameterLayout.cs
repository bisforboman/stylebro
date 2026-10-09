using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1107 (StyleCop SA1116: split parameters start on the line after the opening parenthesis) and
/// BRO1108 (SA1117: parameters all on one line, or each on its own line), for parameter lists and argument lists.
/// </summary>
internal static class ParameterLayout
{
    /// <summary>The .editorconfig key for where the first item of a split list goes (BRO1107, and BRO1108's fix).</summary>
    public const string FirstItemKey = "stylebro_split_list_first_item";

    /// <summary>The lists both rules look at: parameters, arguments, attribute arguments and array sizes.</summary>
    public static readonly SyntaxKind[] ListKinds =
    [
        SyntaxKind.ParameterList,
        SyntaxKind.BracketedParameterList,
        SyntaxKind.ArgumentList,
        SyntaxKind.BracketedArgumentList,
        SyntaxKind.AttributeArgumentList,
        SyntaxKind.ArrayRankSpecifier,
    ];

    /// <summary>
    /// BRO1107: the first item shares its line with the opening parenthesis while the second item starts on a later
    /// line. Like StyleCop, when the first two share a line, BRO1108 reports the list instead. Never when
    /// <paramref name="options"/> set <c>stylebro_split_list_first_item = same_line</c>: the first item may stay there.
    /// </summary>
    /// <param name="joinsEmptyLists">Whether BRO1116 is on (see <see cref="GetLines"/>).</param>
    public static SyntaxNode? GetFirstItemToMove(SyntaxNode list, SourceText text, AnalyzerConfigOptions? options, Func<bool>? joinsEmptyLists = null)
    {
        var (open, items) = GetList(list);
        if (items.Count < 2)
        {
            return null;
        }

        // The layout first, then the setting and the gaps (CanRewrite looks at every item's trivia): most lists are fine.
        var line = GetLines(list, text, joinsEmptyLists);
        var openLine = line(open.SpanStart);
        return line(items[0].SpanStart) == openLine && line(items[1].SpanStart) > openLine
            && !(options is not null && IsSameLine(options)) && CanRewrite(list, items) ? items[0] : null;
    }

    /// <summary>
    /// BRO1108: the items are neither all on one line nor each on their own line; the first two decide which one was
    /// intended, like StyleCop. Returns the first item that breaks it. When the first two start on the same line,
    /// every item has to start on that line; otherwise no item may start on the line where the previous one ends.
    /// </summary>
    public static SyntaxNode? GetFirstMisplacedItem(SyntaxNode list, SourceText text, Func<bool>? joinsEmptyLists = null)
    {
        var (_, items) = GetList(list);
        if (items.Count < 2)
        {
            return null;
        }

        var misplaced = GetMisplacedItem(items, GetLines(list, text, joinsEmptyLists));
        return misplaced is not null && CanRewrite(list, items) ? misplaced : null;
    }

    /// <summary>
    /// The edits that put every item on its own line (with <paramref name="firstOnly"/>, only the first item: BRO1107),
    /// starting on the line after the opening parenthesis, one indentation level deeper than the line with the
    /// parenthesis. A moved item's later lines (a lambda's block body) move by the same amount, unlike StyleCop's fix
    /// (StyleCop #1620, #3183). Items already at the start of a line keep their indentation; everything else in the
    /// list stays as it is. With <paramref name="sameLine"/>, a first item on the opening parenthesis's line stays there,
    /// and moved items are indented like the first item that already starts a line (aligned under the first item, or
    /// one level deeper: whatever the list does), else one level deeper than the line with the parenthesis.
    /// </summary>
    public static IEnumerable<TextChange> GetChanges(SyntaxNode list, SourceText text, string indentUnit, bool firstOnly = false, bool sameLine = false)
    {
        var (open, items) = GetList(list);
        var openLine = text.Lines.GetLineFromPosition(open.SpanStart);
        var indentation = new string(text.ToString(openLine.Span).TakeWhile(c => c is ' ' or '\t').ToArray()) + indentUnit;
        if (sameLine && items.FirstOrDefault(i => Line(text, i.SpanStart) > Line(text, i.GetFirstToken().GetPreviousToken().Span.End)) is { } lineStart)
        {
            indentation = LeadingWhitespace(text, text.Lines.GetLineFromPosition(lineStart.SpanStart));
        }

        var lineBreak = text.ToString(TextSpan.FromBounds(openLine.End, openLine.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        var previousEnd = open.Span.End;
        foreach (var item in items)
        {
            if (Line(text, item.SpanStart) == Line(text, previousEnd) && !(sameLine && previousEnd == open.Span.End))
            {
                // Replace the spaces between the previous token ('(' or ',') and the item with a line break.
                yield return new TextChange(TextSpan.FromBounds(previousEnd, item.SpanStart), lineBreak + indentation);
                foreach (var change in Reindent(item, text, indentation))
                {
                    yield return change;
                }
            }

            if (firstOnly)
            {
                yield break;
            }

            previousEnd = item.GetLastToken().GetNextToken().Span.End; // the ',' after the item
        }
    }

    /// <summary>Whether <see cref="FirstItemKey"/> is <c>same_line</c> (default <c>next_line</c>, like StyleCop).</summary>
    public static bool IsSameLine(AnalyzerConfigOptions options) =>
        options.TryGetValue(FirstItemKey, out var value) && value.Trim().Equals("same_line", System.StringComparison.OrdinalIgnoreCase);

    public static (SyntaxToken Open, IReadOnlyList<SyntaxNode> Items) GetList(SyntaxNode list)
    {
        return list switch
        {
            BaseParameterListSyntax parameters => (parameters.GetFirstToken(), parameters.Parameters),
            BaseArgumentListSyntax arguments => (arguments.GetFirstToken(), arguments.Arguments),
            AttributeArgumentListSyntax attributeArguments => (attributeArguments.OpenParenToken, attributeArguments.Arguments),
            ArrayRankSpecifierSyntax rank => (rank.OpenBracketToken, rank.Sizes),
            _ => (default, []),
        };
    }

    private static SyntaxNode? GetMisplacedItem(IReadOnlyList<SyntaxNode> items, Func<int, int> line)
    {
        var firstLine = line(items[0].SpanStart);
        if (line(items[1].SpanStart) == firstLine)
        {
            return items.Skip(2).FirstOrDefault(item => line(item.SpanStart) != firstLine);
        }

        for (var i = 1; i < items.Count; i++)
        {
            if (line(items[i].SpanStart) == line(items[i - 1].Span.End))
            {
                return items[i];
            }
        }

        return null;
    }

    /// <summary>
    /// The edits that move the later lines of an item that now starts a line at <paramref name="indentation"/>: each
    /// line indented at least as deep as the item's old line keeps its depth relative to it. Nothing when a token spans
    /// lines (a multi-line string: its text can't be reindented).
    /// </summary>
    private static IEnumerable<TextChange> Reindent(SyntaxNode item, SourceText text, string indentation)
    {
        var first = Line(text, item.SpanStart);
        var last = Line(text, item.Span.End);
        if (first == last || item.DescendantTokens().Any(t => Line(text, t.SpanStart) != Line(text, t.Span.End)))
        {
            yield break;
        }

        var prefix = LeadingWhitespace(text, text.Lines[first]);
        for (var number = first + 1; number <= last; number++)
        {
            var line = text.Lines[number];
            var old = LeadingWhitespace(text, line);
            if (old.Length < line.Span.Length && old.StartsWith(prefix, System.StringComparison.Ordinal) && indentation != prefix)
            {
                yield return new TextChange(new TextSpan(line.Start, old.Length), indentation + old.Substring(prefix.Length));
            }
        }
    }

    private static string LeadingWhitespace(SourceText text, TextLine line) =>
        new(text.ToString(line.Span).TakeWhile(c => c is ' ' or '\t').ToArray());

    /// <summary>
    /// Skipped: lists with syntax errors, and lists with a comment or directive between the opening parenthesis and
    /// an item or between items, since the fix replaces exactly those gaps.
    /// </summary>
    private static bool CanRewrite(SyntaxNode list, IReadOnlyList<SyntaxNode> items)
    {
        if (list.ContainsDiagnostics)
        {
            return false;
        }

        foreach (var item in items)
        {
            var before = item.GetFirstToken().GetPreviousToken();
            if (before.TrailingTrivia.Concat(item.GetFirstToken().LeadingTrivia)
                .Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The line of a position in the list as it is once BRO1116 (when on) has joined every empty '()' inside it that is
    /// split over lines: those line breaks don't count, so BRO1107/BRO1108 judge the list the same before and after that
    /// fix, and the result doesn't depend on which fix runs first (<c>N(</c> / <c>), M(5,</c> was "each on its own line"
    /// before the join and "all on one line" after it).
    /// </summary>
    private static Func<int, int> GetLines(SyntaxNode list, SourceText text, Func<bool>? joinsEmptyLists)
    {
        int Plain(int position) => Line(text, position);
        if (joinsEmptyLists is null)
        {
            return Plain;
        }

        // Only an item that spans lines can hold one (most don't); its tokens are scanned for '(' + ')' with a line break.
        List<(int At, int Breaks)>? joined = null;
        foreach (var item in GetList(list).Items)
        {
            if (Line(text, item.SpanStart) == Line(text, item.Span.End))
            {
                continue;
            }

            var previous = default(SyntaxToken);
            foreach (var token in item.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.CloseParenToken) && previous.IsKind(SyntaxKind.OpenParenToken)
                    && (previous.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia) || token.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia))
                    && token.Parent is { } empty && (empty.IsKind(SyntaxKind.ArgumentList) || empty.IsKind(SyntaxKind.ParameterList))
                    && ListGaps.GetFindings(empty, text).Any(f => f.Id == DiagnosticIds.EmptyListOnOneLine))
                {
                    (joined ??= []).Add((token.SpanStart, Line(text, token.SpanStart) - Line(text, previous.SpanStart)));
                }

                previous = token;
            }
        }

        return joined is null || !joinsEmptyLists() ? Plain : position => Plain(position) - joined.Where(j => j.At <= position).Sum(j => j.Breaks);
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
