using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1524 (StyleCop issue #651): a conditional expression is on one line, or the condition, the '? a' part and the
/// ': b' part each start their own line. Only expressions already split are checked: when the line breaks next to one of
/// '?' and ':', the other one gets a line break too, on the side BRO1520's setting
/// (dotnet_style_operator_placement_when_wrapping) asks for. A token with a line break on the wrong side is BRO1520's,
/// so the two fixes never edit the same gap.
/// </summary>
internal static class ConditionalLayout
{
    /// <summary>Every '?' or ':' that needs a line break, with the edit that inserts it.</summary>
    public static IEnumerable<(SyntaxToken Token, bool Beginning, TextChange Change)> GetFindings(SyntaxNode root, SourceText text, AnalyzerConfigOptions options)
    {
        var beginning = WrappingPlacement.Read(options, WrappingPlacement.OperatorKey, beginning: true);
        foreach (var conditional in TreeWalk.Nodes(root).OfType<ConditionalExpressionSyntax>())
        {
            foreach (var finding in Find(conditional, text, beginning))
            {
                yield return finding;
            }
        }
    }

    /// <summary>
    /// Skipped: syntax errors, anything but whitespace and line breaks next to '?' or ':' (comments, directives), chains
    /// ('a ? x : b ? y : z', laid out as a list by many teams: each link checked on its own would become a staircase), and a token whose part ('? a' or ': b') spans several lines: its other lines would need
    /// re-indenting.
    /// </summary>
    private static IEnumerable<(SyntaxToken Token, bool Beginning, TextChange Change)> Find(ConditionalExpressionSyntax conditional, SourceText text, bool beginning)
    {
        if (conditional.ContainsDiagnostics
            || conditional.WhenFalse is ConditionalExpressionSyntax
            || (conditional.Parent is ConditionalExpressionSyntax parent && parent.WhenFalse == conditional))
        {
            yield break;
        }

        var question = conditional.QuestionToken;
        var colon = conditional.ColonToken;
        SyntaxToken[] tokens = [question.GetPreviousToken(), question, question.GetNextToken(), colon.GetPreviousToken(), colon, colon.GetNextToken()];
        var gaps = new[] { (tokens[0], tokens[1]), (tokens[1], tokens[2]), (tokens[3], tokens[4]), (tokens[4], tokens[5]) }
            .Select(g => g.Item1.TrailingTrivia.Concat(g.Item2.LeadingTrivia).ToList())
            .ToList();
        if (gaps.SelectMany(g => g).Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            yield break;
        }

        var breaks = gaps.Select(g => g.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia))).ToList();
        var firstBreak = breaks.IndexOf(true);
        if (firstBreak < 0)
        {
            yield break;
        }

        // The new line is indented like the first part that already starts a line ('?', 'a', ':' or 'b').
        var startsLine = new[] { tokens[1], tokens[2], tokens[4], tokens[5] }[firstBreak];
        var line = text.Lines.GetLineFromPosition(startsLine.SpanStart);
        var indentation = text.ToString(TextSpan.FromBounds(line.Start, startsLine.SpanStart));
        var first = text.Lines.GetLineFromPosition(conditional.SpanStart);
        var lineBreak = text.ToString(TextSpan.FromBounds(first.End, first.EndIncludingLineBreak)); // split, so not the last line

        foreach (var (token, before, after, part) in new[] { (question, breaks[0], breaks[1], conditional.WhenTrue), (colon, breaks[2], breaks[3], conditional.WhenFalse) })
        {
            if (before || after || Line(text, part.SpanStart) != Line(text, part.Span.End))
            {
                continue;
            }

            var span = beginning
                ? TextSpan.FromBounds(token.GetPreviousToken().Span.End, token.SpanStart)
                : TextSpan.FromBounds(token.Span.End, token.GetNextToken().SpanStart);
            yield return (token, beginning, new TextChange(span, lineBreak + indentation));
        }
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
