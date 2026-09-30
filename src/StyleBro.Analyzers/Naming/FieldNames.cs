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

/// <summary>
/// Shared logic for BRO1303 (StyleCop SA1306 + SA1309 for private fields): which fields are checked, their new name,
/// and whether renaming is safe.
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

    /// <summary>The new name of a field under BRO1303 or BRO1306, or null when neither applies or it already fits.</summary>
    public static string? GetNewName(IFieldSymbol field, FieldStyle style)
    {
        return IsPascalChecked(field) ? GetPascalName(field.Name)
            : IsChecked(field) ? GetNewName(field.Name, style)
            : null;
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
    /// string.
    /// </summary>
    public static bool IsDataMember(IFieldSymbol field) =>
        !field.IsConst && !field.IsStatic && field.DeclaredAccessibility != Accessibility.Private;

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
        if (field.GetAttributes().Length > 0 || type.IsSerializable)
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
                if ((token.IsKind(SyntaxKind.StringLiteralToken) && (token.ValueText == oldName || (IsDataMember(field) && word.IsMatch(token.ValueText))))
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
}
