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
    /// An empty string outside a context that requires a constant: a regular or verbatim literal ("" or @""), a raw
    /// literal whose value is empty (only a multi-line one can be) or an interpolated string without content ($"",
    /// $@"", an empty interpolated raw string), like StyleCop 1.3. 'string.Empty' is not a constant, so attribute
    /// arguments, parameter defaults, case labels, patterns and const declarations keep theirs. A UTF-8 literal (""u8) is
    /// a different kind of literal. Neither form is replaced in an expression tree: a constant and a field access are
    /// different trees (owner's decision 2026-10-27, trial 58).
    /// </summary>
    public static bool ShouldReplace(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        return IsEmpty(expression, model, cancellationToken)
            && !IsInConstantContext(expression)
            && !NullChecks.IsInExpressionTreeOrTwin(expression, model, cancellationToken);
    }

    /// <summary>The expression as a diagnostic's message shows it: its first line.</summary>
    public static string Describe(ExpressionSyntax expression)
    {
        var text = expression.ToString();
        var lineBreak = text.IndexOfAny(new[] { '\r', '\n' });
        return lineBreak < 0 ? text : text.Substring(0, lineBreak) + "...";
    }

    private static bool IsEmpty(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken) => expression switch
    {
        LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.StringLiteralExpression)
            && literal.Token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken
            && literal.Token.ValueText.Length == 0,

        // As a FormattableString, an IFormattable or an interpolated string handler, 'string.Empty' wouldn't compile or
        // would mean something else (the same check as BRO1138's).
        InterpolatedStringExpressionSyntax interpolated => interpolated.Contents.All(c => c is InterpolatedStringTextSyntax { TextToken.ValueText.Length: 0 })
            && model.GetTypeInfo(interpolated, cancellationToken).ConvertedType?.SpecialType is SpecialType.System_String or SpecialType.System_Object,
        _ => false,
    };

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
            && model.GetSymbolInfo(access, cancellationToken).Symbol is IFieldSymbol { ContainingType.SpecialType: SpecialType.System_String }
            && !NullChecks.IsInExpressionTreeOrTwin(access, model, cancellationToken);
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
