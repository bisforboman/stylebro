using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>How private fields are named: 'count' (StyleCop, the default) or '_count' (.NET runtime style).</summary>
internal enum FieldStyle
{
    CamelCase,
    UnderscoreCamelCase,
}

/// <summary>Which field naming rule applies to a field.</summary>
internal enum FieldRule
{
    /// <summary>BRO1303: private field casing (SA1306, SA1309).</summary>
    PrivateCasing,

    /// <summary>BRO1306: PascalCase for constants, static readonly and non-private fields (SA1303, SA1311, SA1307, SA1304).</summary>
    PascalCasing,

    /// <summary>BRO1307: no 'm_', 's_' or 't_' prefix (SA1308).</summary>
    Prefix,

    /// <summary>BRO1308: no underscore inside the name (SA1310).</summary>
    Underscore,
}

/// <summary>
/// Shared logic for the field naming rules BRO1303, BRO1306, BRO1307 and BRO1308: which fields are checked, their new
/// name, and whether renaming is safe. Every field gets at most one rule, and its new name is the complete correct
/// name, so one rename is enough: a prefix goes first (BRO1307), then underscores inside the name (BRO1308), then the
/// casing (BRO1303, BRO1306).
/// </summary>
internal static class FieldNames
{
    public const string StyleKey = "stylebro_private_field_naming";

    public static FieldStyle GetStyle(AnalyzerConfigOptions options)
    {
        return options.TryGetValue(StyleKey, out var value) && value.Trim() == "_camelCase"
            ? FieldStyle.UnderscoreCamelCase
            : FieldStyle.CamelCase;
    }

    /// <summary>
    /// Private fields that StyleCop's SA1306 checks (BRO1303): not constants and not static readonly (those are
    /// PascalCase).
    /// </summary>
    public static bool IsChecked(IFieldSymbol field)
    {
        return IsSourceField(field)
            && field.DeclaredAccessibility == Accessibility.Private
            && !field.IsConst
            && !(field.IsStatic && field.IsReadOnly);
    }

    /// <summary>
    /// Fields that are PascalCase (BRO1306): constants (SA1303) and static readonly fields (SA1311) of any
    /// accessibility, and public, internal and protected internal fields (SA1307, SA1304). Protected fields are
    /// camelCase in StyleCop (SA1306) and are left out of both rules; so are enum members (SA1300).
    /// </summary>
    public static bool IsPascalChecked(IFieldSymbol field)
    {
        return IsSourceField(field)
            && field.ContainingType.TypeKind != TypeKind.Enum
            && (field.IsConst
                || (field.IsStatic && field.IsReadOnly)
                || field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal);
    }

    /// <summary>The field's new name under whichever rule applies, or null when it already fits.</summary>
    public static string? GetNewName(IFieldSymbol field, FieldStyle style) => GetRename(field, style)?.NewName;

    /// <summary>
    /// The rule that applies to <paramref name="field"/> and the new name, or null when the name fits (or has no safe
    /// replacement). The new name follows the field's convention: PascalCase for constants, static readonly and
    /// non-private fields, <paramref name="style"/> for private fields, and camelCase for protected fields (StyleCop's
    /// SA1306). Enum members aren't fields for these rules (SA1300).
    /// </summary>
    public static (FieldRule Rule, string NewName)? GetRename(IFieldSymbol field, FieldStyle style)
    {
        if (!IsSourceField(field) || field.ContainingType.TypeKind == TypeKind.Enum)
        {
            return null;
        }

        var name = field.Name;
        var hasPrefix = name.Length >= 2 && name[0] is 'm' or 's' or 't' && name[1] == '_';
        var core = hasPrefix ? name.Substring(2) : name;
        if (hasPrefix || core.TrimStart('_').Contains('_'))
        {
            var casing = IsPascalChecked(field) ? FieldCasing.Pascal
                : IsChecked(field) && style == FieldStyle.UnderscoreCamelCase ? FieldCasing.UnderscoreCamel
                : FieldCasing.Camel;
            return GetJoinedName(core, casing) is { } joined && joined != name
                ? (hasPrefix ? FieldRule.Prefix : FieldRule.Underscore, joined)
                : null;
        }

        var newName = IsPascalChecked(field) ? GetPascalName(name)
            : IsChecked(field) ? GetNewName(name, style)
            : null;
        return newName is null ? null : (IsPascalChecked(field) ? FieldRule.PascalCasing : FieldRule.PrivateCasing, newName);
    }

