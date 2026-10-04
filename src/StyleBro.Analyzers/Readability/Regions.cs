using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1112 (StyleCop SA1124: no regions) and BRO1113 (SA1123: no regions inside a code element):
/// finding each '#region' with its '#endregion', and the edits that remove both lines.
/// </summary>
internal static class Regions
{
    /// <summary>
    /// Every '#region' with its matching '#endregion', and whether it's inside a code element (a method, property,
    /// accessor or other member body; BRO1113) rather than between members or types (BRO1112). Regions without a
    /// matching '#endregion' are left out.
    /// </summary>
    public static IEnumerable<(RegionDirectiveTriviaSyntax Region, EndRegionDirectiveTriviaSyntax EndRegion, bool InCodeElement)> GetRegions(SyntaxNode root)
    {
        foreach (var trivia in TreeWalk.Trivia(root))
        {
            // Not in code an '#if' turns off: there the region's surroundings are plain text, so whether it's BRO1112's
            // or BRO1113's, and what removing it means for the code around it, is only known where the code is active.
            // In a multi-targeted project the inactive copy's removal won the merge and left BRO1506's blank line
            // (Newtonsoft.Json's samples, a second run).
            if (trivia.GetStructure() is RegionDirectiveTriviaSyntax { IsActive: true } region
                && region.GetRelatedDirectives().OfType<EndRegionDirectiveTriviaSyntax>().FirstOrDefault() is { } endRegion)
            {
                yield return (region, endRegion, IsInCodeElement(trivia));
            }
        }
    }

    /// <summary>
    /// The edits that remove the given directives' lines. A blank line next to a removed run of lines goes too when it
    /// would otherwise double an existing blank line, follow an opening brace or precede a closing brace, so removing
    /// a region doesn't create a blank-line violation of its own.
    /// </summary>
    public static IEnumerable<TextChange> GetChanges(IEnumerable<DirectiveTriviaSyntax> directives, SourceText text)
    {
        var removed = new SortedSet<int>(directives.Select(d => text.Lines.GetLineFromPosition(d.SpanStart).LineNumber));
        var extra = new SortedSet<int>();
        foreach (var run in GetRuns(removed))
        {
            var before = run.First - 1;
            var after = run.Last + 1;
            if (after >= text.Lines.Count || before < 0)
            {
                continue;
            }

            var beforeText = text.Lines[before].ToString().Trim();
            var afterText = text.Lines[after].ToString().Trim();
            if (afterText.Length == 0 && (beforeText.Length == 0 || beforeText.EndsWith("{")))
            {
                extra.Add(after);

                // Blank lines on both sides, then '}': the one left would precede '}' (BRO1518; a second run in
                // Newtonsoft.Json's Issue1307.cs).
                if (beforeText.Length == 0 && after + 1 < text.Lines.Count && text.Lines[after + 1].ToString().TrimStart().StartsWith("}"))
                {
                    extra.Add(before);
                }
            }
            else if (beforeText.Length == 0 && afterText.StartsWith("}"))
            {
                extra.Add(before);
            }
        }

        // One edit per run of removed lines. A run that ends the file (its last line has no line break) takes the line
        // break before it along, so the file keeps its ending.
        foreach (var run in GetRuns(new SortedSet<int>(removed.Union(extra))))
        {
            var first = text.Lines[run.First];
            var last = text.Lines[run.Last];
            var start = last.EndIncludingLineBreak == last.End && run.First > 0 ? text.Lines[run.First - 1].End : first.Start;
            yield return new TextChange(TextSpan.FromBounds(start, last.EndIncludingLineBreak), string.Empty);
        }
    }

    private static IEnumerable<(int First, int Last)> GetRuns(SortedSet<int> lines)
    {
        int? first = null;
        var last = 0;
        foreach (var line in lines)
        {
            if (first is not null && line != last + 1)
            {
                yield return (first.Value, last);
                first = null;
            }

            first ??= line;
            last = line;
        }

        if (first is not null)
        {
            yield return (first.Value, last);
        }
    }

    /// <summary>
    /// Inside a code element: within a statement block ('{ }' of a method, accessor, lambda, ...), like StyleCop's
    /// SA1123. A region in an expression body (a switch expression after '=>') is SA1124's, probed with StyleCop 1.2.
    /// </summary>
    private static bool IsInCodeElement(SyntaxTrivia trivia)
    {
        var position = trivia.SpanStart;
        return trivia.Token.Parent?.AncestorsAndSelf().Any(n => n is BlockSyntax && n.Span.Contains(position)) == true;
    }
}
