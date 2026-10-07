using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1106, used by both the analyzer and the code fix.</summary>
internal static class EmptyStrings
{
    /// <summary>The .editorconfig key: string_empty (default) or literal.</summary>
    public const string StyleKey = "stylebro_empty_string_style";

    /// <summary>Whether <see cref="StyleKey"/> asks for "" instead of 'string.Empty'.</summary>
    public static bool PrefersLiteral(AnalyzerConfigOptions options) =>
        options.TryGetValue(StyleKey, out var value) && value.Trim().Equals("literal", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>
    /// 'string.Empty' (also 'String.Empty', 'System.String.Empty') for the literal style: the member access binds to
    /// the field. Skipped inside 'nameof' ('nameof("")' doesn't compile) and with a comment inside the access.
    /// </summary>
    public static bool ShouldReplaceWithLiteral(MemberAccessExpressionSyntax access, SemanticModel model, CancellationToken cancellationToken)
    {
        return access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            && access.Name is IdentifierNameSyntax { Identifier.ValueText: "Empty" }
            && !access.DescendantTrivia().Any(t => access.Span.Contains(t.Span) && !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia))
            && !IsInNameof(access)
            && model.GetSymbolInfo(access, cancellationToken).Symbol is IFieldSymbol { ContainingType.SpecialType: SpecialType.System_String };
    }

    private static bool IsInNameof(SyntaxNode node)
    {
        for (var parent = node.Parent; parent is ExpressionSyntax or ArgumentSyntax or ArgumentListSyntax; parent = parent.Parent)
        {
            if (parent is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } })
            {
                return true;
            }
        }

        return false;
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
