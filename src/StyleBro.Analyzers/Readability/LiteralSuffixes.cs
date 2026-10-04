using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1122 (StyleCop SA1139): <c>1L</c> instead of <c>(long)1</c>. Like StyleCop: casts to int, uint,
/// long, ulong, float, double and decimal of a numeric literal, optionally signed and parenthesized, that aren't
/// redundant (IDE0004's) and have a constant value. BRO1122 writes upper-case suffixes, which is what BRO1135 wants.
/// </summary>
internal static class LiteralSuffixes
{
    /// <summary>
    /// BRO1135: the numeric literal's text with its integer suffix ('u', 'l', 'ul', 'lu' in any case) in upper case, or
    /// null when it has none in lower case. Real literals never end in 'u' or 'l' ('f', 'd', 'm' and the exponent's 'e'
    /// aren't checked), and in hex literals the suffix letters can't be digits.
    /// </summary>
    public static string? GetUpperCaseSuffix(SyntaxToken literal)
    {
        var text = literal.Text;
        var start = text.Length;
        while (start > 0 && text[start - 1] is 'u' or 'U' or 'l' or 'L')
        {
            start--;
        }

        var suffix = text.Substring(start);
        return suffix.Any(char.IsLower) ? text.Substring(0, start) + suffix.ToUpperInvariant() : null;
    }

    /// <summary>
    /// The edit that replaces the cast with the suffixed literal, or null. Unlike StyleCop, the new literal must have
    /// exactly the cast's value, not just its type: <c>(decimal)0.1234567890123456789</c> rounds through double, and
    /// <c>(float)</c> of a double literal can round differently than a float literal.
    /// </summary>
    public static TextChange? GetChange(CastExpressionSyntax cast, SemanticModel model, CancellationToken cancellationToken)
    {
        if (cast.Type is not PredefinedTypeSyntax type || Suffix(type.Keyword.Kind()) is not { } suffix || cast.ContainsDiagnostics
            || cast.DescendantTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        var expression = WalkDownParentheses(cast.Expression);
        var negative = false;
        if (expression is PrefixUnaryExpressionSyntax unary)
        {
            if (!unary.IsKind(SyntaxKind.UnaryMinusExpression) && !unary.IsKind(SyntaxKind.UnaryPlusExpression))
            {
                return null;
            }

            negative = unary.IsKind(SyntaxKind.UnaryMinusExpression);
            expression = WalkDownParentheses(unary.Operand);
        }

        if (expression is not LiteralExpressionSyntax literal || !literal.Token.IsKind(SyntaxKind.NumericLiteralToken))
        {
            return null;
        }

        var target = model.GetTypeInfo(cast, cancellationToken).Type;
        var constant = model.GetConstantValue(cast, cancellationToken);
        if (target is null || !constant.HasValue || constant.Value is null
            || SymbolEqualityComparer.Default.Equals(target, model.GetTypeInfo(literal, cancellationToken).Type))
        {
            // Redundant casts are IDE0004's; no constant means a compile error like (ulong)-1.
            return null;
        }

        var newText = StripSuffix(literal.Token.Text) + suffix;
        var token = SyntaxFactory.ParseToken(newText);
        if (!token.IsKind(SyntaxKind.NumericLiteralToken) || token.Text != newText || token.ContainsDiagnostics || token.Value is null)
        {
            return null;
        }

        var value = negative ? Negate(token.Value) : token.Value;
        if (value is null || value.GetType() != constant.Value.GetType() || !value.Equals(constant.Value)
            || (value is decimal m && !decimal.GetBits(m).SequenceEqual(decimal.GetBits((decimal)constant.Value))))
        {
            // Decimals also compare their scale: (decimal)1.50 is 1.5, but 1.50M prints as "1.50".
            return null;
        }

        // 'a-(long)-1' would become 'a--1L'.
        var previous = cast.GetFirstToken().GetPreviousToken();
        var replacement = cast.Expression.ToString().Remove(literal.SpanStart - cast.Expression.SpanStart, literal.Span.Length)
            .Insert(literal.SpanStart - cast.Expression.SpanStart, newText);
        if (previous.Span.End == cast.SpanStart && (previous.IsKind(SyntaxKind.MinusToken) || previous.IsKind(SyntaxKind.PlusToken)
            || previous.IsKind(SyntaxKind.MinusMinusToken) || previous.IsKind(SyntaxKind.PlusPlusToken)) && replacement[0] is '-' or '+')
        {
            return null;
        }

        return new TextChange(cast.Span, replacement);
    }

    private static string? Suffix(SyntaxKind keyword) => keyword switch
    {
        SyntaxKind.IntKeyword => string.Empty,
        SyntaxKind.UIntKeyword => "U",
        SyntaxKind.LongKeyword => "L",
        SyntaxKind.ULongKeyword => "UL",
        SyntaxKind.FloatKeyword => "F",
        SyntaxKind.DoubleKeyword => "D",
        SyntaxKind.DecimalKeyword => "M",
        _ => null,
    };

    /// <summary>The literal without its suffix (StyleCop's StripLiteralSuffix: d/f are digits in hex literals).</summary>
    private static string StripSuffix(string text)
    {
        var hex = text.Length > 2 && (text[1] == 'x' || text[1] == 'X');
        var end = text.Length;
        while (end > 0 && (text[end - 1] is 'L' or 'U' or 'M' or 'l' or 'u' or 'm' || (!hex && text[end - 1] is 'D' or 'F' or 'd' or 'f')))
        {
            end--;
        }

        return text.Substring(0, end);
    }

    /// <summary>The negated value, or null where negating changes the type (unsigned) or overflows.</summary>
    private static object? Negate(object value) => value switch
    {
        int i when i != int.MinValue => -i,
        long l when l != long.MinValue => -l,
        float f => -f,
        double d => -d,
        decimal m => -m,
        _ => null,
    };

    private static ExpressionSyntax WalkDownParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }
}
