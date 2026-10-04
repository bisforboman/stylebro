using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1107 (StyleCop SA1116: split parameters start on the line after the opening parenthesis) and
/// BRO1108 (SA1117: parameters all on one line, or each on its own line), for parameter lists and argument lists.
/// </summary>
internal static class ParameterLayout
{
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
    /// line. Like StyleCop, when the first two share a line, BRO1108 reports the list instead.
    /// </summary>
    public static SyntaxNode? GetFirstItemToMove(SyntaxNode list, SourceText text)
    {
        var (open, items) = GetList(list);
        if (items.Count < 2 || !CanRewrite(list, items))
        {
            return null;
        }

        var openLine = Line(text, open.SpanStart);
        return Line(text, items[0].SpanStart) == openLine && Line(text, items[1].SpanStart) > openLine ? items[0] : null;
    }

    /// <summary>
    /// BRO1108: the items are neither all on one line nor each on their own line; the first two decide which one was
    /// intended, like StyleCop. Returns the first item that breaks it. When the first two start on the same line,
    /// every item has to start on that line; otherwise no item may start on the line where the previous one ends.
    /// </summary>
    public static SyntaxNode? GetFirstMisplacedItem(SyntaxNode list, SourceText text)
    {
        var (_, items) = GetList(list);
        if (items.Count < 2 || !CanRewrite(list, items))
        {
            return null;
        }

        var firstLine = Line(text, items[0].SpanStart);
        if (Line(text, items[1].SpanStart) == firstLine)
        {
            return items.Skip(2).FirstOrDefault(item => Line(text, item.SpanStart) != firstLine);
        }

        for (var i = 1; i < items.Count; i++)
        {
            if (Line(text, items[i].SpanStart) == Line(text, items[i - 1].Span.End))
            {
                return items[i];
            }
        }

        return null;
    }

    /// <summary>
    /// The edits that put every item on its own line (with <paramref name="firstOnly"/>, only the first item: BRO1107),
    /// starting on the line after the opening parenthesis, one indentation level deeper than the line with the
    /// parenthesis. A moved item's later lines (a lambda's block body) move by the same amount, unlike StyleCop's fix
    /// (StyleCop #1620, #3183). Items already at the start of a line keep their indentation; everything else in the
    /// list stays as it is.
    /// </summary>
    public static IEnumerable<TextChange> GetChanges(SyntaxNode list, SourceText text, string indentUnit, bool firstOnly = false)
    {
        var (open, items) = GetList(list);
        var openLine = text.Lines.GetLineFromPosition(open.SpanStart);
        var indentation = new string(text.ToString(openLine.Span).TakeWhile(c => c is ' ' or '\t').ToArray()) + indentUnit;
        var lineBreak = text.ToString(TextSpan.FromBounds(openLine.End, openLine.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        var previousEnd = open.Span.End;
        foreach (var item in items)
        {
            if (Line(text, item.SpanStart) == Line(text, previousEnd))
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

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
