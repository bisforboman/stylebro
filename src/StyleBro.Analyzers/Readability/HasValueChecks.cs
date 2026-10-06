using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1148: 'x.HasValue' on a nullable value type becomes 'x is not null' and '!x.HasValue' becomes
/// 'x is null', or '!= null' / '== null', in the form BRO1133's stylebro_null_check_style asks for, so BRO1133 has
/// nothing left to change. The compiler emits the same HasValue call for both.
/// </summary>
internal static class HasValueChecks
{
    private const string HasValue = ".HasValue";

    /// <summary>
    /// The form '<paramref name="node"/>' (an 'x.HasValue') should take and the edits, or null when it isn't
    /// System.Nullable's HasValue or is skipped: a parent that would bind the new form differently (the same places
    /// BRO1133 skips), 'nameof', comments or line breaks in the replaced text, a pattern in an expression tree or before
    /// C# 7/9, and a '!=' that would call a user-defined operator (Guid's lifted '==').
    /// </summary>
    public static (string Form, ImmutableArray<TextChange> Changes)? GetFix(MemberAccessExpressionSyntax node, SemanticModel model, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        if (!node.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            || node.Name is not IdentifierNameSyntax { Identifier.ValueText: "HasValue" }
            || node.Span.End - node.Expression.Span.End != HasValue.Length)
        {
            return null;
        }

        var negated = node.Parent.IsKind(SyntaxKind.LogicalNotExpression);
        var target = negated ? (ExpressionSyntax)node.Parent! : node;

        // BRO1405 removes parentheses around 'x.HasValue' (but not around the null check that replaces it): judge the place
        // they leave it in, or the result would depend on which fix 'dotnet format' runs first.
        var judged = !negated && target.Parent is ParenthesizedExpressionSyntax parentheses
            && Severities.IsOn(model.Compilation.Options, node.SyntaxTree, DiagnosticIds.UnnecessaryParentheses, cancellationToken)
            ? parentheses
            : target;
        var pattern = NullChecks.PrefersPattern(options);
        if ((negated && node.SpanStart - target.SpanStart != 1)
            || !NullChecks.IsLooseParent(judged)
            || target.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } }
            || (pattern && ((CSharpParseOptions)node.SyntaxTree.Options).LanguageVersion < (negated ? LanguageVersion.CSharp7 : LanguageVersion.CSharp9))
            || model.GetSymbolInfo(node, cancellationToken).Symbol is not IPropertySymbol { ContainingType.OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
        {
            return null;
        }

        var form = (pattern, negated) switch
        {
            (true, true) => "is null",
            (true, false) => "is not null",
            (false, true) => "== null",
            (false, false) => "!= null",
        };
        if (pattern
            ? NullChecks.IsInExpressionTree(target, model, cancellationToken)
            : !IsBuiltIn(model, target, node.Expression, negated, cancellationToken))
        {
            return null;
        }

        var replace = new TextChange(TextSpan.FromBounds(node.Expression.Span.End, node.Span.End), " " + form);
        return (form, negated
            ? ImmutableArray.Create(new TextChange(new TextSpan(target.SpanStart, 1), string.Empty), replace)
            : ImmutableArray.Create(replace));
    }

    // 'x != null' on a nullable value type calls the underlying type's operator, lifted, when it has one (Guid): harmless
    // at run time, but a banned-API list may forbid it (BRO1133 skips the same).
    private static bool IsBuiltIn(SemanticModel model, ExpressionSyntax target, ExpressionSyntax receiver, bool negated, CancellationToken cancellationToken)
    {
        var replacement = SyntaxFactory.BinaryExpression(
            negated ? SyntaxKind.EqualsExpression : SyntaxKind.NotEqualsExpression,
            receiver.WithoutTrivia(),
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        return Speculation.SymbolAfterReplacing(model, target, replacement, cancellationToken) is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator };
    }
}