    /// <summary>
    /// The words of <paramref name="core"/> (split at underscores) joined in <paramref name="casing"/>: 'with_underscore'
    /// -> 'withUnderscore', 'MAX_VALUE' -> 'MaxValue' (Pascal) or 'maxValue' (camel). An all-capitals word of more than
    /// one letter counts as a word, not an acronym, when there are several words.
    /// </summary>
    private static string? GetJoinedName(string core, FieldCasing casing)
    {
        var words = core.Split(new[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !char.IsLetter(words[0][0]))
        {
            return null;
        }

        // 'Int32_0': an underscore between two digits separates numbers; joining them ('Int320') would change the name's
        // meaning, so such names are left alone.
        for (var i = 1; i < words.Length; i++)
        {
            if (char.IsDigit(words[i - 1][words[i - 1].Length - 1]) && char.IsDigit(words[i][0]))
            {
                return null;
            }
        }

        var several = words.Length > 1;
        var first = words[0];
        var rest = string.Concat(words.Skip(1).Select(w => Capitalize(w, several)));
        var result = casing switch
        {
            FieldCasing.Pascal => Capitalize(first, several) + rest,
            _ => (char.IsLower(first[0]) ? first : CamelCaseNames.GetNewName(first) ?? first) + rest,
        };

        if (casing == FieldCasing.UnderscoreCamel)
        {
            result = "_" + result;
        }

        return SyntaxFacts.IsValidIdentifier(result) && SyntaxFacts.GetKeywordKind(result) == SyntaxKind.None ? result : null;
    }

    private static string Capitalize(string word, bool lowerRest)
    {
        var rest = lowerRest && IsAllUpper(word) ? word.Substring(1).ToLowerInvariant() : word.Substring(1);
        return char.ToUpperInvariant(word[0]) + rest;
    }

    private static bool IsAllUpper(string word) => word.Length > 1 && word.All(c => !char.IsLetter(c) || char.IsUpper(c));

    /// <summary>'lowerConst' -> 'LowerConst', '_value' -> 'Value'. Null for one-letter prefixes (SA1308) and names that fit.</summary>
    public static string? GetPascalName(string name)
    {
        if (name.Length > 2 && char.IsLetter(name[0]) && name[1] == '_')
        {
            return null;
        }

        var core = name.TrimStart('_');
        if (core.Length == 0 || !char.IsLetter(core[0]))
        {
            return null;
        }

        var result = char.ToUpperInvariant(core[0]) + core.Substring(1);
        return result != name && SyntaxFacts.IsValidIdentifier(result) ? result : null;
    }

    /// <summary>
    /// Whether code outside the type may use the field's name as data: serializers write public instance fields under
    /// their name (Json.NET does by default), so their names are also looked for inside strings, not just as a whole
    /// string.
    /// </summary>
    public static bool IsDataMember(IFieldSymbol field) =>
        !field.IsConst && !field.IsStatic && field.DeclaredAccessibility != Accessibility.Private;

    /// <summary>
    /// Whether a serializer may write the type's members by name: [Serializable] (binary serialization writes every
    /// field), or an attribute that looks like a serializer setting ('JsonObject(MemberSerialization.Fields)',
    /// 'DataContract', 'MessagePackObject', 'ProtoContract', ...).
    /// </summary>
    public static bool IsSerialized(INamedTypeSymbol type)
    {
        return type.IsSerializable || type.GetAttributes().Any(a =>
            a.AttributeClass?.Name is { } name
            && (name.Contains("Serializ") || name.Contains("Json") || name.Contains("DataContract") || name.Contains("MessagePack") || name.Contains("Proto")));
    }

    /// <summary>
    /// Whether another member of the type or a base type contains the member's name, like 'ShouldSerializeitems' or
    /// 'itemsSpecified' for 'items', or 'OnnameChanged': conventions that pair members by name (Json.NET, XML
    /// serialization, data binding) would break if only one of them were renamed.
    /// </summary>
    public static bool HasRelatedMemberName(INamedTypeSymbol type, ISymbol member)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            // Accessors ('get_items') and backing fields belong to the member itself.
            if (current.GetMembers().Any(m => m.Name != member.Name && !m.IsImplicitlyDeclared && !IsPartOfAnotherSymbol(m) && m.Name.Contains(member.Name)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPartOfAnotherSymbol(ISymbol member) =>
        member is IMethodSymbol { AssociatedSymbol: not null } or IFieldSymbol { AssociatedSymbol: not null };

    private static bool IsSourceField(IFieldSymbol field) =>
        !field.IsImplicitlyDeclared && field.Locations.Any(l => l.IsInSource);

    /// <summary>
    /// The field's name in <paramref name="style"/>, or null when it already fits or has no safe replacement.
    /// camelCase: 'Count' and '_count' -> 'count'. _camelCase: 'Count', 'count' and '__count' -> '_count'.
    /// </summary>
    public static string? GetNewName(string name, FieldStyle style)
    {
        // 'm_count', 's_count', 't_count': a one-letter prefix is StyleCop's SA1308, and 's_' is the .NET runtime's
        // convention for static fields. Left alone in both styles.
        if (name.Length > 2 && char.IsLetter(name[0]) && name[1] == '_')
        {
            return null;
        }

        if (style == FieldStyle.CamelCase)
        {
            return CamelCaseNames.GetNewName(name);
        }

        var core = name.TrimStart('_');
        if (core.Length == 0 || !char.IsLetter(core[0]))
        {
            return null;
        }

        var camel = char.IsLower(core[0]) ? core : CamelCaseNames.GetNewName(core);
        var result = camel is null ? null : "_" + camel;
        return result != name ? result : null;
    }

    /// <summary>
    /// Whether renaming <paramref name="field"/> to <paramref name="newName"/> is safe. Skipped: the type or a base
    /// type already has a member with the new name; another checked field gets the same new name; the field has
    /// attributes or the type is [Serializable] (serializers can depend on field names); the old name appears in a
    /// string in the type (reflection, e.g. GetField("_count")), in code excluded by '#if', or as an inferred
    /// anonymous type member or tuple element name. Locals and parameters with the new name are no reason to skip:
    /// the fix qualifies the references they would hide ('this.count').
    /// </summary>
    public static bool CanRename(IFieldSymbol field, string newName, FieldStyle style, CancellationToken cancellationToken)
    {
        var type = field.ContainingType;
        if (field.GetAttributes().Length > 0 || IsSerialized(type) || HasRelatedMemberName(type, field))
        {
            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (!current.GetMembers(newName).IsEmpty)
            {
                return false;
            }
        }

        if (type.GetMembers().OfType<IFieldSymbol>().Any(f =>
            !SymbolEqualityComparer.Default.Equals(f, field) && GetNewName(f, style) == newName))
        {
            return false;
        }

        var oldName = field.Name;
        var word = new Regex(@"(?<![\w@])" + Regex.Escape(oldName) + @"(?!\w)");
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            var declaration = reference.GetSyntax(cancellationToken);
            foreach (var token in declaration.DescendantTokens(descendIntoTrivia: true))
            {
                if ((token.IsKind(SyntaxKind.StringLiteralToken) && word.IsMatch(token.ValueText))
                    || (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == oldName && CamelCaseNames.IsInferredMemberName(token)))
                {
                    return false;
                }
            }

            if (declaration.DescendantTrivia(descendIntoTrivia: true)
                .Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && word.IsMatch(t.ToString())))
            {
                return false;
            }
        }

        return true;
    }

    private enum FieldCasing
    {
        Camel,
        UnderscoreCamel,
        Pascal,
    }
}
