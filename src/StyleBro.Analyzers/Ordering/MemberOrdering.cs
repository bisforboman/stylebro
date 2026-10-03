using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Ordering;

internal sealed class OrderingViolation
{
    public OrderingViolation(MemberDeclarationSyntax member, MemberDeclarationSyntax shouldPrecede, string reason)
    {
        Member = member;
        ShouldPrecede = shouldPrecede;
        Reason = reason;
    }

    /// <summary>Gets the out-of-place member.</summary>
    public MemberDeclarationSyntax Member { get; }

    /// <summary>Gets the first earlier member that <see cref="Member"/> should come before.</summary>
    public MemberDeclarationSyntax ShouldPrecede { get; }

    public string Reason { get; }
}

/// <summary>
/// Shared ordering logic used by both the analyzer and the code fix, so they can never disagree
/// (which is what keeps the fix idempotent: sorted output never produces a diagnostic).
/// </summary>
internal static class MemberOrdering
{
    /// <summary>
    /// The members BRO1001 orders: a type's, and (StyleCop's SA1201/SA1202/SA1204 at the outer level) the types and
    /// namespaces of a namespace or a file.
    /// </summary>
    public static SyntaxList<MemberDeclarationSyntax>? GetMembers(SyntaxNode container) => container switch
    {
        TypeDeclarationSyntax type => type.Members,
        BaseNamespaceDeclarationSyntax ns => ns.Members,
        CompilationUnitSyntax unit => unit.Members,
        _ => null,
    };

    /// <summary>
    /// The accessibility of each member that is a partial type without an access modifier, from its other parts (null for
    /// the rest). By its modifiers alone such a part looks private or internal, and BRO1007 adds the real modifier in the
    /// same run: sorting by the modifiers would then want another run.
    /// </summary>
    public static MemberAccess?[]? GetPartialAccess(SyntaxNode container, SemanticModel model, CancellationToken cancellationToken)
    {
        if (GetMembers(container) is not { } members)
        {
            return null;
        }

        MemberAccess?[]? result = null;
        for (var i = 0; i < members.Count; i++)
        {
            if (members[i] is BaseTypeDeclarationSyntax type && type.Modifiers.Any(SyntaxKind.PartialKeyword)
                && !type.Modifiers.Any(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.PrivateKeyword)
                && model.GetDeclaredSymbol(type, cancellationToken) is { } symbol)
            {
                result ??= new MemberAccess?[members.Count];
                result[i] = symbol.DeclaredAccessibility switch
                {
                    Accessibility.Public => MemberAccess.Public,
                    Accessibility.Internal => MemberAccess.Internal,
                    Accessibility.ProtectedOrInternal => MemberAccess.ProtectedInternal,
                    Accessibility.Protected => MemberAccess.Protected,
                    Accessibility.ProtectedAndInternal => MemberAccess.PrivateProtected,
                    _ => MemberAccess.Private,
                };
            }
        }

        return result;
    }

