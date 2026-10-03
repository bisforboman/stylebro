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

    /// <summary>A whole word: a run of \w characters not directly after another one or an '@'.</summary>
    private static readonly Regex Word = new(@"(?<![\w@])\w+");

    private enum FieldCasing
    {
        Camel,
        UnderscoreCamel,
        Pascal,
    }

    /// <summary>
    /// The private field style: <see cref="StyleKey"/> when set; otherwise the SDK's naming rule for private fields, when
    /// one asks for camel case with no prefix or with '_'; otherwise camelCase.
    /// </summary>
    public static FieldStyle GetStyle(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue(StyleKey, out var value))
        {
            return value.Trim() == "_camelCase" ? FieldStyle.UnderscoreCamelCase : FieldStyle.CamelCase;
        }

        return GetStyleFromNamingRules(options) ?? FieldStyle.CamelCase;
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

        // Protected fields, like StyleCop with its defaults: the others are camelCase (SA1306, BRO1303); a readonly one's
        // casing isn't checked (SA1306 skips it, and SA1304 leaves non-internal fields to SA1307, which checks public and
        // internal ones only). A leading underscore is SA1309's: removed in the camelCase style, kept in the _camelCase
        // style (where SA1309 is off), which doesn't add one either (SX1309 is about private fields).
        var isProtected = IsProtectedChecked(field);
        var pascal = IsPascalChecked(field);

        var hasPrefix = name.Length >= 2 && name[0] is 'm' or 's' or 't' && name[1] == '_';
        var core = hasPrefix ? name.Substring(2) : name;
        if (hasPrefix || core.TrimStart('_').Contains('_'))
        {
            var casing = pascal ? FieldCasing.Pascal
                : IsChecked(field) && style == FieldStyle.UnderscoreCamelCase ? FieldCasing.UnderscoreCamel
                : FieldCasing.Camel;
            return GetJoinedName(core, casing) is { } joined && joined != name
                ? (hasPrefix ? FieldRule.Prefix : FieldRule.Underscore, joined)
                : null;
        }

        var camelStyle = style == FieldStyle.CamelCase;
        var newName = isProtected && field.IsReadOnly ? (camelStyle ? WithoutLeadingUnderscores(name) : null)
            : isProtected ? (camelStyle ? CamelCaseNames.GetNewName(name) : LowerAfterUnderscores(name))
            : pascal ? GetPascalName(name)
            : IsChecked(field) ? GetNewName(name, style)
            : null;
        return newName is null ? null : (pascal ? FieldRule.PascalCasing : FieldRule.PrivateCasing, newName);
    }

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
    /// string. Protected fields aren't written (and [Serializable] types are skipped anyway): they're guarded like
    /// private ones, by their exact name.
    /// </summary>
    public static bool IsDataMember(IFieldSymbol field) =>
        !field.IsConst && !field.IsStatic
        && field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

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
    /// Whether a member of the type or a base type is paired with this one by name, in the conventions serializers and
    /// data binding use: 'ShouldSerializeitems' and 'Resetitems' (Json.NET, XML serialization, designers),
    /// 'itemsSpecified' (XML serialization), 'itemsChanged'/'itemsChanging' and 'OnitemsChanged'/'OnitemsChanging'
    /// (data binding). Renaming only one of them would break the pairing.
    /// </summary>
    public static bool HasRelatedMemberName(INamedTypeSymbol type, ISymbol member)
    {
        var name = member.Name;
        var paired = new[]
        {
            "ShouldSerialize" + name, "Reset" + name, name + "Specified",
            name + "Changed", name + "Changing", "On" + name + "Changed", "On" + name + "Changing",
        };

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (paired.Any(p => !current.GetMembers(p).IsEmpty))
            {
                return true;
            }
        }

        return false;
    }

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
    public static bool CanRename(IFieldSymbol field, string newName, FieldStyle style, CancellationToken cancellationToken) =>
        CanRename(field, newName, style, TypeFacts.For(field.ContainingType, cancellationToken));

    /// <summary>Like the other overload, with the type's facts gathered once for all its fields.</summary>
    public static bool CanRename(IFieldSymbol field, string newName, FieldStyle style, TypeFacts facts)
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

        // Another field of the type would get the same name (the field itself is one of them).
        if (facts.CountNewName(style, newName) > 1)
        {
            return false;
        }

        // The old name as a word in a string or in code excluded by '#if', or as an inferred member name. A field
        // name made of other characters than \w (rare formatting characters) is checked with the exact pattern.
        var oldName = field.Name;
        return !(facts.InferredNames.Contains(oldName) || (Word.Match(oldName) is { Success: true } m && m.Length == oldName.Length
            ? facts.Words.Contains(oldName)
            : facts.Texts.Any(t => Regex.IsMatch(t, @"(?<![\w@])" + Regex.Escape(oldName) + @"(?!\w)"))));
    }

    /// <summary>
    /// Whether an active SDK naming rule for fields asks for this one-letter prefix ('s_' for static fields, as the .NET
    /// runtime does): BRO1307 then leaves such names alone instead of contradicting the team's own rule.
    /// </summary>
    public static bool IsPrefixRequiredByNamingRule(string prefix, AnalyzerConfigOptions options)
    {
        string? Get(string key) => options.TryGetValue(key, out var v) ? v.Split(':')[0].Trim() : null;
        foreach (var key in options.Keys)
        {
            if (!key.StartsWith("dotnet_naming_rule.", System.StringComparison.Ordinal) || !key.EndsWith(".symbols", System.StringComparison.Ordinal))
            {
                continue;
            }

            var rule = key.Substring("dotnet_naming_rule.".Length, key.Length - "dotnet_naming_rule.".Length - ".symbols".Length);
            var kinds = Get($"dotnet_naming_symbols.{Get(key)}.applicable_kinds");
            if (Get($"dotnet_naming_rule.{rule}.style") is { } style
                && Get($"dotnet_naming_rule.{rule}.severity") is not ("none" or "silent")
                && (kinds is null || kinds.Split(',').Select(k => k.Trim()).Any(k => k is "*" or "field"))
                && Get($"dotnet_naming_style.{style}.required_prefix") == prefix)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The style of the SDK naming rule (dotnet_naming_rule.*) that covers private instance fields: its symbols apply to
    /// fields (or '*') and private (or '*') with no required modifiers ('static' or 'readonly' rules are for other fields),
    /// it isn't turned off, and its style is camel case without a prefix or with '_' (anything else: null). The lowest
    /// 'priority' wins, then the rule name, so the choice doesn't depend on key order.
    /// </summary>
    private static FieldStyle? GetStyleFromNamingRules(AnalyzerConfigOptions options)
    {
        string? Get(string key) => options.TryGetValue(key, out var v) ? v.Split(':')[0].Trim() : null;
        static bool Has(string? list, string item) =>
            list is null || list.Split(',').Select(p => p.Trim()).Any(p => p == "*" || p.Equals(item, System.StringComparison.OrdinalIgnoreCase));

        var candidates = new System.Collections.Generic.List<(int Priority, string Name, FieldStyle Style)>();
        foreach (var key in options.Keys)
        {
            if (!key.StartsWith("dotnet_naming_rule.", System.StringComparison.Ordinal) || !key.EndsWith(".symbols", System.StringComparison.Ordinal))
            {
                continue;
            }

            var rule = key.Substring("dotnet_naming_rule.".Length, key.Length - "dotnet_naming_rule.".Length - ".symbols".Length);
            var symbols = Get(key);
            var style = Get($"dotnet_naming_rule.{rule}.style");
            if (symbols is null || style is null || Get($"dotnet_naming_rule.{rule}.severity") is "none" or "silent"
                || !Has(Get($"dotnet_naming_symbols.{symbols}.applicable_kinds"), "field")
                || !Has(Get($"dotnet_naming_symbols.{symbols}.applicable_accessibilities"), "private")
                || !string.IsNullOrEmpty(Get($"dotnet_naming_symbols.{symbols}.required_modifiers"))
                || Get($"dotnet_naming_style.{style}.capitalization") != "camel_case"
                || !string.IsNullOrEmpty(Get($"dotnet_naming_style.{style}.required_suffix"))
                || !string.IsNullOrEmpty(Get($"dotnet_naming_style.{style}.word_separator")))
            {
                continue;
            }

            var prefix = Get($"dotnet_naming_style.{style}.required_prefix") ?? string.Empty;
            if (prefix is not ("" or "_"))
            {
                continue;
            }

            var priority = int.TryParse(Get($"dotnet_naming_rule.{rule}.priority"), out var p) ? p : int.MaxValue;
            candidates.Add((priority, rule, prefix == "_" ? FieldStyle.UnderscoreCamelCase : FieldStyle.CamelCase));
        }

        return candidates.Count == 0 ? null : candidates.OrderBy(c => c.Priority).ThenBy(c => c.Name, System.StringComparer.Ordinal).First().Style;
    }

    /// <summary>Protected and private protected fields that aren't constants or static readonly.</summary>
    private static bool IsProtectedChecked(IFieldSymbol field) =>
        IsSourceField(field)
        && field.DeclaredAccessibility is Accessibility.Protected or Accessibility.ProtectedAndInternal
        && !field.IsConst
        && !(field.IsStatic && field.IsReadOnly);

    /// <summary>'_size' -> 'size', '_Size' -> 'Size'; null without a leading underscore or a valid result.</summary>
    private static string? WithoutLeadingUnderscores(string name)
    {
        var core = name.TrimStart('_');
        return core.Length < name.Length && core.Length > 0 && char.IsLetter(core[0]) && SyntaxFacts.IsValidIdentifier(core)
            && SyntaxFacts.GetKeywordKind(core) == SyntaxKind.None
            ? core
            : null;
    }

    /// <summary>'_Count' -> '_count'; null when it already starts lower-case after the underscores.</summary>
    private static string? LowerAfterUnderscores(string name)
    {
        var index = name.Length - name.TrimStart('_').Length;
        if (index == name.Length || !char.IsUpper(name[index]))
        {
            return null;
        }

        var result = name.Substring(0, index) + CamelCaseNames.GetNewName(name.Substring(index));
        return SyntaxFacts.IsValidIdentifier(result) ? result : null;
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

    private static bool IsSourceField(IFieldSymbol field) =>
        !field.IsImplicitlyDeclared && field.Locations.Any(l => l.IsInSource);

    /// <summary>
    /// What <see cref="CanRename(IFieldSymbol, string, FieldStyle, TypeFacts)"/> needs from a type's declarations, read
    /// once: the words in its strings and in code excluded by '#if', and its inferred anonymous/tuple member names.
    /// </summary>
    internal sealed class TypeFacts
    {
        private readonly INamedTypeSymbol type;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<FieldStyle, System.Collections.Generic.Dictionary<string, int>> newNames = new();

        private TypeFacts(INamedTypeSymbol type)
        {
            this.type = type;
        }

        public System.Collections.Generic.List<string> Texts { get; } = new();

        public System.Collections.Generic.HashSet<string> Words { get; } = new(System.StringComparer.Ordinal);

        public System.Collections.Generic.HashSet<string> InferredNames { get; } = new(System.StringComparer.Ordinal);

        public static TypeFacts For(INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            var facts = new TypeFacts(type);

            // Only the type's own field names matter as inferred names; checking just those skips most identifiers.
            var fieldNames = new System.Collections.Generic.HashSet<string>(type.GetMembers().OfType<IFieldSymbol>().Select(f => f.Name), System.StringComparer.Ordinal);
            foreach (var reference in type.DeclaringSyntaxReferences)
            {
                var declaration = reference.GetSyntax(cancellationToken);
                foreach (var token in declaration.DescendantTokens(descendIntoTrivia: true))
                {
                    if (token.IsKind(SyntaxKind.StringLiteralToken))
                    {
                        facts.Texts.Add(token.ValueText);
                    }
                    else if (token.IsKind(SyntaxKind.IdentifierToken) && fieldNames.Contains(token.ValueText) && CamelCaseNames.IsInferredMemberName(token))
                    {
                        facts.InferredNames.Add(token.ValueText);
                    }
                }

                facts.Texts.AddRange(declaration.DescendantTrivia(descendIntoTrivia: true)
                    .Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)).Select(t => t.ToString()));
            }

            foreach (var text in facts.Texts)
            {
                foreach (Match match in Word.Matches(text))
                {
                    facts.Words.Add(match.Value);
                }
            }

            return facts;
        }

        /// <summary>How many of the type's fields <see cref="GetNewName(IFieldSymbol, FieldStyle)"/> gives this name.</summary>
        public int CountNewName(FieldStyle style, string newName)
        {
            var counts = newNames.GetOrAdd(style, s =>
            {
                var map = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
                foreach (var f in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (GetNewName(f, s) is { } name)
                    {
                        map[name] = map.TryGetValue(name, out var n) ? n + 1 : 1;
                    }
                }

                return map;
            });
            return counts.TryGetValue(newName, out var count) ? count : 0;
        }
    }
}
