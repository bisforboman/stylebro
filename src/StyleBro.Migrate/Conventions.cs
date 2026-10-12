using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Documentation;

namespace StyleBro.Migrate;

/// <summary>
/// The conventions a repository's code already follows, for the settings StyleBro has (owner's decision 2026-10-09,
/// docs/decisions.md: "Detect the main ones"). 'stylebro-migrate init' counts both forms of each in the repository's own
/// C# (syntax only; generated, vendored and EF Core migration files and submodules don't count) and writes the form most
/// of the code uses when it is clear: at least <see cref="Share"/> of at least <see cref="MinimumSample"/> places, or all of
/// at least <see cref="MinimumAgreeing"/>. Ocelot
/// writes '=>' at the start of a wrapped line; StyleBro's default (end) moved it in 115 files.
/// </summary>
internal static class Conventions
{
    /// <summary>The share of places one form needs before init writes it.</summary>
    public const double Share = 0.75;

    /// <summary>
    /// The fewest places a majority is judged on. Fewer places (but at least <see cref="MinimumAgreeing"/>) are followed only
    /// when they all agree; mixed ones turn the rule off like a missing majority.
    /// </summary>
    public const int MinimumSample = 10;

    /// <summary>
    /// The fewest places init judges at all (owner's decision 2026-10-10, docs/decisions.md): with fewer, the default stays and
    /// nothing is written. The package check's test project had one '""', and init switched BRO1106 to literals for it.
    /// </summary>
    public const int MinimumAgreeing = 3;

    /// <summary>The longest report line (the verdicts fit; the summary's lists wrap).</summary>
    public const int Width = 120;

    /// <summary>
    /// The key that makes 'dotnet format' sort usings: it sorts whenever this (or dotnet_separate_import_directive_groups) is
    /// set, whatever the value, and an .editorconfig can't unset a key the preset sets. So the preset doesn't set it, and init
    /// writes it only when the code sorts them (owner's decision 2026-10-11: "Detect, like other conventions"; the preset
    /// re-sorted SmartEnum's 44 files).
    /// </summary>
    public const string SortUsingsKey = "dotnet_sort_system_directives_first";

    /// <summary>
    /// The files' line endings (unit: files). Without the key, 'dotnet format' writes the OS's line ending into the lines it
    /// rewrites: on Windows CRLF into LF files (SmartEnum: 26 files with mixed endings). Files git converts on checkout don't
    /// count (<see cref="ConvertedKey"/>).
    /// </summary>
    public const string EndOfLineKey = "end_of_line";

    /// <summary>Files with a '//' header: [without, with] an empty '//' line at its top or bottom (<see cref="EmptyHeaderLines"/>).</summary>
    public const string HeaderKey = "file header";

    /// <summary>The C# files' charset (unit: files): [no BOM, BOM] (<see cref="ContradictedCharset"/>).</summary>
    public const string CharsetKey = "charset";

    /// <summary>
    /// BRO1307's severity: private static fields with the .NET runtime's 's_' (and 't_' for thread-static) prefix, or without
    /// (owner's decision 2026-10-12: "Detect both"; actions/runner got ~541 's_' statics renamed). There is no switch for static
    /// fields alone, so BRO1307 goes off: repositories with 's_' statics rarely use 'm_' (a naming rule that requires 's_'
    /// would keep them too, but IDE1006 then also suggests 's_' for constants and the other static fields).
    /// </summary>
    public const string StaticPrefixKey = "dotnet_diagnostic." + DiagnosticIds.FieldPrefix + ".severity";

    /// <summary>
    /// IDE0049's severity (unit: files): files that name built-in types only with keywords, or that use framework names
    /// ('String', 'Int32') IDE0049 would rewrite (owner's decision 2026-10-12: "Detect both"; actions/runner got 'String' ->
    /// 'string' 6,424 times in 501 files). Files, not places: every 'int' would outvote the few 'String's.
    /// </summary>
    public const string TypeAliasKey = "dotnet_diagnostic.IDE0049.severity";

    /// <summary>
    /// Every convention init detects: the setting, what is counted, StyleBro's default (or the preset's), and the two values
    /// with what each looks like, and the line that stops StyleBro enforcing either form when the code has no clear majority
    /// (owner's decision 2026-10-10, docs/decisions.md; null: no StyleBro rule of its own enforces it, e.g. the SDK formatter's
    /// brace placement, so the default stays). <c>AlsoKeys</c> get the same value (else, catch and finally go together).
    /// </summary>
    public static readonly IReadOnlyList<Convention> All = new Convention[]
    {
        new(StyleBro.Analyzers.Naming.FieldNames.StyleKey, "private fields", "camelCase", ("camelCase", "named 'field'"), ("_camelCase", "named '_field'"), Off(DiagnosticIds.PrivateFieldNaming)),
        new("csharp_new_line_before_open_brace", "'{' of multi-line blocks", "all", ("all", "on its own line"), ("none", "at the end of the line"), null),
        new("csharp_new_line_before_else", "else/catch/finally after '}'", "true", ("true", "on a new line"), ("false", "on the '}' line"), null, "csharp_new_line_before_catch", "csharp_new_line_before_finally"),
        new("csharp_prefer_braces", "one-line if/else/loop/using/lock bodies", "true", ("true", "with braces"), ("when_multiline", "without braces"), "csharp_prefer_braces = when_multiline") { Enforcer = DiagnosticIds.BracesOmitted, EnforcedValues = new[] { "true" } },
        new("dotnet_style_operator_placement_when_wrapping", "operators where a line wraps", "beginning_of_line", ("beginning_of_line", "at the start of the line"), ("end_of_line", "at the end of the line"), Off(DiagnosticIds.OperatorPlacement)) { Enforcer = DiagnosticIds.OperatorPlacement },
        new(StyleBro.Analyzers.Layout.WrappingPlacement.ArrowKey, "'=>' where a line wraps", "end_of_line", ("end_of_line", "at the end of the line"), ("beginning_of_line", "at the start of the line"), Off(DiagnosticIds.ArrowPlacement)),
        new(StyleBro.Analyzers.Layout.WrappingPlacement.EqualsKey, "'=' where a line wraps", "end_of_line", ("end_of_line", "at the end of the line"), ("beginning_of_line", "at the start of the line"), Off(DiagnosticIds.EqualsPlacement)),
        new("stylebro_trailing_comma", "multi-line initializers", "include", ("include", "with a trailing comma"), ("omit", "without a trailing comma"), Off(DiagnosticIds.TrailingComma)),
        new("stylebro_empty_string_style", "empty strings", "string_empty", ("string_empty", "string.Empty"), ("literal", "\"\""), Off(DiagnosticIds.EmptyString)),
        new("stylebro_null_check_style", "null checks", "pattern_matching", ("pattern_matching", "'is null'"), ("equality_operator", "'== null'"), Off(DiagnosticIds.NullCheckStyle)),
        new("stylebro_summary_layout", "one-line <summary> texts", "multi_line", ("multi_line", "on three lines"), ("single_line_when_fits", "on one line"), Off(DiagnosticIds.SummaryLayout)),
        new("stylebro_inheritdoc_style", "<inheritdoc/> tags", "compact", ("compact", "'<inheritdoc/>'"), ("spaced", "'<inheritdoc />'"), null),
        new("csharp_prefer_simple_default_expression", "default values", "false", ("false", "'default(T)'"), ("true", "'default'"), null),
        new("stylebro_closing_parenthesis_placement", "')' of split lists", "last_item", ("last_item", "after the last item"), ("own_line", "on its own line"), Off(DiagnosticIds.CloseParenthesisOnLastItemLine)),
        new("stylebro_split_list_first_item", "split lists", "next_line", ("next_line", "first item on the next line"), ("same_line", "first item after '('"), Off(DiagnosticIds.SplitParametersStartOnNewLine)),
        new("stylebro_constructor_initializer_placement", "': base(...)'/': this(...)'", "own_line", ("own_line", "on its own line"), ("same_line", "on the constructor's line"), Off(DiagnosticIds.ConstructorInitializerLine)),
        new("stylebro_constraint_placement", "'where' constraints", "own_line", ("own_line", "on their own line"), ("same_line", "on the declaration's line"), Off(DiagnosticIds.ConstraintOnOwnLine)),
        new("stylebro_object_creation_parentheses", "'new T { ... }' with an initializer", "omit", ("omit", "without '()'"), ("include", "with '()'"), Off(DiagnosticIds.ObjectCreationParentheses)),
        new("stylebro_blank_line_between_switch_sections", "switch sections", "include", ("include", "after a blank line"), ("omit", "without a blank line"), Off(DiagnosticIds.BlankLineBetweenSwitchSections)),
        new("csharp_using_directive_placement", "files' using directives", "outside_namespace", ("outside_namespace", "outside the namespace"), ("inside_namespace", "inside the namespace"), Off(DiagnosticIds.UsingPlacement)) { Enforcer = DiagnosticIds.UsingPlacement },
        new(ArithmeticKey, "mixed arithmetic ('a + b * c')", "always_for_clarity", ("always_for_clarity", "with parentheses"), ("never_if_unnecessary", "without parentheses"), Off(DiagnosticIds.ArithmeticPrecedence)) { Enforcer = DiagnosticIds.ArithmeticPrecedence, EnforcedValues = new[] { "always_for_clarity" } },
        new(SingleLineStatementsKey, "statements on their 'if'/loop/'case' line", "false", ("false", "on a line of their own"), ("true", "on their owner's line"), null) { KeepAny = true },
        new(InheritDocKey, "overrides/interface implementations", "warning", ("warning", "with a doc comment"), ("none", "without a doc comment"), Off(DiagnosticIds.InheritDocumentation)),
        new(MemberOrderKey, "types' members", "warning", ("warning", "in BRO1001's order"), ("none", "in another order"), Off(DiagnosticIds.MemberOrdering)) { Unit = "type" },
        new(OverloadsKey, "overloads BRO1001 could split", "false", ("false", "apart"), ("true", "next to each other"), null),
        new(ItemsPerLineKey, "split lists' items", "warning", ("warning", "one per line"), ("none", "grouped on lines"), Off(DiagnosticIds.ParametersOnSameOrSeparateLines)),
        new(SuffixCaseKey, "unsigned literal suffixes", "warning", ("warning", "upper case ('0U', '1UL')"), ("none", "lower case ('0u', '1ul')"), Off(DiagnosticIds.LiteralSuffixCase)),
        new(StaticPrefixKey, "private static fields", "warning", ("warning", "without a prefix"), ("none", "with an 's_'/'t_' prefix"), Off(DiagnosticIds.FieldPrefix)) { Unit = "field" },
        new(RelativeUsingsKey, "usings inside a namespace", "warning", ("warning", "fully qualified"), ("none", "relative ('using Common;')"), Off(DiagnosticIds.QualifiedUsing)) { Unit = "using" },
        new(TypeAliasKey, "built-in type names", "warning", ("warning", "only keywords ('int')"), ("none", "framework names ('Int32')"), Off("IDE0049"), "dotnet_style_predefined_type_for_locals_parameters_members", "dotnet_style_predefined_type_for_member_access") { Unit = "file" },
        new(EndOfLineKey, "C# files' line endings", "unset", ("lf", "LF"), ("crlf", "CRLF"), null) { Unit = "file" },
    };

