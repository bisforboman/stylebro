using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1104, used by both the analyzer and the code fix: what 'new T()' for a value type becomes.</summary>
internal static class DefaultValueConstructors
{
    /// <summary>
    /// The replacement for a parameterless 'new T()' or 'new()' of a value type, or null when it must stay:
    /// with arguments or an initializer; for a type parameter or a struct with its own parameterless constructor
    /// (both would run code that 'default' skips); and as a statement on its own ('default(T);' doesn't compile).
    /// </summary>
    public static string? GetReplacement(BaseObjectCreationExpressionSyntax creation, SemanticModel model, CancellationToken cancellationToken)
    {
        if (creation.ArgumentList is { Arguments.Count: > 0 }
            || creation.Initializer is not null
            || creation.Parent is ExpressionStatementSyntax)
        {
            return null;
        }

        var type = model.GetTypeInfo(creation, cancellationToken).Type;
        if (type is null || !type.IsValueType || type.TypeKind is TypeKind.TypeParameter or TypeKind.Error)
        {
            return null;
        }

        if (model.GetSymbolInfo(creation, cancellationToken).Symbol is not IMethodSymbol { IsImplicitlyDeclared: true })
        {
            return null;
        }

        var typeText = creation is ObjectCreationExpressionSyntax explicitCreation
            ? explicitCreation.Type.ToString()
            : type.ToMinimalDisplayString(model, creation.SpanStart);

        // Like StyleCop: an enum's zero member, and the well-known "empty" members. Those members are not
        // constants, so a parameter's default value (which must be constant) gets 'default(T)' instead. 'nint.Zero'
        // compiles only with C# 11 on .NET 7+, and a multi-targeted project analyzes the same file once per target
        // framework: an answer that depended on it would differ between the copies. So 'nint'/'nuint' always get
        // 'default(nint)', which means the same everywhere.
        if (type.TypeKind == TypeKind.Enum && GetZeroMember(type) is { } zero)
        {
            return typeText + "." + zero;
        }

        if (!IsParameterDefaultValue(creation)
            && GetEmptyMember(type) is { } empty
            && typeText is not ("nint" or "nuint"))
        {
            return typeText + "." + empty;
        }

        return "default(" + typeText + ")";
    }

    private static string? GetZeroMember(ITypeSymbol enumType)
    {
        return enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && System.Convert.ToDecimal(f.ConstantValue, CultureInfo.InvariantCulture) == 0m)
            ?.Name;
    }

    private static string? GetEmptyMember(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_IntPtr or SpecialType.System_UIntPtr => "Zero",
            _ => type.ToDisplayString() switch
            {
                "System.Threading.CancellationToken" => "None",
                "System.Guid" => "Empty",
                _ => null,
            },
        };
    }

    private static bool IsParameterDefaultValue(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent is ParenthesizedExpressionSyntax)
        {
            parent = parent.Parent;
        }

        return parent is EqualsValueClauseSyntax { Parent: ParameterSyntax };
    }
}
