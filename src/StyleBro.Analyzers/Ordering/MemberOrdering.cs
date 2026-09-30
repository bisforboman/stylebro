using System.Collections.Generic;
using System.Linq;
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

    /// <summary>The out-of-place member.</summary>
    public MemberDeclarationSyntax Member { get; }

    /// <summary>The first earlier member that <see cref="Member"/> should come before.</summary>
    public MemberDeclarationSyntax ShouldPrecede { get; }

    public string Reason { get; }
}

/// <summary>
/// Shared ordering logic used by both the analyzer and the code fix, so they can never disagree
/// (which is what keeps the fix idempotent: sorted output never produces a diagnostic).
/// </summary>
internal static class MemberOrdering
{
    public static OrderingViolation? FindFirstViolation(TypeDeclarationSyntax type, MemberOrderOptions options)
    {
        var keys = GetKeys(type, options);
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

                return new OrderingViolation(type.Members[i], type.Members[j], Describe(keys[i], keys[j]));
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
    public static TypeDeclarationSyntax Sort(TypeDeclarationSyntax type, MemberOrderOptions options)
    {
        var keys = GetKeys(type, options);
        if (keys is null)
        {
            return type;
        }

        var members = type.Members;
        var count = members.Count;
        var order = SortedOrder(keys);
        if (order.Select((source, slot) => source == slot).All(unchanged => unchanged))
        {
            return type;
        }

        var newLine = DetectNewLine(type);
        var split = members.Select(m => SplitLeadingTrivia(m.GetLeadingTrivia())).ToArray();
        var result = new MemberDeclarationSyntax[count];

        for (var slot = 0; slot < count; slot++)
        {
            var source = order[slot];
            var layout = split[slot].Layout;

            // A member led by a '//' comment that lands right below another member (no blank line in this slot's
            // layout) would leave the comment glued to that member's code, which BRO1504 (SA1515) reports. Sorting
            // must not create a new violation that only a second 'dotnet format' run fixes, so add the blank line.
            if (slot > 0 && source != slot && !layout.Any(SyntaxKind.EndOfLineTrivia) && StartsWithLineComment(split[source].Content))
            {
                layout = layout.Insert(0, SyntaxFactory.EndOfLine(newLine));
            }

            var member = members[source].WithLeadingTrivia(layout.AddRange(split[source].Content));

            if (slot < count - 1 && !EndsWithNewLine(member))
            {
                member = member.WithTrailingTrivia(member.GetTrailingTrivia().Add(SyntaxFactory.EndOfLine(newLine)));
            }

            result[slot] = member;
        }

        return type.WithMembers(SyntaxFactory.List(result));
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
            _ => member.GetFirstToken(),
        };
    }

    public static string GetDisplayName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            DestructorDeclarationSyntax destructor => "~" + destructor.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax conversion =>
                conversion.ImplicitOrExplicitKeyword.Text + " operator " + conversion.Type,
            _ => GetNameToken(member).ValueText,
        };
    }

    private static MemberKey[]? GetKeys(TypeDeclarationSyntax type, MemberOrderOptions options)
    {
        var members = type.Members;
        if (members.Count < 2 || HasDirectivesBetweenMembers(type))
        {
            return null;
        }

        var inInterface = type.IsKind(SyntaxKind.InterfaceDeclaration);
        var keys = new MemberKey[members.Count];
        for (var i = 0; i < members.Count; i++)
        {
            var key = GetKey(members[i], inInterface, options);
            if (key is null)
            {
                // Unknown member (e.g. incomplete code while typing): leave the type alone.
                return null;
            }

            keys[i] = key.Value;
        }

        // Sorting must not change what field initializers compute; skip the type if it could.
        return InitializerOrder.ReordersDependentInitializers(type, SortedOrder(keys)) ? null : keys;
    }

    /// <summary>Maps each slot to the index of the member that belongs there after sorting.</summary>
    private static int[] SortedOrder(MemberKey[] keys)
    {
        // OrderBy is stable, so members with equal keys keep their relative order.
        return Enumerable.Range(0, keys.Length).OrderBy(i => keys[i]).ToArray();
    }

    private static MemberKey? GetKey(MemberDeclarationSyntax member, bool inInterface, MemberOrderOptions options)
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
        var access = GetAccess(member, inInterface);

        return new MemberKey(
            kind.Value,
            access,
            options.KindRank(kind.Value),
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
            _ => null,
        };
    }

    private static MemberAccess GetAccess(MemberDeclarationSyntax member, bool inInterface)
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

        // No modifier: interface members are public, class/struct members (including nested types) private.
        return inInterface ? MemberAccess.Public : MemberAccess.Private;
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
    private static bool HasDirectivesBetweenMembers(TypeDeclarationSyntax type)
    {
        foreach (var member in type.Members)
        {
            if (member.GetLeadingTrivia().Any(t => t.IsDirective))
            {
                return true;
            }
        }

        return type.CloseBraceToken.LeadingTrivia.Any(t => t.IsDirective);
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
