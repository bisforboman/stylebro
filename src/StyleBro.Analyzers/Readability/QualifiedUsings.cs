using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1126 (StyleCop SA1135): a using directive inside a namespace names its namespace or type fully
/// (<c>using System.IO;</c>, not <c>using IO;</c> inside <c>namespace System</c>). Only relevant to teams that put
/// usings inside the namespace (StyleCop's SA1200 default).
/// </summary>
internal static class QualifiedUsings
{
    private static readonly SymbolDisplayFormat Qualified = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    /// <summary>
    /// The fully qualified name, or null when the directive is fine or can't be fixed safely. Like StyleCop: not for
    /// names starting with 'global::' or an alias, and not for a type in the namespace the directive is in. Unlike
    /// StyleCop, also not when an enclosing namespace has a member with the qualified name's first part (inside
    /// <c>namespace Foo.System</c>, <c>System.IO</c> would mean <c>Foo.System.IO</c>).
    /// </summary>
    public static string? GetQualifiedName(UsingDirectiveSyntax directive, SemanticModel model, CancellationToken cancellationToken)
    {
        // 'global::' counts as an alias.
        if (directive.Parent is not BaseNamespaceDeclarationSyntax container || directive.Name is not { } name || directive.ContainsDiagnostics
            || StartsWithAlias(name, model, cancellationToken))
        {
            return null;
        }

        var symbol = model.GetSymbolInfo(name, cancellationToken).Symbol;
        if (symbol is not (INamespaceSymbol or INamedTypeSymbol))
        {
            return null;
        }

        var qualified = symbol.ToDisplayString(Qualified);
        if (qualified == Canonical(name)
            || (symbol is INamedTypeSymbol && symbol.ContainingNamespace?.ToDisplayString(Qualified) == container.Name.ToString()))
        {
            return null;
        }

        // The qualified name's roots (and its type arguments' roots) must not be hidden by an enclosing namespace's members.
        var enclosing = model.GetDeclaredSymbol(container, cancellationToken) as INamespaceSymbol;
        var roots = Roots(symbol).ToList();
        for (var ns = enclosing; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
        {
            if (roots.Any(root => ns.GetMembers(root).Any()))
            {
                return null;
            }
        }

        return qualified;
    }

    /// <summary>The name as written, normalized like StyleCop's canonical string ('A.B&lt;int, C&gt;').</summary>
    private static string Canonical(TypeSyntax type)
    {
        var builder = new StringBuilder();
        Append(builder, type);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, TypeSyntax type)
    {
        switch (type)
        {
            case QualifiedNameSyntax qualified:
                Append(builder, qualified.Left);
                builder.Append('.');
                Append(builder, qualified.Right);
                break;
            case GenericNameSyntax generic:
                builder.Append(generic.Identifier.Text).Append('<');
                for (var i = 0; i < generic.TypeArgumentList.Arguments.Count; i++)
                {
                    builder.Append(i > 0 ? ", " : string.Empty);
                    Append(builder, generic.TypeArgumentList.Arguments[i]);
                }

                builder.Append('>');
                break;
            case IdentifierNameSyntax identifier:
                builder.Append(identifier.Identifier.Text);
                break;
            default:
                builder.Append(type.ToString());
                break;
        }
    }

    /// <summary>The first part of the qualified name of the symbol and of each type argument ("System" for System.IO).</summary>
    private static IEnumerable<string> Roots(ISymbol symbol)
    {
        var root = symbol;
        while (root.ContainingSymbol is { } parent && parent is not INamespaceSymbol { IsGlobalNamespace: true })
        {
            root = parent;
        }

        yield return root.Name;
        if (symbol is INamedTypeSymbol type)
        {
            foreach (var argument in type.TypeArguments.Where(a => a.SpecialType == SpecialType.None))
            {
                foreach (var name in Roots(argument))
                {
                    yield return name;
                }
            }
        }
    }

    private static bool StartsWithAlias(NameSyntax name, SemanticModel model, CancellationToken cancellationToken)
    {
        var first = name.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().FirstOrDefault();
        return first is not null && model.GetAliasInfo(first, cancellationToken) is not null;
    }
}