    public static OrderingViolation? FindFirstViolation(SyntaxNode container, MemberOrderOptions options, MemberAccess?[]? partialAccess = null)
    {
        var keys = GetKeys(container, options, partialAccess);
        if (keys is null)
        {
            return null;
        }

        var maxIndex = 0;
        for (var i = 1; i < keys.Length; i++)
        {
            var comparison = keys[i].CompareTo(keys[maxIndex]);
            if (comparison < 0)
            {
                var j = 0;
                while (keys[j].CompareTo(keys[i]) <= 0)
                {
                    j++;
                }

                var members = GetMembers(container)!.Value;
                return new OrderingViolation(members[i], members[j], Describe(keys[i], keys[j]));
            }

            if (comparison > 0)
            {
                maxIndex = i;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the type with its members sorted. Blank-line layout stays with the position ("slot"),
    /// while comments, doc comments and attributes travel with the member.
    /// </summary>
    public static SyntaxNode Sort(SyntaxNode container, MemberOrderOptions options, MemberAccess?[]? partialAccess = null)
    {
        var keys = GetKeys(container, options, partialAccess);
        if (keys is null)
        {
            return container;
        }

        var members = GetMembers(container)!.Value;
        var count = members.Count;
        var order = SortedOrder(keys);
        if (order.Select((source, slot) => source == slot).All(unchanged => unchanged))
        {
            return container;
        }

        var newLine = DetectNewLine(container);
        var split = members.Select(m => SplitLeadingTrivia(m.GetLeadingTrivia())).ToArray();
        var result = new MemberDeclarationSyntax[count];

        for (var slot = 0; slot < count; slot++)
        {
            var source = order[slot];
            var layout = split[slot].Layout;

            // Blank lines stay with the slot, so a slot without one can end up between members that need one: a method
            // below a field where two fields sat before (BRO1505, SA1516), or a '//' comment below code (BRO1504,
            // SA1515). The sort must not create a violation that only a second 'dotnet format' run fixes, so it adds
            // the blank line in exactly those cases; violations that already existed are left to those rules.
            if (slot > 0 && !layout.Any(SyntaxKind.EndOfLineTrivia))
            {
                var createsSeparation = NeedsSeparation(members[order[slot - 1]], members[source]) && !NeedsSeparation(members[slot - 1], members[slot]);
                var createsComment = StartsWithLineComment(split[source].Content) && !StartsWithLineComment(split[slot].Content);
                if (createsSeparation || createsComment)
                {
                    layout = layout.Insert(0, SyntaxFactory.EndOfLine(newLine));
                }
            }

            var member = members[source].WithLeadingTrivia(layout.AddRange(split[source].Content));

            if (slot < count - 1 && !EndsWithNewLine(member))
            {
                member = member.WithTrailingTrivia(member.GetTrailingTrivia().Add(SyntaxFactory.EndOfLine(newLine)));
            }
            else if (slot == count - 1 && !EndsWithNewLine(members[count - 1]) && EndsWithNewLine(member))
            {
                // The last slot keeps the file's ending: no line break after the last type when there was none.
                var trailing = member.GetTrailingTrivia();
                member = member.WithTrailingTrivia(trailing.RemoveAt(trailing.Count - 1));
            }

            result[slot] = member;
        }

        var sorted = SyntaxFactory.List(result);
        return container switch
        {
            TypeDeclarationSyntax type => type.WithMembers(sorted),
            BaseNamespaceDeclarationSyntax ns => ns.WithMembers(sorted),
            CompilationUnitSyntax unit => unit.WithMembers(sorted),
            _ => container,
        };
    }

    public static SyntaxToken GetNameToken(MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier,
            MethodDeclarationSyntax method => method.Identifier,
            ConstructorDeclarationSyntax constructor => constructor.Identifier,
            DestructorDeclarationSyntax destructor => destructor.Identifier,
            PropertyDeclarationSyntax property => property.Identifier,
            EventDeclarationSyntax evt => evt.Identifier,
            IndexerDeclarationSyntax indexer => indexer.ThisKeyword,
            OperatorDeclarationSyntax op => op.OperatorToken,
            ConversionOperatorDeclarationSyntax conversion => conversion.OperatorKeyword,
            DelegateDeclarationSyntax del => del.Identifier,
            BaseTypeDeclarationSyntax nested => nested.Identifier,
            BaseNamespaceDeclarationSyntax ns => ns.Name.GetFirstToken(),
            _ => member.GetFirstToken(),
        };
    }

    public static string GetDisplayName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            DestructorDeclarationSyntax destructor => "~" + destructor.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax conversion =>
                conversion.ImplicitOrExplicitKeyword.Text + " operator " + conversion.Type,
            _ => GetNameToken(member).ValueText,
        };
    }

