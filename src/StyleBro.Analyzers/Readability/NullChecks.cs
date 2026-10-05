using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1133: null checks written as 'x == null' / 'x != null' or as 'x is null' / 'x is not null',
/// whichever stylebro_null_check_style asks for. Only checks that mean the same in both forms are reported: no
/// user-defined '==' is involved (the core library's are null-safe), the operand can be null, and the new form compiles
/// there (no patterns in expression trees, 'is not' needs C# 9).
/// </summary>
internal static class NullChecks
{
    /// <summary>pattern_matching ('x is null', the default) or equality_operator ('x == null').</summary>
    public const string StyleKey = "stylebro_null_check_style";

    /// <summary>
    /// The form <paramref name="node"/> should take ("is null", "== null", ...) and the edits that make it so, or null when
    /// it is no null check in the other form or is skipped. <paramref name="languageVersion"/>: the lowest C# version the
    /// file is compiled with (the fix knows its other copies); by default the tree's own.
    /// </summary>
    public static (string Form, ImmutableArray<TextChange> Changes)? GetFix(
        ExpressionSyntax node,
        SemanticModel model,
        AnalyzerConfigOptions options,
        CancellationToken cancellationToken,
        LanguageVersion? languageVersion = null)
    {
        var pattern = PrefersPattern(options);
        return node switch
        {
            BinaryExpressionSyntax binary when pattern => ToPattern(binary, model, cancellationToken, languageVersion ?? ((CSharpParseOptions)binary.SyntaxTree.Options).LanguageVersion),
            IsPatternExpressionSyntax isPattern when !pattern => ToEquality(isPattern, model, cancellationToken),
            _ => null,
        };
    }

    internal static bool PrefersPattern(AnalyzerConfigOptions options) =>
        !options.TryGetValue(StyleKey, out var value) || value.Split(':')[0].Trim().ToLowerInvariant() != "equality_operator";

    // Places where 'x == null' binds the same as 'x is null' did: '==' binds looser than 'is', so no operator of the
    // same or a tighter level may take the check as an operand ('b == x is null' would become '(b == x) == null'). BRO1148
    // asks the same for the 'x.HasValue' it replaces.
    internal static bool IsLooseParent(ExpressionSyntax isPattern) =>
        isPattern.Parent is ParenthesizedExpressionSyntax or ArgumentSyntax or EqualsValueClauseSyntax or ArrowExpressionClauseSyntax
            or ReturnStatementSyntax or YieldStatementSyntax or IfStatementSyntax or WhileStatementSyntax or DoStatementSyntax
            or ForStatementSyntax or LambdaExpressionSyntax or WhenClauseSyntax or InterpolationSyntax or InitializerExpressionSyntax
            or AnonymousObjectMemberDeclaratorSyntax or SwitchExpressionArmSyntax or ConditionalExpressionSyntax or AssignmentExpressionSyntax
        || isPattern.Parent.IsKind(SyntaxKind.LogicalAndExpression) || isPattern.Parent.IsKind(SyntaxKind.LogicalOrExpression)
        || isPattern.Parent.IsKind(SyntaxKind.BitwiseAndExpression) || isPattern.Parent.IsKind(SyntaxKind.BitwiseOrExpression)
        || isPattern.Parent.IsKind(SyntaxKind.ExclusiveOrExpression);

    // Patterns aren't allowed in expression trees (CS8122): a lambda, or a query clause, converted to an Expression.
    internal static bool IsInExpressionTree(SyntaxNode node, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!node.Ancestors().Any(a => a is AnonymousFunctionExpressionSyntax or QueryExpressionSyntax))
        {
            return false;
        }

        for (var operation = model.GetOperation(node, cancellationToken); operation is not null; operation = operation.Parent)
        {
            if (operation is IAnonymousFunctionOperation
                && operation.Parent?.Type?.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions")
            {
                return true;
            }
        }

        return false;
    }

    // 'x == null' -> 'x is null', 'x != null' -> 'x is not null', 'null == x' -> 'x is null'.
    private static (string, ImmutableArray<TextChange>)? ToPattern(BinaryExpressionSyntax binary, SemanticModel model, CancellationToken cancellationToken, LanguageVersion languageVersion)
    {
        var not = binary.IsKind(SyntaxKind.NotEqualsExpression);
        if (!not && !binary.IsKind(SyntaxKind.EqualsExpression))
        {
            return null;
        }

        var nullOnLeft = binary.Left.IsKind(SyntaxKind.NullLiteralExpression);
        var operand = nullOnLeft ? binary.Right : binary.Left;
        if ((nullOnLeft ? binary.Left : binary.Right) is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }
            || languageVersion < (not ? LanguageVersion.CSharp9 : LanguageVersion.CSharp7)
            || !IsSimpleOperand(operand)
            || !MeansTheSame(model.GetSymbolInfo(binary, cancellationToken).Symbol, model.GetTypeInfo(operand, cancellationToken).Type, model)
            || IsInExpressionTree(binary, model, cancellationToken))
        {
            return null;
        }

