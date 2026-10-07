using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// Members whose declaration order is visible at run time, so sorting them changes behavior:
/// <list type="bullet">
/// <item>instance fields of a struct (sequential layout is the default) or of a type with [StructLayout]: the memory
/// layout that interop, <see langword="unsafe"/> code and Marshal see;</item>
/// <item>fields and properties of a type that a serializer writes in declaration order (Json.NET, DataContract,
/// XmlSerializer, MessagePack, protobuf), recognized by a serializer attribute on the type or one of its members. Found
/// on Newtonsoft.Json: sorting changed the order of properties in the JSON its tests compare.</item>
/// </list>
/// A type is skipped when the sort would change the relative order of those members. Syntax only: a plain class that is
/// serialized without attributes can't be recognized (StyleCop's fix reorders those too).
/// </summary>
internal static class ObservableOrder
{
    public static bool ReordersObservableMembers(TypeDeclarationSyntax type, int[] order)
    {
        var members = type.Members;
        var layout = type is StructDeclarationSyntax || type.IsKind(SyntaxKind.RecordStructDeclaration) || HasAttribute(type.AttributeLists, "StructLayout");
        var serialized = HasSerializerAttribute(type.AttributeLists) || members.Any(m => HasSerializerAttribute(m.AttributeLists));
        if (!layout && !serialized)
        {
            return false;
        }

        bool IsObservable(MemberDeclarationSyntax member) =>
            !member.Modifiers.Any(SyntaxKind.StaticKeyword) && !member.Modifiers.Any(SyntaxKind.ConstKeyword)
            && (member is FieldDeclarationSyntax || (serialized && member is PropertyDeclarationSyntax));

        var before = Enumerable.Range(0, members.Count).Where(i => IsObservable(members[i])).ToList();
        var after = order.Where(i => IsObservable(members[i])).ToList();
        return !before.SequenceEqual(after);
    }

    private static bool HasSerializerAttribute(SyntaxList<AttributeListSyntax> lists) =>
        lists.SelectMany(l => l.Attributes).Any(a =>
        {
            var name = a.Name.ToString();
            return name.Contains("Serializ") || name.Contains("Json") || name.Contains("DataContract") || name.Contains("DataMember")
                || name.StartsWith("Xml", System.StringComparison.Ordinal) || name.Contains(".Xml") || name.Contains("MessagePack") || name.Contains("Proto");
        });

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> lists, string name) =>
        lists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString() is var text && (text == name || text == name + "Attribute" || text.EndsWith("." + name, System.StringComparison.Ordinal)));
}
