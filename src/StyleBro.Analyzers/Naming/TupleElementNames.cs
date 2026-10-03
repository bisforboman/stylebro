using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1311 (SA1316): element names in tuple types use the configured casing, PascalCase by default ('(int Count, string
/// Name)'). Like StyleCop only the first letter counts, '_' is fine, and names a member has to take from a library member
/// it overrides or implements aren't reported. An override of a member in the solution is reported: the fix renames
/// both.
/// </summary>
internal static class TupleElementNames
{
    public const string CasingKey = "stylebro_tuple_element_name_casing";
    public const string OldNameKey = "OldName";

    /// <summary>Whether names are camelCase ('camelCase'); PascalCase otherwise, StyleCop's default.</summary>
    public static bool IsCamelCase(AnalyzerConfigOptions options) =>
        options.TryGetValue(CasingKey, out var value) && value.Trim().Equals("camelCase", StringComparison.OrdinalIgnoreCase);

    /// <summary>The name with its first letter in the right case, or null when it's right already (or can't be fixed).</summary>
    public static string? GetNewName(string name, bool camelCase)
    {
        if (name.Length == 0 || name == "_" || !char.IsLetter(name[0]) || char.IsLower(name[0]) == camelCase)
        {
            return null;
        }

        var newName = (camelCase ? char.ToLowerInvariant(name[0]) : char.ToUpperInvariant(name[0])) + name.Substring(1);
        return SyntaxFacts.GetKeywordKind(newName) == SyntaxKind.None && SyntaxFacts.GetContextualKeywordKind(newName) == SyntaxKind.None
            ? newName
            : null;
    }

    /// <summary>
    /// Whether the element is in the signature of a member that overrides or implements a library member whose own
    /// declaration names the tuple elements: the names have to match (CS8139), and the library can't be renamed (StyleCop's
    /// CanTupleElementNameBeChanged). 'IComparer&lt;(int a, int b)&gt;.Compare' takes its names from the type argument,
    /// which is in the solution, so that one can be renamed.
    /// </summary>
    public static bool InheritsNames(TupleElementSyntax element, SemanticModel model, CancellationToken cancellationToken)
    {
        return element.FirstAncestorOrSelf<MemberDeclarationSyntax>() is { } member
            && IsInSignature(element, member)
            && model.GetDeclaredSymbol(member, cancellationToken) is { } symbol
            && CamelCaseNamingAnalyzer.GetBaseMembers(symbol).Any(b => !b.Locations.Any(l => l.IsInSource) && HasTupleNames(b.OriginalDefinition));
    }

    /// <summary>Whether the node is in the member's return type, parameter types or property/event type, not its body.</summary>
    public static bool IsInSignature(SyntaxNode node, MemberDeclarationSyntax member)
    {
        TypeSyntax? type = member switch
        {
            MethodDeclarationSyntax m => m.ReturnType,
            BasePropertyDeclarationSyntax p => p.Type,
            _ => null,
        };
        var parameters = member switch
        {
            BaseMethodDeclarationSyntax m => m.ParameterList,
            IndexerDeclarationSyntax i => (BaseParameterListSyntax)i.ParameterList,
            _ => null,
        };
        return (type is not null && type.Span.Contains(node.Span)) || (parameters is not null && parameters.Span.Contains(node.Span));
    }

    /// <summary>Whether a member's own signature (before type arguments are filled in) names tuple elements.</summary>
    private static bool HasTupleNames(ISymbol member) => member switch
    {
        IMethodSymbol method => HasTupleNames(method.ReturnType) || method.Parameters.Any(p => HasTupleNames(p.Type)),
        IPropertySymbol property => HasTupleNames(property.Type) || property.Parameters.Any(p => HasTupleNames(p.Type)),
        IEventSymbol e => HasTupleNames(e.Type),
        _ => false,
    };

    private static bool HasTupleNames(ITypeSymbol type) => type switch
    {
        INamedTypeSymbol { IsTupleType: true } tuple => tuple.TupleElements.Any(e => e.IsExplicitlyNamedTupleElement || HasTupleNames(e.Type)),
        INamedTypeSymbol named => named.TypeArguments.Any(HasTupleNames),
        IArrayTypeSymbol array => HasTupleNames(array.ElementType),
        _ => false,
    };
}