    /// <summary>
    /// The rules that decide what CSharpier decides too (owner's decision 2026-10-11: "Detect it, turn layout rules off";
    /// StabilityMatrix's pre-commit hook rejected 161 files StyleBro had formatted): whitespace and indentation (IDE0055),
    /// where lines break (lists, constraints, constructor initializers, query clauses, wrapped operators, call chains,
    /// conditionals), single-line blocks, accessors and enums, and the blank lines CSharpier normalizes itself (after '{',
    /// before '}', before '{'/else/while, runs of them, and the ones BRO1504/BRO1505 add where CSharpier removes them:
    /// before a comment in an initializer or above 'else', between accessors; checked on StabilityMatrix). Blank lines
    /// CSharpier keeps as written (after '}', after attributes, around doc comments, between switch sections) and everything
    /// that isn't layout (naming, ordering, documentation, braces, readability) stay StyleBro's. The list is documented in
    /// docs/getting-started.md.
    /// </summary>
    public static readonly string[] CSharpierRules =
    {
        "IDE0055",
        DiagnosticIds.ConstructorInitializerLine,
        DiagnosticIds.SplitParametersStartOnNewLine,
        DiagnosticIds.ParametersOnSameOrSeparateLines,
        DiagnosticIds.OpenParenthesisOnNameLine,
        DiagnosticIds.CloseParenthesisOnLastItemLine,
        DiagnosticIds.ConstraintOnOwnLine,
        DiagnosticIds.EmptyListOnOneLine,
        DiagnosticIds.CommaOnItemLine,
        DiagnosticIds.FirstItemFollowsOpening,
        DiagnosticIds.ItemFollowsComma,
        DiagnosticIds.EnumValueOnOwnLine,
        DiagnosticIds.QueryClauseBlankLine,
        DiagnosticIds.QueryClausesOnSeparateLines,
        DiagnosticIds.QueryClauseAfterMultiLineClause,
        DiagnosticIds.MultiLineQueryClause,
        DiagnosticIds.BlankLineBeforeOpenBrace,
        DiagnosticIds.BlankLineBeforeChainedBlock,
        DiagnosticIds.BlankLineAfterOpenBrace,
        DiagnosticIds.BlankLineBeforeComment,
        DiagnosticIds.ElementsSeparatedByBlankLine,
        DiagnosticIds.SingleLineStatementBlock,
        DiagnosticIds.SingleLineElement,
        DiagnosticIds.AccessorsConsistentLayout,
        DiagnosticIds.BlankLineBeforeWhile,
        DiagnosticIds.MultipleBlankLines,
        DiagnosticIds.BlankLineBeforeCloseBrace,
        DiagnosticIds.OperatorPlacement,
        DiagnosticIds.ArrowPlacement,
        DiagnosticIds.EqualsPlacement,
        DiagnosticIds.CallChainLayout,
        DiagnosticIds.ConditionalLayout,
        DiagnosticIds.AutoAccessorsOnOneLine,
    };

    /// <summary>
    /// Overload groups (same-named methods of a type) whose members have different BRO1001 sort keys, so the default sort
    /// could split them: when the code keeps them next to each other, init writes 'true' (owner's decision 2026-10-11:
    /// "Detect"; Scrutor's public and private 'CanDecorate' overloads were split).
    /// </summary>
    private const string OverloadsKey = StyleBro.Analyzers.Ordering.MemberOrderOptions.KeepOverloadsTogetherKey;

    /// <summary>
    /// BRO1001's severity: types (with members that sort apart) already in its order, or not (owner's decision 2026-10-11:
    /// "Detect, like other conventions"; it reordered 407 files of StabilityMatrix and 167 of Flow.Launcher, which never
    /// sorted). Type members only: namespaces mostly hold one type per file, and BRO1001 reports per type.
    /// </summary>
    private const string MemberOrderKey = "dotnet_diagnostic." + DiagnosticIds.MemberOrdering + ".severity";

    /// <summary>
    /// BRO1108's severity: the items of argument and parameter lists that span lines each on a line of their own, or grouped
    /// (owner's decision 2026-10-12: "Detect, like other conventions"; metadata-extractor groups them, and BRO1108 split
    /// 'GetIndexedDescription(TagX, 1,' + '"a", "b")' (two lines) into one item per line in 25 files). A migration keeps SA1117's setting.
    /// </summary>
    private const string ItemsPerLineKey = "dotnet_diagnostic." + DiagnosticIds.ParametersOnSameOrSeparateLines + ".severity";

    /// <summary>BRO1135's severity: unsigned literal suffixes ('u', 'ul') in upper case or not (Orleans: 114 '0u' became '0U').</summary>
    private const string SuffixCaseKey = "dotnet_diagnostic." + DiagnosticIds.LiteralSuffixCase + ".severity";

    /// <summary>
    /// Files whose checkout ending git picks per machine (core.autocrlf, or 'text' without 'eol'): CRLF on Windows, LF on
    /// Linux. Writing one machine's ending would make 'format' rewrite every file on the other OS; without the key, 'format'
    /// writes the OS's ending, which is what such a checkout has anyway.
    /// </summary>
    private const string ConvertedKey = "end_of_line (converted by git)";

    /// <summary>
    /// The casing of private constants and private static readonly fields (BRO1306), three forms: [PascalCase, camelCase,
    /// _camelCase] (owner's policy, trial 11: Ical.Net had 65 camelCase or '_camelCase' names and 26 PascalCase ones, and
    /// BRO1306 renamed them in 28 files). A clear form is written as <see cref="StyleBro.Analyzers.Naming.FieldNames.StaticStyleKey"/>
    /// (PascalCase is BRO1306's default); mixed turns BRO1306 off.
    /// </summary>
    public const string StaticCasingKey = StyleBro.Analyzers.Naming.FieldNames.StaticStyleKey;

    /// <summary>
    /// BRO1126's severity: usings inside a namespace that name their namespace fully ('using Company.App.Common;'), or
    /// relative to an enclosing namespace ('using Common;'; owner's policy, trial 11: Fitnet had 97 such usings, and BRO1126
    /// expanded every one).
    /// </summary>
    public const string RelativeUsingsKey = "dotnet_diagnostic." + DiagnosticIds.QualifiedUsing + ".severity";

    /// <summary>
    /// Files with a list of 2 or more usings: [sorted with System first only, sorted without only, sorted both ways
    /// (nothing tells them apart), unsorted].
    /// </summary>
    private const string UsingsKey = "using order";

    /// <summary>
    /// The SDK option that keeps 'if (a) return;' and 'case 1: a(); break;' on one line. The preset sets it to false, which
    /// splits them (NSubstitute: 58 lines), so init keeps them once the code has <see cref="MinimumAgreeing"/> (owner's
    /// decision 2026-10-10, docs/decisions.md).
    /// </summary>
    private const string SingleLineStatementsKey = "csharp_preserve_single_line_statements";

    /// <summary>
    /// BRO1601's severity: '/// &lt;inheritdoc/&gt;' on undocumented overrides and implementations is a convention too
    /// (owner's decision 2026-10-10: NCronJob left 43 of 68 undocumented). 'none' is the form "without one".
    /// </summary>
    private const string InheritDocKey = "dotnet_diagnostic." + DiagnosticIds.InheritDocumentation + ".severity";

    /// <summary>The SDK option BRO1406 follows ('never_if_unnecessary' turns it off).</summary>
    private const string ArithmeticKey = "dotnet_style_parentheses_in_arithmetic_binary_operators";

    /// <summary>The framework names IDE0049 rewrites to keywords.</summary>
    private static readonly HashSet<string> FrameworkTypeNames = new(StringComparer.Ordinal)
    {
        "String", "Object", "Boolean", "Char", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Single", "Double", "Decimal",
    };

    /// <summary>The SDK formatter's options (IDE0055's): with CSharpier they don't apply.</summary>
    private static readonly string[] FormatterKeys = { "csharp_new_line_before_open_brace", "csharp_new_line_before_else", SingleLineStatementsKey };

    /// <summary>
    /// What shows that CSharpier formats the repository (a file, relative to the root), or null: a .csharpierrc(.json/.yaml/.yml),
    /// a 'csharpier' tool in [.config/]dotnet-tools.json, or a CSharpier.MsBuild package reference in an MSBuild file.
    /// </summary>
    public static string? CSharpier(string root)
    {
        if (new[] { ".csharpierrc", ".csharpierrc.json", ".csharpierrc.yaml", ".csharpierrc.yml" }.FirstOrDefault(f => File.Exists(Path.Combine(root, f))) is { } config)
        {
            return config;
        }

        if (new[] { Path.Combine(".config", "dotnet-tools.json"), "dotnet-tools.json" }
            .FirstOrDefault(f => File.Exists(Path.Combine(root, f)) && System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(Path.Combine(root, f)), @"""csharpier""", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) is { } tools)
        {
            return tools.Replace('\\', '/');
        }

        return StyleCopSetup.EnumerateFiles(root).Where(StyleCopSetup.IsMSBuild)
            .FirstOrDefault(f => File.ReadAllText(f).Contains("CSharpier.MsBuild", StringComparison.OrdinalIgnoreCase)) is { } project
            ? Path.GetRelativePath(root, project).Replace('\\', '/')
            : null;
    }

    /// <summary>
    /// The lines that turn <see cref="CSharpierRules"/> off (with the '# init:' comment saying why) and the report line, for a
    /// repository CSharpier formats (<paramref name="evidence"/>: <see cref="CSharpier"/>).
    /// </summary>
    public static (List<string> Lines, string Report) CSharpierLines(string evidence)
    {
        var lines = new List<string> { $"# init: CSharpier formats this repository ({evidence}): IDE0055 and the {CSharpierRules.Length - 1} StyleBro rules for what it decides (layout, line breaks, blank lines it normalizes) are off, so the two formatters don't undo each other. To have StyleBro format instead: remove CSharpier, then these lines." };
        lines.AddRange(CSharpierRules.Select(Off));
        return (lines, $"  {"off",-8} CSharpier ({evidence}) -> IDE0055 and {CSharpierRules.Length - 1} layout rules are off: {string.Join(", ", CSharpierRules.Skip(1))}");
    }

    /// <summary>Whether CSharpier decides a convention (<see cref="CSharpierRules"/>): its rule is one of them, or it is an SDK formatter option.</summary>
    public static bool IsCSharpiers(Convention convention) =>
        FormatterKeys.Contains(convention.Key) || CSharpierRules.Contains(convention.Enforcer ?? (convention.Off is { } off ? RuleOf(off) : null));

