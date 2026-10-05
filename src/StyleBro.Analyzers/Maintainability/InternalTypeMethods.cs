using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>
/// Shared logic for BRO1409: an ordinary method declared <c>public</c> in a type that can't be seen outside the assembly
/// gains nothing from <c>public</c>; <c>internal</c> says what it means. Deliberately narrow: reflection's defaults and
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

    /// <summary>Per compilation: every source method that implements an interface member for some source type.</summary>
    private static readonly ConditionalWeakTable<Compilation, HashSet<IMethodSymbol>> Implementations = new();

    /// <summary>
    /// The <c>public</c> keyword to replace with <c>internal</c>, or null. Syntax checks first (cheap), then the symbol.
    /// </summary>
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
            || GetImplementations(model.Compilation, cancellationToken).Contains(symbol.OriginalDefinition))
        {
            return null;
        }

        return keyword;
    }

    /// <summary>
    /// Syntax only: whether some enclosing type declaration doesn't say <c>public</c> or <c>protected</c> (no modifier:
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
    /// <c>object</c>/<c>ValueType</c>: frameworks call public methods of such types by convention (controllers, hubs,
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
    /// Methods that implement an interface member for some type of the compilation, also for a derived type that
    /// implements the interface with an inherited method (<c>class D : B, IDisposable</c>, <c>B.Dispose</c> public).
    /// </summary>
    private static HashSet<IMethodSymbol> GetImplementations(Compilation compilation, CancellationToken cancellationToken) =>
        Implementations.GetValue(compilation, c =>
        {
            var result = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            var pending = new Stack<INamespaceOrTypeSymbol>();
            pending.Push(c.Assembly.GlobalNamespace);
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var member in pending.Pop().GetMembers())
                {
                    if (member is INamespaceSymbol ns)
                    {
                        pending.Push(ns);
                    }
                    else if (member is INamedTypeSymbol type)
                    {
                        pending.Push(type);
                        if (type.TypeKind is TypeKind.Class or TypeKind.Struct)
                        {
                            foreach (var @interface in type.AllInterfaces)
                            {
                                foreach (var interfaceMember in @interface.GetMembers())
                                {
                                    if (interfaceMember is IMethodSymbol && type.FindImplementationForInterfaceMember(interfaceMember) is IMethodSymbol implementation)
                                    {
                                        result.Add(implementation.OriginalDefinition);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        });
}
