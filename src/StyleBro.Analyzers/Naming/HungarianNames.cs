using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// BRO1310 (SA1305): no Hungarian notation. Like StyleCop, a name that starts with one or two lower-case letters followed
/// by an upper-case one ('iCount', 'strName' doesn't match: three letters) uses a Hungarian prefix, unless the prefix
/// is a common word ('is', 'on', 'to', ...) or allowed by configuration. The new name drops the prefix ('count').
/// The other naming rules include this in their new name when BRO1310 is on, so a name gets one rename.
/// </summary>
internal sealed class HungarianNames
{
    public const string AllowedKey = "stylebro_allowed_hungarian_prefixes";
    public const string AllowCommonKey = "stylebro_allow_common_hungarian_prefixes";

    /// <summary>StyleCop's list of words that look like a prefix but aren't one.</summary>
    private static readonly string[] Common = { "as", "at", "by", "do", "go", "if", "in", "is", "it", "no", "of", "on", "or", "to" };

    private static readonly Regex Prefix = new(@"^(?<prefix>[a-z]{1,2})[A-Z]");

    private readonly HashSet<string> allowed;

    private HungarianNames(HashSet<string> allowed)
    {
        this.allowed = allowed;
        Key = string.Join(",", allowed.OrderBy(p => p, StringComparer.Ordinal));
    }

    /// <summary>Gets the allowed prefixes as one string, for caches: equal settings give equal keys.</summary>
    public string Key { get; }

    /// <summary>The settings for a file: StyleCop's allowedHungarianPrefixes and allowCommonHungarianPrefixes (default true).</summary>
    public static HungarianNames Read(AnalyzerConfigOptions options)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal);
        if (options.TryGetValue(AllowedKey, out var list))
        {
            allowed.UnionWith(list.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0));
        }

        if (!(options.TryGetValue(AllowCommonKey, out var common) && common.Trim().Equals("false", StringComparison.OrdinalIgnoreCase)))
        {
            allowed.UnionWith(Common);
        }

        return new HungarianNames(allowed);
    }

    /// <summary>Like StyleCop, names inside a '*NativeMethods' class keep their Win32 prefixes ('lpBuffer').</summary>
    public static bool IsInNativeMethods(SyntaxNode node) =>
        node.Ancestors().OfType<ClassDeclarationSyntax>().Any(c => c.Identifier.ValueText.EndsWith("NativeMethods", StringComparison.Ordinal));

    /// <summary>
    /// A parameter of an extern, [DllImport] or [LibraryImport] method or local function keeps the native API's name
    /// ('hWnd', 'dwFlags'), like BRO1309 leaves such methods alone (StyleCop #2859).
    /// </summary>
    public static bool IsExternParameter(SyntaxNode node)
    {
        var (modifiers, attributes) = node.Parent?.Parent switch
        {
            MethodDeclarationSyntax method => (method.Modifiers, method.AttributeLists),
            LocalFunctionStatementSyntax function => (function.Modifiers, function.AttributeLists),
            _ => (default, default),
        };
        return node is ParameterSyntax
            && (modifiers.Any(SyntaxKind.ExternKeyword)
                || attributes.SelectMany(list => list.Attributes).Any(a => GetName(a.Name) is "DllImport" or "DllImportAttribute" or "LibraryImport" or "LibraryImportAttribute"));

        static string GetName(NameSyntax name) => name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => string.Empty,
        };
    }

    /// <inheritdoc cref="IsInNativeMethods(SyntaxNode)"/>
    public static bool IsInNativeMethods(ISymbol symbol)
    {
        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.TypeKind == TypeKind.Class && type.Name.EndsWith("NativeMethods", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The new name of a variable or parameter: the casing fix (BRO1301/BRO1302) and, with <paramref name="hungarian"/>,
    /// no prefix. Names starting upper-case (members, types) only get the casing fix, so they can't block a rename.
    /// </summary>
    public static string? GetVariableName(string name, HungarianNames? hungarian)
    {
        var camel = CamelCaseNames.GetNewName(name);
        return hungarian is not null && name.Length > 0 && !char.IsUpper(name[0]) && hungarian.GetNewName(camel ?? name) is { } stripped
            ? stripped
            : camel;
    }

    /// <summary>The name without its Hungarian prefix ('iCount' -> 'count', 'pURL' -> 'url'); null when there is none.</summary>
    public string? GetNewName(string name)
    {
        var match = Prefix.Match(name);
        if (!match.Success || allowed.Contains(match.Groups["prefix"].Value))
        {
            return null;
        }

        return CamelCaseNames.GetNewName(name.Substring(match.Groups["prefix"].Length));
    }
}
