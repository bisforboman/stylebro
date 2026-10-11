using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// The SDK's 'var' preference (IDE0007/IDE0008: csharp_style_var_for_built_in_types, _when_type_is_apparent,
/// _elsewhere) for the declarations BRO1142 writes when it splits <c>bool a = true, b = true;</c>. Splitting into explicit
/// types where the repository enforces 'var' made IDE0007 report every new declaration (spreadcheetah: 32 warnings).
/// </summary>
internal static class VarPreference
{
    /// <summary>
    /// Per variable of <paramref name="declaration"/>: whether its own declaration should say 'var' (true) or keep the type
    /// (false). Null when the preference asks for 'var' where it can't be written with the same meaning (no initializer,
    /// <see langword="null"/>, a target-typed expression, another inferred type): then the declaration isn't split. The
    /// semantic model is asked for only when some variable wants 'var'.
    /// </summary>
    public static bool[]? GetUseVar(LocalDeclarationStatementSyntax declaration, AnalyzerConfigOptions options, CompilationOptions? compilationOptions, Func<SemanticModel?> getModel, CancellationToken cancellationToken)
    {
        var type = declaration.Declaration.Type;
        var variables = declaration.Declaration.Variables;
        var wants = type.IsVar || declaration.IsConst || type is RefTypeSyntax
            ? new bool[variables.Count]
            : variables.Select(v => Prefers(options, Key(type, v.Initializer?.Value), compilationOptions, declaration.SyntaxTree, cancellationToken)).ToArray();
        if (!wants.Any(w => w))
        {
            return wants;
        }

        if (getModel() is not { } model || model.GetTypeInfo(type, cancellationToken).Type is not { TypeKind: not TypeKind.Error } declared)
        {
            return null;
        }

        for (var i = 0; i < variables.Count; i++)
        {
            if (wants[i] && !InfersTheSameType(variables[i].Initializer?.Value, declared, model, cancellationToken))
            {
                return null;
            }
        }

        return wants;
    }

    // Like the SDK: a built-in type first, then a type the initializer makes apparent, then everything else.
    private static string Key(TypeSyntax type, ExpressionSyntax? value) =>
        type is PredefinedTypeSyntax ? "csharp_style_var_for_built_in_types"
        : value is ObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax or CastExpressionSyntax
            or BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } ? "csharp_style_var_when_type_is_apparent"
        : "csharp_style_var_elsewhere";

    // 'true' with a severity that reports (or IDE0007 turned on by its own severity key).
    private static bool Prefers(AnalyzerConfigOptions options, string key, CompilationOptions? compilationOptions, SyntaxTree tree, CancellationToken cancellationToken)
    {
        if (!options.TryGetValue(key, out var value) || value.Split(':') is not { } parts || parts[0].Trim() != "true")
        {
            return false;
        }

        return (parts.Length > 1 && parts[1].Trim() is "suggestion" or "warning" or "error")
            || Severities.IsOn(compilationOptions, tree, "IDE0007", cancellationToken, enabledByDefault: false);
    }

    private static bool InfersTheSameType(ExpressionSyntax? value, ITypeSymbol declared, SemanticModel model, CancellationToken cancellationToken) =>
        value is not null and not InitializerExpressionSyntax and not ImplicitObjectCreationExpressionSyntax
            and not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression }
        && model.GetTypeInfo(value, cancellationToken).Type is { } inferred
        && SymbolEqualityComparer.Default.Equals(inferred, declared);
}
