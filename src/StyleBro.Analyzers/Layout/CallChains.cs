using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1523: when a call chain is split (a '.' or '?.' of the chain starts a line), every call after the chain's first
/// line starts its own line. A link is a '.Name' or '?.Name' of the chain; it has to start a line when it follows a call
/// ('...)' + '.Name') and its part of the chain has a call ('.WriteTo.Sink(x)' is one part, a trailing '.Count' isn't a
/// call). The chain's first line may hold any number of calls (like Roslynator's RCS0054). The fix only rewrites the gap
/// before the link: a line break plus the indentation of the chain's lines that already start with a link.
/// </summary>
internal static class CallChains
{
    /// <summary>Every link that should start its line, with the edit that puts it there.</summary>
    public static IEnumerable<(SyntaxToken Link, TextChange Change)> GetFindings(SyntaxNode root, SourceText text)
    {
        foreach (var node in TreeWalk.Nodes(root))
        {
            if (node is not (InvocationExpressionSyntax or ElementAccessExpressionSyntax or ConditionalAccessExpressionSyntax
                or MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression })
                || IsInChain(node))
            {
                continue;
            }

            foreach (var finding in GetFindings((ExpressionSyntax)node, text))
            {
                yield return finding;
            }
        }
    }

    private static IEnumerable<(SyntaxToken Link, TextChange Change)> GetFindings(ExpressionSyntax chain, SourceText text)
    {
        if (Line(text, chain.SpanStart) == Line(text, chain.Span.End))
        {
            yield break;
        }

        var links = new List<(SyntaxToken Token, bool Call)>();
        Walk(chain, links);
        links.Reverse();
        var starts = links.Select(l => StartsLine(l.Token, text)).ToList();

        // Skipped: chains that aren't split, directives other than regions or syntax errors anywhere in the chain, and
        // chains inside an interpolated string's hole (a line break there needs C# 11). Region lines are ignored, so
        // the chain is judged the same before and after BRO1112/BRO1113 remove them.
        var regionLines = new HashSet<int>();
        foreach (var directive in chain.ContainsDirectives ? chain.DescendantTrivia().Where(t => t.IsDirective) : [])
        {
            if (!directive.IsKind(SyntaxKind.RegionDirectiveTrivia) && !directive.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
            {
                yield break;
            }

            regionLines.Add(Line(text, directive.SpanStart));
        }

        if (!starts.Contains(true) || chain.ContainsDiagnostics
            || chain.Ancestors().Any(a => a is InterpolationSyntax))
        {
            yield break;
        }

        var firstLine = Line(text, chain.SpanStart);
        for (var i = 1; i < links.Count; i++)
        {
            var token = links[i].Token;
            var line = Line(text, token.SpanStart);
            if (starts[i] || !links[i - 1].Call || line == firstLine || !links.Skip(i).Any(l => l.Call))
            {
                continue;
            }

            var previous = token.GetPreviousToken();
            if (previous.TrailingTrivia.Concat(token.LeadingTrivia).Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                continue; // a comment in the gap
            }

            // The indentation of the nearest link before it that starts a line, else of the first one after it.
            var reference = Enumerable.Range(0, i).Reverse().Where(j => starts[j]).Concat(Enumerable.Range(i, links.Count - i).Where(j => starts[j])).First();
            var referenceLine = text.Lines.GetLineFromPosition(links[reference].Token.SpanStart);
            var indentation = Indentation(text, referenceLine);

            // A part that goes on over more lines (a lambda body) keeps its other lines as they are; skipped unless
            // they're all at least as deep as its new line. Moving other links never changes those lines'
            // indentation, so the decision doesn't depend on which links were fixed first.
            var end = i + 1 < links.Count ? links[i + 1].Token.GetPreviousToken().Span.End : chain.Span.End;
            if (Enumerable.Range(line + 1, Line(text, end) - line)
                .Where(n => !regionLines.Contains(n))
                .Select(n => text.ToString(text.Lines[n].Span))
                .Any(l => l.Trim().Length > 0 && !l.StartsWith(indentation, System.StringComparison.Ordinal)))
            {
                continue;
            }

            var lineBreak = text.ToString(TextSpan.FromBounds(text.Lines[referenceLine.LineNumber - 1].End, referenceLine.Start));
            yield return (token, new TextChange(TextSpan.FromBounds(previous.Span.End, token.SpanStart), lineBreak + indentation));
        }
    }

    /// <summary>Whether the node continues a chain that an enclosing node belongs to (only the outermost is analyzed).</summary>
    private static bool IsInChain(SyntaxNode node) => node.Parent switch
    {
        InvocationExpressionSyntax invocation => invocation.Expression == node,
        ElementAccessExpressionSyntax access => access.Expression == node,
        MemberAccessExpressionSyntax access => access.Expression == node && access.IsKind(SyntaxKind.SimpleMemberAccessExpression),
        ConditionalAccessExpressionSyntax => true,
        PostfixUnaryExpressionSyntax suppression => suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression) && IsInChain(suppression),
        _ => false,
    };

    /// <summary>
    /// Collects the chain's links from the last to the first: the '.' of a member access or the '?' of a conditional
    /// access, and whether the member is called.
    /// </summary>
    private static void Walk(ExpressionSyntax expression, List<(SyntaxToken Token, bool Call)> links)
    {
        var call = false;
        while (true)
        {
            switch (expression)
            {
                case InvocationExpressionSyntax invocation:
                    expression = invocation.Expression;
                    call = true;
                    continue;

                case ElementAccessExpressionSyntax access:
                    expression = access.Expression;
                    continue;

                case PostfixUnaryExpressionSyntax suppression when suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                    expression = suppression.Operand;
                    continue;

                case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                    links.Add((access.OperatorToken, call));
                    call = false;
                    expression = access.Expression;
                    continue;

                case MemberBindingExpressionSyntax binding:
                    var question = binding.OperatorToken.GetPreviousToken();
                    links.Add((question.IsKind(SyntaxKind.QuestionToken) ? question : binding.OperatorToken, call));
                    return;

                case ConditionalAccessExpressionSyntax conditional:
                    Walk(conditional.WhenNotNull, links);
                    expression = conditional.Expression;
                    call = false;
                    continue;

                default:
                    return;
            }
        }
    }

    private static bool StartsLine(SyntaxToken token, SourceText text) =>
        Line(text, token.GetPreviousToken().Span.End) < Line(text, token.SpanStart);

    private static string Indentation(SourceText text, TextLine line) =>
        new(text.ToString(line.Span).TakeWhile(c => c is ' ' or '\t').ToArray());

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