    private static MemberKey[]? GetKeys(SyntaxNode container, MemberOrderOptions options, MemberAccess?[]? partialAccess)
    {
        if (GetMembers(container) is not { } members || members.Count < 2 || HasDirectivesBetweenMembers(members))
        {
            return null;
        }

        // A file's first type carries the file header in its leading trivia when nothing comes before it: moving the type
        // would move the header. Left alone.
        if (container is CompilationUnitSyntax && members[0].GetFirstToken().GetPreviousToken().IsKind(SyntaxKind.None)
            && members[0].GetLeadingTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        var inInterface = container.IsKind(SyntaxKind.InterfaceDeclaration);
        var inNamespace = container is not TypeDeclarationSyntax;
        var keys = new MemberKey[members.Count];
        for (var i = 0; i < members.Count; i++)
        {
            var key = GetKey(members[i], partialAccess?[i] ?? GetAccess(members[i], inInterface, inNamespace), options);
            if (key is null)
            {
                // Unknown member (e.g. incomplete code while typing): leave the type alone.
                return null;
            }

            keys[i] = key.Value;
        }

        // Sorting must not change what field initializers compute; skip the type if it could.
        return container is TypeDeclarationSyntax type && InitializerOrder.ReordersDependentInitializers(type, SortedOrder(keys)) ? null : keys;
    }

    /// <summary>Maps each slot to the index of the member that belongs there after sorting.</summary>
    private static int[] SortedOrder(MemberKey[] keys)
    {
        // OrderBy is stable, so members with equal keys keep their relative order.
        return Enumerable.Range(0, keys.Length).OrderBy(i => keys[i]).ToArray();
    }

    private static MemberKey? GetKey(MemberDeclarationSyntax member, MemberAccess access, MemberOrderOptions options)
    {
        var kind = GetKind(member);
        if (kind is null)
        {
            return null;
        }

        var modifiers = member.Modifiers;
        var isConstant = modifiers.Any(SyntaxKind.ConstKeyword);
        var isStatic = isConstant || modifiers.Any(SyntaxKind.StaticKeyword);
        var isReadonly = member is FieldDeclarationSyntax && modifiers.Any(SyntaxKind.ReadOnlyKeyword);
        return new MemberKey(
            kind.Value,
            access,
            kind.Value == MemberKind.Namespace ? -1 : options.KindRank(kind.Value),
            options.AccessRank(access),
            constantRank: options.ConstantsFirst && !isConstant ? 1 : 0,
            staticRank: options.StaticFirst && !isStatic ? 1 : 0,
            readonlyRank: options.ReadonlyFirst && member is FieldDeclarationSyntax && !isReadonly ? 1 : 0);
    }

    private static MemberKind? GetKind(MemberDeclarationSyntax member)
    {
        return member switch
        {
            FieldDeclarationSyntax => MemberKind.Field,
            EventFieldDeclarationSyntax or EventDeclarationSyntax => MemberKind.Event,
            ConstructorDeclarationSyntax => MemberKind.Constructor,
            DestructorDeclarationSyntax => MemberKind.Finalizer,
            DelegateDeclarationSyntax => MemberKind.Delegate,
            EnumDeclarationSyntax => MemberKind.Enum,
            InterfaceDeclarationSyntax => MemberKind.Interface,
            PropertyDeclarationSyntax => MemberKind.Property,
            IndexerDeclarationSyntax => MemberKind.Indexer,
            MethodDeclarationSyntax => MemberKind.Method,
            ConversionOperatorDeclarationSyntax => MemberKind.Conversion,
            OperatorDeclarationSyntax => MemberKind.Operator,
            StructDeclarationSyntax => MemberKind.Struct,
            RecordDeclarationSyntax record => record.IsKind(SyntaxKind.RecordStructDeclaration) ? MemberKind.Struct : MemberKind.Class,
            ClassDeclarationSyntax => MemberKind.Class,
            BaseNamespaceDeclarationSyntax => MemberKind.Namespace,
            _ => null,
        };
    }

    private static MemberAccess GetAccess(MemberDeclarationSyntax member, bool inInterface, bool inNamespace)
    {
        // A static constructor has no access modifier. StyleCop treats it as public, so it comes before every instance
        // constructor (public ones too, since static comes first). Treating it as private moved it below them.
        if (IsExplicitInterfaceImplementation(member)
            || (member is ConstructorDeclarationSyntax && member.Modifiers.Any(SyntaxKind.StaticKeyword)))
        {
            return MemberAccess.Public;
        }

        var modifiers = member.Modifiers;
        var isPublic = modifiers.Any(SyntaxKind.PublicKeyword);
        var isInternal = modifiers.Any(SyntaxKind.InternalKeyword);
        var isProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
        var isPrivate = modifiers.Any(SyntaxKind.PrivateKeyword);

        if (isPublic)
        {
            return MemberAccess.Public;
        }

        if (isProtected && isInternal)
        {
            return MemberAccess.ProtectedInternal;
        }

        if (isProtected && isPrivate)
        {
            return MemberAccess.PrivateProtected;
        }

        if (isInternal)
        {
            return MemberAccess.Internal;
        }

        if (isProtected)
        {
            return MemberAccess.Protected;
        }

        if (isPrivate)
        {
            return MemberAccess.Private;
        }

        // No modifier: interface members are public, types in a namespace (and namespaces) internal, class/struct members
        // (including nested types) private.
        return inInterface ? MemberAccess.Public : inNamespace ? MemberAccess.Internal : MemberAccess.Private;
    }

    private static bool IsExplicitInterfaceImplementation(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.ExplicitInterfaceSpecifier is not null,
            PropertyDeclarationSyntax property => property.ExplicitInterfaceSpecifier is not null,
            EventDeclarationSyntax evt => evt.ExplicitInterfaceSpecifier is not null,
            IndexerDeclarationSyntax indexer => indexer.ExplicitInterfaceSpecifier is not null,
            _ => false,
        };
    }

