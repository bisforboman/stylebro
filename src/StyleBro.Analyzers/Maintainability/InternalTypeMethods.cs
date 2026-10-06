using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>
/// Shared logic for BRO1409: an ordinary method declared <see langword="public"/> in a type that can't be seen outside the assembly
/// gains nothing from <see langword="public"/>; <see langword="internal"/> says what it means. Deliberately narrow: reflection's defaults and
/// most reflection-based libraries see public members only, so everything that such code (or the compiler) looks for is
/// left alone. See docs/rules/BRO1409.md.
/// </summary>
internal static class InternalTypeMethods
{
    /// <summary>
    /// Names that the compiler or external code finds by convention (patterns, entry points, startup classes, middleware),
    /// and records' members.
    /// </summary>
    private static readonly HashSet<string> ConventionNames = new(System.StringComparer.Ordinal)
    {
        "Main", "Dispose", "DisposeAsync", "GetEnumerator", "GetAsyncEnumerator", "MoveNext", "MoveNextAsync",
        "GetAwaiter", "GetResult", "Add", "Deconstruct", "GetPinnableReference", "Configure", "ConfigureServices",
        "ConfigureContainer", "Invoke", "InvokeAsync", "PrintMembers", "Equals", "GetHashCode", "ToString",
    };

    private static readonly ConditionalWeakTable<Compilation, Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>> DerivedTypes = new();

    /// <summary>The <see langword="public"/> keyword to replace with <see langword="internal"/>, or null. Syntax checks first (cheap), then the symbol.</summary>
    public static SyntaxToken? GetPublicKeyword(MethodDeclarationSyntax method, SemanticModel model, CancellationToken cancellationToken)
    {
        var keyword = default(SyntaxToken);
        foreach (var modifier in method.Modifiers)
        {
            switch (modifier.Kind())
            {
                case SyntaxKind.PublicKeyword:
                    keyword = modifier;
                    break;

                case SyntaxKind.OverrideKeyword:
                case SyntaxKind.VirtualKeyword:
                case SyntaxKind.AbstractKeyword:
                case SyntaxKind.ExternKeyword:
                case SyntaxKind.PartialKeyword:
                    return null;
            }
        }

        if (!keyword.IsKind(SyntaxKind.PublicKeyword)
            || method.AttributeLists.Count > 0
            || method.Parent is not TypeDeclarationSyntax or InterfaceDeclarationSyntax
            || ConventionNames.Contains(method.Identifier.ValueText)
            || method.ContainsDiagnostics
            || HasConditionalDirectives(method)
            || !MayBeHidden(method))
        {
            return null;
        }

        if (model.GetDeclaredSymbol(method, cancellationToken) is not { MethodKind: MethodKind.Ordinary } symbol
            || symbol.ContainingType is not { } type
            || IsVisibleOutside(type)
            || HasAttributesOrForeignBase(type)
            || ImplementsInterfaceMember(symbol, model.Compilation, cancellationToken))
        {
            return null;
        }

        return keyword;
    }

    /// <summary>
    /// Syntax only: whether some enclosing type declaration doesn't say <see langword="public"/> or <see langword="protected"/> (no modifier:
    /// internal or private, or another part says it; the symbol decides). Skips the symbol work in public types.
    /// </summary>
    private static bool MayBeHidden(MethodDeclarationSyntax method)
    {
        foreach (var type in method.Ancestors().OfType<BaseTypeDeclarationSyntax>())
        {
            // '#if' in the header: the base list (interfaces) differs per target framework.
            if ((type.BaseList is { } list && HasConditionalDirectives(list)) || type.OpenBraceToken.LeadingTrivia.Any(IsConditional))
            {
                return false;
            }

            if (!type.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword))
                || type.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// '#if' and its kin: the code differs per target framework. '#region' and '#pragma' don't count (BRO1112/BRO1113
    /// remove regions in the same run; skipping them made a second run find more).
    /// </summary>
    private static bool HasConditionalDirectives(SyntaxNode node) =>
        node.ContainsDirectives && node.DescendantTrivia().Any(IsConditional);

    private static bool IsConditional(SyntaxTrivia trivia) =>
        trivia.Kind() is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia
            or SyntaxKind.EndIfDirectiveTrivia or SyntaxKind.DisabledTextTrivia;

    /// <summary>Whether code outside the assembly can see the type (it and every containing type public or protected).</summary>
    private static bool IsVisibleOutside(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            // File-local types are internal here.
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A type (or a containing type) with an attribute, or deriving from a type from a referenced assembly other than
    /// <see langword="object"/>/<c>ValueType</c>: frameworks call public methods of such types by convention (controllers, hubs,
    /// test base classes, MonoBehaviour messages) or by reflection.
    /// </summary>
    private static bool HasAttributesOrForeignBase(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.GetAttributes().Length > 0)
            {
                return true;
            }
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
            {
                break;
            }

            if (!current.Locations.Any(l => l.IsInSource))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the method implements an interface member for its type or for a derived type that implements the
    /// interface with the inherited method (<c>class D : B, IDisposable</c>, <c>B.Dispose</c> public). Only interface
    /// members with the method's name can be implemented by it (implicitly), and only by the type and the types derived
    /// from it. Checking every interface member of every type of the compilation took ~70 ms per edit in a big file:
    /// the IDE gets a new compilation for every edit.
    /// </summary>
    private static bool ImplementsInterfaceMember(IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken)
    {
        var type = method.ContainingType;
        IEnumerable<INamedTypeSymbol> candidates = type.TypeKind == TypeKind.Class && !type.IsSealed && !type.IsStatic
            && GetDerivedTypes(compilation, cancellationToken).TryGetValue(type.OriginalDefinition, out var derived)
                ? derived.Prepend(type)
                : new[] { type };
        foreach (var candidate in candidates)
        {
            foreach (var @interface in candidate.AllInterfaces)
            {
                foreach (var member in @interface.GetMembers(method.Name))
                {
                    if (member is IMethodSymbol
                        && candidate.FindImplementationForInterfaceMember(member) is IMethodSymbol implementation
                        && SymbolEqualityComparer.Default.Equals(implementation.OriginalDefinition, method.OriginalDefinition))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>Per compilation: each source class with the source classes derived from it (directly or not).</summary>
    private static Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> GetDerivedTypes(Compilation compilation, CancellationToken cancellationToken) =>
        DerivedTypes.GetValue(compilation, c =>
        {
            var result = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
            var pending = new Stack<INamespaceOrTypeSymbol>();
            pending.Push(c.Assembly.GlobalNamespace);
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var container = pending.Pop();
                if (container is INamespaceSymbol ns)
                {
                    foreach (var child in ns.GetNamespaceMembers())
                    {
                        pending.Push(child);
                    }
                }

                foreach (var type in container.GetTypeMembers())
                {
                    pending.Push(type);
                    for (var current = type.BaseType; current is not null && current.Locations.Any(l => l.IsInSource); current = current.BaseType)
                    {
                        if (!result.TryGetValue(current.OriginalDefinition, out var list))
                        {
                            result[current.OriginalDefinition] = list = new List<INamedTypeSymbol>();
                        }

                        list.Add(type);
                    }
                }
            }

            return result;
        });
}