    /// <summary>
    /// Counts every convention in the repository's own C# files: key -> count per value (in <see cref="Convention.Values"/>'
    /// order). <paramref name="settings"/>: the root .editorconfig's settings (which members BRO1601 checks).
    /// </summary>
    public static Dictionary<string, int[]> Count(string root, IReadOnlyDictionary<string, string>? settings = null)
    {
        var counts = NewCounts();
        var trees = new List<SyntaxTree>();
        var documented = new HashSet<SyntaxTree>();
        var generates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var converted = ConvertedByGit(root);

        // Parsing and counting is per file: in parallel, each file with its own counts, added up in file order.
        var parsed = Migration.OwnCSharpFiles(root)
            .AsParallel()
            .AsOrdered()
            .Select(file =>
            {
                var text = File.ReadAllText(file);
                if (Migration.IsGeneratedOrVendored(Path.GetRelativePath(root, file), text))
                {
                    return default;
                }

                var tree = CSharpSyntaxTree.ParseText(text);
                var own = NewCounts();
                Count(tree, own, settings: settings);
                CountConditional(tree, own);
                if (LineEnding(text) is var ending and >= 0)
                {
                    _ = converted.TryGetValue(Path.GetFullPath(file), out var byAttribute) ? own[ConvertedKey][byAttribute ? 1 : 0]++ : own[EndOfLineKey][ending]++;
                }

                own[CharsetKey][HasBom(file) ? 1 : 0]++;
                return (File: file, Tree: tree, Counts: own);
            })
            .Where(p => p.Tree is not null)
            .ToList();
        foreach (var (file, tree, own) in parsed)
        {
            foreach (var (key, values) in own)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    counts[key][i] += values[i];
                }
            }

