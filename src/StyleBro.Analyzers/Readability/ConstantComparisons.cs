using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1103, used by both the analyzer and the code fix: which comparisons have a constant on the
/// left, and how to swap them.
/// </summary>
internal static class ConstantComparisons
{
    /// <summary>
    /// A comparison ('==', '!=', '&lt;', '&gt;', '&lt;=', '&gt;=') with a constant-like left operand and a
    /// non-constant right operand, using an operator that is safe to call with its operands reversed.
    /// </summary>
    public static bool ShouldSwap(BinaryExpressionSyntax binary, SemanticModel model, System.Threading.CancellationToken cancellationToken)
    {
        if (GetFlippedOperator(binary.Kind()) is null
            || !IsConstantLike(binary.Left, model, cancellationToken)
            || IsConstantLike(binary.Right, model, cancellationToken))
        {
            return false;
        }

        // Built-in operators are symmetric. So are the operators the .NET core library defines (string, decimal,
        // DateTime, Guid, ...). A user-defined operator might not be, and reversing its operands could even pick
        // a different overload, so those comparisons are left alone.
        if (model.GetSymbolInfo(binary, cancellationToken).Symbol is not IMethodSymbol op)
        {
            return false;
        }

        return op.MethodKind == MethodKind.BuiltinOperator
            || (op.MethodKind == MethodKind.UserDefinedOperator
                && SymbolEqualityComparer.Default.Equals(op.ContainingAssembly, model.Compilation.ObjectType.ContainingAssembly));
    }

    /// <summary>'1 == x' becomes 'x == 1', and '0 &lt; x' becomes 'x &gt; 0'. Layout and comments stay in place.</summary>
    public static BinaryExpressionSyntax Swap(BinaryExpressionSyntax binary)
    {
        var kind = binary.Kind();
        var flipped = GetFlippedOperator(kind)!.Value;
        var left = binary.Left;
        var right = binary.Right;

        // In '1 == 2 == x' the left operand is itself a comparison of the same precedence; moving it to the right
        // needs parentheses to keep the meaning: 'x == (1 == 2)'.
        ExpressionSyntax newRight = NeedsParentheses(left, kind)
            ? SyntaxFactory.ParenthesizedExpression(left.WithoutTrivia())
            : left.WithoutTrivia();

        return SyntaxFactory.BinaryExpression(
            flipped.ExpressionKind,
            right.WithTriviaFrom(left),
            SyntaxFactory.Token(flipped.TokenKind).WithTriviaFrom(binary.OperatorToken),
            newRight.WithTriviaFrom(right));
    }

    private static bool IsConstantLike(ExpressionSyntax expression, SemanticModel model, System.Threading.CancellationToken cancellationToken)
    {
        if (model.GetConstantValue(expression, cancellationToken).HasValue)
        {
            return true;
        }

        // Like StyleCop, static readonly fields such as 'string.Empty' count as constants.
        var inner = expression;
        while (inner is ParenthesizedExpressionSyntax parenthesized)
        {
            inner = parenthesized.Expression;
        }

        return model.GetSymbolInfo(inner, cancellationToken).Symbol is IFieldSymbol { IsStatic: true, IsReadOnly: true };
    }

    private static bool NeedsParentheses(ExpressionSyntax operand, SyntaxKind comparison)
    {
        var isEquality = comparison is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression;
        return operand.Kind() switch
        {
            SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression => isEquality,
            SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression
                or SyntaxKind.LessThanOrEqualExpression or SyntaxKind.GreaterThanOrEqualExpression
                or SyntaxKind.IsExpression or SyntaxKind.AsExpression or SyntaxKind.IsPatternExpression => !isEquality,
            _ => false,
        };
    }

    private static (SyntaxKind ExpressionKind, SyntaxKind TokenKind)? GetFlippedOperator(SyntaxKind kind)
    {
        return kind switch
        {
            SyntaxKind.EqualsExpression => (SyntaxKind.EqualsExpression, SyntaxKind.EqualsEqualsToken),
            SyntaxKind.NotEqualsExpression => (SyntaxKind.NotEqualsExpression, SyntaxKind.ExclamationEqualsToken),
            SyntaxKind.LessThanExpression => (SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken),
            SyntaxKind.GreaterThanExpression => (SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken),
            SyntaxKind.LessThanOrEqualExpression => (SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken),
            SyntaxKind.GreaterThanOrEqualExpression => (SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken),
            _ => null,
        };
    }
}
