using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// Parameter names a framework binds by name, which BRO1302/BRO1310/BRO1313 never rename (owner's decision 2026-10-17,
/// trial 29): Harmony injects a patch method's parameters by their names ('__instance', '__result', '__state', '___field',
/// or the patched method's own parameter names), so a rename compiles and the patch silently stops working. A name that
/// starts with '__' is that convention wherever it appears (Harmony used as a package: the strings it matches aren't in
/// the solution); every parameter of a Harmony patch method is kept too. Syntax only.
/// </summary>
internal static class BoundParameterNames
{
    // Harmony's patch methods found by name inside a patch class (annotations; https://harmony.pardeike.net/articles/annotations.html).
    private static readonly string[] PatchMethodNames = ["Prefix", "Postfix", "Transpiler", "Finalizer", "Prepare", "Cleanup", "TargetMethod", "TargetMethods"];

    /// <summary>Whether <paramref name="parameter"/>'s name is bound by name at run time.</summary>
    public static bool IsBound(ParameterSyntax parameter)
    {
        return parameter.Identifier.ValueText.StartsWith("__", StringComparison.Ordinal)
            || (parameter.Parent?.Parent is MethodDeclarationSyntax method && IsHarmonyPatch(method));
    }

    /// <summary>
    /// A Harmony patch method: a Harmony attribute on it or on one of its parameters ('[HarmonyPrefix]', '[HarmonyPatch]',
    /// '[HarmonyArgument]', ...), or one of Harmony's patch method names in a type with a Harmony attribute.
    /// </summary>
    private static bool IsHarmonyPatch(MethodDeclarationSyntax method)
    {
        if (HasHarmonyAttribute(method.AttributeLists))
        {
            return true;
        }

        foreach (var parameter in method.ParameterList.Parameters)
        {
            if (HasHarmonyAttribute(parameter.AttributeLists))
            {
                return true;
            }
        }

        return Array.IndexOf(PatchMethodNames, method.Identifier.ValueText) >= 0
            && method.Parent is TypeDeclarationSyntax type
            && HasHarmonyAttribute(type.AttributeLists);
    }

    private static bool HasHarmonyAttribute(Microsoft.CodeAnalysis.SyntaxList<AttributeListSyntax> lists)
    {
        foreach (var list in lists)
        {
            foreach (var attribute in list.Attributes)
            {
                var name = attribute.Name switch
                {
                    QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                    AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
                    SimpleNameSyntax simple => simple.Identifier.ValueText,
                    _ => string.Empty,
                };
                if (name.StartsWith("Harmony", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