            trees.Add(tree);
            if (GeneratesDocumentation(root, Path.GetDirectoryName(file)!, generates))
            {
                documented.Add(tree);
            }
        }

        CountInheritDoc(trees, counts, settings, documented);
        CountRelativeUsings(trees, counts);
        return counts;
    }

    /// <summary>
    /// BRO1126's places: usings inside a namespace (not global, not 'global::' or alias-qualified), relative when an
    /// enclosing namespace has a namespace the code declares named like their first part ('using Common;' inside
    /// 'namespace App.Contracts' with an 'App.Common'), else fully qualified. Across files: the declared namespaces are
    /// the repository's (ponytail: syntax only, so a name relative to a package's namespace counts as qualified).
    /// </summary>
    public static void CountRelativeUsings(IReadOnlyList<SyntaxTree> trees, Dictionary<string, int[]> counts)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);
        var usings = new List<(string[] Namespace, string First)>();
        foreach (var tree in trees)
        {
            foreach (var ns in tree.GetRoot().DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseNamespaceDeclarationSyntax>())
            {
                var parts = ns.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().Reverse()
                    .SelectMany(n => n.Name.ToString().Replace(" ", string.Empty).Split('.'))
                    .ToArray();
                for (var i = 1; i <= parts.Length; i++)
                {
                    declared.Add(string.Join(".", parts.Take(i)));
                }

                foreach (var directive in ns.Usings.Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None)))
                {
                    if (directive.NamespaceOrType is NameSyntax name and not AliasQualifiedNameSyntax
                        && name.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().FirstOrDefault() is { } first)
                    {
                        usings.Add((parts, first.Identifier.ValueText));
                    }
                }
            }
        }

        foreach (var (parts, first) in usings)
        {
            var relative = Enumerable.Range(1, parts.Length).Any(i => declared.Contains(string.Join(".", parts.Take(i)) + "." + first));
            counts[RelativeUsingsKey][relative ? 1 : 0]++;
        }
    }

    /// <summary>
    /// Whether the project a folder belongs to generates documentation (BRO1601 asks for none otherwise, like StyleCop's
    /// SA0001): its .csproj or a Directory.Build.props up to the root sets GenerateDocumentationFile or DocumentationFile.
    /// </summary>
    public static bool GeneratesDocumentation(string root, string directory, Dictionary<string, bool>? cache = null)
    {
        // ponytail: text match, ignores conditions and imports other than Directory.Build.props.
        cache ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (cache.TryGetValue(directory, out var known))
        {
            return known;
        }

        // The nearest .csproj and every Directory.Build.props from the folder up to the root.
        var files = new List<string>();
        var projectFound = false;
        for (var current = directory; current is not null && current.Length >= root.TrimEnd('\\', '/').Length; current = Path.GetDirectoryName(current))
        {
            if (!projectFound && Directory.GetFiles(current, "*.csproj") is { Length: > 0 } projects)
            {
                files.AddRange(projects);
                projectFound = true;
            }

            files.Add(Path.Combine(current, "Directory.Build.props"));
        }

        var result = files.Where(File.Exists).Any(f => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"<GenerateDocumentationFile>\s*true\s*<|<DocumentationFile>", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        cache[directory] = result;
        return result;
    }

    /// <summary>
    /// Adds the places inside '#if' code that is inactive without symbols ('#if NETSTANDARD2_0'): 'format' edits them in the
    /// target framework that defines them (NSubstitute's nullability polyfill had 22 one-line summaries init didn't see). The
    /// file is parsed again with every symbol its directives mention defined, and only the places that were inactive count.
    /// </summary>
    public static void CountConditional(SyntaxTree tree, Dictionary<string, int[]> counts)
    {
        var root = tree.GetRoot();
        var inactive = root.ContainsDirectives
            ? root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)).Select(t => t.Span).ToList()
            : new List<TextSpan>();
        if (inactive.Count == 0)
        {
            return;
        }

        // ponytail: an '#else' branch inactive both ways ('#if A ... #else ...') isn't counted.
        var symbols = root.DescendantTrivia().Select(t => t.GetStructure()).OfType<ConditionalDirectiveTriviaSyntax>()
            .SelectMany(d => d.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            .Select(n => n.Identifier.ValueText)
            .Distinct();
        var options = ((CSharpParseOptions)tree.Options).WithPreprocessorSymbols(symbols);
        Count(CSharpSyntaxTree.ParseText(tree.GetText(), options), counts, inactive);
    }

    /// <summary>
    /// BRO1601's places: the overrides and interface implementations it checks (<see cref="DocumentationComments.InheritsDocumentation"/>),
    /// with a doc comment or without. Needs symbols: one compilation of the files against the running .NET's libraries
    /// (ponytail: no package references, so a member implementing a package's interface isn't seen; enough for a majority).
    /// <paramref name="only"/>: the trees of projects that generate documentation (null: all).
    /// </summary>
    public static void CountInheritDoc(IReadOnlyList<SyntaxTree> trees, Dictionary<string, int[]> counts, IReadOnlyDictionary<string, string>? settings = null, ISet<SyntaxTree>? only = null)
    {
        // Cheap first: only members of types with a base list can override or implement anything.
        var candidates = trees.Where(t => only?.Contains(t) ?? true).SelectMany(t => t.GetRoot().DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax)
            .OfType<MemberDeclarationSyntax>()
            .Where(m => m.Parent is TypeDeclarationSyntax { BaseList: not null } && DocumentationComments.MemberKinds.Any(k => m.IsKind(k))
                && !DocumentationComments.HasConditionalDirective(m)))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var libraries = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator)
            .Where(p => p.Length > 0 && Path.GetDirectoryName(p) == runtime)
            .Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("init", trees, libraries);
        var options = new Options(settings);
        foreach (var group in candidates.GroupBy(m => m.SyntaxTree))
        {
            var model = compilation.GetSemanticModel(group.Key);
            foreach (var member in group)
            {
                var symbols = member is BaseFieldDeclarationSyntax field
                    ? field.Declaration.Variables.Select(v => model.GetDeclaredSymbol(v)).ToList()
                    : new List<ISymbol?> { model.GetDeclaredSymbol(member) };
                if (symbols.All(s => s is not null) && DocumentationComments.InheritsDocumentation(symbols!, options))
                {
                    counts[InheritDocKey][DocumentationComments.HasDocumentation(member) ? 0 : 1]++;
                }
            }
        }
    }

    /// <summary>An empty count for every convention.</summary>
    public static Dictionary<string, int[]> NewCounts()
    {
        var counts = All.ToDictionary(c => c.Key, _ => new int[2], StringComparer.Ordinal);
        counts[UsingsKey] = new int[4];
        counts[StaticCasingKey] = new int[3];
        counts[CharsetKey] = new int[2];
        counts[HeaderKey] = new int[2];
        counts[ConvertedKey] = new int[2];
        return counts;
    }

    /// <summary>A text's line ending: 0 LF, 1 CRLF, -1 none or both.</summary>
    public static int LineEnding(string text)
    {
        var (lf, crlf) = (0, 0);
        for (var i = text.IndexOf('\n'); i >= 0; i = text.IndexOf('\n', i + 1))
        {
            _ = i > 0 && text[i - 1] == '\r' ? crlf++ : lf++;
        }

        return lf > 0 && crlf == 0 ? 0 : crlf > 0 && lf == 0 ? 1 : -1;
    }

    /// <summary>
    /// The files under the root whose line ending git converts on checkout (full paths), each with whether a .gitattributes
    /// 'text' attribute converts it (else core.autocrlf does): 'git ls-files --eol' shows another ending in the working tree
    /// than in the index, and no 'eol' attribute fixes it (<see cref="ConvertedKey"/>).
    /// </summary>
    public static Dictionary<string, bool> ConvertedByGit(string root)
    {
        var (code, output) = PreviewCommand.Git(root, "ls-files", "--eol", "-z");
        return (code == 0 ? Converted(output) : []).ToDictionary(f => Path.GetFullPath(Path.Combine(root, f.Path)), f => f.ByAttribute, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The C# files' line endings for 'stylebro-migrate': init's line-ending convention alone (files git converts on checkout
    /// don't count). The end_of_line line when the files clearly agree (none when mixed or too few), and what to report
    /// (null without C# files).
    /// </summary>
    public static (string? Line, string? Report) LineEndings(string root)
    {
        var counts = EndingCounts(root);
        var (lf, crlf) = (counts[EndOfLineKey][0], counts[EndOfLineKey][1]);
        if (lf + crlf + counts[ConvertedKey].Sum() == 0)
        {
            return (null, null);
        }

        if (ConvertedOutnumber(counts))
        {
            return (null, $"Line endings: {OutnumberedVerdict(counts)}");
        }

        var winner = lf + crlf >= MinimumAgreeing ? Winner(lf, crlf) : -1;
        var found = winner < 0 ? $"{lf} LF, {crlf} CRLF" : $"{(winner == 0 ? lf : crlf)} of {lf + crlf} {(winner == 0 ? "LF" : "CRLF")}";
        var report = $"Line endings: C# files {found} -> " + (winner < 0 ? "end_of_line not set ('format' writes your OS's ending into the lines it changes)" : $"end_of_line = {(winner == 0 ? "lf" : "crlf")}")
            + (ConvertedNote(counts[ConvertedKey]) is { } note ? $"; {note}" : string.Empty);
        return (winner < 0 ? null : $"end_of_line = {(winner == 0 ? "lf" : "crlf")}", report);
    }

    /// <summary>The C# files' line endings alone (<see cref="Count(string, IReadOnlyDictionary{string, string}?)"/>'s, without parsing).</summary>
    public static Dictionary<string, int[]> EndingCounts(string root)
    {
        var counts = NewCounts();
        var converted = ConvertedByGit(root);
        foreach (var file in Migration.OwnCSharpFiles(root))
        {
            var text = File.ReadAllText(file);
            if (!Migration.IsGeneratedOrVendored(Path.GetRelativePath(root, file), text) && LineEnding(text) is var ending and >= 0)
            {
                _ = converted.TryGetValue(Path.GetFullPath(file), out var byAttribute) ? counts[ConvertedKey][byAttribute ? 1 : 0]++ : counts[EndOfLineKey][ending]++;
            }
        }

        return counts;
    }

    /// <summary>
    /// Whether git converts at least as many C# files on checkout as it leaves alone (init and the migration both ask). Then
    /// the converted files are the convention, the machine's own ending, and the few committed with a fixed ending are
    /// outliers, not a vote: xbox-live-developer-tools has 145 converted files and 3 committed as CRLF, and
    /// 'end_of_line = crlf' gave 12,593 ENDOFLINE errors in an LF checkout.
    /// </summary>
    public static bool ConvertedOutnumber(IReadOnlyDictionary<string, int[]> counts) =>
        counts.TryGetValue(ConvertedKey, out var converted) && converted.Sum() > 0
        && converted.Sum() >= (counts.TryGetValue(EndOfLineKey, out var fixedEndings) ? fixedEndings.Sum() : 0);

    /// <summary>What init and the migration say when <see cref="ConvertedOutnumber"/>.</summary>
    public static string OutnumberedVerdict(IReadOnlyDictionary<string, int[]> counts)
    {
        var converted = counts[ConvertedKey].Sum();
        var others = counts.TryGetValue(EndOfLineKey, out var fixedEndings) ? fixedEndings.Sum() : 0;
        return $"git converts {converted} of {converted + others} C# files on checkout, so end_of_line isn't set (each machine keeps its own ending; the {Plural(others, "file")} committed with a fixed ending don't decide)";
    }

    /// <summary>
    /// The repository's own end_of_line when git gives C# files each machine's ending on checkout (owner's decision
    /// 2026-10-12: "Keep endings out of it, warn"). kubernetes-client says 'end_of_line = lf' with '*.cs text=auto': a
    /// Windows checkout has CRLF, so IDE0055 at error failed the build 266 times and 'format' rewrote 205 files to LF (4
    /// tests compare against "\r\n"). Then the generated settings unset end_of_line for C# files ('format' and IDE0055 leave
    /// the checkout's endings alone, probed with dotnet format and the build), IDE0055 is a suggestion, and the report says
    /// how to settle it. Null without a contradiction; <paramref name="own"/>: <see cref="Migration.OwnSettings"/>.
    /// </summary>
    public static (List<string> Lines, string Report)? EndingContradiction(string root, IReadOnlyDictionary<string, string> own)
    {
        if (!own.TryGetValue(EndOfLineKey, out var value) || value.ToLowerInvariant() is not ("lf" or "crlf") || PreviewCommand.Git(root, "ls-files", "--eol", "-z", "--", "*.cs") is not (0, var output))
        {
            return null;
        }

        var count = PickedPerMachine(output);
        if (count == 0)
        {
            return null;
        }

        var why = $"your .editorconfig says end_of_line = {value}, but git checks out {Plural(count, "C# file")} with each machine's ending (a .gitattributes 'text' attribute without 'eol', or core.autocrlf), so {(value == "lf" ? "Windows" : "Linux and macOS")} checkouts don't match it";
        return (
            [
                $"# Line endings: {why}. Until that's settled, formatting and IDE0055 leave each file's endings as checked out. To settle it: '*.cs text eol={value}' in .gitattributes (then 'git add --renormalize .'), or remove end_of_line from .editorconfig; then run stylebro-migrate again.",
                $"{EndOfLineKey} = unset",
                "dotnet_diagnostic.IDE0055.severity = suggestion",
            ],
            $"Line endings: {why}. Set end_of_line = unset for C# files and IDE0055 to suggestion, so neither 'format' nor the build touches endings. To settle it: '*.cs text eol={value}' in .gitattributes (then 'git add --renormalize .'), or remove end_of_line from .editorconfig.");
    }

    /// <summary>
    /// The report note when the repository's own end_of_line (<paramref name="own"/>) says one ending and the C# files git
    /// leaves alone clearly have the other (the usual majority), or null. Nothing enforced the key, so the files decide, like a
    /// convention key at severity silent (owner's decision 2026-10-12, extended: "Keep endings out of it, warn"):
    /// CleanArchitecture says 'end_of_line = crlf' with 349 of 350 files LF and no conversion, and 'format' turned 86 files to
    /// CRLF, 44 with no other change. 'unset' would be worse there: probed, 'dotnet format' then writes the OS's ending into
    /// every file it formats (a Windows run turned an untouched LF file CRLF), while without the key it keeps a file's ending.
    /// </summary>
    public static (string Ending, string Note)? ContradictedEnding(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> own)
    {
        if (!own.TryGetValue(EndOfLineKey, out var setting) || ConvertedOutnumber(counts) || !counts.TryGetValue(EndOfLineKey, out var endings)
            || endings.Sum() < MinimumAgreeing)
        {
            return null;
        }

        var value = setting.Split(':')[0].Trim().ToLowerInvariant();
        var files = Winner(endings[0], endings[1]) switch { 0 => "lf", 1 => "crlf", _ => null };
        return value is not ("lf" or "crlf") || files is null || files == value
            ? null
            : (files, $"Line endings: your .editorconfig says end_of_line = {value}, but {endings[files == "lf" ? 0 : 1]} of {Plural(endings.Sum(), "C# file")} are {files.ToUpperInvariant()} as committed and git doesn't convert them: nothing enforced it, so end_of_line = {files} is written for C# files and 'format' leaves the endings alone. To have {value.ToUpperInvariant()} instead: remove that line and run 'stylebro-migrate format' (it rewrites the files), or add '*.cs text eol={value}' to .gitattributes and run 'git add --renormalize .'.");
    }

    /// <summary>
    /// The note and the charset line when the repository's own charset (utf-8 or utf-8-bom for C# files) doesn't have
    /// <see cref="Winner"/>'s share of the C# files (BOM or not), or null. Like an end_of_line the files contradict (owner's
    /// decision 2026-10-12, "Keep endings out of it, warn"): nothing enforced it, so the files decide. Ical.Net says
    /// 'charset = utf-8' with 108 of 223 files starting with a BOM, and 'format' stripped all 108. Files clearly of the other
    /// form get their charset; mixed files get 'charset = unset', which 'dotnet format' leaves each file's BOM alone with (probed;
    /// 'utf-8' stripped the BOM from files it didn't otherwise change).
    /// </summary>
    public static (string Line, string Note)? ContradictedCharset(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> own)
    {
        if (!own.TryGetValue(CharsetKey, out var setting) || !counts.TryGetValue(CharsetKey, out var files) || files.Sum() < MinimumAgreeing)
        {
            return null;
        }

        var value = setting.Split(':')[0].Trim().ToLowerInvariant();
        var keyForm = value switch { "utf-8" => 0, "utf-8-bom" => 1, _ => -1 };
        var winner = Winner(files[0], files[1]);
        if (keyForm < 0 || winner == keyForm)
        {
            return null;
        }

        var found = $"{files[1]} of {Plural(files.Sum(), "C# file")} start with a BOM";
        var line = winner < 0 ? $"{CharsetKey} = unset" : $"{CharsetKey} = {(winner == 1 ? "utf-8-bom" : "utf-8")}";
        var keeps = winner < 0 ? "'format' leaves each file's BOM (or its absence) as it is" : "'format' leaves the files alone";
        return (line, $"Charset: your .editorconfig says charset = {value}, but {found}: nothing enforced it, so {line} is written for C# files and {keeps}. To have {value} everywhere: remove that line and run 'stylebro-migrate format' (it rewrites the files).");
    }

    /// <summary>
    /// The lines that turn BRO1120 off, or null, when the repository's file headers keep empty '//' lines: its
    /// file_header_template starts or ends with a line break (IDE0073 writes '//' lines there), or most files' headers have
    /// them (owner's policy, trial 11: Ical.Net's template is '\nCopyright ...\nLicensed under the MIT license.\n', and BRO1120
    /// removed both empty lines in 222 of 223 files, which every new file from the template brings back). A BRO1120
    /// severity the repository sets stays.
    /// </summary>
    public static List<string>? EmptyHeaderLines(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> own)
    {
        var rule = DiagnosticIds.EmptyComment;
        if (own.ContainsKey($"dotnet_diagnostic.{rule}.severity"))
        {
            return null;
        }

        string why;
        if (own.TryGetValue("file_header_template", out var template) && (template.StartsWith(@"\n", StringComparison.Ordinal) || template.EndsWith(@"\n", StringComparison.Ordinal)))
        {
            why = "your file_header_template starts or ends with an empty line, which IDE0073 writes as an empty '//' line";
        }
        else if (counts.TryGetValue(HeaderKey, out var headers) && headers.Sum() >= MinimumAgreeing && Winner(headers[0], headers[1]) == 1)
        {
            why = $"{headers[1]} of {Plural(headers.Sum(), "file header")} start or end with an empty '//' line";
        }
        else
        {
            return null;
        }

        return [$"# init: file headers: {why}; {rule} would remove those lines, so it is off. To turn it on: remove the next line (and the empty lines from the template).", Off(rule)];
    }

    /// <summary>Whether a file starts with the UTF-8 byte order mark.</summary>
    public static bool HasBom(string file)
    {
        using var stream = File.OpenRead(file);
        Span<byte> start = stackalloc byte[3];
        return stream.Read(start) == 3 && start[0] == 0xEF && start[1] == 0xBB && start[2] == 0xBF;
    }

    /// <summary>The prefix ('s_' or 't_') an active naming rule of the repository requires (BRO1307 then keeps it), or null.</summary>
    public static string? PrefixRequiredByNamingRule(IReadOnlyDictionary<string, string> own) =>
        new[] { "s_", "t_" }.FirstOrDefault(p => StyleBro.Analyzers.Naming.FieldNames.IsPrefixRequiredByNamingRule(p, new Options(own)));

    /// <summary>
    /// How many files in 'git ls-files --eol -z' output get the machine's ending on checkout: stored as LF, and converted
    /// here (<see cref="Converted"/>) or by a 'text' attribute without 'eol' (LF on Linux, CRLF on Windows).
    /// </summary>
    public static int PickedPerMachine(string output) =>
        output.Split('\0').Count(entry => entry.IndexOf('\t') is var tab and > 0 && entry.Substring(0, tab) is var info
            && info.StartsWith("i/lf ", StringComparison.Ordinal) && !info.Contains("eol=", StringComparison.Ordinal)
            && (System.Text.RegularExpressions.Regex.IsMatch(info, @"\battr/text\b") || info.Contains("w/crlf", StringComparison.Ordinal)));

    /// <summary>The paths in 'git ls-files --eol -z' output ('i/lf    w/crlf  attr/text=auto \tpath') that git converts, and whether a 'text' attribute does.</summary>
    public static IEnumerable<(string Path, bool ByAttribute)> Converted(string output)
    {
        foreach (var entry in output.Split('\0'))
        {
            var tab = entry.IndexOf('\t');
            var info = tab < 0 ? string.Empty : entry.Substring(0, tab);
            var fields = info.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length >= 2 && fields[0] is "i/lf" or "i/crlf" && fields[1] is "w/lf" or "w/crlf"
                && fields[0].Substring(2) != fields[1].Substring(2) && !info.Contains("eol=", StringComparison.Ordinal))
            {
                yield return (entry.Substring(tab + 1), System.Text.RegularExpressions.Regex.IsMatch(info, @"\battr/text\b"));
            }
        }
    }

    /// <summary>Adds one file's places to <paramref name="counts"/> (with <paramref name="only"/>: those starting in these spans).</summary>
    public static void Count(SyntaxTree tree, Dictionary<string, int[]> counts, IReadOnlyList<TextSpan>? only = null, IReadOnlyDictionary<string, string>? settings = null)
    {
        bool Skip(int position) => only is not null && !only.Any(s => s.Contains(position));
        var text = tree.GetText();
        int Line(int position) => text.Lines.GetLineFromPosition(position).LineNumber;
        bool StartsLine(SyntaxToken token)
        {
            var line = text.Lines.GetLineFromPosition(token.SpanStart);
            return string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, token.SpanStart)));
        }

        static bool EndsLine(SyntaxToken token) => token.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        void Add(string key, bool second) => counts[key][second ? 1 : 0]++;

        // A token where a line wraps: before it (the second value, beginning_of_line) or after it (the first).
        void Wrap(string key, SyntaxToken token, bool beginningIsFirst = false)
        {
            var starts = StartsLine(token);
            var ends = EndsLine(token);
            if (starts != ends)
            {
                Add(key, starts != beginningIsFirst);
            }
        }

        var root = tree.GetRoot();
        if (only is null)
        {
            var (underscore, plain) = Migration.CountPrivateFields(tree);
            counts[StyleBro.Analyzers.Naming.FieldNames.StyleKey][0] += plain;
            counts[StyleBro.Analyzers.Naming.FieldNames.StyleKey][1] += underscore;
            UsingPlacement();
            UsingOrder();
            StaticPrefixes();
            StaticFieldCasing();
            HeaderEmptyLines();
            TypeAliases();
            Overloads();
            MemberOrder();
        }

        foreach (var token in root.DescendantTokens())
        {
            if (Skip(token.SpanStart))
            {
                continue;
            }

            if (token.IsKind(SyntaxKind.OpenBraceToken) && token.Parent is BlockSyntax or BaseTypeDeclarationSyntax or BaseNamespaceDeclarationSyntax or AccessorListSyntax or SwitchStatementSyntax
                && token.Parent.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseBraceToken)) is var close && Line(close.SpanStart) != Line(token.SpanStart)
                && token.GetPreviousToken() is { RawKind: not 0 } before && !before.IsKind(SyntaxKind.OpenBraceToken) && !before.IsKind(SyntaxKind.CloseBraceToken)
                && !before.IsKind(SyntaxKind.SemicolonToken) && !before.IsKind(SyntaxKind.ColonToken))
            {
                Add("csharp_new_line_before_open_brace", Line(before.SpanStart) == Line(token.SpanStart));
            }
            else if (token.IsKind(SyntaxKind.NumericLiteralToken) && token.Text.TrimEnd('l', 'L') is [_, .., 'u' or 'U'])
            {
                // An unsigned suffix ('0u', '1UL'; real literals never end in 'u' or 'l'): lower case anywhere in it is BRO1135's.
                // 'L' alone doesn't count: nobody writes '1l' (it reads as 11), so it would only outvote the 'u's.
                Add(SuffixCaseKey, StyleBro.Analyzers.Readability.LiteralSuffixes.GetUpperCaseSuffix(token, lOnly: false) is not null);
            }
            else if ((token.IsKind(SyntaxKind.ElseKeyword) || token.IsKind(SyntaxKind.CatchKeyword) || token.IsKind(SyntaxKind.FinallyKeyword))
                && token.GetPreviousToken().IsKind(SyntaxKind.CloseBraceToken))
            {
                Add("csharp_new_line_before_else", Line(token.GetPreviousToken().SpanStart) == Line(token.SpanStart));
            }
        }

        foreach (var node in root.DescendantNodes(descendIntoTrivia: true))
        {
            if (Skip(node.SpanStart))
            {
                continue;
            }

            switch (node)
            {
                case IfStatementSyntax or ElseClauseSyntax or ForStatementSyntax or CommonForEachStatementSyntax or WhileStatementSyntax or UsingStatementSyntax or LockStatementSyntax:
                    var body = node switch
                    {
                        IfStatementSyntax s => s.Statement,
                        ElseClauseSyntax s => s.Statement,
                        ForStatementSyntax s => s.Statement,
                        CommonForEachStatementSyntax s => s.Statement,
                        WhileStatementSyntax s => s.Statement,
                        UsingStatementSyntax s => s.Statement,
                        _ => ((LockStatementSyntax)node).Statement,
                    };
                    var inner = body is BlockSyntax { Statements.Count: 1 } block ? block.Statements[0] : body;
                    if (inner is not (BlockSyntax or IfStatementSyntax or UsingStatementSyntax) && Line(inner.SpanStart) == Line(inner.Span.End))
                    {
                        Add("csharp_prefer_braces", body is not BlockSyntax);
                    }

                    // csharp_preserve_single_line_statements: a body without braces on its owner's line ('if (a) return;').
                    if (body is not (BlockSyntax or IfStatementSyntax or UsingStatementSyntax))
                    {
                        Add(SingleLineStatementsKey, Line(body.GetFirstToken().GetPreviousToken().SpanStart) == Line(body.SpanStart));
                    }

                    break;

                case SwitchSectionSyntax section:
                    // 'case 1: a(); break;': the statements on the last label's line.
                    foreach (var statement in section.Statements.Where(s => s is not BlockSyntax))
                    {
                        Add(SingleLineStatementsKey, Line(section.Labels.Last().Span.End) == Line(statement.SpanStart));
                    }

                    break;

                case BinaryExpressionSyntax binary:
                    Wrap("dotnet_style_operator_placement_when_wrapping", binary.OperatorToken, beginningIsFirst: true);
                    if ((binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
                        && (binary.Left.IsKind(SyntaxKind.NullLiteralExpression) || binary.Right.IsKind(SyntaxKind.NullLiteralExpression)))
                    {
                        Add("stylebro_null_check_style", true);
                    }

                    Arithmetic(binary);
                    break;

                case ConditionalExpressionSyntax conditional:
                    Wrap("dotnet_style_operator_placement_when_wrapping", conditional.QuestionToken, beginningIsFirst: true);
                    Wrap("dotnet_style_operator_placement_when_wrapping", conditional.ColonToken, beginningIsFirst: true);
                    break;

                case ArrowExpressionClauseSyntax arrow:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.ArrowKey, arrow.ArrowToken);
                    break;

                case SwitchExpressionArmSyntax arm:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.ArrowKey, arm.EqualsGreaterThanToken);
                    break;

                case EqualsValueClauseSyntax equals:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.EqualsKey, equals.EqualsToken);
                    break;

                case AssignmentExpressionSyntax assignment:
                    Wrap(StyleBro.Analyzers.Layout.WrappingPlacement.EqualsKey, assignment.OperatorToken);
                    break;

                case InitializerExpressionSyntax initializer when !initializer.IsKind(SyntaxKind.ComplexElementInitializerExpression):
                    TrailingComma(initializer.OpenBraceToken, initializer.CloseBraceToken, initializer.Expressions.Count, initializer.Expressions.SeparatorCount);
                    break;

                case AnonymousObjectCreationExpressionSyntax anonymous:
                    TrailingComma(anonymous.OpenBraceToken, anonymous.CloseBraceToken, anonymous.Initializers.Count, anonymous.Initializers.SeparatorCount);
                    break;

                case EnumDeclarationSyntax enumeration:
                    TrailingComma(enumeration.OpenBraceToken, enumeration.CloseBraceToken, enumeration.Members.Count, enumeration.Members.SeparatorCount);
                    break;

                case SwitchExpressionSyntax switchExpression:
                    TrailingComma(switchExpression.OpenBraceToken, switchExpression.CloseBraceToken, switchExpression.Arms.Count, switchExpression.Arms.SeparatorCount);
                    break;

                case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal when literal.Token.ValueText.Length == 0
                    && literal.Token.Text is "\"\"" or "@\"\"" && !InConstantContext(literal):
                    Add("stylebro_empty_string_style", true);
                    break;

                case MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Empty" } access
                    when access.Expression is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword } or IdentifierNameSyntax { Identifier.ValueText: "String" }:
                    Add("stylebro_empty_string_style", false);
                    break;

                case IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression.RawKind: (int)SyntaxKind.NullLiteralExpression } or UnaryPatternSyntax { Pattern: ConstantPatternSyntax { Expression.RawKind: (int)SyntaxKind.NullLiteralExpression } } }:
                    Add("stylebro_null_check_style", false);
                    break;

                case DefaultExpressionSyntax:
                    Add("csharp_prefer_simple_default_expression", false);
                    break;

                case LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression }:
                    Add("csharp_prefer_simple_default_expression", true);
                    break;

                case ArgumentListSyntax or ParameterListSyntax:
                    var (open, items, closeToken) = node is ArgumentListSyntax a
                        ? (a.OpenParenToken, a.Arguments.Cast<SyntaxNode>().ToList(), a.CloseParenToken)
                        : (((ParameterListSyntax)node).OpenParenToken, ((ParameterListSyntax)node).Parameters.Cast<SyntaxNode>().ToList(), ((ParameterListSyntax)node).CloseParenToken);
                    if (items.Count > 0 && !closeToken.IsMissing && Line(open.SpanStart) != Line(closeToken.SpanStart))
                    {
                        if (StartsLine(closeToken))
                        {
                            Add("stylebro_closing_parenthesis_placement", true);
                        }
                        else if (Line(items[^1].Span.End) == Line(closeToken.SpanStart) && Line(items[^1].SpanStart) != Line(open.SpanStart))
                        {
                            Add("stylebro_closing_parenthesis_placement", false);
                        }

                        if (items.Count > 1 && Line(items[0].SpanStart) != Line(items[1].SpanStart))
                        {
                            Add("stylebro_split_list_first_item", Line(items[0].SpanStart) == Line(open.SpanStart));
                        }

                        // Items on several lines: each on its own (BRO1108's layout) or grouped ('F(TagX, 1,' + '"a", "b")').
                        if (items.Count > 1 && Line(items[0].SpanStart) != Line(items[^1].SpanStart))
                        {
                            Add(ItemsPerLineKey, Enumerable.Range(1, items.Count - 1).Any(i => Line(items[i].SpanStart) == Line(items[i - 1].Span.End)));
                        }
                    }

                    break;

                case ConstructorInitializerSyntax initializer:
                    Add("stylebro_constructor_initializer_placement", !StartsLine(initializer.ColonToken));
                    break;

                case TypeParameterConstraintClauseSyntax constraint:
                    Add("stylebro_constraint_placement", !StartsLine(constraint.WhereKeyword));
                    break;

                case ObjectCreationExpressionSyntax { Initializer: not null } creation:
                    Add("stylebro_object_creation_parentheses", creation.ArgumentList is not null);
                    break;

                case SwitchStatementSyntax switchStatement:
                    foreach (var section in switchStatement.Sections.Skip(1))
                    {
                        var line = Line(section.SpanStart);
                        Add("stylebro_blank_line_between_switch_sections", line < 1 || !string.IsNullOrWhiteSpace(text.Lines[line - 1].ToString()));
                    }

                    break;

                case XmlElementSyntax { StartTag.Name.LocalName.ValueText: "summary" } summary:
                    var textLines = summary.Content.SelectMany(c => c.DescendantTokens())
                        .Where(t => !t.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) && !string.IsNullOrWhiteSpace(t.Text))
                        .Select(t => Line(t.SpanStart))
                        .Distinct()
                        .ToList();
                    var startLine = Line(summary.StartTag.SpanStart);
                    var endLine = Line(summary.EndTag.SpanStart);
                    if (textLines.Count == 1 && !summary.EndTag.IsMissing)
                    {
                        if (startLine == endLine)
                        {
                            Add("stylebro_summary_layout", true);
                        }
                        else if (textLines[0] != startLine && textLines[0] != endLine)
                        {
                            Add("stylebro_summary_layout", false);
                        }
                    }

                    break;

                case XmlEmptyElementSyntax { Name.LocalName.ValueText: "inheritdoc" } inheritdoc:
                    Add("stylebro_inheritdoc_style", inheritdoc.SlashGreaterThanToken.GetPreviousToken().TrailingTrivia.Any() || inheritdoc.SlashGreaterThanToken.LeadingTrivia.Any());
                    break;
            }
        }

        // Unit = files: one with a namespace and non-global usings on one level only (file-scoped namespaces count as inside).
        void UsingPlacement()
        {
            if (root is CompilationUnitSyntax unit && unit.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseNamespaceDeclarationSyntax>().ToList() is { Count: > 0 } namespaces)
            {
                var outside = unit.Usings.Any(u => u.GlobalKeyword.IsKind(SyntaxKind.None));
                var inside = namespaces.Any(n => n.Usings.Count > 0);
                if (outside != inside)
                {
                    Add("csharp_using_directive_placement", inside);
                }
            }
        }

        // Unit = private static fields (not constants): with an 's_'/'t_' prefix or without.
        void StaticPrefixes()
        {
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>().Where(f => f.Modifiers.Any(SyntaxKind.StaticKeyword)
                && !f.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword) || m.IsKind(SyntaxKind.ConstKeyword))))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    Add(StaticPrefixKey, variable.Identifier.ValueText is ['s' or 't', '_', _, ..]);
                }
            }
        }

        // Unit = files starting with a '//' comment group (a file header): with an empty '//' line at its top or bottom (what
        // BRO1120 removes, and what a file_header_template starting with '\n' writes) or without.
        void HeaderEmptyLines()
        {
            var group = root.GetLeadingTrivia().TakeWhile(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia))
                .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .Select(t => t.ToString())
                .ToList();
            if (group.Count > 0 && !group[0].StartsWith("///", StringComparison.Ordinal))
            {
                counts[HeaderKey][group[0].Trim() == "//" || group[^1].Trim() == "//" ? 1 : 0]++;
            }
        }

        // Unit = private constants and private static readonly fields (BRO1306's): PascalCase, camelCase or _camelCase.
        // Other names ('s_x', '_X', 'x_y') are other rules' (BRO1307, BRO1308) and don't count.
        void StaticFieldCasing()
        {
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>().Where(f => f.Parent is not InterfaceDeclarationSyntax
                && (f.Modifiers.Any(SyntaxKind.ConstKeyword) || (f.Modifiers.Any(SyntaxKind.StaticKeyword) && f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword)))
                && !f.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword))))
            {
                foreach (var name in field.Declaration.Variables.Select(v => v.Identifier.ValueText).Where(n => !n.TrimStart('_').Contains('_')))
                {
                    var form = name switch
                    {
                        [>= 'A' and <= 'Z', ..] => 0,
                        [>= 'a' and <= 'z', ..] => 1,
                        ['_', >= 'a' and <= 'z', ..] => 2,
                        _ => -1,
                    };
                    if (form >= 0)
                    {
                        counts[StaticCasingKey][form]++;
                    }
                }
            }
        }

        // Unit = files naming a built-in type: only with keywords, or with a framework name IDE0049 would rewrite ('String',
        // 'System.Int32'; not 'x.String', 'Other.String' or one in a using directive).
        void TypeAliases()
        {
            var (keywords, names) = (false, false);
            foreach (var node in root.DescendantNodes())
            {
                if (node is PredefinedTypeSyntax predefined && !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                {
                    keywords = true;
                }
                else if (node is IdentifierNameSyntax identifier && FrameworkTypeNames.Contains(identifier.Identifier.ValueText)
                    && !identifier.Ancestors().Any(a => a is UsingDirectiveSyntax)
                    && identifier.Parent switch
                    {
                        QualifiedNameSyntax q when q.Right == identifier => q.Left.ToString() is "System" or "global::System",
                        MemberAccessExpressionSyntax m when m.Name == identifier => m.Expression.ToString() is "System" or "global::System",
                        _ => true,
                    })
                {
                    names = true;
                }
            }

            if (keywords || names)
            {
                Add(TypeAliasKey, names);
            }
        }

        // Unit = files with a list of 2 or more usings (global usings aside): in the order 'dotnet format' sorts them into, or not.
        void UsingOrder()
        {
            var lists = root.DescendantNodesAndSelf(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                .Select(n => n switch { CompilationUnitSyntax u => u.Usings, BaseNamespaceDeclarationSyntax ns => ns.Usings, _ => default })
                .Select(l => l.Where(u => u.GlobalKeyword.IsKind(SyntaxKind.None)).ToList())
                .Where(l => l.Count >= 2)
                .ToList();
            if (lists.Count > 0)
            {
                var systemFirst = lists.All(l => IsSorted(l, systemFirst: true));
                var plain = lists.All(l => IsSorted(l, systemFirst: false));
                counts[UsingsKey][systemFirst ? (plain ? 2 : 0) : plain ? 1 : 3]++;
            }
        }

        // Unit = overload groups whose members BRO1001 sorts to different places (only those can be split): adjacent or not.
        void Overloads()
        {
            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                var inInterface = type.IsKind(SyntaxKind.InterfaceDeclaration);
                var groups = type.Members.Select((member, index) => (Method: member as MethodDeclarationSyntax, Index: index))
                    .Where(p => p.Method is not null)
                    .GroupBy(p => p.Method!.ExplicitInterfaceSpecifier?.Name + "." + p.Method.Identifier.ValueText);
                foreach (var group in groups.Select(g => g.ToList()).Where(g => g.Count > 1))
                {
                    var keys = group.Select(p => StyleBro.Analyzers.Ordering.MemberOrdering.GetDefaultKey(p.Method!, inInterface)).ToList();
                    if (keys.All(k => k is not null) && keys.Any(k => k!.Value.CompareTo(keys[0]!.Value) != 0))
                    {
                        Add(OverloadsKey, group[^1].Index - group[0].Index == group.Count - 1);
                    }
                }
            }
        }

        // Unit = types whose members BRO1001 sorts apart, with the repository's order settings: in its order or not. A type
        // that is in order with overloads kept together counts as sorted: whether they stay together is the overloads
        // convention's call (ponytail: so with keep_overloads_together = false such a type is still sorted on format).
        void MemberOrder()
        {
            var options = StyleBro.Analyzers.Ordering.MemberOrderOptions.Read(new Options(settings));
            var together = options.KeepOverloadsTogether
                ? null
                : StyleBro.Analyzers.Ordering.MemberOrderOptions.Read(new Options(new Dictionary<string, string>(settings ?? new Dictionary<string, string>()) { [OverloadsKey] = "true" }));
            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (StyleBro.Analyzers.Ordering.MemberOrdering.IsSorted(type, options) is { } sorted)
                {
                    Add(MemberOrderKey, !sorted && (together is null || StyleBro.Analyzers.Ordering.MemberOrdering.IsSorted(type, together) != true));
                }
            }
        }

        // BRO1406's places: an operation of another kind inside arithmetic. Without parentheses it is the rule's finding; with
        // them only where they change nothing ('a + (b * c)', not '(a + b) * c').
        void Arithmetic(BinaryExpressionSyntax binary)
        {
            var outer = Level(binary.Kind());
            foreach (var operand in outer == 0 ? Array.Empty<ExpressionSyntax>() : new[] { binary.Left, binary.Right })
            {
                var parenthesized = operand is ParenthesizedExpressionSyntax;
                if ((operand is ParenthesizedExpressionSyntax p ? p.Expression : operand) is BinaryExpressionSyntax inner && Level(inner.Kind()) is var level and > 0
                    && Family(inner.Kind()) != Family(binary.Kind()) && (!parenthesized || level > outer || (level == outer && operand == binary.Left)))
                {
                    Add(ArithmeticKey, !parenthesized);
                }
            }
        }

        static int Level(SyntaxKind kind) => kind switch
        {
            SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression or SyntaxKind.ModuloExpression => 3,
            SyntaxKind.AddExpression or SyntaxKind.SubtractExpression => 2,
            SyntaxKind.LeftShiftExpression or SyntaxKind.RightShiftExpression => 1,
            _ => 0,
        };

        // StyleCop's families: '%' is one of its own.
        static int Family(SyntaxKind kind) => kind == SyntaxKind.ModuloExpression ? 4 : Level(kind);

        void TrailingComma(SyntaxToken open, SyntaxToken close, int items, int separators)
        {
            if (items > 0 && Line(open.SpanStart) != Line(close.SpanStart))
            {
                Add("stylebro_trailing_comma", separators < items);
            }
        }
    }

    /// <summary>
    /// What to write: for each convention with a clear majority (<see cref="Share"/> of at least <see cref="MinimumSample"/>
    /// places, or at least <see cref="MinimumAgreeing"/> places that all agree) that isn't the default, its lines; without one,
    /// the line that turns its rule off (<see cref="Convention.Off"/>; owner's decision 2026-10-10: Bogus had 24 operators at
    /// the start of a line and 50 at the end, and BRO1520 moved 52); and the report: one line per convention found in the code
    /// (the verdict first) and a summary of what was kept and turned off. <paramref name="decided"/>: keys set elsewhere (the
    /// repository's own .editorconfig, the Sonar setup, an SDK naming rule), with why; they're reported, not written, unless
    /// the code clearly contradicts a key in <paramref name="repository"/> (the root .editorconfig's settings) that only a
    /// StyleBro rule enforces (<see cref="Convention.Enforcer"/>): then that rule goes off and the key stays.
    /// <paramref name="isSet"/>: whether the repository or the Sonar setup sets an Off line's key itself (then it stays).
    /// <paramref name="unenforced"/>: keys the repository sets at severity silent (<see cref="InitCommand.Unenforced"/>), with
    /// their values: the code decides, and a default the key contradicts is written too (the block comes later and wins).
    /// </summary>
    public static (List<string> Lines, List<string> Report) Decide(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> decided, Func<string, bool>? isSet = null, IReadOnlyDictionary<string, string>? repository = null, IReadOnlyDictionary<string, string>? unenforced = null)
    {
        static string Value(string setting) => setting.Split(':')[0].Trim().ToLowerInvariant();
        var lines = new List<string>();
        var report = new List<string>();
        var summary = new SummaryLists();
        bool IsSet(string line) => isSet?.Invoke(line.Substring(0, line.IndexOf('=')).Trim()) ?? false;
        foreach (var convention in All)
        {
            var count = counts.TryGetValue(convention.Key, out var c) ? c : new int[2];
            var total = count[0] + count[1];
            if (total == 0)
            {
                continue;
            }

            var mixed = string.Join(", ", Enumerable.Range(0, 2).Where(i => count[i] > 0).Select(i => $"{count[i]} {convention.Values[i].Label}"));
            var winner = convention.KeepAny && count[1] >= MinimumAgreeing ? 1 : Winner(count[0], count[1]);
            var found = winner < 0 ? mixed : $"{count[winner]} of {total} {convention.Values[winner].Label}";
            string tag;
            string verdict;
            if (decided.TryGetValue(convention.Key, out var reason))
            {
                if (total >= MinimumAgreeing && winner >= 0 && Contradicted(convention, convention.Values[winner].Value, repository) is { } value && !IsSet(Off(convention.Enforcer!)))
                {
                    var rule = convention.Enforcer!;
                    (tag, verdict) = ("off", $".editorconfig says {value}, unenforced: {rule} is off");
                    summary.Unfollowed.Add($"{rule} ({convention.What}: your .editorconfig says {value})");
                    lines.Add($"# init: {convention.What}: your .editorconfig says {convention.Key} = {value}, but {count[winner]} of {Plural(total, convention.Unit)} in your code are {convention.Values[winner].Label}; nothing enforced it, so {rule} is off. To enforce it: change the code, then remove the next line.");
                    lines.Add(Off(rule));
                }
                else
                {
                    (tag, verdict) = ("set", reason);
                    found = winner < 0 ? Plural(total, convention.Unit) : found;
                }
            }
            else if (convention.Key == EndOfLineKey && ConvertedOutnumber(counts))
            {
                (tag, verdict) = ("git", OutnumberedVerdict(counts));
            }
            else if (total < MinimumAgreeing)
            {
                (tag, verdict) = ("too few", $"default stays ({convention.Default})");
                found = Plural(total, convention.Unit);
                summary.TooFew.Add(convention.Key);
            }
            else if (winner < 0 && convention.Off is { } off && !IsSet(off))
            {
                var rule = RuleOf(off);
                if (rule is null)
                {
                    (tag, verdict) = ("both", "both allowed");
                    summary.BothAllowed.Add($"{off} ({convention.What})");
                    lines.Add($"# init: {convention.What}: your code mixes both forms ({mixed}), so both are allowed. To choose {convention.Values[0].Label}: change the next line to {convention.Key} = {convention.Values[0].Value}.");
                }
                else
                {
                    (tag, verdict) = ("off", $"{rule} is off");
                    summary.TurnedOff.Add($"{rule} ({convention.What})");
                    lines.Add($"# init: {convention.What}: your code mixes both forms ({mixed}), so {rule} is off. To choose one: set {convention.Key} and remove the next line.");
                }

                lines.Add(off);
            }
            else if (winner < 0)
            {
                (tag, verdict) = ("mixed", $"default stays ({convention.Default})");
            }
            else if (convention.Values[winner].Value == convention.Default && (unenforced is null || !unenforced.TryGetValue(convention.Key, out var silent) || Value(silent) == convention.Default))
            {
                (tag, verdict) = ("default", convention.Default);
                summary.Matched++;
            }
            else if (convention.Off is { } offLine && RuleOf(offLine) is not null && offLine == $"{convention.Key} = {convention.Values[winner].Value}")
            {
                // A rule-only convention (BRO1601): the code's form is "the rule's fix isn't wanted".
                var rule = RuleOf(offLine)!;
                (tag, verdict) = ("off", $"{rule} is off");
                summary.Unfollowed.Add($"{rule} ({convention.What}: {count[winner]} of {total} {convention.Values[winner].Label})");
                lines.Add($"# init: {convention.What}: {count[winner]} of {total} in your code are {convention.Values[winner].Label}, so {rule} is off. To turn it on: remove the next line.");
                lines.Add(offLine);
            }
            else
            {
                var value = convention.Values[winner].Value;
                (tag, verdict) = ("kept", value);
                summary.Written.Add(convention.Key);
                lines.Add($"# init: {convention.What}: {convention.Values[winner].Label} in {count[winner]} of {Plural(total, convention.Unit)} in your code"
                    + (unenforced?.TryGetValue(convention.Key, out var ignored) == true ? $" (your .editorconfig's {convention.Key} = {ignored} isn't enforced)" : string.Empty)
                    + (convention.KeepAny ? $" ({MinimumAgreeing} are enough to keep them: {convention.Key} = {convention.Default} would move each to a line of its own)" : string.Empty));
                lines.AddRange(new[] { convention.Key }.Concat(convention.AlsoKeys).Select(k => $"{k} = {value}"));
            }

            report.Add($"  {tag,-8} {convention.What}: {found} -> {verdict}");
            JsonReport.Add("conventions", new JsonObject
            {
                ["key"] = convention.Key,
                ["what"] = convention.What,
                ["counts"] = new JsonObject { [convention.Values[0].Value] = count[0], [convention.Values[1].Value] = count[1] },
                ["verdict"] = tag == "too few" ? "tooFew" : tag,
                ["result"] = verdict,
            });
        }

        DecideUsings(counts, isSet, lines, report, summary);
        DecideStaticCasing(counts, decided, isSet, lines, report, summary);

        // The 'git' verdict above already says it (only when some files have a fixed ending: else there was no line).
        var saidAbove = ConvertedOutnumber(counts) && counts.TryGetValue(EndOfLineKey, out var fixedEndings) && fixedEndings.Sum() > 0;
        if (!saidAbove && counts.TryGetValue(ConvertedKey, out var converted) && ConvertedNote(converted) is { } note)
        {
            report.Add($"  note     line endings: {note}");
        }

        report.AddRange(Summary(summary));
        return (lines, report);
    }

    /// <summary>
    /// Which of two forms the code clearly follows (0 or 1), or -1: all of fewer than <see cref="MinimumSample"/> places, or
    /// <see cref="Share"/> of more. The caller checks <see cref="MinimumAgreeing"/>.
    /// </summary>
    public static int Winner(int first, int second)
    {
        var total = first + second;
        return total < MinimumSample ? (first == 0 ? 1 : second == 0 ? 0 : -1)
            : second >= Share * total ? 1 : first >= Share * total ? 0 : -1;
    }

    /// <summary>
    /// The end of init's convention report: which settings follow the code, which rules are off (mixed code, or code that
    /// doesn't follow a setting) and how to choose later, which settings allow both forms, and the keys judged on too few
    /// places. Lines longer than <see cref="Width"/> wrap after a list's comma.
    /// </summary>
    public static IEnumerable<string> Summary(SummaryLists lists) => SummaryLines(lists).SelectMany(Wrap);

    /// <summary>A count with its noun: '1 place', '2 places'.</summary>
    /// <summary>
    /// "3 files that git converts on checkout (core.autocrlf) don't count", naming what converts them: [core.autocrlf,
    /// a .gitattributes 'text' attribute]. Null when there are none.
    /// </summary>
    public static string? ConvertedNote(int[] converted)
    {
        var causes = new[] { "core.autocrlf", ".gitattributes 'text'" }.Where((_, i) => converted[i] > 0).ToList();
        return causes.Count == 0 ? null : $"{Plural(converted.Sum(), "file")} that git converts on checkout ({string.Join(", ", causes)}) don't count";
    }

    public static string Plural(int count, string noun) => $"{count} {noun}{(count == 1 ? string.Empty : "s")}";

    /// <summary>
    /// The using order (<see cref="SortUsingsKey"/>): sorted files -> the key (System first unless the files that tell the
    /// two apart clearly don't), unsorted or mixed -> no key, so 'dotnet format' leaves them alone; too few files -> StyleBro's
    /// default (sorted, System first). A key the repository sets wins.
    /// </summary>
    private static void DecideUsings(IReadOnlyDictionary<string, int[]> counts, Func<string, bool>? isSet, List<string> lines, List<string> report, SummaryLists summary)
    {
        const string What = "using directives";
        if (!counts.TryGetValue(UsingsKey, out var c))
        {
            return;
        }

        var (sorted, unsorted) = (c[0] + c[1] + c[2], c[3]);
        var (total, apart) = (sorted + unsorted, c[0] + c[1]);
        if (isSet?.Invoke(SortUsingsKey) == true || isSet?.Invoke("dotnet_separate_import_directive_groups") == true)
        {
            if (total > 0)
            {
                report.Add($"  {"set",-8} {What}: {Plural(total, "file")} -> set in .editorconfig");
            }

            return;
        }

        var sort = total < MinimumAgreeing ? 0 : Winner(sorted, unsorted);
        var system = sort != 0 || apart < MinimumAgreeing ? 0 : Winner(c[0], c[1]);
        var found = $"{sorted} of {Plural(total, "file")} sorted" + (apart > 0 ? $", {c[0]} of {apart} with System first" : string.Empty);
        string tag;
        string verdict;
        string comment;
        string? value = null;
        if (total < MinimumAgreeing)
        {
            (tag, verdict, value, found) = ("too few", "default stays (sorted, System first)", "true", Plural(total, "file"));
            comment = $"too few files with 2 or more to tell ({total}), so StyleBro's default: sorted, System first";
            if (total > 0)
            {
                summary.TooFew.Add(SortUsingsKey);
            }
        }
        else if (sort == 0 && system >= 0)
        {
            value = system == 0 ? "true" : "false";
            (tag, verdict) = system == 0 ? ("default", "sorted, System first") : ("kept", "sorted, System not first");
            comment = $"{found}";
            if (system == 0)
            {
                summary.Matched++;
            }
            else
            {
                summary.Written.Add(SortUsingsKey);
            }
        }
        else
        {
            comment = (sort == 1 ? $"{unsorted} of {total} files not sorted"
                : sort < 0 ? $"your code mixes sorted and unsorted files ({found})"
                : $"sorted, but with System first and without ({found})")
                + $", so they aren't sorted ('dotnet format' sorts them whenever {SortUsingsKey} is set). To sort them: add {SortUsingsKey} = true";
            (tag, verdict) = (sort == 1 ? "kept" : "both", "not sorted");
            if (sort == 1)
            {
                summary.Written.Add("unsorted usings");
            }
            else
            {
                summary.BothAllowed.Add($"{SortUsingsKey} not set ({What})");
            }
        }

        lines.Add($"# init: {What}: {comment}");
        if (value is not null)
        {
            lines.Add($"{SortUsingsKey} = {value}");
        }

        if (total > 0)
        {
            report.Add($"  {tag,-8} {What}: {found} -> {verdict}");
        }

        JsonReport.Add("conventions", new JsonObject
        {
            ["key"] = SortUsingsKey,
            ["what"] = What,
            ["counts"] = new JsonObject { ["systemFirst"] = c[0], ["systemNotFirst"] = c[1], ["sorted"] = c[2], ["unsorted"] = c[3] },
            ["verdict"] = tag == "too few" ? "tooFew" : tag,
            ["result"] = verdict,
        });
    }

    /// <summary>
    /// The casing of private constants and static readonly fields (<see cref="StaticCasingKey"/>), like a convention with three
    /// forms: a clear one (<see cref="Winner"/>'s share against the other two together) is written unless it is BRO1306's
    /// default (PascalCase); mixed turns BRO1306 off (it has no switch for these fields alone, so its public fields go too).
    /// A key or naming rule the repository sets (<paramref name="decided"/>), or BRO1306's own severity, wins.
    /// </summary>
    private static void DecideStaticCasing(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> decided, Func<string, bool>? isSet, List<string> lines, List<string> report, SummaryLists summary)
    {
        const string What = "private constants/static readonly fields";
        string[] forms = ["PascalCase", "camelCase", "_camelCase"];
        if (!counts.TryGetValue(StaticCasingKey, out var c) || c.Sum() is not (> 0 and var total))
        {
            return;
        }

        var off = Off(DiagnosticIds.FieldPascalCase);
        var winner = Enumerable.Range(0, 3).FirstOrDefault(i => Winner(c[i], total - c[i]) == 0, -1);
        var mixed = string.Join(", ", Enumerable.Range(0, 3).Where(i => c[i] > 0).Select(i => $"{c[i]} {forms[i]}"));
        var found = winner < 0 ? mixed : $"{c[winner]} of {total} {forms[winner]}";
        string tag;
        string verdict;
        if (decided.TryGetValue(StaticCasingKey, out var reason) || isSet?.Invoke(KeyOf(off)) == true)
        {
            (tag, verdict, found) = ("set", reason ?? $"{DiagnosticIds.FieldPascalCase}'s severity is set", Plural(total, "field"));
        }
        else if (total < MinimumAgreeing)
        {
            (tag, verdict, found) = ("too few", "default stays (PascalCase)", Plural(total, "field"));
            summary.TooFew.Add(StaticCasingKey);
        }
        else if (winner < 0)
        {
            (tag, verdict) = ("off", $"{DiagnosticIds.FieldPascalCase} is off");
            summary.TurnedOff.Add($"{DiagnosticIds.FieldPascalCase} ({What})");
            lines.Add($"# init: {What}: your code mixes their casing ({mixed}), so {DiagnosticIds.FieldPascalCase} is off (it checks constants, static readonly and non-private fields). To choose one: set {StaticCasingKey} (PascalCase, camelCase or _camelCase) and remove the next line.");
            lines.Add(off);
        }
        else if (winner == 0)
        {
            (tag, verdict) = ("default", forms[0]);
            summary.Matched++;
        }
        else
        {
            (tag, verdict) = ("kept", forms[winner]);
            summary.Written.Add(StaticCasingKey);
            lines.Add($"# init: {What}: {forms[winner]} in {c[winner]} of {Plural(total, "field")} in your code");
            lines.Add($"{StaticCasingKey} = {forms[winner]}");
        }

        report.Add($"  {tag,-8} {What}: {found} -> {verdict}");
        JsonReport.Add("conventions", new JsonObject
        {
            ["key"] = StaticCasingKey,
            ["what"] = What,
            ["counts"] = new JsonObject { [forms[0]] = c[0], [forms[1]] = c[1], [forms[2]] = c[2] },
            ["verdict"] = tag == "too few" ? "tooFew" : tag,
            ["result"] = verdict,
        });

        static string KeyOf(string line) => line.Substring(0, line.IndexOf('=')).Trim();
    }

    /// <summary>
    /// The value of the convention's SDK key in the repository's own settings when its <see cref="Convention.Enforcer"/>
    /// enforces it and the code's form (<paramref name="code"/>) is the other one; else null.
    /// </summary>
    private static string? Contradicted(Convention convention, string code, IReadOnlyDictionary<string, string>? repository)
    {
        if (convention.Enforcer is null || repository is null || !repository.TryGetValue(convention.Key, out var setting))
        {
            return null;
        }

        var value = setting.Split(':')[0].Trim().ToLowerInvariant();
        return value != code && convention.Values.Any(v => v.Value == value) && (convention.EnforcedValues?.Contains(value) ?? true) ? value : null;
    }

    /// <summary>The rule id of a 'dotnet_diagnostic.X.severity' line, or null for an option line.</summary>
    private static string? RuleOf(string line) => line.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal) ? line.Split('.')[1] : null;

    /// <summary>Splits a line longer than <see cref="Width"/> after the last ', ' that fits (else a space); later parts are indented.</summary>
    private static IEnumerable<string> Wrap(string line)
    {
        while (line.Length > Width)
        {
            var comma = line.LastIndexOf(", ", Width - 1, StringComparison.Ordinal);
            var at = comma > 4 ? comma + 1 : line.LastIndexOf(' ', Width);
            if (at <= 4)
            {
                break;
            }

            yield return line.Substring(0, at);
            line = "    " + line.Substring(at + 1);
        }

        yield return line;
    }

    private static IEnumerable<string> SummaryLines(SummaryLists lists)
    {
        var (written, matched) = (lists.Written, lists.Matched);
        if (written.Count + matched + lists.TurnedOff.Count + lists.Unfollowed.Count + lists.BothAllowed.Count + lists.TooFew.Count == 0)
        {
            yield break;
        }

        yield return "Summary:";
        if (written.Count + matched > 0)
        {
            var kept = $"  Kept your style for {Plural(written.Count + matched, "setting")}";
            yield return matched == 0 ? $"{kept}: {string.Join(", ", written)}."
                : written.Count == 0 ? $"{kept}, all already StyleBro's default."
                : $"{kept} ({written.Count} written, {matched} already StyleBro's default): {string.Join(", ", written)}.";
        }

        if (lists.BothAllowed.Count > 0)
        {
            yield return $"  Allowed both forms for {Plural(lists.BothAllowed.Count, "setting")} because your code mixes them: {string.Join(", ", lists.BothAllowed)}.";
        }

        if (lists.TurnedOff.Count > 0)
        {
            yield return $"  Turned {Plural(lists.TurnedOff.Count, "rule")} off because your code mixes both forms: {string.Join(", ", lists.TurnedOff)}.";
        }

        if (lists.Unfollowed.Count > 0)
        {
            yield return $"  Turned {Plural(lists.Unfollowed.Count, "rule")} off because your code doesn't do what {(lists.Unfollowed.Count == 1 ? "it enforces" : "they enforce")}: {string.Join(", ", lists.Unfollowed)}.";
        }

        if (lists.TurnedOff.Count + lists.Unfollowed.Count + lists.BothAllowed.Count > 0)
        {
            yield return "  To choose later: set the key in .editorconfig, remove its init line, run 'stylebro-migrate format'.";
        }

        if (lists.TooFew.Count > 0)
        {
            yield return $"  {Plural(lists.TooFew.Count, "setting")} had fewer than {MinimumAgreeing} places to tell, StyleBro's defaults stay: {string.Join(", ", lists.TooFew)}.";
        }
    }

    /// <summary>The line that turns a rule off.</summary>
    private static string Off(string id) => $"dotnet_diagnostic.{id}.severity = none";

    /// <summary>Whether a list of usings is in the order 'dotnet format' sorts them into (Roslyn's: namespaces, static, aliases).</summary>
    private static bool IsSorted(List<UsingDirectiveSyntax> usings, bool systemFirst)
    {
        static int Group(UsingDirectiveSyntax u) => u.Alias is not null ? 2 : u.StaticKeyword.IsKind(SyntaxKind.None) ? 0 : 1;
        static string[] Segments(UsingDirectiveSyntax u) =>
            (u.Alias?.Name.Identifier.ValueText ?? u.NamespaceOrType.ToString()).Replace("global::", string.Empty).Replace(" ", string.Empty).Split('.');

        int Compare(UsingDirectiveSyntax a, UsingDirectiveSyntax b)
        {
            var (group, sa, sb) = (Group(a), Segments(a), Segments(b));
            if (group != Group(b))
            {
                return group.CompareTo(Group(b));
            }

            if (systemFirst && group < 2 && (sa[0] == "System") != (sb[0] == "System"))
            {
                return sa[0] == "System" ? -1 : 1;
            }

            for (var i = 0; i < Math.Min(sa.Length, sb.Length); i++)
            {
                var c = string.Compare(sa[i], sb[i], StringComparison.OrdinalIgnoreCase);
                if (c != 0)
                {
                    return c;
                }
            }

            return sa.Length.CompareTo(sb.Length);
        }

        return Enumerable.Range(1, usings.Count - 1).All(i => Compare(usings[i - 1], usings[i]) <= 0);
    }

    /// <summary>Whether an empty string literal has to stay a literal (BRO1106 leaves constant contexts alone).</summary>
    private static bool InConstantContext(SyntaxNode literal)
    {
        return literal.Ancestors().Any(a => a is AttributeArgumentSyntax or ParameterSyntax or CaseSwitchLabelSyntax or PatternSyntax
            || (a is FieldDeclarationSyntax field && field.Modifiers.Any(SyntaxKind.ConstKeyword))
            || (a is LocalDeclarationStatementSyntax local && local.IsConst));
    }

    /// <summary>What init's summary lists (<see cref="Summary"/>).</summary>
    internal sealed class SummaryLists
    {
        /// <summary>Gets the keys written because the code follows another form than StyleBro's default.</summary>
        public List<string> Written { get; } = new();

        /// <summary>Gets or sets how many settings the code already follows in StyleBro's default form.</summary>
        public int Matched { get; set; }

        /// <summary>Gets the rules turned off because the code mixes both forms.</summary>
        public List<string> TurnedOff { get; } = new();

        /// <summary>Gets the rules turned off because the code clearly doesn't do what they enforce.</summary>
        public List<string> Unfollowed { get; } = new();

        /// <summary>Gets the option lines that allow both forms because the code mixes them.</summary>
        public List<string> BothAllowed { get; } = new();

        /// <summary>Gets the keys with too few places to tell.</summary>
        public List<string> TooFew { get; } = new();
    }

    /// <summary>A convention init detects (see <see cref="All"/>).</summary>
    internal sealed class Convention
    {
        public Convention(string key, string what, string defaultValue, (string Value, string Label) first, (string Value, string Label) second, string? off, params string[] alsoKeys)
        {
            Key = key;
            What = what;
            Default = defaultValue;
            Values = new[] { first, second };
            Off = off;
            AlsoKeys = alsoKeys;
        }

        public string Key { get; }

        public string What { get; }

        public string Default { get; }

        public (string Value, string Label)[] Values { get; }

        /// <summary>Gets the line written when the code has no clear majority (see <see cref="Conventions.All"/>), or null.</summary>
        public string? Off { get; }

        public string[] AlsoKeys { get; }

        /// <summary>
        /// Gets the StyleBro rule that enforces the SDK key <see cref="Key"/>, which the repository may set itself. When the
        /// code clearly does the other thing, nothing enforced that key, and the rule goes off instead (owner's decision
        /// 2026-10-10: NCronJob says end_of_line, 85 of 87 operators start the line, and BRO1520 moved 82).
        /// </summary>
        public string? Enforcer { get; init; }

        /// <summary>Gets the values of the key that <see cref="Enforcer"/> enforces (null: both; 'when_multiline' removes no braces).</summary>
        public string[]? EnforcedValues { get; init; }

        /// <summary>Gets what is counted: places (a token, a statement, ...) or files.</summary>
        public string Unit { get; init; } = "place";

        /// <summary>Gets a value indicating whether <see cref="MinimumAgreeing"/> places of the second form keep it, majority or not.</summary>
        public bool KeepAny { get; init; }
    }

    /// <summary>The root .editorconfig's settings as analyzer options (BRO1601's stylebro_document_* keys).</summary>
    internal sealed class Options(IReadOnlyDictionary<string, string>? settings) : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
    {
        public override IEnumerable<string> Keys => settings?.Keys ?? Enumerable.Empty<string>();

        public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
        {
            value = null;
            return settings?.TryGetValue(key, out value) == true;
        }
    }
}
