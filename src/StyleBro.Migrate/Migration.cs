using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Migrate;

/// <summary>
/// Turns a <see cref="StyleCopSetup"/> into .editorconfig settings. A StyleBro rule is on only when StyleCop enforced
/// every rule it replaces (a team that turned SA1201 off doesn't get BRO1001's full sort), with the weakest of their
/// severities; each SDK rule StyleBro relies on gets the strongest severity of the StyleCop rules it covers,
/// with its options taken from stylecop.json. Every key the preset sets is written, so the result overrides the preset
/// completely (an .editorconfig beats the preset's global config).
/// </summary>
internal static class Migration
{
    public const string BeginMarker = "# BEGIN stylebro-migrate";
    public const string EndMarker = "# END stylebro-migrate";
    public const string MainSection = "*.cs";

    /// <summary>The StyleBro rules that read documentation as XML, like the StyleCop rules they replace.</summary>
    private static readonly HashSet<string> XmlDocumentationRules = new(StringComparer.Ordinal)
    {
        "BRO1603", "BRO1604", "BRO1605", "BRO1606", "BRO1607", "BRO1608", "BRO1609", "BRO1610", "BRO1611",
    };

    /// <summary>
    /// The StyleBro rules that write English sentences (property summary verbs, the constructor and finalizer sentences).
    /// StyleCop checks the rules they replace against translated texts when stylecop.json's documentationCulture isn't
    /// English, so they're off then. The other documentation rules check structure or punctuation, not wording.
    /// </summary>
    private static readonly HashSet<string> EnglishWordingRules = new(StringComparer.Ordinal)
    {
        "BRO1604", "BRO1605", "BRO1606", "BRO1607",
    };

    private static readonly Regex StyleCopIds = new(@"SA(\d{4})(?:-SA(\d{4}))?");

