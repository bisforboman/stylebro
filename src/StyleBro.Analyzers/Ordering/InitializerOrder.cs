using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// Field, event and auto-property initializers run in textual order: static ones when the type is
/// initialized, instance ones in every constructor. Sorting can swap two of them, which changes behavior
/// when one reads state the other sets up, e.g. <c>static int A = 5; static readonly int B = A + 1;</c>
/// becomes <c>B == 1</c> once B is moved above A. Such types are skipped rather than risking a wrong fix.
/// </summary>
/// <remarks>
/// Syntax only, so it is conservative: an initializer counts as reading state when it mentions any member
/// of the type other than a constant (a method may read a field indirectly) or creates an instance of the type (its
/// constructor may). References that never read state, like <c>typeof(C)</c>, <c>nameof(x)</c> or
/// <c>other.Name</c>, are ignored. Side effects outside the type (two calls to an external counter) are not tracked.
/// </remarks>
internal static class InitializerOrder
{
    /// <summary>
    /// True when the sort described by <paramref name="order"/> (slot -> original index) swaps two
    /// initializers of the same kind (static or instance) and at least one of them reads state of the type.
    /// </summary>
    public static bool ReordersDependentInitializers(TypeDeclarationSyntax type, int[] order)
    {
        var members = type.Members;
        var slotOf = new int[order.Length];
        for (var slot = 0; slot < order.Length; slot++)
        {
            slotOf[order[slot]] = slot;
        }

        TypeState? state = null;
        for (var i = 0; i < members.Count; i++)
        {
            if (!HasInitializer(members[i]))
            {
                continue;
            }

            for (var j = i + 1; j < members.Count; j++)
            {
                if (slotOf[j] > slotOf[i] || !HasInitializer(members[j]) || IsStatic(members[i]) != IsStatic(members[j]))
                {
                    continue;
                }

                state ??= new TypeState(type);
                if (state.IsReadBy(members[i]) || state.IsReadBy(members[j]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasInitializer(MemberDeclarationSyntax member) => GetInitializers(member).Any();

    private static bool IsStatic(MemberDeclarationSyntax member) => member.Modifiers.Any(SyntaxKind.StaticKeyword);

    private static IEnumerable<ExpressionSyntax> GetInitializers(MemberDeclarationSyntax member)
    {
        switch (member)
        {
            // Constants are compile-time values; their order never matters.
            case BaseFieldDeclarationSyntax field when !field.Modifiers.Any(SyntaxKind.ConstKeyword):
                return field.Declaration.Variables
                    .Where(v => v.Initializer is not null)
                    .Select(v => v.Initializer!.Value);

            case PropertyDeclarationSyntax { Initializer: { } initializer }:
                return new[] { initializer.Value };

            default:
                return Enumerable.Empty<ExpressionSyntax>();
        }
    }

    private static TypeSyntax? GetDeclaredType(MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseFieldDeclarationSyntax field => field.Declaration.Type,
            PropertyDeclarationSyntax property => property.Type,
            _ => null,
        };
    }

    /// <summary>The names through which an initializer can reach state of one type declaration.</summary>
    private sealed class TypeState
    {
        private readonly HashSet<string> typeNames = new();
        private readonly HashSet<string> memberNames = new();

        public TypeState(TypeDeclarationSyntax type)
        {
            typeNames.Add(type.Identifier.ValueText);
            foreach (var member in type.Members)
            {
                switch (member)
                {
                    case BaseTypeDeclarationSyntax nested:
                        typeNames.Add(nested.Identifier.ValueText);
                        break;

                    // Constants hold the same value whenever they are read, so they never make order matter.
                    case BaseFieldDeclarationSyntax field when !field.Modifiers.Any(SyntaxKind.ConstKeyword):
                        memberNames.UnionWith(field.Declaration.Variables.Select(v => v.Identifier.ValueText));
                        break;

                    case MethodDeclarationSyntax or PropertyDeclarationSyntax or EventDeclarationSyntax or DelegateDeclarationSyntax:
                        memberNames.Add(MemberOrdering.GetNameToken(member).ValueText);
                        break;
                }
            }
        }

        public bool IsReadBy(MemberDeclarationSyntax member)
        {
            foreach (var initializer in GetInitializers(member))
            {
                foreach (var node in initializer.DescendantNodesAndSelf(descendIntoChildren: n => !IsInert(n)))
                {
                    switch (node)
                    {
                        case SimpleNameSyntax name when IsStateReference(name):
                            return true;

                        case ImplicitObjectCreationExpressionSyntax when CreatesThisType(GetDeclaredType(member)):
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary><c>typeof(...)</c> and <c>nameof(...)</c> name things without touching them.</summary>
        private static bool IsInert(SyntaxNode node)
        {
            return node is TypeOfExpressionSyntax
                || node is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } };
        }

        private bool IsStateReference(SimpleNameSyntax name)
        {
            var text = name.Identifier.ValueText;
            if (typeNames.Contains(text))
            {
                // 'C.Member' or 'new C()' reach the type's state; 'List<C>' or a declaration type don't.
                return name.Parent switch
                {
                    MemberAccessExpressionSyntax access => access.Expression == name,
                    ObjectCreationExpressionSyntax creation => creation.Type == name,
                    _ => false,
                };
            }

            if (!memberNames.Contains(text))
            {
                return false;
            }

            // In 'other.Name', 'x?.Name' or 'Name: value', the name belongs to something else.
            return name.Parent switch
            {
                MemberAccessExpressionSyntax access => access.Name != name,
                MemberBindingExpressionSyntax => false,
                NameColonSyntax or NameEqualsSyntax => false,
                _ => true,
            };
        }

        /// <summary>'new()' creates the declared type, so its constructor may read the type's state.</summary>
        private bool CreatesThisType(TypeSyntax? declaredType)
        {
            var name = declaredType switch
            {
                QualifiedNameSyntax qualified => qualified.Right,
                NullableTypeSyntax { ElementType: SimpleNameSyntax element } => element,
                SimpleNameSyntax simple => simple,
                _ => null,
            };

            return name is not null && typeNames.Contains(name.Identifier.ValueText);
        }
    }
}
