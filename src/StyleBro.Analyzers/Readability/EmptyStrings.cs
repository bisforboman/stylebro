using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1106, used by both the analyzer and the code fix.</summary>
internal static class EmptyStrings
{
    /// <summary>
    /// An empty regular or verbatim string literal ("" or @"") outside a context that requires a constant.
    /// 'string.Empty' is not a constant, so attribute arguments, parameter defaults, case labels, patterns and
    /// const declarations keep "". Interpolated ($""), UTF-8 (""u8) and raw literals are different kinds of literal.
    /// </summary>
    public static bool ShouldReplace(LiteralExpressionSyntax literal)
    {
        return literal.IsKind(SyntaxKind.StringLiteralExpression)
            && literal.Token.IsKind(SyntaxKind.StringLiteralToken)
            && literal.Token.ValueText.Length == 0
            && !IsInConstantContext(literal);
    }

    private static bool IsInConstantContext(SyntaxNode node)
    {
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case AttributeArgumentSyntax:
                case CaseSwitchLabelSyntax:
                case ConstantPatternSyntax:
                case EqualsValueClauseSyntax { Parent: ParameterSyntax }:
                    return true;

                case VariableDeclarationSyntax declaration:
                    return declaration.Parent switch
                    {
                        FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ConstKeyword),
                        LocalDeclarationStatementSyntax local => local.IsConst,
                        _ => false,
                    };

                case StatementSyntax:
                case MemberDeclarationSyntax:
                    return false;
            }
        }

        return false;
    }
}
