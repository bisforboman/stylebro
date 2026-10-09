using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1312 (SA1300 for namespaces): the logic the analyzer and the rename share. A namespace part is renamed in every
/// declaration and reference at once, so the analyzer only reports a part whose rename can be complete: every part of
/// the namespace comes from this project's source, none of it is in generated code, and the project's root namespace
/// (resource names, new files) doesn't contain it.
/// </summary>
public static class NamespaceNames
{
    /// <summary>StyleCop's allowedNamespaceComponents: parts that keep their casing ('eBay', 'iOS').</summary>
    public const string AllowedKey = "stylebro_allowed_namespace_components";

    /// <summary>The diagnostic property with the namespace's full old name ('myCompany.data').</summary>
    public const string NamespaceKey = "Namespace";

    // Per tree, once: the naming analyzers ask for every rename candidate, and the header text was built each time (the
    // whole license comment of every Newtonsoft.Json file, for each of its fields).
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SyntaxTree, System.Runtime.CompilerServices.StrongBox<bool>> GeneratedTrees = new();

    /// <summary>The parts StyleCop's allowedNamespaceComponents setting keeps.</summary>
    public static HashSet<string> ReadAllowed(AnalyzerConfigOptions options)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        if (options.TryGetValue(AllowedKey, out var list))
        {
            allowed.UnionWith(list.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0));
        }

        return allowed;
    }

    /// <summary>
    /// The namespace a name in a namespace declaration or in code refers to ('data' in 'namespace myCompany.data' or in
    /// 'using myCompany.data;'), or null.
    /// </summary>
    public static INamespaceSymbol? GetNamespace(SemanticModel model, IdentifierNameSyntax name)
    {
        if (name.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>() is { } declaration && declaration.Name.Span.Contains(name.Span))
        {
            // The declared namespace is the last part; walk up once for every part to the right of this one.
            var symbol = model.GetDeclaredSymbol(declaration) as INamespaceSymbol;
            var right = declaration.Name.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Count(n => n.SpanStart > name.SpanStart);
            for (var i = 0; i < right && symbol is not null; i++)
            {
                symbol = symbol.ContainingNamespace;
            }

            return symbol;
        }

        var info = model.GetSymbolInfo(name);
        return info.Symbol as INamespaceSymbol ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] as INamespaceSymbol : null);
    }

    /// <summary>The namespace with this full name ('a.b'), or null.</summary>
    public static INamespaceSymbol? Find(INamespaceSymbol global, string fullName)
    {
        var current = global;
        foreach (var part in fullName.Split('.'))
        {
            current = current.GetMembers(part).OfType<INamespaceSymbol>().FirstOrDefault();
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// Whether the namespace and every namespace inside it come only from assemblies <paramref name="isOwn"/> accepts.
    /// A part that a library also declares ('myCompany.common' from a package) can't be renamed: the library keeps it.
    /// </summary>
    public static bool IsOnlyFrom(INamespaceSymbol ns, Func<IAssemblySymbol, bool> isOwn) =>
        ns.ConstituentNamespaces.All(c => c.ContainingAssembly is { } assembly && isOwn(assembly))
            && ns.GetNamespaceMembers().All(n => IsOnlyFrom(n, isOwn));

    /// <summary>
    /// Generated code (by file name or an auto-generated header, like Roslyn's own check): a resx designer file, a XAML or
    /// Razor '.g.cs'. A tool writes it again from the project's settings, so a rename there wouldn't last.
    /// </summary>
    public static bool IsGenerated(SyntaxTree tree) => GeneratedTrees.GetValue(tree, t => new(IsGeneratedCore(t))).Value;

    /// <summary>
    /// Whether a part of the type is generated, like a Razor component's half of a code-behind class: the markup uses
    /// the type's names, and a rename can't edit generated code (eShopOnWeb: a renamed field broke its '.razor' page).
    /// </summary>
    public static bool HasGeneratedPart(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.Any(r => IsGenerated(r.SyntaxTree));

    private static bool IsGeneratedCore(SyntaxTree tree)
    {
        var name = tree.FilePath.Substring(tree.FilePath.LastIndexOfAny(new[] { '/', '\\' }) + 1);
        if (name.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
            || new[] { ".designer.cs", ".generated.cs", ".g.cs", ".g.i.cs" }.Any(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var leading = tree.GetRoot().GetLeadingTrivia().ToFullString();
        return leading.IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0
            || leading.IndexOf("<autogenerated", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
