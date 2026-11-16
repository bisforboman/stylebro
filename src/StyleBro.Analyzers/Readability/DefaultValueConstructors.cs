using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1104, used by both the analyzer and the code fix: what 'new T()' for a value type becomes.</summary>
internal static class DefaultValueConstructors
{
    /// <summary>
    /// The replacement for a parameterless 'new T()' or 'new()' of a value type, or null when it must stay:
    /// with arguments or an initializer; for a type parameter or a struct with its own parameterless constructor
    /// (both would run code that 'default' skips); and as a statement on its own ('default(T);' doesn't compile).
    /// With csharp_prefer_simple_default_expression = true, the 'default' literal instead of 'default(T)' where it
    /// means the same; <paramref name="languageVersion"/> is the lowest C# version the code compiles with (null: the
    /// tree's own), the literal needs 7.1. <paramref name="numericIntPtr"/>: whether 'nint.Zero' compiles everywhere
    /// the file is compiled (null: in the model's compilation, see <see cref="HasNumericIntPtr"/>).
    /// </summary>
    public static string? GetReplacement(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel model,
        AnalyzerConfigOptions options,
        CancellationToken cancellationToken,
        LanguageVersion? languageVersion = null,
        bool? numericIntPtr = null)
    {
        var replacement = GetTypedReplacement(creation, model, numericIntPtr, cancellationToken);
        return replacement is not null
            && replacement.StartsWith("default(", System.StringComparison.Ordinal)
            && PrefersDefaultLiteral(options)
            && (languageVersion ?? ((CSharpParseOptions)creation.SyntaxTree.Options).LanguageVersion) >= LanguageVersion.CSharp7_1
            && BindsTheSameWithTheOthers(creation, model, numericIntPtr, cancellationToken)
            ? "default"
            : replacement;
    }

    /// <summary>
    /// Whether 'nint.Zero' compiles: from C# 11 on a runtime with numeric IntPtr (.NET 7 and later) 'nint' is
    /// 'System.IntPtr' itself, with its members; before, 'nint' has none.
    /// </summary>
    public static bool HasNumericIntPtr(Compilation compilation) =>
        compilation is CSharpCompilation { LanguageVersion: >= LanguageVersion.CSharp11 }
        && compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr);

    /// <summary>Asked only for a 'new nint()': the fix's answer for every copy of the file, else the model's compilation.</summary>
    private static bool IsNumericIntPtr(bool? numericIntPtr, SemanticModel model) => numericIntPtr ?? HasNumericIntPtr(model.Compilation);

    /// <summary>The replacement as StyleCop writes it: 'default(T)' or a named member.</summary>
    private static string? GetTypedReplacement(BaseObjectCreationExpressionSyntax creation, SemanticModel model, bool? numericIntPtr, CancellationToken cancellationToken)
    {
        if (creation.ArgumentList is { Arguments.Count: > 0 }
            || creation.Initializer is not null
            || creation.Parent is ExpressionStatementSyntax
            || !Trivia.IsBlank(creation, creation.Span))
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
        // compiles only with C# 11 on .NET 7+ (like StyleCop 1.3), so elsewhere 'nint'/'nuint' get 'default(nint)'. A
        // multi-targeted project analyzes the same file once per target framework: the fix passes whether every
        // copy's compilation qualifies, so all copies get the same text.
        if (type.TypeKind == TypeKind.Enum && GetZeroMember(type) is { } zero)
        {
            return typeText + "." + zero;
        }

        if (!IsParameterDefaultValue(creation)
            && GetEmptyMember(type) is { } empty
            && (typeText is not ("nint" or "nuint") || IsNumericIntPtr(numericIntPtr, model)))
        {
            return typeText + "." + empty;
        }

        return "default(" + typeText + ")";
    }

    /// <summary>
    /// Whether the creation, as the 'default' literal, still means the same, checked together with every other
    /// creation that would become 'default' and every 'default(T)' (IDE0034's) in the statement, initializer or
    /// expression body (<see cref="Speculation.GetContainer"/>): each alone may be fine where all together aren't
    /// ('M(default, default)' ambiguous, 'object o = b ? default : default' null). Including the 'default(T)'s makes one
    /// fix at a time give what Fix All gives. Each must also have no conversion ('object o = default' is null).
    /// </summary>
    private static bool BindsTheSameWithTheOthers(BaseObjectCreationExpressionSyntax creation, SemanticModel model, bool? numericIntPtr, CancellationToken cancellationToken)
    {
        if (Speculation.GetContainer(creation) is not { } container)
        {
            return false;
        }

        var group = new List<ExpressionSyntax> { creation };
        group.AddRange(container.DescendantNodes().OfType<ExpressionSyntax>().Where(e => e is DefaultExpressionSyntax
            || (e is BaseObjectCreationExpressionSyntax other && other != creation
                && GetTypedReplacement(other, model, numericIntPtr, cancellationToken)?.StartsWith("default(", System.StringComparison.Ordinal) == true)));
        return group.All(e => model.GetTypeInfo(e, cancellationToken) is var info
                && info.Type is not null
                && SymbolEqualityComparer.Default.Equals(info.Type, info.ConvertedType))
            && Speculation.BindTheSame(model, group, "default", cancellationToken);
    }

    /// <summary>The SDK's csharp_prefer_simple_default_expression is set to true ('true:warning' too).</summary>
    private static bool PrefersDefaultLiteral(AnalyzerConfigOptions options) =>
        options.TryGetValue("csharp_prefer_simple_default_expression", out var value)
        && value.Split(':')[0].Trim().Equals("true", System.StringComparison.OrdinalIgnoreCase);

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
