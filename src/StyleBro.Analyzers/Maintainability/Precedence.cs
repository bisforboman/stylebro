using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>
/// BRO1406 (SA1407) arithmetic and BRO1407 (SA1408) conditional expressions declare precedence: an operand that is an
/// operation of another family gets parentheses. Which ones follow StyleCop.
/// </summary>
internal static class Precedence
{
    public static readonly SyntaxKind[] Kinds =
    {
        SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, SyntaxKind.MultiplyExpression, SyntaxKind.DivideExpression,
        SyntaxKind.ModuloExpression, SyntaxKind.LeftShiftExpression, SyntaxKind.RightShiftExpression,
        SyntaxKind.LogicalAndExpression, SyntaxKind.LogicalOrExpression, SyntaxKind.AndPattern, SyntaxKind.OrPattern,
    };

    /// <summary>The operands of this operation that need parentheses, with the rule for each.</summary>
    public static IEnumerable<(SyntaxNode Operand, string Id)> GetFindings(SyntaxNode node)
    {
        var (left, right, op) = node switch
        {
            BinaryExpressionSyntax b => (b.Left, b.Right, b.OperatorToken),
            BinaryPatternSyntax p => ((SyntaxNode)p.Left, (SyntaxNode)p.Right, p.OperatorToken),
            _ => (null!, null!, default),
        };
        if (op.RawKind == 0)
        {
            yield break;
        }

        foreach (var operand in new[] { left, right })
        {
            var inner = Operator(operand);
            if (inner.RawKind == 0)
            {
                continue;
            }

            if (IsLogical(op))
            {
                // SA1408: '&&'/'and' and '||'/'or' don't mix without parentheses.
                if (IsLogical(inner) && !SameLogicalFamily(op, inner))
                {
                    yield return (operand, DiagnosticIds.ConditionalPrecedence);
                }
            }
            else if (operand is BinaryExpressionSyntax && !SameArithmeticFamily(op, inner))
            {
                // SA1407: any other operation inside '+ -', '* /', '%' or '<< >>'.
                yield return (operand, DiagnosticIds.ArithmeticPrecedence);
            }
        }
    }

    /// <summary>
    /// '(' before and ')' after each operand. Operands that start or end at the same place share one edit there
    /// ('((' / '))'), so nested findings are fixed in one pass.
    /// </summary>
    public static List<TextChange> GetChanges(IEnumerable<SyntaxNode> operands)
    {
        var opens = new Dictionary<int, int>();
        var closes = new Dictionary<int, int>();
        foreach (var operand in operands.Distinct())
        {
            opens[operand.SpanStart] = opens.TryGetValue(operand.SpanStart, out var o) ? o + 1 : 1;
            closes[operand.Span.End] = closes.TryGetValue(operand.Span.End, out var c) ? c + 1 : 1;
        }

        return opens.Keys.Union(closes.Keys).OrderBy(p => p)
            .Select(p => new TextChange(new TextSpan(p, 0), new string(')', closes.TryGetValue(p, out var c) ? c : 0) + new string('(', opens.TryGetValue(p, out var o) ? o : 0)))
            .ToList();
    }

    private static SyntaxToken Operator(SyntaxNode node) => node switch
    {
        BinaryExpressionSyntax b => b.OperatorToken,
        BinaryPatternSyntax p => p.OperatorToken,
        _ => default,
    };

    private static bool IsLogical(SyntaxToken token) => IsAnd(token) || IsOr(token);

    private static bool IsAnd(SyntaxToken token) => token.Kind() is SyntaxKind.AmpersandAmpersandToken or SyntaxKind.AndKeyword;

    private static bool IsOr(SyntaxToken token) => token.Kind() is SyntaxKind.BarBarToken or SyntaxKind.OrKeyword;

    private static bool SameLogicalFamily(SyntaxToken a, SyntaxToken b) => (IsAnd(a) && IsAnd(b)) || (IsOr(a) && IsOr(b));

    private static bool SameArithmeticFamily(SyntaxToken a, SyntaxToken b) =>
        (IsAdditive(a) && IsAdditive(b)) || (IsMultiplicative(a) && IsMultiplicative(b)) || (IsShift(a) && IsShift(b));

    private static bool IsAdditive(SyntaxToken token) => token.Kind() is SyntaxKind.PlusToken or SyntaxKind.MinusToken;

    private static bool IsMultiplicative(SyntaxToken token) => token.Kind() is SyntaxKind.AsteriskToken or SyntaxKind.SlashToken;

    private static bool IsShift(SyntaxToken token) => token.Kind() is SyntaxKind.LessThanLessThanToken or SyntaxKind.GreaterThanGreaterThanToken;
}
