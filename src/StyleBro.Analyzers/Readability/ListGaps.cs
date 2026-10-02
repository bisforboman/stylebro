using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for the gaps in parameter and argument lists (the same lists as BRO1107/BRO1108):
/// BRO1116 (StyleCop SA1112: an empty list's ')' on the line of '('), BRO1117 (SA1113: a comma on the line of the item
/// before it), BRO1118 (SA1114: no blank line between '(' and the first item) and BRO1119 (SA1115: no blank line
/// before a later item). Each fix rewrites only whitespace in one gap; a gap with a comment or directive is skipped.
/// </summary>
internal static class ListGaps
{
    public static IEnumerable<Finding> GetFindings(SyntaxNode list, SourceText text)
    {
        var (open, items) = ParameterLayout.GetList(list);
        if (list.ContainsDiagnostics || list.ContainsDirectives || open.RawKind == 0)
        {
            yield break;
        }

        var close = list.GetLastToken();

        // BRO1116: '()' split over lines. Like StyleCop, not for attributes.
        if (items.Count == 0)
        {
            if (list is not AttributeArgumentListSyntax && Line(text, open.SpanStart) != Line(text, close.SpanStart) && IsPlain(open, close))
            {
                yield return new Finding(close.GetLocation(), DiagnosticIds.EmptyListOnOneLine, new TextChange(TextSpan.FromBounds(open.Span.End, close.SpanStart), string.Empty));
            }

            yield break;
        }

        // BRO1118: blank lines between '(' and the first item.
        var first = items[0].GetFirstToken();

        // Only whole blank lines are removed, so a comment on a line of its own keeps the list from being reported.
        if (GetBlankLines(open, first, text) is { } blankFirst)
        {
            yield return new Finding(items[0].GetLocation(), DiagnosticIds.FirstItemFollowsOpening, new TextChange(blankFirst, string.Empty));
        }

        for (var i = 1; i < items.Count; i++)
        {
            var comma = items[i].GetFirstToken().GetPreviousToken();
            var previousEnd = items[i - 1].GetLastToken();
            if (!comma.IsKind(SyntaxKind.CommaToken))
            {
                continue;
            }

            // BRO1117: the comma starts a line instead of ending the item before it.
            if (Line(text, previousEnd.Span.End) < Line(text, comma.SpanStart) && IsPlain(previousEnd, comma) && IsPlain(comma, items[i].GetFirstToken()))
            {
                var span = TextSpan.FromBounds(previousEnd.Span.End, items[i].SpanStart);

                // The comma goes up; the item keeps its line, without the blank lines before it (BRO1119's, done here
                // so one run is enough).
                var gap = text.ToString(TextSpan.FromBounds(previousEnd.Span.End, comma.SpanStart));
                var lastBreak = gap.LastIndexOf('\n');
                var keep = lastBreak > 0 && gap[lastBreak - 1] == '\r' ? gap.Substring(lastBreak - 1) : gap.Substring(lastBreak);
                yield return new Finding(comma.GetLocation(), DiagnosticIds.CommaOnItemLine, new TextChange(span, "," + keep));
                continue;
            }

            // BRO1119: blank lines before a later item.
            if (GetBlankLines(comma, items[i].GetFirstToken(), text) is { } blank)
            {
                yield return new Finding(items[i].GetLocation(), DiagnosticIds.ItemFollowsComma, new TextChange(blank, string.Empty));
            }
        }
    }

    /// <summary>The whole blank lines between two tokens on different lines, or null when there are none.</summary>
    private static TextSpan? GetBlankLines(SyntaxToken before, SyntaxToken after, SourceText text)
    {
        var from = Line(text, before.SpanStart) + 1;
        var to = Line(text, after.SpanStart);
        if (to - from < 1)
        {
            return null;
        }

        for (var line = from; line < to; line++)
        {
            if (text.ToString(text.Lines[line].Span).Trim().Length > 0)
            {
                return null;
            }
        }

        return TextSpan.FromBounds(text.Lines[from].Start, text.Lines[to].Start);
    }

    private static bool IsPlain(SyntaxToken before, SyntaxToken after) =>
        before.TrailingTrivia.Concat(after.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

    /// <summary>One problem: where it's reported, its rule, and the edit.</summary>
    public sealed class Finding
    {
        public Finding(Location location, string id, TextChange change)
        {
            Location = location;
            Id = id;
            Change = change;
        }

        public Location Location { get; }

        public string Id { get; }

        public TextChange Change { get; }
    }
}
