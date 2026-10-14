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

    /// <summary>The C# files' charset (unit: files): [no BOM, BOM] (<see cref="ContradictedFileSetting"/>).</summary>
    public const string CharsetKey = "charset";

    /// <summary>
    /// The C# files' last line break (unit: files): [without, with] (<see cref="ContradictedFileSetting"/>). The starter kit says
    /// 'insert_final_newline = false' with 1,203 of 1,716 files ending in one; nothing enforced it until init turned the
    /// whitespace pass on, and 'format' removed the line break from 1,092 files.
    /// </summary>
    public const string FinalNewlineKey = "insert_final_newline";

    /// <summary>
    /// The C# files with trailing whitespace (unit: files): [without, with]. 'dotnet format' doesn't read trim_trailing_whitespace:
    /// its whitespace pass trims every line it formats whatever the key says (probed), so 'false' can't be kept; only a note.
    /// </summary>
    public const string TrailingWhitespaceKey = "trim_trailing_whitespace";

    /// <summary>BRO1527's severity: properties whose accessors are all auto-accessors, on one line or on several (sourcegit: 732 and 306).</summary>
    public const string AutoAccessorsKey = "dotnet_diagnostic." + DiagnosticIds.AutoAccessorsOnOneLine + ".severity";

    /// <summary>
    /// BRO1113's severity: '#region's inside members (the starter kit's '#region Arrange/Act/Assert' in 38 tests, which BRO1113
    /// removed). Only the regions are counted: from <see cref="MinimumAgreeing"/> on, the code uses them and BRO1113 goes off.
    /// BRO1112 (regions between members) is off in the preset, so only a team's own setting turns it on, and that wins.
    /// </summary>
    public const string RegionsKey = "dotnet_diagnostic." + DiagnosticIds.NoRegionsInCodeElements + ".severity";

    /// <summary>
    /// One-line jumps ('if (x) return;', 'break', 'continue', 'throw', 'goto', 'yield break' on the 'if' line) against jumps
    /// in braces: from <see cref="MinimumAgreeing"/> braceless ones on, BRO1514 leaves them (the starter kit: 878 of 1,130
    /// bodies with braces, and BRO1514 braced 194 one-line jumps).
    /// </summary>
    public const string SingleLineJumpsKey = StyleBro.Analyzers.Layout.Braces.SingleLineJumpsKey;

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

    /// <summary>The SDK key for spaces inside parentheses: a list of flags (<see cref="Spacing"/>), 'false' for none.</summary>
    public const string SpaceBetweenParenthesesKey = "csharp_space_between_parentheses";

    /// <summary>The comment start of init's line that turns IDE0055 off for mixed spacing (kept on a second init).</summary>
    public const string SpacingComment = "# init: spacing:";

    /// <summary>What the using order's '# init:' comment is about ('# init: using directives: ...').</summary>
    public const string UsingsWhat = "using directives";

    /// <summary>The whitespace keys judged against the .editorconfig that decides each file's value (<see cref="FileSettings"/>).</summary>
    public static readonly string[] FileKeys = { EndOfLineKey, CharsetKey, FinalNewlineKey, TrailingWhitespaceKey };

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
        new(SingleLineJumpsKey, "one-line jumps ('if (x) return;')", "false", ("false", "in braces"), ("true", "on the 'if' line without braces"), null) { KeepAny = true, Keeps = $"without it {DiagnosticIds.BracesOmitted} would add braces to each" },
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
        new(SingleLineStatementsKey, "statements on their 'if'/loop/'case' line", "false", ("false", "on a line of their own"), ("true", "on their owner's line"), null) { KeepAny = true, Keeps = "csharp_preserve_single_line_statements = false would move each to a line of its own" },
        new(InheritDocKey, "overrides/interface implementations", "warning", ("warning", "with a doc comment"), ("none", "without a doc comment"), Off(DiagnosticIds.InheritDocumentation)),
        new(MemberOrderKey, "types' members", "warning", ("warning", "in BRO1001's order"), ("none", "in another order"), Off(DiagnosticIds.MemberOrdering)) { Unit = "type" },
        new(OverloadsKey, "overloads BRO1001 could split", "false", ("false", "apart"), ("true", "next to each other"), null),
        new(ItemsPerLineKey, "split lists' items", "warning", ("warning", "one per line"), ("none", "grouped on lines"), Off(DiagnosticIds.ParametersOnSameOrSeparateLines)),
        new(SuffixCaseKey, "unsigned literal suffixes", "warning", ("warning", "upper case ('0U', '1UL')"), ("none", "lower case ('0u', '1ul')"), Off(DiagnosticIds.LiteralSuffixCase)),
        new(AccessModifiersKey, "private-by-default members", "for_non_interface_members", ("for_non_interface_members", "with 'private'"), ("omit_if_default", "without 'private'"), Off(DiagnosticIds.AccessModifier)) { Enforcer = DiagnosticIds.AccessModifier, EnforcedValues = new[] { "for_non_interface_members" }, Unit = "member" },
        new(PropertySummaryKey, "property summaries", "warning", ("warning", "starting with 'Gets'/'Sets'"), ("none", "in their own words"), Off(DiagnosticIds.PropertySummaryWording)) { Unit = "summary" },
        new(ConstructorSummaryKey, "constructor summaries", "warning", ("warning", "with the standard sentence"), ("none", "in their own words"), Off(DiagnosticIds.ConstructorSummary)) { Unit = "summary" },
        new(CombinedLocalsKey, "adjacent locals of one type", "warning", ("warning", "declared one by one"), ("none", "declared together ('int a, b;')"), Off(DiagnosticIds.CombinedLocals)),
        new(CombinedAttributesKey, "declarations with 2+ attributes", "warning", ("warning", "one per list ('[A]' '[B]')"), ("none", "combined ('[A, B]')"), Off(DiagnosticIds.CombinedAttributes)),
        new(CombinedFieldsKey, "adjacent fields of one type", "warning", ("warning", "declared one by one"), ("none", "declared together ('int a, b;')"), Off(DiagnosticIds.CombinedFields)),
        new(StaticPrefixKey, "private static fields", "warning", ("warning", "without a prefix"), ("none", "with an 's_'/'t_' prefix"), Off(DiagnosticIds.FieldPrefix)) { Unit = "field" },
        new(RelativeUsingsKey, "usings inside a namespace", "warning", ("warning", "fully qualified"), ("none", "relative ('using Common;')"), Off(DiagnosticIds.QualifiedUsing)) { Unit = "using" },
        new(TypeAliasKey, "built-in type names", "warning", ("warning", "only keywords ('int')"), ("none", "framework names ('Int32')"), Off("IDE0049"), "dotnet_style_predefined_type_for_locals_parameters_members", "dotnet_style_predefined_type_for_member_access") { Unit = "file" },
        new(AutoAccessorsKey, "auto-property accessors ('{ get; set; }')", "warning", ("warning", "on one line"), ("none", "on several lines"), Off(DiagnosticIds.AutoAccessorsOnOneLine)) { Unit = "property declaration" },
        new(RegionsKey, "'#region's inside members", "warning", ("warning", "without"), ("none", "'#region's"), Off(DiagnosticIds.NoRegionsInCodeElements)) { Unit = "region", KeepAny = true, Keeps = $"{DiagnosticIds.NoRegionsInCodeElements} would remove them" },
        new(EndOfLineKey, "C# files' line endings", "unset", ("lf", "LF"), ("crlf", "CRLF"), null) { Unit = "file" },
        new(SeparationKey, "neighbouring members", "warning", ("warning", "with a blank line between"), ("none", "without one"), Off(DiagnosticIds.ElementsSeparatedByBlankLine)) { Unit = "member pair" },
        new(DocBlankLineKey, "doc comments below code", "warning", ("warning", "after a blank line"), ("none", "right below the code"), Off(DiagnosticIds.BlankLineBeforeDocumentation)) { Unit = "doc comment" },
        new(CommentBlankLineKey, "'//' comments above code", "warning", ("warning", "right above the code"), ("none", "with a blank line below"), Off(DiagnosticIds.BlankLineAfterComment)) { Unit = "comment" },
        new(DocElementBlankLineKey, "doc comments above their element", "warning", ("warning", "right above it"), ("none", "with a blank line below"), Off(DiagnosticIds.BlankLineAfterDocumentation)) { Unit = "doc comment" },
        new(PrimaryParametersKey, "primary-constructor parameters", "warning", ("warning", "named 'logger'"), ("none", "named like fields ('_logger')"), Off(DiagnosticIds.ParameterCasing)) { Unit = "parameter" },
        new(StyleBro.Analyzers.Naming.TupleElementNames.CasingKey, "tuple element names", "PascalCase", ("PascalCase", "'(int Count, ...)'"), ("camelCase", "'(int count, ...)'"), Off(DiagnosticIds.TupleElementCasing)) { Unit = "element" },
    };

    /// <summary>
    /// The spacing options IDE0055 enforces that real code varies on (Bogus writes 'foreach( var x in y )' in 341 places and
    /// 'foreach (var x in y)' in 187; IDE0055 rewrote the 341): the key, the flag of csharp_space_between_parentheses (null:
    /// a true/false key), what is counted, and the two forms (the SDK's default first). Counts: [default form, other form,
    /// neither] (one-sided parentheses like 'F( x)': IDE0055 changes them either way).
    /// </summary>
    public static readonly (string Key, string? Flag, string What, string Default, string Other)[] Spacing =
    {
        ("csharp_space_after_keywords_in_control_flow_statements", null, "control-flow keywords", "'if (x)'", "'if(x)'"),
        (SpaceBetweenParenthesesKey, "control_flow_statements", "control-flow parentheses", "'if (x)'", "'if ( x )'"),
        (SpaceBetweenParenthesesKey, "expressions", "parenthesized expressions", "'(a + b)'", "'( a + b )'"),
        (SpaceBetweenParenthesesKey, "type_casts", "cast parentheses", "'(int)x'", "'( int )x'"),
        ("csharp_space_after_cast", null, "casts", "'(int)x'", "'(int) x'"),
        ("csharp_space_between_method_call_parameter_list_parentheses", null, "call arguments", "'F(x)'", "'F( x )'"),
        ("csharp_space_between_method_declaration_parameter_list_parentheses", null, "declared parameters", "'void F(int x)'", "'void F( int x )'"),
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
    /// The SDK key BRO1404/BRO1007 follow: members of a class, struct or record that are private by default, with 'private' or
    /// without an access modifier (issue #129: a trial repository leaves 'private' out everywhere, and BRO1404 added it in 58
    /// files). 'omit_if_default' turns both rules off; IDE0040, which would remove the modifiers, is off in init's block.
    /// </summary>
    private const string AccessModifiersKey = "dotnet_style_require_accessibility_modifiers";

    /// <summary>
    /// BRO1142's severity: locals declared together ('int n = h.Length, i = 0;') or one by one (issue #129: ~180 such
    /// declarations in a trial repository's hot loops, all split). Places: a declaration of two or more, and a one-variable
    /// declaration right after one it could have been combined with.
    /// </summary>
    private const string CombinedLocalsKey = "dotnet_diagnostic." + DiagnosticIds.CombinedLocals + ".severity";

    /// <summary>
    /// BRO1604's severity: property summaries that start the StyleCop way ('Gets the samples', 'Gets or sets ...') or in the
    /// code's own words ('The samples, float32 interleaved'; issue #132: 0 of ~100 in a trial repository, and BRO1604
    /// rewrote them all). Owner's decision 2026-10-12.
    /// </summary>
    private const string PropertySummaryKey = "dotnet_diagnostic." + DiagnosticIds.PropertySummaryWording + ".severity";

    /// <summary>BRO1606's severity: constructor summaries with the standard sentence ('Initializes a new instance of ...') or in their own words ('Opens a file ...').</summary>
    private const string ConstructorSummaryKey = "dotnet_diagnostic." + DiagnosticIds.ConstructorSummary + ".severity";

    /// <summary>
    /// BRO1102's severity: declarations with two or more attributes, combined in a list ('[A, B]') or one per list (tusdotnet:
    /// 76 combined and 42 separate, and BRO1102 split 75 lists). Not parameters or type parameters (BRO1102 leaves them).
    /// </summary>
    private const string CombinedAttributesKey = "dotnet_diagnostic." + DiagnosticIds.CombinedAttributes + ".severity";

    /// <summary>BRO1114's severity: fields declared together ('int _rate, _channels;') or one by one, counted like <see cref="CombinedLocalsKey"/>.</summary>
    private const string CombinedFieldsKey = "dotnet_diagnostic." + DiagnosticIds.CombinedFields + ".severity";

    /// <summary>
    /// Files whose checkout ending git picks per machine (core.autocrlf, or 'text' without 'eol'): CRLF on Windows, LF on
    /// Linux. Writing one machine's ending would make 'format' rewrite every file on the other OS; without the key, 'format'
    /// writes the OS's ending, which is what such a checkout has anyway.
    /// </summary>
    private const string ConvertedKey = "end_of_line (converted by git)";

    /// <summary>
    /// Files with a list of 2 or more usings: [sorted with System first only, sorted without only, sorted both ways
    /// (nothing tells them apart), unsorted].
    /// </summary>
    private const string UsingsKey = "using order";

    /// <summary>Interface members (methods, properties, indexers, events): [with an access modifier, without].</summary>
    private const string InterfaceAccessKey = "interface member access";

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

    /// <summary>
    /// BRO1505's severity: neighbouring members of a type that need a blank line between them (one spans several lines, or is
    /// documented), with one or without (owner's policy, trial 14: Kavita's compact code got blank lines in 392 files).
    /// </summary>
    private const string SeparationKey = "dotnet_diagnostic." + DiagnosticIds.ElementsSeparatedByBlankLine + ".severity";

    /// <summary>BRO1513's severity: doc comments right below code, or after a blank line (Kavita: 312 files changed).</summary>
    private const string DocBlankLineKey = "dotnet_diagnostic." + DiagnosticIds.BlankLineBeforeDocumentation + ".severity";

    /// <summary>
    /// BRO1506's severity: a '//' comment right above the code it's about, or with a blank line below (owner's choice
    /// 2026-10-14: RecyclableMemoryStream keeps one below commented-out tests; BRO1506 removed it).
    /// </summary>
    private const string CommentBlankLineKey = "dotnet_diagnostic." + DiagnosticIds.BlankLineAfterComment + ".severity";

    /// <summary>
    /// BRO1511's severity: doc comments right above their element, or with a blank line between (trial 20: MoreLINQ has one in
    /// 324 of 491 places, and BRO1511 removed them in 115 files).
    /// </summary>
    private const string DocElementBlankLineKey = "dotnet_diagnostic." + DiagnosticIds.BlankLineAfterDocumentation + ".severity";

    /// <summary>
    /// BRO1302's severity, judged on primary-constructor parameters of classes and structs: named like parameters ('logger') or
    /// like the fields they replace ('_logger'; trial 20: BRO1302 renamed some of Altinn's, guards kept others). BRO1302 has no
    /// switch for them alone, so the code's '_' turns it off for every parameter.
    /// </summary>
    private const string PrimaryParametersKey = "dotnet_diagnostic." + DiagnosticIds.ParameterCasing + ".severity";

    /// <summary>The SDK option BRO1406 follows ('never_if_unnecessary' turns it off).</summary>
    private const string ArithmeticKey = "dotnet_style_parentheses_in_arithmetic_binary_operators";

    /// <summary>The StyleCop way to begin a property summary (BRO1604) or a constructor summary (BRO1606).</summary>
    private static readonly string[] PropertySummaryStarts = { "Gets", "Sets", "Initializes" };

    private static readonly string[] ConstructorSummaryStarts = { "Initializes a new instance", "Initializes static members", "Prevents a default instance" };

    /// <summary>The framework names IDE0049 rewrites to keywords.</summary>
    private static readonly HashSet<string> FrameworkTypeNames = new(StringComparer.Ordinal)
    {
        "String", "Object", "Boolean", "Char", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Single", "Double", "Decimal",
    };

    /// <summary>CSharpier's configuration files.</summary>
    private static readonly string[] CSharpierConfigs = { ".csharpierrc", ".csharpierrc.json", ".csharpierrc.yaml", ".csharpierrc.yml" };

    /// <summary>The SDK formatter's options (IDE0055's): with CSharpier they don't apply.</summary>
    private static readonly string[] FormatterKeys = { "csharp_new_line_before_open_brace", "csharp_new_line_before_else", SingleLineStatementsKey };

    /// <summary>
    /// What shows that CSharpier formats the repository (a file, relative to the root), or null: a .csharpierrc(.json/.yaml/.yml)
    /// or a 'csharpier' tool in [.config/]dotnet-tools.json in the root or a folder the code is run from (a solution's or a
    /// project's folder, or one above it: tusdotnet keeps its solution and tool manifest under Source/), or a CSharpier.MsBuild
    /// package reference in an MSBuild file.
    /// </summary>
    public static string? CSharpier(string root) => CSharpierScope(root)?.Evidence;

    /// <summary>
    /// <see cref="CSharpier"/>'s evidence and the folder CSharpier formats (relative to the root, '/'-separated): empty when it
    /// covers every C# file under the root (tusdotnet: all its code is under Source/, so the whole repository), else that
    /// folder, whose own section gets the CSharpier settings.
    /// </summary>
    public static (string Evidence, string Folder)? CSharpierScope(string root) => CSharpierScopes(root) is [var first, ..] ? first : null;

    /// <summary>
    /// Every folder CSharpier formats (<see cref="CSharpierScope"/>), one entry each: synth1 configures it in src/Pretty/ and
    /// src/Pretty2/, and with only the first recognised BRO1110 fought CSharpier in Pretty2 forever. One entry with an empty
    /// folder when it covers every C# file; a folder inside another's is left out (the outer section reaches it).
    /// </summary>
    public static List<(string Evidence, string Folder)> CSharpierScopes(string root)
    {
        var files = StyleCopSetup.EnumerateFiles(root).ToList();
        string Relative(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
        var folders = files.Where(f => Path.GetExtension(f).ToLowerInvariant() is ".sln" or ".slnx" or ".slnf" or ".csproj")
            .SelectMany(f => Ancestors(Path.GetDirectoryName(f)!))
            .Prepend(root)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f.Length);
        var evidences = new List<string>();
        foreach (var folder in folders)
        {
            var evidence = CSharpierConfigs.Select(f => Path.Combine(folder, f)).FirstOrDefault(File.Exists)
                ?? new[] { Path.Combine(folder, ".config", "dotnet-tools.json"), Path.Combine(folder, "dotnet-tools.json") }
                    .FirstOrDefault(f => File.Exists(f) && System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"""csharpier""", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
            if (evidence is not null)
            {
                evidences.Add(evidence);
            }
        }

        if (evidences.Count == 0 && files.Where(StyleCopSetup.IsMSBuild).FirstOrDefault(f => File.ReadAllText(f).Contains("CSharpier.MsBuild", StringComparison.OrdinalIgnoreCase)) is { } msbuild)
        {
            evidences.Add(msbuild);
        }

        var cs = files.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Select(Relative).ToList();
        var result = new List<(string Evidence, string Folder)>();
        foreach (var evidence in evidences)
        {
            var scope = Relative(CSharpierFolder(evidence));
            scope = scope == "." ? string.Empty : scope;
            if (scope.Length == 0 || cs.All(f => f.StartsWith(scope + "/", StringComparison.OrdinalIgnoreCase)))
            {
                return [(Relative(evidence), string.Empty)];
            }

            if (!result.Any(r => (scope + "/").StartsWith(r.Folder + "/", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add((Relative(evidence), scope));
            }
        }

        return result;

        IEnumerable<string> Ancestors(string folder)
        {
            for (var f = folder; f.Length >= root.Length && f.StartsWith(root, StringComparison.OrdinalIgnoreCase); f = Path.GetDirectoryName(f)!)
            {
                yield return f;
            }
        }
    }

    /// <summary>
    /// The lines that turn <see cref="CSharpierRules"/> off (with the '# init:' comment saying why) and the report line, for a
    /// repository CSharpier formats (<paramref name="evidence"/>: <see cref="CSharpier"/>), and the keys that tell the fixes
    /// so (stylebro_layout_by, and CSharpier's line width: <see cref="CSharpierPrintWidth"/>), so a fix that rewrites code lays
    /// it out as CSharpier would where it can, and the brace rules judge a statement by how CSharpier will break it.
    /// </summary>
    public static (List<string> Lines, string Report) CSharpierLines(string evidence, string root, IReadOnlyDictionary<string, string>? settings, string folder = "")
    {
        var what = folder.Length == 0 ? "this repository" : folder + "/";
        var lines = new List<string> { $"# init: CSharpier formats {what} ({evidence}): IDE0055 and the {CSharpierRules.Length - 1} StyleBro rules for what it decides (layout, line breaks, blank lines it normalizes) are off, so the two formatters don't undo each other. To have StyleBro format instead: remove CSharpier, then these lines." };
        lines.AddRange(CSharpierRules.Select(Off));
        lines.Add($"{StyleBro.Analyzers.Layout.CSharpierLayout.Key} = csharpier");
        lines.Add($"{StyleBro.Analyzers.Layout.CSharpierLayout.WidthKey} = {CSharpierPrintWidth(CSharpierFolder(Path.Combine(root, evidence)), settings, root)}");
        lines.AddRange(CSharpierUsingLines(settings));
        return (lines, $"  {"off",-8} CSharpier ({evidence}) -> IDE0055 and {CSharpierRules.Length - 1} layout rules are off: {string.Join(", ", CSharpierRules.Skip(1))}");
    }

    /// <summary>
    /// The using sort keys CSharpier's order needs, where the repository sets them otherwise (<paramref name="settings"/>):
    /// CSharpier sorts using directives itself (System first, no blank lines added between groups), and 'dotnet format' sorts
    /// them whenever a sort key is set, whatever its value. Mediator says dotnet_sort_system_directives_first = false, and
    /// 'format' reordered the usings of every file CSharpier had sorted (and CSharpier put them back).
    /// </summary>
    public static List<string> CSharpierUsingLines(IReadOnlyDictionary<string, string>? settings)
    {
        string? Value(string key) => settings is not null && settings.TryGetValue(key, out var v) ? v.Split(':')[0].Trim().ToLowerInvariant() : null;
        var lines = new List<string>();
        if (Value(SortUsingsKey) is { } systemFirst && systemFirst != "true")
        {
            lines.Add($"{SortUsingsKey} = true");
        }

        if (Value("dotnet_separate_import_directive_groups") is { } separate && separate != "false")
        {
            lines.Add("dotnet_separate_import_directive_groups = false");
        }

        return lines;
    }

    /// <summary>
    /// CSharpier's line width for the code under <paramref name="folder"/>: printWidth from the nearest .csharpierrc at or above
    /// it, up to <paramref name="root"/> (CSharpier then ignores .editorconfig), else the .editorconfig's max_line_length
    /// (<paramref name="settings"/>), else CSharpier's default (100).
    /// </summary>
    public static int CSharpierPrintWidth(string folder, IReadOnlyDictionary<string, string>? settings, string? root = null)
    {
        // CSharpier reads the nearest .csharpierrc above a file: from the folder it formats up to the root.
        root ??= folder;
        var configs = new List<string>();
        for (var f = folder; f is not null && f.StartsWith(root, StringComparison.OrdinalIgnoreCase); f = Path.GetDirectoryName(f))
        {
            configs.AddRange(CSharpierConfigs.Select(c => Path.Combine(f, c)));
        }

        if (configs.FirstOrDefault(File.Exists) is { } config)
        {
            return System.Text.RegularExpressions.Regex.Match(File.ReadAllText(config), @"printWidth""?\s*:\s*(\d+)") is { Success: true } match
                ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)
                : StyleBro.Analyzers.Layout.CSharpierLayout.DefaultWidth;
        }

        return settings is not null && settings.TryGetValue("max_line_length", out var value) && int.TryParse(value.Trim(), out var width) && width > 0
            ? width
            : StyleBro.Analyzers.Layout.CSharpierLayout.DefaultWidth;
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
                CountFile(text, own);
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
        counts[FinalNewlineKey] = new int[2];
        counts[TrailingWhitespaceKey] = new int[2];
        counts[HeaderKey] = new int[2];
        counts[ConvertedKey] = new int[2];
        counts[InterfaceAccessKey] = new int[2];
        foreach (var aspect in Spacing)
        {
            counts[SpacingCountKey(aspect)] = new int[3];
        }

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
    public static (string? Line, string? Report) LineEndings(string root) => LineEndings(EndingCounts(root));

    /// <summary><see cref="LineEndings(string)"/> from counts (<see cref="EndingCounts"/>), e.g. without the files a nearer .editorconfig decides.</summary>
    public static (string? Line, string? Report) LineEndings(IReadOnlyDictionary<string, int[]> counts)
    {
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

    /// <summary>
    /// The C# files' line endings, charset and last line breaks alone (<see cref="Count(string, IReadOnlyDictionary{string, string}?)"/>'s,
    /// without parsing; the migration asks).
    /// </summary>
    public static Dictionary<string, int[]> EndingCounts(string root)
    {
        var counts = NewCounts();
        var converted = ConvertedByGit(root);
        foreach (var file in Migration.OwnCSharpFiles(root))
        {
            var text = File.ReadAllText(file);
            var relative = Path.GetRelativePath(root, file);
            if (Migration.IsGeneratedOrVendored(relative, text))
            {
                continue;
            }

            if (LineEnding(text) is var ending and >= 0)
            {
                _ = converted.TryGetValue(Path.GetFullPath(file), out var byAttribute) ? counts[ConvertedKey][byAttribute ? 1 : 0]++ : counts[EndOfLineKey][ending]++;
            }

            counts[CharsetKey][HasBom(file) ? 1 : 0]++;
            CountFile(text, counts);
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
    public static string OutnumberedVerdict(IReadOnlyDictionary<string, int[]> counts) => OutnumberedFound(counts) + ", so " + OutnumberedResult(counts);

    /// <summary>How many C# files git converts (the first half of <see cref="OutnumberedVerdict"/>).</summary>
    public static string OutnumberedFound(IReadOnlyDictionary<string, int[]> counts)
    {
        var converted = counts[ConvertedKey].Sum();
        var others = counts.TryGetValue(EndOfLineKey, out var fixedEndings) ? fixedEndings.Sum() : 0;
        return $"git converts {converted} of {converted + others} C# files on checkout";
    }

    /// <summary>
    /// What follows (the second half): trial 20's report said "1 of 1 CRLF -> git converts 258 of 259 ...", counting the one
    /// committed file as the finding.
    /// </summary>
    public static string OutnumberedResult(IReadOnlyDictionary<string, int[]> counts)
    {
        var others = counts.TryGetValue(EndOfLineKey, out var fixedEndings) ? fixedEndings.Sum() : 0;
        return $"end_of_line isn't set (each machine keeps its own ending; the {Plural(others, "file")} committed with a fixed ending {(others == 1 ? "doesn't" : "don't")} decide)";
    }

    /// <summary>
    /// The repository's own end_of_line when git gives C# files each machine's ending on checkout (owner's decision
    /// 2026-10-12: "Keep endings out of it, warn"). kubernetes-client says 'end_of_line = lf' with '*.cs text=auto': a
    /// Windows checkout has CRLF, so IDE0055 at error failed the build 266 times and 'format' rewrote 205 files to LF (4
    /// tests compare against "\r\n"). Then the generated settings unset end_of_line for C# files ('format' and IDE0055 leave
    /// the checkout's endings alone, probed with dotnet format and the build), IDE0055 is a suggestion, and the report says
    /// how to settle it. Null without a contradiction; <paramref name="own"/>: <see cref="Migration.OwnSettings"/>;
    /// <paramref name="excluded"/>: how many of those files a nearer .editorconfig decides end_of_line for (<see cref="FileSettings"/>).
    /// </summary>
    public static (List<string> Lines, string Report)? EndingContradiction(string root, IReadOnlyDictionary<string, string> own, int excluded = 0)
    {
        if (!own.TryGetValue(EndOfLineKey, out var value) || value.ToLowerInvariant() is not ("lf" or "crlf") || PreviewCommand.Git(root, "ls-files", "--eol", "-z", "--", "*.cs") is not (0, var output))
        {
            return null;
        }

        return EndingContradiction(value.ToLowerInvariant(), PickedPerMachine(output) - excluded);
    }

    /// <summary>
    /// The report note when the repository's own end_of_line (<paramref name="own"/>) says one ending and the C# files git
    /// leaves alone clearly have the other (the usual majority), or null. Nothing enforced the key, so the files decide, like a
    /// convention key at severity silent (owner's decision 2026-10-12, extended: "Keep endings out of it, warn"):
    /// CleanArchitecture says 'end_of_line = crlf' with 349 of 350 files LF and no conversion, and 'format' turned 86 files to
    /// CRLF, 44 with no other change. 'unset' would be worse there: probed, 'dotnet format' then writes the OS's ending into
    /// every file it formats (a Windows run turned an untouched LF file CRLF), while without the key it keeps a file's ending.
    /// </summary>
    public static (string Ending, string Note)? ContradictedEnding(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> own, string? file = null)
    {
        if (!own.TryGetValue(EndOfLineKey, out var setting) || ConvertedOutnumber(counts) || !counts.TryGetValue(EndOfLineKey, out var endings)
            || endings.Sum() < MinimumAgreeing)
        {
            return null;
        }

        // The minority is converted where 'format' runs (DynamicData: 794 of 847 LF, and its 53 CRLF files became LF, 21 of
        // them whole-file diffs, while the note said 'format' left endings alone). 'unset' would convert more (see above).
        var value = setting.Split(':')[0].Trim().ToLowerInvariant();
        var files = Winner(endings[0], endings[1]) switch { 0 => "lf", 1 => "crlf", _ => null };
        var others = files == "lf" ? endings[1] : endings[0];
        var keeps = others == 0
            ? "'format' leaves the endings alone"
            : $"'format' converts the other {others} ({value.ToUpperInvariant()}) to {files?.ToUpperInvariant()} where it formats them";
        return value is not ("lf" or "crlf") || files is null || files == value
            ? null
            : (files, $"Line endings: {file ?? "your .editorconfig"} says end_of_line = {value}, but {endings[files == "lf" ? 0 : 1]} of {Plural(endings.Sum(), "C# file")} are {files.ToUpperInvariant()} as committed and git doesn't convert them: nothing enforced it, so end_of_line = {files} is written for C# files{(file is null ? string.Empty : $" in {file}")} and {keeps}. To keep {value.ToUpperInvariant()}: put 'end_of_line = {value}' below the stylebro-migrate block, in a [*.cs] section (a later line wins; the block's own lines come back on the next init) and run 'stylebro-migrate format' (it rewrites the files), or add '*.cs text eol={value}' to .gitattributes and run 'git add --renormalize .'.");
    }

    /// <summary>
    /// The line and the note when the repository's own <paramref name="key"/> (<see cref="CharsetKey"/> or
    /// <see cref="FinalNewlineKey"/>, for C# files) doesn't have <see cref="Winner"/>'s share of the C# files, or null. Like an
    /// end_of_line the files contradict (owner's decision 2026-10-12, "Keep endings out of it, warn"): nothing enforced it, so
    /// the files decide. Ical.Net says 'charset = utf-8' with 108 of 223 files starting with a BOM, and 'format' stripped all
    /// 108; the starter kit says 'insert_final_newline = false' with 1,203 of 1,716 files ending in a line break, and 'format'
    /// removed 1,092. Files clearly of the other form get their value; mixed files get 'unset', which 'dotnet format' leaves
    /// each file alone with (probed for both keys).
    /// </summary>
    public static (string Line, string Note)? ContradictedFileSetting(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> own, string key, string? file = null)
    {
        var (title, values, found, each) = key == CharsetKey
            ? ("Charset", new[] { "utf-8", "utf-8-bom" }, "start with a BOM", "each file's BOM (or its absence)")
            : ("Final line breaks", new[] { "false", "true" }, "end with a line break", "each file's last line break (or its absence)");
        if (!own.TryGetValue(key, out var setting) || !counts.TryGetValue(key, out var files) || files.Sum() < MinimumAgreeing)
        {
            return null;
        }

        var value = setting.Split(':')[0].Trim().ToLowerInvariant();
        var keyForm = Array.IndexOf(values, value);
        var winner = Winner(files[0], files[1]);
        if (keyForm < 0 || winner == keyForm)
        {
            return null;
        }

        var line = $"{key} = {(winner < 0 ? "unset" : values[winner])}";
        var keeps = winner < 0 ? $"'format' leaves {each} as it is" : "'format' leaves the files alone";
        return (line, $"{title}: {file ?? "your .editorconfig"} says {key} = {value}, but {files[1]} of {Plural(files.Sum(), "C# file")} {found}: nothing enforced it, so {line} is written for C# files{(file is null ? string.Empty : $" in {file}")} and {keeps}. To have {key} = {value} everywhere: put that line below the stylebro-migrate block, in a [*.cs] section (a later line wins; the block's own lines come back on the next init) and run 'stylebro-migrate format' (it rewrites the files).");
    }

    /// <summary>
    /// C# files grouped by the .editorconfig that decides one whitespace key (<see cref="FileKeys"/>) for them, the way the
    /// compiler and 'dotnet format' read it: the nearest file that sets the key wins, within it the last matching section, and
    /// the lookup stops at 'root = true' (Roslyn's own <see cref="AnalyzerConfigSet"/> per file). Bogus's Source/.editorconfig
    /// ('[*]', not root = true) says insert_final_newline = false over the root's '[*.cs]' true: init judged the files against
    /// the root's key, wrote 'unset' into the root block, which never reached them, and 'format' stripped the last line break
    /// from 48 files. The stylebro blocks don't count (init's own lines aren't the repository's), and the files are the ones
    /// <see cref="Count(string, IReadOnlyDictionary{string, string}?)"/> counts.
    /// </summary>
    public static List<FileSettingGroup> FileSettings(string root)
    {
        var top = Path.GetFullPath(root).TrimEnd('\\', '/');
        var configs = new Dictionary<string, AnalyzerConfigSet>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in StyleCopSetup.EnumerateFiles(root, ownRoot: true).Where(f => Path.GetFileName(f) == ".editorconfig"))
        {
            var path = Path.GetFullPath(file);
            var text = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(path), @"# BEGIN stylebro-.*?# END stylebro-[^\n]*", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
            configs[Path.GetDirectoryName(path)!] = AnalyzerConfigSet.Create(new[] { AnalyzerConfig.Parse(text, path) });
        }

        var (code, output) = PreviewCommand.Git(root, "ls-files", "--eol", "-z");
        var converted = (code == 0 ? Converted(output) : []).ToDictionary(f => Path.GetFullPath(Path.Combine(root, f.Path)), f => f.ByAttribute, StringComparer.OrdinalIgnoreCase);
        var picked = new HashSet<string>((code == 0 ? PickedPerMachinePaths(output) : []).Select(p => Path.GetFullPath(Path.Combine(root, p))), StringComparer.OrdinalIgnoreCase);
        var groups = new Dictionary<(string Key, string? Source, string? Value), FileSettingGroup>();
        foreach (var file in Migration.OwnCSharpFiles(root).Select(Path.GetFullPath))
        {
            var text = File.ReadAllText(file);
            if (Migration.IsGeneratedOrVendored(Path.GetRelativePath(top, file), text))
            {
                continue;
            }

            var own = FileCounts();
            if (LineEnding(text) is var ending and >= 0)
            {
                _ = converted.TryGetValue(file, out var viaAttribute) ? own[ConvertedKey][viaAttribute ? 1 : 0]++ : own[EndOfLineKey][ending]++;
            }

            own[CharsetKey][HasBom(file) ? 1 : 0]++;
            CountFile(text, own);

            // The chain from the file's folder up to the root: the nearest file that sets the key decides ('unset' included:
            // no value). Folders whose .editorconfig says 'root = true' aren't in it: init judges them on their own.
            var chain = new List<(string Folder, IReadOnlyDictionary<string, string> Options)>();
            for (var folder = Path.GetDirectoryName(file); folder is not null && folder.Length >= top.Length; folder = Path.GetDirectoryName(folder))
            {
                if (configs.TryGetValue(folder, out var config))
                {
                    chain.Add((folder, config.GetOptionsForSourcePath(file).AnalyzerOptions));
                }
            }

            foreach (var key in FileKeys)
            {
                var (source, value) = chain.FirstOrDefault(c => c.Options.ContainsKey(key)) is { Options: not null } decider
                    ? (Path.GetRelativePath(top, decider.Folder).Replace('\\', '/') is var relative && relative == "." ? string.Empty : relative, decider.Options[key].Trim().Trim('"').ToLowerInvariant())
                    : ((string?)null, (string?)null);
                var group = groups.TryGetValue((key, source, value), out var known) ? known : groups[(key, source, value)] = new FileSettingGroup(key, source, value == "unset" ? null : value);
                foreach (var (k, values) in own)
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        group.Counts[k][i] += values[i];
                    }
                }

                group.PickedPerMachine += picked.Contains(file) ? 1 : 0;
            }
        }

        return groups.Values.ToList();
    }

    /// <summary>Empty per-file counts for the whitespace keys (<see cref="FileSettings"/>).</summary>
    public static Dictionary<string, int[]> FileCounts() =>
        new[] { EndOfLineKey, ConvertedKey, CharsetKey, FinalNewlineKey, TrailingWhitespaceKey }.ToDictionary(k => k, _ => new int[2], StringComparer.Ordinal);

    /// <summary>
    /// The lines (comment first) and the report note when the end_of_line one .editorconfig sets (<paramref name="value"/>, for
    /// C# files) meets <paramref name="picked"/> C# files that git gives each machine's ending (<see cref="EndingContradiction(string, IReadOnlyDictionary{string, string}, int)"/>),
    /// or null. <paramref name="file"/>: the .editorconfig that says it, when it isn't the root's.
    /// </summary>
    public static (List<string> Lines, string Report)? EndingContradiction(string value, int picked, string? file = null)
    {
        if (value is not ("lf" or "crlf") || picked == 0)
        {
            return null;
        }

        var where = file ?? "your .editorconfig";
        var why = $"{where} says end_of_line = {value}, but git checks out {Plural(picked, "C# file")} with each machine's ending (a .gitattributes 'text' attribute without 'eol', or core.autocrlf), so {(value == "lf" ? "Windows" : "Linux and macOS")} checkouts don't match it";
        var settle = $"'*.cs text eol={value}' in .gitattributes (then 'git add --renormalize .'), or remove end_of_line from {file ?? ".editorconfig"}";
        return (
            [
                $"# {(file is null ? string.Empty : "init: ")}Line endings: {why}. Until that's settled, formatting and IDE0055 leave each file's endings as checked out. To settle it: {settle}; then run stylebro-migrate again.",
                $"{EndOfLineKey} = unset",
                "dotnet_diagnostic.IDE0055.severity = suggestion",
            ],
            $"Line endings: {why}. Set end_of_line = unset for C# files{(file is null ? string.Empty : $" in {file}")} and IDE0055 to suggestion, so neither 'format' nor the build touches endings. To settle it: {settle}.");
    }

    /// <summary>
    /// The note when the repository says 'trim_trailing_whitespace = false' and C# files have trailing whitespace: 'dotnet
    /// format' doesn't read the key and trims every line its whitespace pass formats (probed), so nothing can keep it but
    /// IDE0055 off. Null otherwise.
    /// </summary>
    public static string? TrailingWhitespaceNote(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> own, string? file = null) =>
        own.TryGetValue(TrailingWhitespaceKey, out var setting) && setting.Split(':')[0].Trim().Equals("false", StringComparison.OrdinalIgnoreCase)
        && counts.TryGetValue(TrailingWhitespaceKey, out var files) && files[1] > 0
            ? $"Trailing whitespace: {file ?? "your .editorconfig"} says {TrailingWhitespaceKey} = false, but 'dotnet format' doesn't read that key: its whitespace pass (IDE0055) trims trailing whitespace anyway, in {Plural(files[1], "C# file")} here. To keep it: dotnet_diagnostic.IDE0055.severity = none (no whitespace formatting at all)."
            : null;

    /// <summary>Counts a file's last line break and trailing whitespace (unit: files; empty files don't count).</summary>
    public static void CountFile(string text, Dictionary<string, int[]> counts)
    {
        if (text.Length == 0)
        {
            return;
        }

        counts[FinalNewlineKey][text[^1] == '\n' ? 1 : 0]++;
        counts[TrailingWhitespaceKey][System.Text.RegularExpressions.Regex.IsMatch(text, @"[ \t]\r?\n") ? 1 : 0]++;
    }

    /// <summary>
    /// The ReSharper/Rider keys that align code in columns ('resharper_int_align_variables = true' and the like; RestSharp:
    /// 'var doc      = CreateAttributesXml();'), set to true; the comments' alignment aside (IDE0055 leaves the spaces before
    /// a trailing comment). IDE0055's whitespace formatting flattens such columns: 414 hunks in 171 of RestSharp's files.
    /// </summary>
    public static List<string> AlignmentKeys(IReadOnlyDictionary<string, string> own) =>
        own.Where(p => System.Text.RegularExpressions.Regex.IsMatch(p.Key, @"^resharper_(csharp_)?int_align_(?!comments$)\w+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                && p.Value.Split(':')[0].Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// The lines that turn IDE0055 off (with the '# init:' comment) and the report line, when the repository aligns code in
    /// columns (<see cref="AlignmentKeys"/>), or null: without such keys, or when the repository sets IDE0055's severity itself.
    /// Like CSharpier (owner's policy, trial 12): another formatter decides the spacing, and the two would undo each other.
    /// </summary>
    public static (List<string> Lines, string Report)? AlignmentLines(IReadOnlyDictionary<string, string> own)
    {
        var keys = AlignmentKeys(own);
        if (keys.Count == 0 || own.ContainsKey("dotnet_diagnostic.IDE0055.severity"))
        {
            return null;
        }

        var named = string.Join(", ", keys.Take(3)) + (keys.Count > 3 ? $" and {keys.Count - 3} more" : string.Empty);
        return (
            [$"# init: column alignment: your .editorconfig aligns code in columns for ReSharper/Rider ({named} = true), and IDE0055's whitespace formatting would flatten it ('var x      = 1;' -> 'var x = 1;'), so IDE0055 is off: 'format' skips the whitespace pass. To have StyleBro format the whitespace instead: remove the next line.", Off("IDE0055")],
            $"  {"off",-8} column alignment ({named} = true) -> IDE0055 is off: its whitespace formatting would flatten aligned code");
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
    public static int PickedPerMachine(string output) => PickedPerMachinePaths(output).Count(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

    /// <summary>The paths of <see cref="PickedPerMachine"/>'s files (any extension).</summary>
    public static IEnumerable<string> PickedPerMachinePaths(string output) =>
        output.Split('\0').Where(entry => entry.IndexOf('\t') is var tab and > 0 && entry.Substring(0, tab) is var info
            && info.StartsWith("i/lf ", StringComparison.Ordinal) && !info.Contains("eol=", StringComparison.Ordinal)
            && (System.Text.RegularExpressions.Regex.IsMatch(info, @"\battr/text\b") || info.Contains("w/crlf", StringComparison.Ordinal)))
            .Select(entry => entry.Substring(entry.IndexOf('\t') + 1));

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
            Separation();
            CommentBlankLines();

            // Unit = regions inside members (BRO1113's): only the uses count.
            counts[RegionsKey][1] += StyleBro.Analyzers.Readability.Regions.GetRegions(root).Count(r => r.InCodeElement);
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
                Add(SuffixCaseKey, StyleBro.Analyzers.Readability.LiteralSuffixes.GetUpperCaseSuffix(token, StyleBro.Analyzers.Readability.SuffixScope.All) is not null);
            }
            else if ((token.IsKind(SyntaxKind.ElseKeyword) || token.IsKind(SyntaxKind.CatchKeyword) || token.IsKind(SyntaxKind.FinallyKeyword))
                && token.GetPreviousToken() is { RawKind: (int)SyntaxKind.CloseBraceToken, Parent: { } closed } closeBrace
                && Line(closed.GetFirstToken().SpanStart) != Line(closeBrace.SpanStart))
            {
                // Only after a block that spans lines: in a one-line block ('{ a(); } else b();') the layout isn't a choice
                // (trial 20: one-line methods counted as 'else on the } line' and turned the code K&R).
                Add("csharp_new_line_before_else", Line(token.GetPreviousToken().SpanStart) == Line(token.SpanStart));
            }
            else if (token.IsKind(SyntaxKind.OpenParenToken))
            {
                Spaces(token);
            }
        }

        foreach (var node in root.DescendantNodes(descendIntoTrivia: true))
        {
            if (Skip(node.SpanStart))
            {
                continue;
            }

            // Interface members that could take 'public': not the ones with another modifier (static, private, abstract, ...).
            if (node is MethodDeclarationSyntax or BasePropertyDeclarationSyntax or EventFieldDeclarationSyntax && node.Parent is InterfaceDeclarationSyntax
                && ((MemberDeclarationSyntax)node).Modifiers.All(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.NewKeyword))
            {
                Add(InterfaceAccessKey, !((MemberDeclarationSyntax)node).Modifiers.Any(SyntaxKind.PublicKeyword));
            }

            // Primary-constructor parameters of classes and structs (not records': BRO1302 leaves those) named like a field or not.
            if (node is ParameterSyntax { Parent: ParameterListSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax }, Identifier.ValueText: [var first, ..] }
                && (first == '_' || char.IsLower(first)))
            {
                Add(PrimaryParametersKey, first == '_');
            }

            // Tuple type element names (BRO1311's: only the first letter counts, '_' is fine).
            if (node is TupleElementSyntax { Identifier.ValueText: [var letter, ..] } && char.IsLetter(letter))
            {
                Add(StyleBro.Analyzers.Naming.TupleElementNames.CasingKey, char.IsLower(letter));
            }

            if (node is MemberDeclarationSyntax member && IsPrivateByDefault(member))
            {
                var access = member.Modifiers.Where(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword or SyntaxKind.PrivateKeyword).ToList();
                if (access.Count <= 1 && access.All(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                {
                    Add(AccessModifiersKey, access.Count == 0);
                }
            }

            // Attributes: once per declaration (at its first list) with 2+ in the lists of one target.
            if (node is AttributeListSyntax attributeList && attributeList.Parent is { } owner and not (ParameterSyntax or TypeParameterSyntax or CompilationUnitSyntax)
                && owner.ChildNodes().OfType<AttributeListSyntax>().First() == attributeList)
            {
                var lists = owner.ChildNodes().OfType<AttributeListSyntax>().Where(l => l.Target?.Identifier.Text == attributeList.Target?.Identifier.Text).ToList();
                if (lists.Sum(l => l.Attributes.Count) > 1)
                {
                    Add(CombinedAttributesKey, lists.Any(l => l.Attributes.Count > 1));
                }
            }

            if (node is FieldDeclarationSyntax or LocalDeclarationStatementSyntax)
            {
                var key = node is FieldDeclarationSyntax ? CombinedFieldsKey : CombinedLocalsKey;
                if (Declared(node).Variables.Count > 1)
                {
                    Add(key, true);
                }
                else if (node.Parent?.ChildNodes().TakeWhile(n => n != node).LastOrDefault() is { } previous && CouldCombine(previous, node))
                {
                    Add(key, false);
                }
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

                    // One-line jumps: 'if (a) return;' on the 'if' line, or a lone jump in braces.
                    if (node is IfStatementSyntax ifStatement && (body is BlockSyntax { Statements.Count: 1 } single ? single.Statements[0] : body) is var jump
                        && jump is ReturnStatementSyntax or ThrowStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax or GotoStatementSyntax
                            or YieldStatementSyntax { RawKind: (int)SyntaxKind.YieldBreakStatement }
                        && (body is BlockSyntax || Line(ifStatement.IfKeyword.SpanStart) == Line(jump.Span.End)))
                    {
                        Add(SingleLineJumpsKey, body is not BlockSyntax);
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

                case BasePropertyDeclarationSyntax { AccessorList: { Accessors.Count: > 0 } list } property
                    when list.Accessors.All(a => a.Body is null && a.ExpressionBody is null && a.AttributeLists.Count == 0):
                    // BRO1527's places: what it would put on one line, or what already is (from the name to the '}').
                    if (StyleBro.Analyzers.Layout.AutoAccessorLines.GetChanges(property, text, new Options(settings), _ => false) is not null)
                    {
                        Add(AutoAccessorsKey, true);
                    }
                    else if (Line(list.OpenBraceToken.GetPreviousToken().Span.End) == Line(list.CloseBraceToken.SpanStart))
                    {
                        Add(AutoAccessorsKey, false);
                    }

                    break;

                case XmlElementSyntax { StartTag.Name.LocalName.ValueText: "summary" } summary:
                    var textLines = summary.Content.SelectMany(c => c.DescendantTokens())
                        .Where(t => !t.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) && !string.IsNullOrWhiteSpace(t.Text))
                        .Select(t => Line(t.SpanStart))
                        .Distinct()
                        .ToList();

                    // How property and constructor summaries begin (BRO1604/BRO1606); one that starts with an element isn't counted.
                    var summaryKey = summary.Parent?.ParentTrivia.Token.Parent?.AncestorsAndSelf().FirstOrDefault(a => a is MemberDeclarationSyntax) switch
                    {
                        PropertyDeclarationSyntax => PropertySummaryKey,
                        ConstructorDeclarationSyntax => ConstructorSummaryKey,
                        _ => null,
                    };
                    var firstToken = summary.Content.SelectMany(c => c.DescendantTokens()).FirstOrDefault(t => !t.IsKind(SyntaxKind.XmlTextLiteralNewLineToken) && !string.IsNullOrWhiteSpace(t.Text));
                    if (summaryKey is not null && firstToken.IsKind(SyntaxKind.XmlTextLiteralToken))
                    {
                        var starts = summaryKey == PropertySummaryKey ? PropertySummaryStarts : ConstructorSummaryStarts;
                        var words = string.Join(" ", summary.Content.SelectMany(c => c.DescendantTokens()).Where(t => t.IsKind(SyntaxKind.XmlTextLiteralToken)).Select(t => t.Text.Trim()).Where(t => t.Length > 0));
                        Add(summaryKey, !starts.Any(s => words == s || words.StartsWith(s + " ", StringComparison.Ordinal)));
                    }

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

        // IDE0055's spacing options (Spacing): a control-flow keyword and the space before its '(', and the spaces inside
        // parentheses on one line (not empty ones), and after a cast's ')'.
        void Spaces(SyntaxToken open)
        {
            var parent = open.Parent;
            var close = parent?.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.CloseParenToken)) ?? default;
            if (parent is null || close.IsMissing || !close.IsKind(SyntaxKind.CloseParenToken) || close == open.GetNextToken() || Line(open.SpanStart) != Line(close.SpanStart))
            {
                return;
            }

            var flag = parent switch
            {
                IfStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or CommonForEachStatementSyntax or SwitchStatementSyntax
                    or UsingStatementSyntax or LockStatementSyntax or FixedStatementSyntax or CatchDeclarationSyntax => "control_flow_statements",
                ParenthesizedExpressionSyntax => "expressions",
                CastExpressionSyntax => "type_casts",
                ArgumentListSyntax { Parent: InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax } => "csharp_space_between_method_call_parameter_list_parentheses",
                ParameterListSyntax { Parent: BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax } => "csharp_space_between_method_declaration_parameter_list_parentheses",
                _ => null,
            };
            if (flag is null)
            {
                return;
            }

            static bool Spaced(SyntaxTriviaList trivia) => trivia.Any(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            var (after, before) = (Spaced(open.TrailingTrivia), Spaced(close.GetPreviousToken().TrailingTrivia) || Spaced(close.LeadingTrivia));
            counts[SpacingCountKey(Spacing.First(a => a.Flag == flag || a.Key == flag))][after == before ? (after ? 1 : 0) : 2]++;
            if (flag == "control_flow_statements" && open.GetPreviousToken() is var keyword && keyword.IsKeyword() && Line(keyword.SpanStart) == Line(open.SpanStart))
            {
                counts[SpacingCountKey(Spacing[0])][Spaced(keyword.TrailingTrivia) ? 0 : 1]++;
            }

            if (parent is CastExpressionSyntax cast && Line(cast.Expression.SpanStart) == Line(close.SpanStart))
            {
                counts[SpacingCountKey(Spacing[4])][Spaced(close.TrailingTrivia) ? 1 : 0]++;
            }
        }

        // Unit = pairs of a type's neighbouring members that BRO1505 wants a blank line between (with one or without), and
        // documented members whose doc comment BRO1513 wants a blank line above (after a blank line or right below code).
        void Separation()
        {
            var adjacent = StyleBro.Analyzers.Layout.ElementSeparation.AllowsAdjacentSingleLineMembers(new Options(settings));
            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                for (var i = 1; i < type.Members.Count; i++)
                {
                    if (StyleBro.Analyzers.Layout.ElementSeparation.NeedsBlankLine(type.Members[i - 1], type.Members[i], text, allowAdjacentSingleLine: adjacent))
                    {
                        Add(SeparationKey, !StyleBro.Analyzers.Layout.ElementSeparation.HasBlankLineBetween(type.Members[i - 1], type.Members[i], text));
                    }
                }
            }

            foreach (var member in root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax).OfType<MemberDeclarationSyntax>())
            {
                var documentation = StyleBro.Analyzers.Layout.DocumentationBlankLines.GetDocumentation(member);
                if (documentation.RawKind == 0)
                {
                    continue;
                }

                // The nearest non-blank line above: code BRO1513 wants a blank line below (not '{', a comment or '#if').
                var line = text.Lines.GetLineFromPosition(documentation.FullSpan.Start).LineNumber;
                var above = line - 1;
                while (above >= 0 && text.Lines[above].ToString().Trim().Length == 0)
                {
                    above--;
                }

                if (above >= 0 && text.ToString(TextSpan.FromBounds(text.Lines[line].Start, documentation.FullSpan.Start)).Trim().Length == 0
                    && StyleBro.Analyzers.Layout.DocumentationBlankLines.WantsBlankLineAbove(text.Lines[above + 1], text))
                {
                    Add(DocBlankLineKey, above == line - 1);
                }

                // BRO1511: a blank line between the doc comment and its element, or none (only whitespace and docs between).
                if (StyleBro.Analyzers.Layout.DocumentationBlankLines.GetBlankLinesAfterDocumentation(member, text) is not null)
                {
                    Add(DocElementBlankLineKey, true);
                }
                else if (member.GetLeadingTrivia().SkipWhile(t => t != documentation).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia) || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
                {
                    Add(DocElementBlankLineKey, false);
                }
            }
        }

        // Unit = '//' comments (the last line of a group) BRO1506 checks: with a blank line below, or right above the code.
        void CommentBlankLines()
        {
            foreach (var comment in root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)))
            {
                if (StyleBro.Analyzers.Layout.TrailingBlankLines.GetCheckedBlankLines(comment, text, removeStandalone: false) is { } blank)
                {
                    Add(CommentBlankLineKey, blank.Count > 0);
                }
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

        // BRO1404's declarations that are private when nothing says otherwise: members of a class, struct or record, without
        // what it skips (partial members, 'file' types, explicit implementations, static constructors).
        static bool IsPrivateByDefault(MemberDeclarationSyntax member) =>
            member.Parent is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax
            && !member.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword) || m.IsKind(SyntaxKind.FileKeyword))
            && member switch
            {
                BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or BaseFieldDeclarationSyntax or MethodDeclarationSyntax { ExplicitInterfaceSpecifier: null } => true,
                BasePropertyDeclarationSyntax property => property.ExplicitInterfaceSpecifier is null,
                ConstructorDeclarationSyntax constructor => !constructor.Modifiers.Any(SyntaxKind.StaticKeyword),
                _ => false,
            };

        static VariableDeclarationSyntax Declared(SyntaxNode node) => node is FieldDeclarationSyntax field ? field.Declaration : ((LocalDeclarationStatementSyntax)node).Declaration;

        // A one-variable declaration right after another of the same kind, type and modifiers ('var' can't declare two; fields
        // with attributes would share them).
        static bool CouldCombine(SyntaxNode previous, SyntaxNode node) =>
            previous.RawKind == node.RawKind
            && Declared(previous).Variables.Count == 1
            && Declared(previous).Type.ToString() == Declared(node).Type.ToString()
            && !Declared(node).Type.IsVar
            && previous switch
            {
                FieldDeclarationSyntax field => field.AttributeLists.Count == 0 && ((FieldDeclarationSyntax)node).AttributeLists.Count == 0
                    && field.Modifiers.ToString() == ((FieldDeclarationSyntax)node).Modifiers.ToString(),
                LocalDeclarationStatementSyntax local => local.Modifiers.ToString() == ((LocalDeclarationStatementSyntax)node).Modifiers.ToString()
                    && local.UsingKeyword.IsKind(SyntaxKind.None) == ((LocalDeclarationStatementSyntax)node).UsingKeyword.IsKind(SyntaxKind.None),
                _ => false,
            };

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
    public static (List<string> Lines, List<string> Report) Decide(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> decided, Func<string, bool>? isSet = null, IReadOnlyDictionary<string, string>? repository = null, IReadOnlyDictionary<string, string>? unenforced = null, string? spacing = null)
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
            var found = winner < 0 ? mixed : count[1 - winner] == 0 && convention.KeepAny ? $"{count[winner]} {convention.Values[winner].Label}" : $"{count[winner]} of {total} {convention.Values[winner].Label}";
            string tag;
            string verdict;
            if (decided.TryGetValue(convention.Key, out var reason))
            {
                if (total >= MinimumAgreeing && winner < 0 && Contradicted(convention, null, repository) is { } setting && !IsSet(Off(convention.Enforcer!)))
                {
                    // Mixed code under a key nothing enforced (the starter kit: beginning_of_line, 63 % of 771 operators start
                    // the line, and BRO1520 moved 285): the key was never the code's, so the rule goes off as for mixed code.
                    var rule = convention.Enforcer!;
                    (tag, verdict) = ("off", $".editorconfig says {setting}, unenforced, code mixed: {rule} is off");
                    summary.TurnedOff.Add($"{rule} ({convention.What}: your .editorconfig says {setting})");
                    lines.Add($"# init: {convention.What}: your .editorconfig says {convention.Key} = {setting}, but your code mixes both forms ({mixed}); nothing enforced it, so {rule} is off. To enforce it: change the code, then remove the next line.");
                    lines.Add(Off(rule));
                }
                else if (total >= MinimumAgreeing && winner >= 0 && Contradicted(convention, convention.Values[winner].Value, repository) is { } value && !IsSet(Off(convention.Enforcer!)))
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
                (tag, verdict) = ("git", OutnumberedResult(counts));
                found = OutnumberedFound(counts);
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
                summary.Unfollowed.Add($"{rule} ({convention.What}: {found})");
                lines.Add(convention.KeepAny
                    ? $"# init: {convention.What}: kept, because your code has some ({Plural(count[winner], convention.Unit)}; any {MinimumAgreeing} keep them: {convention.Keeps}), so {rule} is off. To turn it on: remove the next line."
                    : $"# init: {convention.What}: {count[winner]} of {total} in your code are {convention.Values[winner].Label}, so {rule} is off. To turn it on: remove the next line.");
                lines.Add(offLine);
            }
            else
            {
                var value = convention.Values[winner].Value;
                (tag, verdict) = ("kept", convention.KeepAny ? $"{value} ({MinimumAgreeing} are enough)" : value);
                summary.Written.Add(convention.Key);
                lines.Add((convention.KeepAny
                        ? $"# init: {convention.What}: allowed, because your code has some ({count[winner]} of {Plural(total, convention.Unit)} {convention.Values[winner].Label}; any {MinimumAgreeing} keep them allowed: {convention.Keeps})"
                        : $"# init: {convention.What}: {convention.Values[winner].Label} in {count[winner]} of {Plural(total, convention.Unit)} in your code")
                    + (unenforced?.TryGetValue(convention.Key, out var ignored) == true ? $" (your .editorconfig's {convention.Key} = {ignored} isn't enforced; to keep it, set it again below this block)" : string.Empty));
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

        DecideUsings(counts, decided, isSet, lines, report, summary, repository);
        DecideStaticCasing(counts, decided, isSet, lines, report, summary);
        DecideSpacing(counts, spacing, repository, lines, report, summary);

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

    public static string Plural(int count, string noun) => $"{count} {(count == 1 ? noun : PluralOf(noun))}";

    /// <summary>A noun's plural, English rules: 'summary' -> 'summaries', 'class' -> 'classes', 'file' -> 'files'.</summary>
    public static string PluralOf(string noun) =>
        noun.Length > 1 && noun[^1] == 'y' && !"aeiou".Contains(noun[^2]) ? noun[..^1] + "ies"
        : new[] { "s", "x", "z", "ch", "sh" }.Any(e => noun.EndsWith(e, StringComparison.Ordinal)) ? noun + "es"
        : noun + "s";

    /// <summary>
    /// The note when the repository sets the sort key (<see cref="SortUsingsKey"/> or dotnet_separate_import_directive_groups)
    /// and at least <see cref="MinimumAgreeing"/> files aren't in that order (<paramref name="c"/>: <see cref="UsingsKey"/>'s
    /// counts), or null. 'dotnet format' sorts usings whenever the key is set, whatever its value, and 'unset' doesn't help
    /// (an .editorconfig can't remove a key): the starter kit says 'dotnet_sort_system_directives_first = false' with 262 of
    /// 1,120 files in another order (not a clear majority either way of thinking: 77 % are in that order), and 'format'
    /// reordered 264 files. The key is the team's, so init only names it as the cause, with the count.
    /// </summary>
    public static string? UnsortedUnderKey(int[] c, IReadOnlyDictionary<string, string>? repository)
    {
        string? Value(string key) => repository is not null && repository.TryGetValue(key, out var v) ? v.Split(':')[0].Trim().ToLowerInvariant() : null;
        var key = Value(SortUsingsKey) is not null ? SortUsingsKey : Value("dotnet_separate_import_directive_groups") is not null ? "dotnet_separate_import_directive_groups" : null;
        var total = c.Sum();
        if (key is null || total < MinimumAgreeing)
        {
            return null;
        }

        // ponytail: without dotnet_sort_system_directives_first, the order counts as System first ('dotnet format''s default).
        var systemFirst = Value(SortUsingsKey) != "false";
        var sorted = c[systemFirst ? 0 : 1] + c[2];
        return total - sorted < MinimumAgreeing
            ? null
            : $"using directives: your .editorconfig sets {key} = {Value(key)}, and 'dotnet format' sorts usings whenever it is set (to any value): {total - sorted} of {Plural(total, "file")} aren't in that order ({(systemFirst ? "System first" : "System not first")}), and 'format' will reorder them. If your team doesn't keep usings in that order, remove {key} from .editorconfig (init can't: 'unset' sorts them too).";
    }

    /// <summary>The count key of a <see cref="Spacing"/> aspect.</summary>
    public static string SpacingCountKey((string Key, string? Flag, string What, string Default, string Other) aspect) =>
        "spacing: " + aspect.Key + (aspect.Flag is null ? string.Empty : "=" + aspect.Flag);

    /// <summary>
    /// The lines and the report line when the repository's <see cref="AccessModifiersKey"/> says 'always' (interface members
    /// too) and the code's interface members don't follow it, clearly or mixed, while nothing enforces the key in the build
    /// (<paramref name="enforcedInBuild"/>: EnforceCodeStyleInBuild with IDE0040 on): BaGetter says 'always:warning', IDE-only,
    /// none of its 102 interface members has one, and BRO1404 added 'public' to all of them. A key the code contradicts
    /// that nothing enforces doesn't win: for_non_interface_members keeps the rest of 'always'. Null otherwise.
    /// </summary>
    public static (List<string> Lines, string Report)? InterfaceAccess(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> repository, bool enforcedInBuild)
    {
        if (enforcedInBuild || !repository.TryGetValue(AccessModifiersKey, out var setting) || setting.Split(':')[0].Trim().ToLowerInvariant() != "always"
            || counts[InterfaceAccessKey] is not [var with, var without] || with + without < MinimumAgreeing || Winner(with, without) == 0)
        {
            return null;
        }

        var found = Winner(with, without) < 0 ? $"mixes both forms ({with} with, {without} without)" : $"has {without} of {Plural(with + without, "interface member")} without one";
        var note = $"interface members: your .editorconfig says {AccessModifiersKey} = {setting}, but your code {found}; nothing enforces it in the build, so {DiagnosticIds.AccessModifier} adds no access modifier to them ({AccessModifiersKey} = for_non_interface_members). To enforce it: change the code, then remove the next line.";
        return ([$"# init: {note}", $"{AccessModifiersKey} = for_non_interface_members"], $"  {"off",-8} {note.Substring(0, note.IndexOf(" To enforce", StringComparison.Ordinal))}");
    }

    /// <summary>The folder a piece of <see cref="CSharpier"/>'s evidence applies to: its own, or the one above '.config'.</summary>
    private static string CSharpierFolder(string evidence)
    {
        var folder = Path.GetDirectoryName(evidence)!;
        return Path.GetFileName(folder) == ".config" ? Path.GetDirectoryName(folder)! : folder;
    }

    /// <summary>
    /// The using order (<see cref="SortUsingsKey"/>): sorted files -> the key (System first unless the files that tell the
    /// two apart clearly don't), unsorted or mixed -> no key, so 'dotnet format' leaves them alone; too few files -> StyleBro's
    /// default (sorted, System first). A key the repository sets wins.
    /// </summary>
    private static void DecideUsings(IReadOnlyDictionary<string, int[]> counts, IReadOnlyDictionary<string, string> decided, Func<string, bool>? isSet, List<string> lines, List<string> report, SummaryLists summary, IReadOnlyDictionary<string, string>? repository = null)
    {
        const string What = UsingsWhat;
        if (!counts.TryGetValue(UsingsKey, out var c))
        {
            return;
        }

        var (sorted, unsorted) = (c[0] + c[1] + c[2], c[3]);
        var (total, apart) = (sorted + unsorted, c[0] + c[1]);
        var found = $"{sorted} of {Plural(total, "file")} sorted" + (apart > 0 ? $", {c[0]} of {apart} with System first" : string.Empty);
        if (isSet?.Invoke(SortUsingsKey) != true && isSet?.Invoke("dotnet_separate_import_directive_groups") != true && decided.TryGetValue(SortUsingsKey, out var reason))
        {
            // Another tool sorts them (CSharpier): no key, so 'dotnet format' leaves them alone.
            if (total > 0)
            {
                report.Add($"  {"set",-8} {What}: {found} -> {reason}");
            }

            return;
        }

        if (isSet?.Invoke(SortUsingsKey) == true || isSet?.Invoke("dotnet_separate_import_directive_groups") == true)
        {
            if (total > 0)
            {
                report.Add($"  {"set",-8} {What}: {found} -> set in .editorconfig");
            }

            if (UnsortedUnderKey(c, repository) is { } note)
            {
                report.Add($"  {"note",-8} {note}");
            }

            return;
        }

        var sort = total < MinimumAgreeing ? 0 : Winner(sorted, unsorted);
        var system = sort != 0 || apart < MinimumAgreeing ? 0 : Winner(c[0], c[1]);
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
    /// IDE0055's spacing options (<see cref="Spacing"/>): a clear form other than the SDK's default is written (the flags of
    /// csharp_space_between_parentheses together); any aspect with mixed code turns IDE0055 off with one '# init: spacing:'
    /// comment, since IDE0055 can't be narrowed to one option (owner's policy, trial 14: Bogus's 'foreach( var x in y )' in
    /// 341 places, 65 %, became 'foreach (var x in y)'). <paramref name="reason"/>: why IDE0055's spacing isn't init's to decide
    /// (CSharpier, column alignment, the repository's own IDE0055 severity, the last init's lines), then only reported.
    /// A key the repository sets itself (<paramref name="repository"/>) stays.
    /// </summary>
    private static void DecideSpacing(IReadOnlyDictionary<string, int[]> counts, string? reason, IReadOnlyDictionary<string, string>? repository, List<string> lines, List<string> report, SummaryLists summary)
    {
        if (reason is not null)
        {
            // One line: what IDE0055 would do with the spaces isn't init's call here.
            if (Spacing.Any(a => counts.TryGetValue(SpacingCountKey(a), out var c) && c.Sum() > 0))
            {
                report.Add($"  {"set",-8} spacing (csharp_space_*): {reason}");
            }

            return;
        }

        var mixed = new List<string>();
        var flags = new List<string>();
        var flagsSeen = false;
        var keys = new List<string>();
        foreach (var aspect in Spacing)
        {
            var c = counts.TryGetValue(SpacingCountKey(aspect), out var found) ? found : new int[3];
            var total = c.Sum();
            if (total == 0)
            {
                continue;
            }

            var winner = Winner(c[0], c[1] + c[2]) == 0 ? 0 : Winner(c[0] + c[2], c[1]) == 1 ? 1 : -1;
            var text = string.Join(", ", new[] { (c[0], aspect.Default), (c[1], aspect.Other), (c[2], "one-sided") }.Where(p => p.Item1 > 0).Select(p => $"{p.Item1} {p.Item2}"));
            var name = aspect.Flag is null ? aspect.Key : $"{aspect.Key} ({aspect.Flag})";
            string tag;
            string verdict;
            if (repository?.ContainsKey(aspect.Key) == true)
            {
                (tag, verdict) = ("set", $"set in .editorconfig ({repository[aspect.Key]})");
            }
            else if (c.Sum() < MinimumAgreeing)
            {
                (tag, verdict) = ("too few", "default stays");
                summary.TooFew.Add(name);
            }
            else if (winner < 0)
            {
                (tag, verdict) = ("off", "IDE0055 is off");
                mixed.Add($"{aspect.What} ({text})");
            }
            else if (winner == 0)
            {
                (tag, verdict) = ("default", aspect.Default);
                summary.Matched++;
            }
            else
            {
                (tag, verdict) = ("kept", aspect.Other);
                summary.Written.Add(name);
            }

            if (aspect.Flag is not null && repository?.ContainsKey(aspect.Key) != true)
            {
                flagsSeen |= winner == 1;
                if (winner == 1)
                {
                    flags.Add(aspect.Flag);
                }
            }
            else if (aspect.Flag is null && tag == "kept")
            {
                keys.Add($"{aspect.Key} = {(aspect.Key == Spacing[0].Key ? "false" : "true")}");
            }

            report.Add($"  {tag,-8} spacing, {aspect.What}: {(winner < 0 ? text : $"{c[winner]} of {total} {(winner == 0 ? aspect.Default : aspect.Other)}")} -> {verdict}");
        }

        if (flagsSeen)
        {
            keys.Add($"{SpaceBetweenParenthesesKey} = {string.Join(", ", flags)}");
        }

        if (keys.Count > 0)
        {
            lines.Add($"{SpacingComment} your code's spacing differs from the SDK's default ({string.Join("; ", keys)})");
            lines.AddRange(keys);
        }

        if (mixed.Count > 0)
        {
            summary.TurnedOff.Add($"IDE0055 (spacing: {string.Join(", ", mixed.Select(m => m.Substring(0, m.IndexOf(" (", StringComparison.Ordinal))))})");
            lines.Add($"{SpacingComment} your code mixes {string.Join("; ", mixed)}; IDE0055 can't be limited to one spacing option, so it is off: 'format' skips the whitespace pass. To have it format: set the spacing keys (csharp_space_*) and remove the next line.");
            lines.Add(Off("IDE0055"));
        }
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
        var tooFew = total < MinimumAgreeing;
        string tag;
        string verdict;
        if (decided.TryGetValue(StaticCasingKey, out var reason) || isSet?.Invoke(KeyOf(off)) == true)
        {
            (tag, verdict, found) = ("set", reason ?? $"{DiagnosticIds.FieldPascalCase}'s severity is set", Plural(total, "field"));
        }
        else if (tooFew)
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
    /// enforces it and the code's form (<paramref name="code"/>; null: mixed) is the other one; else null.
    /// </summary>
    private static string? Contradicted(Convention convention, string? code, IReadOnlyDictionary<string, string>? repository)
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

    /// <summary>
    /// C# files whose <see cref="Key"/> one .editorconfig decides (<see cref="FileSettings"/>): <see cref="Source"/> is its folder,
    /// relative to the root ('' the root's own, null when no file sets the key), <see cref="Value"/> what it says (lower case;
    /// null for 'unset' or none).
    /// </summary>
    internal sealed class FileSettingGroup(string key, string? source, string? value)
    {
        public string Key { get; } = key;

        public string? Source { get; } = source;

        public string? Value { get; } = value;

        /// <summary>Gets the files' counts (<see cref="FileCounts"/>).</summary>
        public Dictionary<string, int[]> Counts { get; } = FileCounts();

        /// <summary>Gets or sets how many of the files git gives each machine's ending (<see cref="PickedPerMachine"/>).</summary>
        public int PickedPerMachine { get; set; }

        /// <summary>Gets the .editorconfig (relative path) that decides it, or null for the root's own and for none.</summary>
        public string? File => Source is { Length: > 0 } ? Source + "/.editorconfig" : null;
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

        /// <summary>Gets what <see cref="Default"/> would do to the second form, for the '# init:' comment of a <see cref="KeepAny"/> convention.</summary>
        public string? Keeps { get; init; }
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