    /// <summary>Every StyleBro rule and the StyleCop rules it replaces, read from the analyzers' descriptions.</summary>
    public static IReadOnlyList<(string Id, string Title, IReadOnlyList<string> StyleCop)> StyleBroRules()
    {
        var analyzers = typeof(StyleBro.Analyzers.DiagnosticIds).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!);
        return analyzers.SelectMany(a => a.SupportedDiagnostics)
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .Select(d => (d.Id, d.Title.ToString(), (IReadOnlyList<string>)ExpandIds(d.Description.ToString())))
            .ToList();
    }

    /// <summary>The settings for the whole repository. <paramref name="fieldStyle"/> null: inferred from the code.</summary>
    public static Result Generate(StyleCopSetup setup, string root, string? fieldStyle = null)
    {
        var result = new Result();
        var lines = result.Lines;
        result.FieldStyle = fieldStyle ?? InferFieldStyle(setup, root, result.Notes);

        lines.Add("# StyleBro rules: on when StyleCop enforced every rule they replace.");
        foreach (var (id, _, styleCop) in StyleBroRules())
        {
            foreach (var sa in styleCop)
            {
                AddReplacement(result, sa, id);
            }

            var relevant = Relevant(setup, id, styleCop, result.FieldStyle);
            var severity = Weakest(setup, relevant);
            if (!setup.DocumentationParsed && XmlDocumentationRules.Contains(id))
            {
                foreach (var sa in relevant.Where(setup.IsOn))
                {
                    result.Reasons[sa] = "StyleCop never checked it here: no project sets GenerateDocumentationFile, so the build doesn't parse documentation (SA0001)";
                }

                severity = Severity.None;
                relevant = [];
            }

            if (NonEnglishCulture(setup) is { } culture && EnglishWordingRules.Contains(id))
            {
                foreach (var sa in relevant.Where(setup.IsOn))
                {
                    result.Reasons[sa] = $"stylecop.json's documentationCulture is {culture}: StyleCop checks the translated text, {id} writes English";
                }

                severity = Severity.None;
                relevant = [];
            }

            // The XML header rule only stands in for StyleCop's XML header; a plain header is IDE0073's (below).
            if (id == StyleBro.Analyzers.DiagnosticIds.FileHeader && !XmlHeader(setup))
            {
                severity = Severity.None;
                relevant = [];
            }

            if (severity > Severity.None)
            {
                result.Covered.UnionWith(styleCop);
            }
            else
            {
                var off = relevant.Where(sa => !setup.IsOn(sa)).ToList();
                foreach (var sa in relevant.Where(setup.IsOn))
                {
                    result.Reasons[sa] = $"{id} also enforces {string.Join(", ", off)}, which {(off.Count == 1 ? "is" : "are")} off";
                }
            }

            lines.Add($"dotnet_diagnostic.{id}.severity = {Name(severity)}");
        }

        // StyleCop's SX1309 (private fields begin with '_') is BRO1303 with '_camelCase'.
        if (result.FieldStyle == "_camelCase" && setup.IsOn("SX1309")
            && !lines.Contains($"dotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.PrivateFieldNaming}.severity = none"))
        {
            result.Covered.Add("SX1309");
        }

        AddMemberOrder(setup, lines);
        lines.Add($"{StyleBro.Analyzers.Naming.FieldNames.StyleKey} = {result.FieldStyle}");

        // StyleCop wants private constants and static readonly fields in PascalCase (SA1303, SA1311), whatever the SDK's
        // naming rules say: pinned, so BRO1306 doesn't follow a naming rule StyleCop never enforced.
        lines.Add($"{StyleBro.Analyzers.Naming.FieldNames.StaticStyleKey} = PascalCase");
        AddDocumentationScope(setup, lines);
        AddPunctuationExclusions(setup, lines);
        if (NonEnglishCulture(setup) is { } documentationCulture)
        {
            result.Notes.Add($"stylecop.json's documentationCulture is {documentationCulture}: {string.Join(", ", EnglishWordingRules.OrderBy(r => r, StringComparer.Ordinal))} (English summary sentences) are off.");
        }

        lines.Add($"{StyleBro.Analyzers.Layout.Braces.ConsecutiveUsingsKey} = {Bool(setup.Setting("layoutRules", "allowConsecutiveUsings") is not { ValueKind: JsonValueKind.False })}");
        AddHungarianPrefixes(setup, lines);
        AddTupleElementCasing(setup, lines, result);
        if (setup.Setting("namingRules", "allowedNamespaceComponents") is { ValueKind: JsonValueKind.Array } components && components.GetArrayLength() > 0)
        {
            lines.Add($"{StyleBro.Analyzers.Naming.NamespaceNames.AllowedKey} = {string.Join(", ", components.EnumerateArray().Select(c => c.GetString()))}");
        }

        if (!lines.Contains($"dotnet_diagnostic.{StyleBro.Analyzers.DiagnosticIds.FileHeader}.severity = none"))
        {
            AddFileHeader(setup, lines);
        }

        lines.Add(string.Empty);
        lines.Add("# Built-in .NET rules, with the severity of the StyleCop rules they cover.");
        var sdkStart = lines.Count;
        AddSdk(setup, lines, result);

        // The team's own SDK settings stay: their code is already formatted with them.
        var own = fieldStyle is null ? OwnKeys(root) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = lines.Skip(sdkStart).Where(line => KeyOf(line) is { } key && own.Contains(key)).ToList();
        if (kept.Count > 0)
        {
            lines.RemoveAll(kept.Contains);
            result.OwnKeys.UnionWith(kept.Select(KeyOf)!);
            result.Notes.Add($"Kept the repository's own settings for: {string.Join(", ", kept.Select(KeyOf))}.");
        }

        // IDE0055 on in a multi-targeted repository: plain 'dotnet format' crashes there, 'stylebro-migrate format' doesn't.
        var formattingOn = lines.Any(l => l.StartsWith("dotnet_diagnostic.IDE0055.severity = ", StringComparison.Ordinal)
            && !l.EndsWith("= none", StringComparison.Ordinal) && !l.EndsWith("= suggestion", StringComparison.Ordinal) && !l.EndsWith("= silent", StringComparison.Ordinal));
        if (formattingOn && InitCommand.MultiTargetedProjects(root).Any())
        {
            result.Notes.Add("This repository has multi-targeted projects. " + InitCommand.MultiTargetedHint.Replace("\n", " "));
        }

        return result;
    }

    /// <summary>The settings for a scope: the lines whose value differs from the repository-wide ones (<paramref name="main"/>).</summary>
    public static List<string> GenerateScope(StyleCopSetup setup, Scope scope, string root, Result main)
    {
        var inScope = Generate(setup.For(scope), root, main.FieldStyle);
        var mainValues = main.Lines.Select(Split).Where(p => p.Key is not null).ToDictionary(p => p.Key!, p => p.Value);
        return inScope.Lines
            .Where(line => Split(line) is { Key: { } key } pair && !main.OwnKeys.Contains(key)
                && (!mainValues.TryGetValue(key, out var value) || value != pair.Value))
            .ToList();

        static (string? Key, string Value) Split(string line)
        {
            var equals = line.IndexOf('=');
            return line.StartsWith("#", StringComparison.Ordinal) || equals < 0
                ? (null, line)
                : (line.Substring(0, equals).Trim(), line.Substring(equals + 1).Trim());
        }
    }

    /// <summary>The keys the root .editorconfig sets for all C# files, outside the generated blocks (init/--write and --modernize).</summary>
    public static HashSet<string> OwnKeys(string root)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(root, ".editorconfig");
        if (!File.Exists(path))
        {
            return keys;
        }

        bool inBlock = false;
        bool inCSharp = false;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("# BEGIN stylebro-", StringComparison.Ordinal) || line.StartsWith("# END stylebro-", StringComparison.Ordinal))
            {
                inBlock = line.StartsWith("# BEGIN stylebro-", StringComparison.Ordinal);
                inCSharp = false;
            }
            else if (line.StartsWith("[", StringComparison.Ordinal))
            {
                inCSharp = StyleCopSetup.AppliesToCSharp(line.Trim('[', ']'));
            }
            else if (!inBlock && inCSharp && KeyOf(line) is { } key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    /// <summary>The blocks to write, per .editorconfig (relative path): the repository-wide settings and every scope's.</summary>
    public static SortedDictionary<string, List<(string Section, List<string> Lines)>> Plan(StyleCopSetup setup, string root, Result main)
    {
        var plan = new SortedDictionary<string, List<(string, List<string>)>>(StringComparer.OrdinalIgnoreCase)
        {
            [".editorconfig"] = [(MainSection, main.Lines)],
        };
        foreach (var scope in setup.Scopes)
        {
            var lines = GenerateScope(setup, scope, root, main);
            if (lines.Count == 0)
            {
                continue;
            }

            if (!plan.TryGetValue(scope.File, out var blocks))
            {
                plan[scope.File] = blocks = [];
            }

            blocks.Add((scope.Section, lines));
        }

        return plan;
    }

    /// <summary>
    /// Directory.Build.props with &lt;StyleBroPreset&gt;none&lt;/StyleBroPreset&gt;: the generated settings replace the preset,
    /// and an .editorconfig can't take back a key the preset sets (any value of the using-sorting keys makes
    /// 'dotnet format' sort usings). Null when it's already there.
    /// </summary>
    public static string? DisablePreset(string? existing)
    {
        const string Property = "<StyleBroPreset>none</StyleBroPreset>";
        if (existing is null)
        {
            return "<Project>\n  <PropertyGroup>\n    <!-- The settings come from stylebro-migrate's block in .editorconfig. -->\n    " + Property + "\n  </PropertyGroup>\n</Project>\n";
        }

        if (existing.Contains("<StyleBroPreset>", StringComparison.Ordinal))
        {
            return null;
        }

        var newLine = existing.Contains("\r\n") ? "\r\n" : "\n";
        var group = Regex.Match(existing, @"<PropertyGroup\s*>");
        if (group.Success)
        {
            return existing.Insert(group.Index + group.Length, newLine + "    " + Property);
        }

        var project = Regex.Match(existing, @"<Project\b[^>]*>");
        return project.Success
            ? existing.Insert(project.Index + project.Length, newLine + "  <PropertyGroup>" + newLine + "    " + Property + newLine + "  </PropertyGroup>")
            : null;
    }

    /// <summary>The block that goes into an .editorconfig.</summary>
    public static string Render(IEnumerable<(string Section, List<string> Lines)> sections)
    {
        var text = new StringBuilder();
        text.Append(BeginMarker).Append('\n');
        text.Append("# Generated from this repository's StyleCop settings by stylebro-migrate. Run it again to update; edits\n");
        text.Append("# between these markers are overwritten.\n");
        var first = true;
        foreach (var (section, lines) in sections)
        {
            if (!first)
            {
                text.Append('\n');
            }

            first = false;
            text.Append('[').Append(section).Append("]\n");
            foreach (var line in lines)
            {
                text.Append(line).Append('\n');
            }
        }

        text.Append(EndMarker).Append('\n');
        return text.ToString();
    }

    /// <summary>The .editorconfig with the block replaced, or appended when there's none yet.</summary>
    public static string Apply(string? existing, string block, string beginMarker = BeginMarker, string endMarker = EndMarker)
    {
        if (string.IsNullOrEmpty(existing))
        {
            return block;
        }

        var normalized = existing.Replace("\r\n", "\n");
        var begin = normalized.IndexOf(beginMarker, StringComparison.Ordinal);
        var end = normalized.IndexOf(endMarker, StringComparison.Ordinal);
        if (begin >= 0 && end > begin)
        {
            var after = normalized.IndexOf('\n', end);
            return normalized.Substring(0, begin) + block + (after < 0 ? string.Empty : normalized.Substring(after + 1));
        }

        return normalized.TrimEnd('\n') + "\n\n" + block;
    }

    /// <summary>The key of a 'key = value' line, or null for comments and other lines.</summary>
    private static string? KeyOf(string line)
    {
        var equals = line.IndexOf('=');
        return line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal) || equals <= 0
            ? null
            : line.Substring(0, equals).Trim();
    }

    /// <summary>
    /// The StyleCop rules that decide whether a StyleBro rule is on. stylecop.json can make some of them moot:
    /// SA1203/SA1204/SA1214 check nothing when elementOrder leaves out constant/static/readonly, and SA1309 (no
    /// leading underscore) is exactly what the '_camelCase' style turns around.
    /// </summary>
    private static List<string> Relevant(StyleCopSetup setup, string id, IReadOnlyList<string> styleCop, string fieldStyle)
    {
        var moot = new HashSet<string>(StringComparer.Ordinal);
        if (setup.Setting("orderingRules", "elementOrder") is { ValueKind: JsonValueKind.Array } order)
        {
            var items = order.EnumerateArray().Select(e => e.GetString()).ToHashSet();
            if (!items.Contains("constant"))
            {
                moot.Add("SA1203");
            }

            if (!items.Contains("static"))
            {
                moot.Add("SA1204");
            }

            if (!items.Contains("readonly"))
            {
                moot.Add("SA1214");
            }
        }

        if (id == StyleBro.Analyzers.DiagnosticIds.PrivateFieldNaming && fieldStyle == "_camelCase")
        {
            moot.Add("SA1309");
        }

        return styleCop.Where(sa => !moot.Contains(sa)).ToList();
    }

    private static bool XmlHeader(StyleCopSetup setup) =>
        setup.Setting("documentationRules", "xmlHeader") is not { ValueKind: JsonValueKind.False };

    private static string Company(StyleCopSetup setup) =>
        setup.Setting("documentationRules", "companyName")?.GetString() ?? "PlaceholderCompany";

    /// <summary>
    /// stylecop.json's copyrightText as an .editorconfig value: line breaks as '\n', its custom variables filled in.
    /// {companyName} and {fileName} stay; both BRO1615 and IDE0073 fill in {fileName} per file.
    /// </summary>
    private static string CopyrightTemplate(StyleCopSetup setup)
    {
        var text = setup.Setting("documentationRules", "copyrightText")?.GetString() ?? "Copyright (c) {companyName}. All rights reserved.";
        if (setup.Setting("documentationRules", "variables") is { ValueKind: JsonValueKind.Object } variables)
        {
            foreach (var variable in variables.EnumerateObject().Where(v => v.Value.ValueKind == JsonValueKind.String))
            {
                text = text.Replace("{" + variable.Name + "}", variable.Value.GetString());
            }
        }

        return text.Replace("\r\n", "\n").Replace("\n", "\\n");
    }

    /// <summary>BRO1615's settings, from stylecop.json's documentationRules.</summary>
    private static void AddFileHeader(StyleCopSetup setup, List<string> lines)
    {
        lines.Add($"{StyleBro.Analyzers.Documentation.FileHeaderOptions.CompanyKey} = {Company(setup)}");
        lines.Add($"{StyleBro.Analyzers.Documentation.FileHeaderOptions.CopyrightKey} = {CopyrightTemplate(setup)}");
        if (setup.Setting("documentationRules", "headerDecoration")?.GetString() is { Length: > 0 } decoration)
        {
            lines.Add($"{StyleBro.Analyzers.Documentation.FileHeaderOptions.DecorationKey} = {decoration}");
        }
    }

    /// <summary>BRO1311's casing, from stylecop.json's namingRules (only camelCase; PascalCase is the default).</summary>
    private static void AddTupleElementCasing(StyleCopSetup setup, List<string> lines, Result result)
    {
        if (setup.Setting("namingRules", "tupleElementNameCasing") is { ValueKind: JsonValueKind.String } casing
            && string.Equals(casing.GetString(), "camelCase", StringComparison.OrdinalIgnoreCase))
        {
            lines.Add($"{StyleBro.Analyzers.Naming.TupleElementNames.CasingKey} = camelCase");
        }

        if (setup.Setting("namingRules", "includeInferredTupleElementNames") is { ValueKind: JsonValueKind.True } && setup.IsOn("SA1316"))
        {
            result.Notes.Add("stylecop.json sets includeInferredTupleElementNames: BRO1311 checks names in tuple types only, not names a tuple literal infers.");
        }
    }

    /// <summary>BRO1310's prefixes, from stylecop.json's namingRules (only what differs from the defaults).</summary>
    private static void AddHungarianPrefixes(StyleCopSetup setup, List<string> lines)
    {
        if (setup.Setting("namingRules", "allowedHungarianPrefixes") is { ValueKind: JsonValueKind.Array } allowed && allowed.GetArrayLength() > 0)
        {
            lines.Add($"{StyleBro.Analyzers.Naming.HungarianNames.AllowedKey} = {string.Join(", ", allowed.EnumerateArray().Select(p => p.GetString()))}");
        }

        if (setup.Setting("namingRules", "allowCommonHungarianPrefixes") is { ValueKind: JsonValueKind.False })
        {
            lines.Add($"{StyleBro.Analyzers.Naming.HungarianNames.AllowCommonKey} = false");
        }
    }

    /// <summary>stylecop.json's documentationCulture when it isn't English (StyleCop's default is en-US), else null.</summary>
    private static string? NonEnglishCulture(StyleCopSetup setup)
    {
        var culture = setup.Setting("documentationRules", "documentationCulture")?.GetString();
        return string.IsNullOrEmpty(culture) || string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase)
            || culture.StartsWith("en-", StringComparison.OrdinalIgnoreCase)
            ? null
            : culture;
    }

    /// <summary>BRO1603's excluded tags, from stylecop.json's excludeFromPunctuationCheck when it isn't StyleCop's default.</summary>
    private static void AddPunctuationExclusions(StyleCopSetup setup, List<string> lines)
    {
        if (setup.Setting("documentationRules", "excludeFromPunctuationCheck") is { ValueKind: JsonValueKind.Array } excluded)
        {
            var tags = excluded.EnumerateArray().Select(t => t.GetString()).ToList();
            if (!tags.SequenceEqual([StyleBro.Analyzers.Documentation.DocumentationPeriods.DefaultExcluded]))
            {
                lines.Add($"{StyleBro.Analyzers.Documentation.DocumentationPeriods.ExcludeKey} = {string.Join(", ", tags)}");
            }
        }
    }

    /// <summary>Which members need documentation (BRO1601), from stylecop.json's documentationRules.</summary>
    private static void AddDocumentationScope(StyleCopSetup setup, List<string> lines)
    {
        foreach (var (name, key, defaultValue) in new[]
        {
            ("documentExposedElements", "stylebro_document_exposed_elements", true),
            ("documentInternalElements", "stylebro_document_internal_elements", true),
            ("documentPrivateElements", "stylebro_document_private_elements", false),
        })
        {
            var value = setup.Setting("documentationRules", name) is { ValueKind: JsonValueKind.True or JsonValueKind.False } setting
                ? setting.GetBoolean()
                : defaultValue;
            lines.Add($"{key} = {Bool(value)}");
        }
    }

    private static void AddMemberOrder(StyleCopSetup setup, List<string> lines)
    {
        // StyleCop's kind and access order (BRO1001's defaults); stylecop.json's elementOrder decides which of
        // constant/static/readonly order members within an accessibility.
        lines.Add("stylebro_member_order = field, constructor, finalizer, delegate, event, enum, interface, property, indexer, conversion, operator, extension, method, struct, class");
        lines.Add("stylebro_member_access_order = public, internal, protected_internal, protected, private_protected, private");
        var items = setup.Setting("orderingRules", "elementOrder") is { ValueKind: JsonValueKind.Array } order
            ? order.EnumerateArray().Select(e => e.GetString()).ToHashSet()
            : ["constant", "static", "readonly"];
        lines.Add($"stylebro_member_constants_first = {Bool(items.Contains("constant"))}");
        lines.Add($"stylebro_member_static_first = {Bool(items.Contains("static"))}");
        lines.Add($"stylebro_member_readonly_first = {Bool(items.Contains("readonly"))}");
    }

    /// <summary>
    /// The private field style: '_camelCase' when StyleCop's SA1309 (no leading underscore) is off and most of the
    /// repository's private fields start with '_', 'camelCase' otherwise.
    /// </summary>
    private static string InferFieldStyle(StyleCopSetup setup, string root, List<string> notes)
    {
        if (setup.IsOn("SX1309"))
        {
            notes.Add("SX1309 (fields begin with an underscore) is on, so BRO1303 uses '_camelCase'.");
            return "_camelCase";
        }

        var (underscore, plain) = CountPrivateFields(root);
        var style = !setup.IsOn("SA1309") && underscore > plain ? "_camelCase" : "camelCase";
        notes.Add($"Private fields: {underscore} named '_field', {plain} named 'field'; SA1309 is {(setup.IsOn("SA1309") ? "on" : "off")}, so BRO1303 uses '{style}'.");
        return style;
    }

    private static void AddSdk(StyleCopSetup setup, List<string> lines, Result result)
    {
        void Rule(string ide, params string[] styleCop)
        {
            foreach (var sa in styleCop)
            {
                AddReplacement(result, sa, ide);
            }

            var severity = Strongest(setup, styleCop);
            if (severity > Severity.None)
            {
                result.Covered.UnionWith(styleCop);
            }

            lines.Add($"dotnet_diagnostic.{ide}.severity = {Name(severity)}");
        }

        // Formatting.
        var spacing = Enumerable.Range(1000, 29).Select(n => "SA" + n).Concat(["SA1134", "SA1137", "SA1500", "SA1107"]).ToArray();
        Rule("IDE0055", spacing);
        lines.Add("csharp_new_line_before_open_brace = all");
        lines.Add("csharp_preserve_single_line_blocks = true"); // BRO1508/BRO1509 expand them; the SDK would also expand '{ get; set; }'

        lines.Add($"csharp_preserve_single_line_statements = {Bool(!setup.IsOn("SA1107"))}");
        if (setup.Setting("indentation", "indentationSize") is { ValueKind: JsonValueKind.Number } size)
        {
            lines.Add($"indent_size = {size.GetInt32()}");
        }

        if (setup.Setting("indentation", "tabSize") is { ValueKind: JsonValueKind.Number } tab)
        {
            lines.Add($"tab_width = {tab.GetInt32()}");
        }

        if (setup.Setting("indentation", "useTabs") is { ValueKind: JsonValueKind.True or JsonValueKind.False } useTabs)
        {
            lines.Add($"indent_style = {(useTabs.GetBoolean() ? "tab" : "space")}");
        }

        // UTF-8 with a byte order mark (SA1412, off by default): 'dotnet format' writes it with this setting.
        if (setup.IsOn("SA1412"))
        {
            result.Covered.Add("SA1412");
            lines.Add("charset = utf-8-bom");
        }

        // Braces.
        // StyleBro's BRO1514-BRO1516 replace IDE0011, whose fix breaks multi-targeted projects.
        Rule("IDE0011");
        lines.Add($"csharp_prefer_braces = {(setup.IsOn("SA1503") ? "true" : setup.IsOn("SA1519") || setup.IsOn("SA1520") ? "when_multiline" : "false")}");

        // Access modifiers, type aliases, parentheses.
        // StyleBro's BRO1404/BRO1007 replace IDE0040, whose fix breaks multi-targeted projects (and which can't check
        // SA1205's partial types without SA1400's every member).
        Rule("IDE0040");

        // SA1400 doesn't ask for modifiers on interface members.
        lines.Add("dotnet_style_require_accessibility_modifiers = for_non_interface_members");

        // Modifier order (the SDK's default order; it puts access modifiers first and static next, like SA1206/SA1207).
        Rule("IDE0036", "SA1206", "SA1207");
        lines.Add("csharp_preferred_modifier_order = public,private,protected,internal,file,static,extern,new,virtual,abstract,sealed,override,readonly,unsafe,required,volatile,async");
        var aliases = setup.Setting("readabilityRules", "allowBuiltInTypeAliases") is { ValueKind: JsonValueKind.True };
        Rule("IDE0049", aliases ? [] : ["SA1121"]);
        lines.Add($"dotnet_style_predefined_type_for_locals_parameters_members = {Bool(setup.IsOn("SA1121") && !aliases)}");
        lines.Add($"dotnet_style_predefined_type_for_member_access = {Bool(setup.IsOn("SA1121") && !aliases)}");

        // StyleBro's BRO1405 replaces IDE0047 (which breaks multi-targeted projects and also removes 'a ?? (b ?? c)').
        Rule("IDE0047");

        // StyleBro's BRO1406/BRO1407 replace IDE0048, whose fix breaks multi-targeted projects.
        Rule("IDE0048");
        var clarity = setup.IsOn("SA1407") || setup.IsOn("SA1408") ? "always_for_clarity" : "never_if_unnecessary";
        lines.Add($"dotnet_style_parentheses_in_arithmetic_binary_operators = {clarity}");
        lines.Add($"dotnet_style_parentheses_in_other_binary_operators = {clarity}");
        lines.Add($"dotnet_style_parentheses_in_relational_binary_operators = {clarity}");
        lines.Add("dotnet_style_parentheses_in_other_operators = never_if_unnecessary");

        // Using directives.
        var placement = setup.Setting("orderingRules", "usingDirectivesPlacement")?.GetString() ?? "insideNamespace";
        Rule("IDE0065", placement == "preserve" ? [] : ["SA1200"]);
        lines.Add($"csharp_using_directive_placement = {(placement == "outsideNamespace" ? "outside_namespace" : "inside_namespace")}");

        // 'dotnet format' sorts usings (SA1210, SA1211, and aliases/static usings: SA1209, SA1216, SA1217) whenever either key is set, whatever its value
        // (even 'false'): so the keys are written only when StyleCop sorted them. System first only for SA1208.
        if (setup.IsOn("SA1208") || setup.IsOn("SA1210"))
        {
            result.Covered.UnionWith(["SA1208", "SA1209", "SA1210", "SA1211", "SA1216", "SA1217"]);
            var systemFirst = setup.IsOn("SA1208") && setup.Setting("orderingRules", "systemUsingDirectivesFirst") is not { ValueKind: JsonValueKind.False };
            lines.Add($"dotnet_sort_system_directives_first = {Bool(systemFirst)}");
            var groups = setup.Setting("orderingRules", "blankLinesBetweenUsingGroups")?.GetString();
            lines.Add($"dotnet_separate_import_directive_groups = {Bool(groups == "require")}");
        }

        // Blank lines: StyleBro's BRO1517-BRO1519 replace the SDK's experimental IDE2000/IDE2002/IDE2003, whose fixes break
        // multi-targeted projects (the options stay for the IDE).
        Rule("IDE2000");
        lines.Add($"dotnet_style_allow_multiple_blank_lines_experimental = {Bool(!setup.IsOn("SA1507"))}");
        Rule("IDE2002");
        lines.Add($"csharp_style_allow_blank_lines_between_consecutive_braces_experimental = {Bool(!setup.IsOn("SA1508"))}");
        Rule("IDE2003");
        lines.Add($"dotnet_style_allow_statement_immediately_after_block_experimental = {Bool(!setup.IsOn("SA1513"))}");

        // 'this.' qualification: required (SA1101, IDE0009) or removed (StyleCop's SX1101, IDE0003).
        Rule("IDE0009", "SA1101");
        Rule("IDE0003", "SX1101");
        foreach (var kind in new[] { "field", "property", "method", "event" })
        {
            lines.Add($"dotnet_style_qualification_for_{kind} = {Bool(setup.IsOn("SA1101"))}");
        }

        // File header: a plain header is IDE0073's file_header_template; the XML header is BRO1615's (above).
        // With a plain header StyleCop's SA1633 only wants one; the text is SA1636's (checked only while SA1635 is on too,
        // line by line, trimmed). IDE0073 compares the text, so it's on only when StyleCop compared it as well. Off is
        // written too: a folder whose StyleCop rules are off must override the repository-wide 'warning'.
        if (!XmlHeader(setup))
        {
            var textOff = new[] { "SA1635", "SA1636" }.Where(sa => !setup.IsOn(sa)).ToList();
            if (setup.IsOn("SA1633") && textOff.Count == 0)
            {
                Rule("IDE0073", "SA1633", "SA1635", "SA1636");
                lines.Add("file_header_template = " + CopyrightTemplate(setup).Replace("{companyName}", Company(setup)));
            }
            else
            {
                Rule("IDE0073");
                if (setup.IsOn("SA1633"))
                {
                    result.Reasons["SA1633"] = $"IDE0073 also checks the header's text, which StyleCop didn't here ({string.Join(", ", textOff)} off)";
                }
            }
        }
    }

    private static void AddReplacement(Result result, string styleCop, string id)
    {
        if (!result.Replacements.TryGetValue(styleCop, out var ids))
        {
            result.Replacements[styleCop] = ids = new(StringComparer.Ordinal);
        }

        ids.Add(id);
    }

    /// <summary>The weakest severity of the given StyleCop rules ('none' when any is off, or there are none).</summary>
    private static Severity Weakest(StyleCopSetup setup, IReadOnlyCollection<string> styleCop)
    {
        var weakest = styleCop.Count == 0 ? Severity.None : styleCop.Min(id => Of(setup, id));
        return weakest == Severity.Silent ? Severity.None : weakest;
    }

    /// <summary>The strictest severity of the given StyleCop rules ('none' when all are off).</summary>
    private static Severity Strongest(StyleCopSetup setup, IReadOnlyCollection<string> styleCop)
    {
        var strongest = styleCop.Count == 0 ? Severity.None : styleCop.Max(id => Of(setup, id));
        return strongest == Severity.Silent ? Severity.None : strongest;
    }

    private static Severity Of(StyleCopSetup setup, string id) => setup.Severities.TryGetValue(id, out var s) ? s : Severity.None;

    /// <summary>'Replaces StyleCop SA1201-SA1204 and SA1214' -> SA1201, SA1202, SA1203, SA1204, SA1214.</summary>
    private static List<string> ExpandIds(string description)
    {
        var ids = new List<string>();
        foreach (Match match in StyleCopIds.Matches(description))
        {
            var first = int.Parse(match.Groups[1].Value);
            var last = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : first;
            for (var n = first; n <= last; n++)
            {
                ids.Add("SA" + n);
            }
        }

        return ids;
    }

    /// <summary>Private instance and static fields (not constants, not static readonly), by whether they start with '_'.</summary>
    private static (int Underscore, int Plain) CountPrivateFields(string root)
    {
        int underscore = 0;
        int plain = 0;
        foreach (var file in StyleCopSetup.EnumerateFiles(root).Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            foreach (var field in tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                var modifiers = field.Modifiers;
                if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) || m.IsKind(SyntaxKind.ConstKeyword))
                    || (modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) && modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword))))
                {
                    continue;
                }

                foreach (var variable in field.Declaration.Variables)
                {
                    var name = variable.Identifier.ValueText;
                    if (name.Length > 1 && name[0] == '_' && char.IsLower(name[1]))
                    {
                        underscore++;
                    }
                    else if (name.Length > 0 && char.IsLower(name[0]) && !(name.Length > 2 && name[1] == '_'))
                    {
                        plain++;
                    }
                }
            }
        }

        return (underscore, plain);
    }

    private static string Name(Severity severity) => severity.ToString().ToLowerInvariant();

    private static string Bool(bool value) => value ? "true" : "false";

    /// <summary>The generated settings, and what the report should mention.</summary>
    public sealed class Result
    {
        public List<string> Lines { get; } = new();

        public List<string> Notes { get; } = new();

        /// <summary>Gets styleCop rules that a StyleBro or SDK rule now enforces (or makes moot).</summary>
        public SortedSet<string> Covered { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets why a StyleCop rule that was on isn't covered, when the migration knows better than the mapping.</summary>
        public SortedDictionary<string, string> Reasons { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets for each StyleCop rule, the StyleBro and SDK rules that replace it (for suppressions).</summary>
        public SortedDictionary<string, SortedSet<string>> Replacements { get; } = new(StringComparer.Ordinal);

        public string FieldStyle { get; set; } = "camelCase";

        /// <summary>Gets keys left out because the repository sets them itself.</summary>
        public HashSet<string> OwnKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
