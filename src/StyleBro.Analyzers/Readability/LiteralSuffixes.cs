using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Which integer suffixes BRO1135 checks (stylebro_upper_case_literal_suffixes).</summary>
internal enum SuffixScope
{
    /// <summary>Every suffix letter.</summary>
    All,

    /// <summary>Only 'l'.</summary>
    LOnly,

    /// <summary>Only a suffix that is just 'l', like Sonar's S818.</summary>
    LoneL,
}

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
    /// aren't checked), and in hex literals the suffix letters can't be digits. <paramref name="scope"/>
    /// (stylebro_upper_case_literal_suffixes): with l_only only an 'l' is reported and upper-cased, 'u' stays as written;
    /// with lone_l (Sonar's S818) only a suffix that is just 'l' ('1l', not '1ul' or '1lu').
    /// </summary>
    public static string? GetUpperCaseSuffix(SyntaxToken literal, SuffixScope scope)
    {
        var text = literal.Text;
        var start = text.Length;
        while (start > 0 && text[start - 1] is 'u' or 'U' or 'l' or 'L')
        {
            start--;
        }

        var suffix = text.Substring(start);
        if (scope == SuffixScope.LoneL)
        {
            return suffix == "l" ? text.Substring(0, start) + "L" : null;
        }

        if (scope == SuffixScope.LOnly)
        {
            return suffix.IndexOf('l') >= 0 ? text.Substring(0, start) + suffix.Replace('l', 'L') : null;
        }

        return suffix.Any(char.IsLower) ? text.Substring(0, start) + suffix.ToUpperInvariant() : null;
    }

    /// <summary>The stylebro_upper_case_literal_suffixes setting: all (default), l_only or lone_l.</summary>
    public static SuffixScope GetScope(AnalyzerConfigOptions options) =>
        !options.TryGetValue("stylebro_upper_case_literal_suffixes", out var value) ? SuffixScope.All
        : value.Trim().Equals("l_only", StringComparison.OrdinalIgnoreCase) ? SuffixScope.LOnly
        : value.Trim().Equals("lone_l", StringComparison.OrdinalIgnoreCase) ? SuffixScope.LoneL
        : SuffixScope.All;

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
            || SymbolEqualityComparer.Default.Equals(target, model.GetTypeInfo(literal, cancellationToken).Type)
            || SuffixedText(literal.Token, negative, suffix, constant.Value, model, cancellationToken) is not { } newText)
        {
            // Redundant casts are IDE0004's; no constant means a compile error like (ulong)-1.
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

    /// <summary>
    /// A literal that converts to another numeric type (BRO1123's tuple literal from <c>new ValueTuple&lt;double, ...&gt;(0, ...)</c>)
    /// written with that type's suffix, as BRO1122 would write the cast (<c>0D</c>, not <c>(double)0</c> for BRO1122's next
    /// run, trial 31); null when BRO1122 is off, the type has no suffix or the suffixed literal's value would differ.
    /// </summary>
    public static string? GetSuffixedLiteral(LiteralExpressionSyntax literal, ITypeSymbol target, SemanticModel model, CancellationToken cancellationToken) =>
        literal.Token.IsKind(SyntaxKind.NumericLiteralToken) && Suffix(target.SpecialType) is { } suffix
        && model.GetOperation(literal, cancellationToken)?.Parent is IConversionOperation { ConstantValue: { HasValue: true, Value: { } constant } }
        && Severities.IsOn(model.Compilation.Options, literal.SyntaxTree, DiagnosticIds.LiteralSuffix, cancellationToken)
            ? SuffixedText(literal.Token, negative: false, suffix, constant, model, cancellationToken)
            : null;

    /// <summary>
    /// The fix's edit: <see cref="GetChange"/>, and when BRO1405 is on, without the parentheses around the cast that
    /// the new literal leaves unnecessary (<c>((double)0).ToString()</c> -> <c>0D.ToString()</c>, not <c>(0D)</c> for
    /// BRO1405's next run). Those it would keep stay (<c>(-1L).ToString()</c>).
    /// </summary>
    public static TextChange? GetFixChange(CastExpressionSyntax cast, SemanticModel model, SourceText text, CancellationToken cancellationToken)
    {
        var change = GetChange(cast, model, cancellationToken);
        if (change is null || cast.Parent is not ParenthesizedExpressionSyntax parenthesized
            || !Severities.IsOn(model.Compilation.Options, cast.SyntaxTree, DiagnosticIds.UnnecessaryParentheses, cancellationToken))
        {
            return change;
        }

        var newText = text.WithChanges(change.Value);
        if (cast.SyntaxTree.WithChangedText(newText).GetRoot(cancellationToken).FindToken(parenthesized.SpanStart).Parent
                is not ParenthesizedExpressionSyntax newParenthesized
            || !Maintainability.Parentheses.IsUnnecessary(newParenthesized, newText))
        {
            return change;
        }

        var edits = Maintainability.Parentheses.GetChanges(new[] { newParenthesized }, newText);
        var length = newParenthesized.Span.Length + edits.Sum(e => e.NewText!.Length - e.Span.Length);
        return new TextChange(parenthesized.Span, newText.WithChanges(edits).ToString(new TextSpan(parenthesized.SpanStart, length)));
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

    private static string? Suffix(SpecialType type) => type switch
    {
        SpecialType.System_UInt32 => "U",
        SpecialType.System_Int64 => "L",
        SpecialType.System_UInt64 => "UL",
        SpecialType.System_Single => "F",
        SpecialType.System_Double => "D",
        SpecialType.System_Decimal => "M",
        _ => null,
    };

    /// <summary>
    /// The literal's digits with <paramref name="suffix"/>, or null when that literal (negated with <paramref name="negative"/>)
    /// isn't exactly <paramref name="constant"/>: <c>(decimal)0.1234567890123456789</c> rounds through double, and
    /// <c>(float)</c> of a double literal can round differently than a float literal.
    /// </summary>
    private static string? SuffixedText(SyntaxToken literal, bool negative, string suffix, object constant, SemanticModel model, CancellationToken cancellationToken)
    {
        // With BRO1135 off the code's unsigned suffixes may be lower case (init turns it off for '0u'): 'u' and 'ul' then, so
        // BRO1122 doesn't bring in upper case nobody asked for. 'L' stays: a lower-case 'l' alone looks like '1' (CS0078).
        if (suffix.Contains('U') && !Severities.IsOn(model.Compilation.Options, literal.SyntaxTree!, DiagnosticIds.LiteralSuffixCase, cancellationToken))
        {
            suffix = suffix.ToLowerInvariant();
        }

        var newText = StripSuffix(literal.Text) + suffix;
        var token = SyntaxFactory.ParseToken(newText);
        if (!token.IsKind(SyntaxKind.NumericLiteralToken) || token.Text != newText || token.ContainsDiagnostics || token.Value is null)
        {
            return null;
        }

        var value = negative ? Negate(token.Value) : token.Value;
        return value is null || value.GetType() != constant.GetType() || !value.Equals(constant)
            || (value is decimal m && !decimal.GetBits(m).SequenceEqual(decimal.GetBits((decimal)constant)))
            ? null // Decimals also compare their scale: (decimal)1.50 is 1.5, but 1.50M prints as "1.50".
            : newText;
    }

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
