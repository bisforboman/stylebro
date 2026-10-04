using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1525 (StyleCop's proposed SA1521, issue #738, never implemented): no blank line between an attribute list and
/// what it applies to, or the next attribute list of the same element. BRO1505 doesn't count these blank lines (an
/// element's code starts at its first attribute), so removing them never changes what BRO1505 wants.
/// </summary>
internal static class AttributeBlankLines
{
    /// <summary>
    /// The blank lines after each attribute list, as a span of whole lines that the fix removes. Assembly and module
    /// attributes aren't checked, and a gap with anything but whitespace (a comment, a documentation comment, a
    /// directive) is left alone: BRO1504 wants a blank line above a comment that follows code.
    /// </summary>
    public static IEnumerable<TextSpan> GetFindings(SyntaxNode root, SourceText text)
    {
        foreach (var list in root.DescendantNodes().OfType<AttributeListSyntax>())
        {
            if (list.Parent is null or CompilationUnitSyntax || list.Parent.ContainsDiagnostics)
            {
                continue;
            }

            var close = list.CloseBracketToken;
            var next = close.GetNextToken();
            if (!close.TrailingTrivia.Concat(next.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                continue;
            }

            var from = text.Lines.GetLineFromPosition(close.Span.End).LineNumber + 1;
            var to = text.Lines.GetLineFromPosition(next.SpanStart).LineNumber;
            if (to > from)
            {
                yield return TextSpan.FromBounds(text.Lines[from].Start, text.Lines[to].Start);
            }
        }
    }
}