        var form = not ? "is not null" : "is null";
        if (!nullOnLeft)
        {
            return (form, ImmutableArray.Create(new TextChange(binary.OperatorToken.Span, not ? "is not" : "is")));
        }

        // 'null == x': the literal and the operator go, 'is null' follows the operand. Comments there would be lost.
        var removed = TextSpan.FromBounds(binary.SpanStart, operand.SpanStart);
        foreach (var trivia in binary.DescendantTrivia(removed))
        {
            if (removed.Contains(trivia.Span) && !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return null;
            }
        }

        return (form, ImmutableArray.Create(new TextChange(removed, string.Empty), new TextChange(new TextSpan(operand.Span.End, 0), " " + form)));
    }

    // 'x is null' -> 'x == null', 'x is not null' -> 'x != null'.
    private static (string, ImmutableArray<TextChange>)? ToEquality(IsPatternExpressionSyntax isPattern, SemanticModel model, CancellationToken cancellationToken)
    {
        var (not, constant) = isPattern.Pattern switch
        {
            ConstantPatternSyntax c => (false, c),
            UnaryPatternSyntax { Pattern: ConstantPatternSyntax c } => (true, c),
            _ => (false, null),
        };
        if (constant?.Expression.IsKind(SyntaxKind.NullLiteralExpression) != true
            || !IsLooseParent(isPattern)
            || (isPattern.Pattern is UnaryPatternSyntax unary && !IsSingleSpace(isPattern.IsKeyword, unary.OperatorToken)))
        {
            return null;
        }

        var replacement = SyntaxFactory.BinaryExpression(
            not ? SyntaxKind.NotEqualsExpression : SyntaxKind.EqualsExpression,
            isPattern.Expression.WithoutTrivia(),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        // On a nullable value type, '==' would name the underlying type's operator (lifted) where the code named none:
        // harmless at run time, but a banned-API list may forbid it (Jellyfin bans Guid's '==').
        var symbol = Speculation.SymbolAfterReplacing(model, isPattern, replacement, cancellationToken);
        var type = model.GetTypeInfo(isPattern.Expression, cancellationToken).Type;
        if (!MeansTheSame(symbol, type, model)
            || (type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && symbol is IMethodSymbol { MethodKind: not MethodKind.BuiltinOperator }))
        {
            return null;
        }

        var end = isPattern.Pattern is UnaryPatternSyntax u ? u.OperatorToken.Span.End : isPattern.IsKeyword.Span.End;
        return (not ? "!= null" : "== null", ImmutableArray.Create(new TextChange(TextSpan.FromBounds(isPattern.IsKeyword.SpanStart, end), not ? "!=" : "==")));
    }

    /// <summary>
    /// Whether the comparison with null calls no user code: a built-in operator, a core library one (string, Type,
    /// Delegate: all null-safe), or a lifted one on a nullable value type (comparing with null only reads HasValue).
    /// The operand must be able to hold null and not be dynamic (its '==' is picked at run time). Pointer comparisons
    /// have no operator symbol.
    /// </summary>
    private static bool MeansTheSame(ISymbol? symbol, ITypeSymbol? type, SemanticModel model)
    {
        if (symbol is not IMethodSymbol op
            || type is null
            || type.TypeKind == TypeKind.Dynamic
            || (type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T))
        {
            return false;
        }

        return op.MethodKind == MethodKind.BuiltinOperator
            || SymbolEqualityComparer.Default.Equals(op.ContainingAssembly, model.Compilation.ObjectType.ContainingAssembly)
            || (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && op.Parameters.Length == 2
                && op.Parameters[0].Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T);
    }

    // Operands that stay the operand of 'is' as they are: no operator in them binds looser than 'is'.
    private static bool IsSimpleOperand(ExpressionSyntax operand) =>
        operand is IdentifierNameSyntax or MemberAccessExpressionSyntax or ConditionalAccessExpressionSyntax
            or ElementAccessExpressionSyntax or InvocationExpressionSyntax or ParenthesizedExpressionSyntax or CastExpressionSyntax
            or ThisExpressionSyntax or AwaitExpressionSyntax
            or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression };

    // 'is not' becomes '!=' only when one space separates them (a line break or comment there would be lost).
    private static bool IsSingleSpace(SyntaxToken isKeyword, SyntaxToken notKeyword) =>
        notKeyword.SpanStart - isKeyword.Span.End == 1 && !isKeyword.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia) && notKeyword.LeadingTrivia.Count == 0;
}
