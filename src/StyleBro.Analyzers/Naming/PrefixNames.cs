using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// Shared logic for BRO1304 (StyleCop SA1302: interface names begin with I) and BRO1305 (SA1314: type parameter names
/// begin with T). Like StyleCop, only the first letter is checked: 'Item' is a fine interface name, 'Type' a fine
/// type parameter name.
/// </summary>
internal static class PrefixNames
{
    /// <summary>'Shape' -> 'IShape', 'iShape' -> 'IShape', '_Shape' -> 'IShape'; null when it already begins with I.</summary>
    public static string? GetInterfaceName(string name) => GetName(name, 'I');

    /// <summary>'Item' -> 'TItem', 't' -> 'T', 'tKey' -> 'TKey', '_x' -> 'TX', '_TKey' -> 'TKey'; null when it already begins with T.</summary>
    public static string? GetTypeParameterName(string name) => GetName(name, 'T');

    /// <summary>
    /// Whether a type with the new name exists anywhere in the compilation's source. The analyzer also looks the new
    /// name up at the declaration (members, type parameters and imported types in scope), and the fix at every
    /// reference, so a rename never makes a name mean something else.
    /// </summary>
    public static bool IsTypeNameTaken(string newName, Compilation compilation) =>
        compilation.ContainsSymbolsWithName(newName, SymbolFilter.Type);

    private static string? GetName(string name, char prefix)
    {
        if (name.Length > 0 && name[0] == prefix)
        {
            return null;
        }

        // Leading underscores go first: '_TVertex' -> 'TVertex', not 'TTVertex' (trial 27, GraphSharp).
        var core = name.TrimStart('_');
        if (core.Length == 0 || !char.IsLetter(core[0]))
        {
            return null;
        }

        if (core[0] == prefix)
        {
            return Valid(core);
        }

        // A lower-case prefix letter that starts a word ('t', 'tKey', 'iShape') just gets capitalized.
        if (core[0] == char.ToLowerInvariant(prefix) && (core.Length == 1 || char.IsUpper(core[1])))
        {
            return Valid(prefix + core.Substring(1));
        }

        return Valid(prefix + char.ToUpperInvariant(core[0]).ToString() + core.Substring(1));
    }

    private static string? Valid(string name) =>
        SyntaxFacts.IsValidIdentifier(name) && SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : null;
}