    /// <summary>
    /// #region, #if, #pragma etc. between members define scopes that reordering would break,
    /// so such types are skipped entirely rather than risking a wrong fix.
    /// </summary>
    private static bool HasDirectivesBetweenMembers(SyntaxList<MemberDeclarationSyntax> members)
    {
        foreach (var member in members)
        {
            if (member.GetLeadingTrivia().Any(t => t.IsDirective))
            {
                return true;
            }
        }

        // What follows the last member: the closing brace, or for a file (and a file-scoped namespace) its end.
        var after = members[members.Count - 1].GetLastToken().GetNextToken(includeZeroWidth: true);
        return after.LeadingTrivia.Any(t => t.IsDirective);
    }

    /// <summary>
    /// Splits leading trivia into the blank-line layout (stays with the slot) and the member's
    /// own content: indentation, comments and doc comments (moves with the member).
    /// </summary>
    private static (SyntaxTriviaList Layout, SyntaxTriviaList Content) SplitLeadingTrivia(SyntaxTriviaList trivia)
    {
        var split = 0;
        for (var i = 0; i < trivia.Count; i++)
        {
            var kind = trivia[i].Kind();
            if (kind == SyntaxKind.EndOfLineTrivia)
            {
                split = i + 1;
            }
            else if (kind != SyntaxKind.WhitespaceTrivia)
            {
                break;
            }
        }

        return (SyntaxFactory.TriviaList(trivia.Take(split)), SyntaxFactory.TriviaList(trivia.Skip(split)));
    }

    /// <summary>Whether two neighbouring members need a blank line between them: all but two fields (BRO1505).</summary>
    private static bool NeedsSeparation(MemberDeclarationSyntax previous, MemberDeclarationSyntax current)
    {
        return !(previous is FieldDeclarationSyntax && current is FieldDeclarationSyntax);
    }

    private static bool StartsWithLineComment(SyntaxTriviaList content)
    {
        var first = content.FirstOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia));
        return first.IsKind(SyntaxKind.SingleLineCommentTrivia) && !first.ToString().StartsWith("///", System.StringComparison.Ordinal);
    }

    private static bool EndsWithNewLine(MemberDeclarationSyntax member)
    {
        var trailing = member.GetTrailingTrivia();
        return trailing.Count > 0 && trailing[trailing.Count - 1].IsKind(SyntaxKind.EndOfLineTrivia);
    }

    private static string DetectNewLine(SyntaxNode node)
    {
        foreach (var trivia in node.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return trivia.ToString();
            }
        }

        return "\n";
    }

    private static string Describe(MemberKey member, MemberKey shouldPrecede)
    {
        return member.FirstDifference(shouldPrecede) switch
        {
            OrderingComponent.Kind => $"{Plural(member.Kind)} should come before {Plural(shouldPrecede.Kind)}",
            OrderingComponent.Access => $"{AccessText(member.Access)} members should come before {AccessText(shouldPrecede.Access)} members",
            OrderingComponent.Constant => "constants should come before other fields",
            OrderingComponent.Static => "static members should come before instance members",
            _ => "readonly fields should come before mutable fields",
        };
    }

    private static string Plural(MemberKind kind)
    {
        return kind switch
        {
            MemberKind.Field => "fields",
            MemberKind.Constructor => "constructors",
            MemberKind.Finalizer => "finalizers",
            MemberKind.Delegate => "delegates",
            MemberKind.Event => "events",
            MemberKind.Enum => "enums",
            MemberKind.Interface => "interfaces",
            MemberKind.Property => "properties",
            MemberKind.Indexer => "indexers",
            MemberKind.Conversion => "conversion operators",
            MemberKind.Operator => "operators",
            MemberKind.Method => "methods",
            MemberKind.Struct => "structs",
            MemberKind.Namespace => "namespaces",
            _ => "classes",
        };
    }

    private static string AccessText(MemberAccess access)
    {
        return access switch
        {
            MemberAccess.Public => "public",
            MemberAccess.Internal => "internal",
            MemberAccess.ProtectedInternal => "protected internal",
            MemberAccess.Protected => "protected",
            MemberAccess.PrivateProtected => "private protected",
            _ => "private",
        };
    }
}
