using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// The naming rules leave names that other assemblies see alone unless <see cref="RenameKey"/> is true (owner's
/// decision, 2026-10-09): renaming a public member, type, parameter (named arguments) or namespace breaks every consumer,
/// and the rename can only follow references in the solution. Internal counts as not visible, InternalsVisibleTo too.
/// </summary>
internal static class PublicApi
{
    public const string RenameKey = "stylebro_rename_public_api";

    /// <summary>Whether the symbol may be renamed: it isn't visible outside the assembly, or the configuration allows it.</summary>
    public static bool CanRename(ISymbol symbol, AnalyzerConfigOptions options) => !IsVisible(symbol) || IsRenameAllowed(options);

    /// <summary>Whether <see cref="RenameKey"/> is true.</summary>
    public static bool IsRenameAllowed(AnalyzerConfigOptions options) =>
        options.TryGetValue(RenameKey, out var value) && value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether another assembly can see the symbol: it and every containing type are public, protected or protected
    /// internal. A parameter or type parameter counts as its member; a namespace when it or a namespace inside it declares
    /// such a type; locals, local functions and lambdas never.
    /// </summary>
    public static bool IsVisible(ISymbol symbol)
    {
        if (symbol is INamespaceSymbol ns)
        {
            return ns.GetTypeMembers().Any(IsVisible) || ns.GetNamespaceMembers().Any(IsVisible);
        }

        // Locals and lambdas have no accessibility, local functions are private: they fail the test below.
        var current = symbol is IParameterSymbol or ITypeParameterSymbol ? symbol.ContainingSymbol : symbol;
        if (current is null)
        {
            return false;
        }

        for (; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }
}
