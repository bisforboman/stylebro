using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Ordering;

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
        var keys = GetKeys(container, options, partialAccess, out var segments, out var anchors);
        if (keys is null)
        {
            return null;
        }

        int Compare(int a, int b) => CompareMembers(keys, segments, anchors, a, b);

        var maxIndex = 0;
        for (var i = 1; i < keys.Length; i++)
        {
            var comparison = Compare(i, maxIndex);
            if (comparison < 0)
            {
                var j = 0;
                while (Compare(j, i) <= 0)
                {
                    j++;
                }

                var members = GetMembers(container)!.Value;
                var reason = anchors is null || anchors[i] == anchors[j] ? Describe(keys[i], keys[j])
                    : keys[anchors[i]].CompareTo(keys[anchors[j]]) != 0 ? Describe(keys[anchors[i]], keys[anchors[j]])
                    : $"overloads of '{GetDisplayName(members[anchors[i] != i ? anchors[i] : anchors[j]])}' should be next to each other";
                return new OrderingViolation(members[i], members[j], reason);
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
    public static SyntaxNode Sort(SyntaxNode container, MemberOrderOptions options, MemberAccess?[]? partialAccess = null, AnalyzerConfigOptions? autoAccessorLines = null)
    {
        var keys = GetKeys(container, options, partialAccess, out var segments, out var anchors);
        if (keys is null)
        {
            return container;
        }

        var members = GetMembers(container)!.Value;
        var count = members.Count;
        var order = SortedOrder(keys, segments, anchors);
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
                var createsSeparation = NeedsSeparation(members[order[slot - 1]], members[source], autoAccessorLines) && !NeedsSeparation(members[slot - 1], members[slot], autoAccessorLines);
                var createsComment = StartsWithLineComment(split[source].Content) && !StartsWithLineComment(split[slot].Content);

                // A doc comment arriving where a member without one sat (field below field: no blank line needed before)
                // wants one too (BRO1513, SA1514; found in Jellyfin). Below a region line the next check adds it.
                var createsDoc = !EndsWithDirective(layout) && StartsWithDocComment(split[source].Content) && !StartsWithDocComment(split[slot].Content);
                if (createsSeparation || createsComment || createsDoc)
                {
                    layout = layout.Insert(0, SyntaxFactory.EndOfLine(newLine));
                }
            }

            // Likewise a doc comment moved right below a region line, which wants a blank line in between (BRO1513, SA1514).
            if (EndsWithDirective(layout) && StartsWithDocComment(split[source].Content) && !StartsWithDocComment(split[slot].Content))
            {
                layout = layout.Add(SyntaxFactory.EndOfLine(newLine));
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
            TypeDeclarationSyntax extension when CSharp14.IsExtensionBlock(extension) => extension.Keyword,
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
            TypeDeclarationSyntax extension when CSharp14.IsExtensionBlock(extension) => "extension" + extension.ParameterList,
            IndexerDeclarationSyntax => "this[]",
            BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax conversion =>
                conversion.ImplicitOrExplicitKeyword.Text + " operator " + conversion.Type,
            _ => GetNameToken(member).ValueText,
        };
    }

    private static MemberKey[]? GetKeys(SyntaxNode container, MemberOrderOptions options, MemberAccess?[]? partialAccess, out int[] segments, out int[]? anchors)
    {
        segments = System.Array.Empty<int>();
        anchors = null;
        if (GetMembers(container) is not { } members || members.Count < 2 || GetSegments(members) is not { } found)
        {
            return null;
        }

        segments = found;

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

        if (options.KeepOverloadsTogether)
        {
            anchors = GetAnchors(members, keys, found);
        }

        // Sorting must not change what field initializers compute, nor an order the runtime sees (struct layout,
        // serialized members); skip the type if it could.
        return container is TypeDeclarationSyntax type
            && (InitializerOrder.ReordersDependentInitializers(type, SortedOrder(keys, segments, anchors)) || ObservableOrder.ReordersObservableMembers(type, SortedOrder(keys, segments, anchors)))
            ? null
            : keys;
    }

    /// <summary>
    /// stylebro_keep_overloads_together: each method's group of overloads (same name, same region) sorts at the place of
    /// the overload that sorts first, the others right behind it in their own order. Constructors and indexers are their
    /// own kinds, so always together; operators aren't grouped ('==' and '!=' pairs stay as written). Every member's
    /// anchor is the index of that first overload (its own index when it has no overloads).
    /// </summary>
    private static int[] GetAnchors(SyntaxList<MemberDeclarationSyntax> members, MemberKey[] keys, int[] segments)
    {
        (int, string)? Group(int i) => members[i] is MethodDeclarationSyntax method
            ? (segments[i], method.ExplicitInterfaceSpecifier?.Name + "." + method.Identifier.ValueText)
            : null;

        var anchors = Enumerable.Range(0, keys.Length).ToArray();
        var first = new Dictionary<(int, string), int>();
        for (var i = 0; i < members.Count; i++)
        {
            if (Group(i) is { } group && (!first.TryGetValue(group, out var anchor) || keys[i].CompareTo(keys[anchor]) < 0))
            {
                first[group] = i;
            }
        }

        for (var i = 0; i < members.Count; i++)
        {
            if (Group(i) is { } group)
            {
                anchors[i] = first[group];
            }
        }

        return anchors;
    }

    /// <summary>
    /// Members are compared within their region only (segments only grow, so a later one never sorts earlier); with
    /// overloads kept together, by their group's first overload first (its key, then its position).
    /// </summary>
    private static int CompareMembers(MemberKey[] keys, int[] segments, int[]? anchors, int a, int b)
    {
        if (segments[a] != segments[b])
        {
            return segments[a].CompareTo(segments[b]);
        }

        if (anchors is not null && anchors[a] != anchors[b])
        {
            var c = keys[anchors[a]].CompareTo(keys[anchors[b]]);
            return c != 0 ? c : anchors[a].CompareTo(anchors[b]);
        }

        return keys[a].CompareTo(keys[b]);
    }

    /// <summary>Maps each slot to the index of the member that belongs there after sorting.</summary>
    private static int[] SortedOrder(MemberKey[] keys, int[] segments, int[]? anchors)
    {
        // OrderBy is stable, so members with equal keys keep their relative order; each region is sorted on its own.
        return anchors is null
            ? Enumerable.Range(0, keys.Length).OrderBy(i => segments[i]).ThenBy(i => keys[i]).ToArray()
            : Enumerable.Range(0, keys.Length).OrderBy(i => segments[i]).ThenBy(i => keys[anchors[i]]).ThenBy(i => anchors[i]).ThenBy(i => keys[i]).ToArray();
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
            _ when CSharp14.IsExtensionBlock(member) => MemberKind.Extension,
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
    /// The stretch each member is in (0, 1, ...): every directive before a member ('#region', '#endregion', '#pragma',
    /// '#nullable') starts a new one, and members are sorted within their stretch only, so regions keep their members and
    /// every member keeps the pragma and nullable state it had. Null (the type is skipped) for conditional directives
    /// ('#if', '#else', ...: in a multi-targeted project each target framework's copy of the file has other members
    /// there and would need other edits) and for a doc comment above a directive (it would stay with the position, not
    /// the member).
    /// </summary>
    private static int[]? GetSegments(SyntaxList<MemberDeclarationSyntax> members)
    {
        var segments = new int[members.Count];
        var segment = 0;
        for (var i = 0; i < members.Count; i++)
        {
            var trivia = members[i].GetLeadingTrivia();
            if (!IsSortableAcross(trivia))
            {
                return null;
            }

            if (i > 0 && LastDirective(trivia) >= 0)
            {
                segment++;
            }

            segments[i] = segment;
        }

        // What follows the last member: the closing brace, or for a file (and a file-scoped namespace) its end.
        var after = members[members.Count - 1].GetLastToken().GetNextToken(includeZeroWidth: true);
        return IsSortableAcross(after.LeadingTrivia) ? segments : null;
    }

    private static bool IsSortableAcross(SyntaxTriviaList trivia)
    {
        var last = LastDirective(trivia);
        for (var i = 0; i <= last; i++)
        {
            var kind = trivia[i].Kind();
            if (kind is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia
                || kind is SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia)
            {
                return false;
            }
        }

        return true;
    }

    private static int LastDirective(SyntaxTriviaList trivia)
    {
        for (var i = trivia.Count - 1; i >= 0; i--)
        {
            if (trivia[i].IsDirective)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Splits leading trivia into the blank-line layout (stays with the slot) and the member's
    /// own content: indentation, comments and doc comments (moves with the member). Directives, and anything above them,
    /// are layout: they mark where a region or a pragma's scope starts or ends, not the member.
    /// </summary>
    private static (SyntaxTriviaList Layout, SyntaxTriviaList Content) SplitLeadingTrivia(SyntaxTriviaList trivia)
    {
        var split = LastDirective(trivia) + 1;
        for (var i = split; i < trivia.Count; i++)
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

    /// <summary>Whether two neighbouring members need a blank line between them (BRO1505's rule; options when BRO1527 is on).</summary>
    private static bool NeedsSeparation(MemberDeclarationSyntax previous, MemberDeclarationSyntax current, AnalyzerConfigOptions? autoAccessorLines)
    {
        return Layout.ElementSeparation.NeedsBlankLine(previous, current, previous.SyntaxTree.GetText(), autoAccessorLines);
    }

    private static bool StartsWithLineComment(SyntaxTriviaList content)
    {
        var first = content.FirstOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia));
        return first.IsKind(SyntaxKind.SingleLineCommentTrivia) && !first.ToString().StartsWith("///", System.StringComparison.Ordinal);
    }

    private static bool StartsWithDocComment(SyntaxTriviaList content) =>
        content.FirstOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)).IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia);

    /// <summary>Whether the last line of the layout is a directive (no blank line below it).</summary>
    private static bool EndsWithDirective(SyntaxTriviaList layout) =>
        layout.LastOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)).IsDirective;

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
            MemberKind.Extension => "extension blocks",
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
