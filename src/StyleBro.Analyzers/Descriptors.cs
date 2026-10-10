using Microsoft.CodeAnalysis;

namespace StyleBro.Analyzers;

internal static class Descriptors
{
    /// <summary>For the rules whose fix can leave a finding on purpose (a rename that would break code it can't see).</summary>
    private const string KeptNote = " If 'dotnet format' doesn't fix a finding, StyleBro kept it on purpose (for example, the name is read by "
        + "reflection): 'stylebro-migrate format' lists each one with the reason. Change it by hand only after checking those uses, "
        + "or suppress or baseline it.";

    public static readonly DiagnosticDescriptor MemberOrdering = new(
        id: DiagnosticIds.MemberOrdering,
        title: "Members should be ordered",
        messageFormat: "'{0}' should come before '{1}' ({2})",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Orders type members by kind, then accessibility, then const/static/readonly. "
            + "The order is configurable with the stylebro_member_* options in .editorconfig. "
            + "Run 'dotnet format analyzers --diagnostics BRO1001' to fix a whole solution. "
            + "Replaces StyleCop SA1201-SA1204 and SA1214.",
        helpLinkUri: HelpBase + DiagnosticIds.MemberOrdering + "/");

    public static readonly DiagnosticDescriptor CommentSpacing = new(
        id: DiagnosticIds.CommentSpacing,
        title: "Single-line comments should begin with a space",
        messageFormat: "Add a space after '//'",
        category: "Spacing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'// note' instead of '//note'. Commented-out code ('////') and separators ('//--') are left alone. "
            + "Replaces StyleCop SA1005.",
        helpLinkUri: HelpBase + DiagnosticIds.CommentSpacing + "/");

    public static readonly DiagnosticDescriptor ConstructorInitializerLine = new(
        id: DiagnosticIds.ConstructorInitializerLine,
        title: "Constructor initializers should be on their own line",
        messageFormat: "Put ': {0}(...)' {1}",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "': base(...)' and ': this(...)' go on the line after the constructor's parameters, one level "
            + "deeper. Replaces StyleCop SA1128.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstructorInitializerLine + "/");

    public static readonly DiagnosticDescriptor EmptyStatement = new(
        id: DiagnosticIds.EmptyStatement,
        title: "Code should not contain empty statements",
        messageFormat: "Remove the empty statement",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A stray ';' in a block, or after the closing brace of a type or namespace, does nothing. "
            + "Replaces StyleCop SA1106.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyStatement + "/");

    public static readonly DiagnosticDescriptor CombinedAttributes = new(
        id: DiagnosticIds.CombinedAttributes,
        title: "Each attribute should be in its own brackets",
        messageFormat: "Put '{0}' in its own brackets",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Writes '[A, B]' as '[A]' and '[B]'. Replaces StyleCop SA1133.",
        helpLinkUri: HelpBase + DiagnosticIds.CombinedAttributes + "/");

    public static readonly DiagnosticDescriptor ConstantOnLeft = new(
        id: DiagnosticIds.ConstantOnLeft,
        title: "Constants should be on the right-hand side of comparisons",
        messageFormat: "Put '{0}' on the right-hand side of the comparison",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'x == 1' reads more naturally than '1 == x'. Replaces StyleCop SA1131.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstantOnLeft + "/");

    public static readonly DiagnosticDescriptor DefaultValueConstructor = new(
        id: DiagnosticIds.DefaultValueConstructor,
        title: "Use default instead of a value type's default constructor",
        messageFormat: "Use '{0}' instead",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'new int()' creates the default value; 'default(int)' says so. Replaces StyleCop SA1129.",
        helpLinkUri: HelpBase + DiagnosticIds.DefaultValueConstructor + "/");

    public static readonly DiagnosticDescriptor EmptyString = new(
        id: DiagnosticIds.EmptyString,
        title: "Use string.Empty for empty strings",
        messageFormat: "Use '{0}' instead of '{1}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'string.Empty' instead of \"\", except where C# requires a constant; or \"\" instead of 'string.Empty' with "
            + "stylebro_empty_string_style = literal. Replaces StyleCop SA1122.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyString + "/");

    public static readonly DiagnosticDescriptor SplitParametersStartOnNewLine = new(
        id: DiagnosticIds.SplitParametersStartOnNewLine,
        title: "Split parameters should start on the line after the declaration",
        messageFormat: "Move the first item to the line after the opening parenthesis",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When parameters or arguments span several lines, the first one starts on its own line too. "
            + "Replaces StyleCop SA1116.",
        helpLinkUri: HelpBase + DiagnosticIds.SplitParametersStartOnNewLine + "/");

    public static readonly DiagnosticDescriptor ParametersOnSameOrSeparateLines = new(
        id: DiagnosticIds.ParametersOnSameOrSeparateLines,
        title: "Parameters should be on the same line or on separate lines",
        messageFormat: "Put the items all on one line or each on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters and arguments are either all on one line or each on its own line. Replaces StyleCop SA1117.",
        helpLinkUri: HelpBase + DiagnosticIds.ParametersOnSameOrSeparateLines + "/");

    public static readonly DiagnosticDescriptor VariableCasing = new(
        id: DiagnosticIds.VariableCasing,
        title: "Variable names should begin with a lower-case letter",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Local variables and query range variables are camelCase: 'total', not 'Total' or '_total'. "
            + "Replaces StyleCop SA1312."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.VariableCasing + "/");

    public static readonly DiagnosticDescriptor ParameterCasing = new(
        id: DiagnosticIds.ParameterCasing,
        title: "Parameter names should begin with a lower-case letter",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters are camelCase: 'count', not 'Count' or '_count'. The fix also renames named arguments "
            + "and the parameters of overrides and implementations. Replaces StyleCop SA1313."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.ParameterCasing + "/");

    public static readonly DiagnosticDescriptor PrivateFieldNaming = new(
        id: DiagnosticIds.PrivateFieldNaming,
        title: "Private field names should be camelCase",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Private fields are 'count', or '_count' with stylebro_private_field_naming = _camelCase. "
            + "Replaces StyleCop SA1306 and SA1309 for private fields."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.PrivateFieldNaming + "/");

    public static readonly DiagnosticDescriptor InterfacePrefix = new(
        id: DiagnosticIds.InterfacePrefix,
        title: "Interface names should begin with I",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Interfaces are named 'IShape', not 'Shape'. Replaces StyleCop SA1302."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.InterfacePrefix + "/");

    public static readonly DiagnosticDescriptor TypeParameterPrefix = new(
        id: DiagnosticIds.TypeParameterPrefix,
        title: "Type parameter names should begin with T",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Type parameters are named 'T' or 'TItem', not 'Item'. Replaces StyleCop SA1314."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.TypeParameterPrefix + "/");

    public static readonly DiagnosticDescriptor FieldPascalCase = new(
        id: DiagnosticIds.FieldPascalCase,
        title: "Constant, static readonly and non-private field names should be PascalCase",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Constants, static readonly fields and public or internal fields are 'MaxCount', not 'maxCount' or "
            + "'_maxCount'. Replaces StyleCop SA1303, SA1311, SA1307 and SA1304."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.FieldPascalCase + "/");

    public static readonly DiagnosticDescriptor FieldPrefix = new(
        id: DiagnosticIds.FieldPrefix,
        title: "Field names should not begin with a prefix",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fields aren't named 'm_count', 's_count' or 't_count'. Replaces StyleCop SA1308."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.FieldPrefix + "/");

    public static readonly DiagnosticDescriptor FieldUnderscore = new(
        id: DiagnosticIds.FieldUnderscore,
        title: "Field names should not contain an underscore",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fields are 'maxValue' or 'MaxValue', not 'max_value' or 'MAX_VALUE'. Replaces StyleCop SA1310."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.FieldUnderscore + "/");

    public static readonly DiagnosticDescriptor ElementPascalCase = new(
        id: DiagnosticIds.ElementPascalCase,
        title: "Element names should begin with an upper-case letter",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types, methods, properties, events, enum members and local functions are PascalCase. "
            + "Replaces StyleCop SA1300 (except for namespaces)."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.ElementPascalCase + "/");

    public static readonly DiagnosticDescriptor NamespacePascalCase = new(
        id: DiagnosticIds.NamespacePascalCase,
        title: "Namespace names should begin with an upper-case letter",
        messageFormat: "Rename namespace '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Every part of a namespace name is PascalCase. Off by default: renaming a namespace renames every type in it. "
            + "Replaces StyleCop SA1300 (namespaces)."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.NamespacePascalCase + "/");

    public static readonly DiagnosticDescriptor ParameterMatchesBase = new(
        id: DiagnosticIds.ParameterMatchesBase,
        title: "Parameter names should match the base member",
        messageFormat: "Rename '{0}' to '{1}', like in the member it overrides or implements",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "A parameter of an override or interface implementation has the name of the base member's parameter "
            + "(proposed for StyleCop in issue #1949; the SDK's CA1725 reports it without a fix). Off by default."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.ParameterMatchesBase + "/");

    public static readonly DiagnosticDescriptor AsyncSuffix = new(
        id: DiagnosticIds.AsyncSuffix,
        title: "Asynchronous method names should end with Async",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "A method that returns Task, Task<T>, ValueTask, ValueTask<T> or IAsyncEnumerable<T> ends in 'Async' "
            + "(Roslynator RCS1046, Meziantou MA0137). Off by default."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.AsyncSuffix + "/");

    public static readonly DiagnosticDescriptor OpenParenthesisOnNameLine = new(
        id: DiagnosticIds.OpenParenthesisOnNameLine,
        title: "Opening parenthesis or bracket should be on the declaration line",
        messageFormat: "Move '{0}' to the end of the previous line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'Method(' starts the list on the line of the name, not on the next line. Replaces StyleCop SA1110.",
        helpLinkUri: HelpBase + DiagnosticIds.OpenParenthesisOnNameLine + "/");

    public static readonly DiagnosticDescriptor CloseParenthesisOnLastItemLine = new(
        id: DiagnosticIds.CloseParenthesisOnLastItemLine,
        title: "Closing parenthesis or bracket should be on the line of the last item",
        messageFormat: "Move '{0}' {1}",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The list ends where its last item ends, not on a line of its own; with "
            + "stylebro_closing_parenthesis_placement = own_line, a split list ends on a line of its own. Replaces StyleCop SA1111.",
        helpLinkUri: HelpBase + DiagnosticIds.CloseParenthesisOnLastItemLine + "/");

    public static readonly DiagnosticDescriptor ConstraintOnOwnLine = new(
        id: DiagnosticIds.ConstraintOnOwnLine,
        title: "Generic type constraints should be on their own line",
        messageFormat: "Move the constraint on '{0}' {1}",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each 'where' clause starts its own line, one level deeper than the declaration. Replaces StyleCop SA1127.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstraintOnOwnLine + "/");

    public static readonly DiagnosticDescriptor NoRegions = new(
        id: DiagnosticIds.NoRegions,
        title: "Do not use regions",
        messageFormat: "Remove the region",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'#region' hides code instead of organizing it. Replaces StyleCop SA1124 (regions between members "
            + "and types; BRO1113 covers regions inside members).",
        helpLinkUri: HelpBase + DiagnosticIds.NoRegions + "/");

    public static readonly DiagnosticDescriptor NoRegionsInCodeElements = new(
        id: DiagnosticIds.NoRegionsInCodeElements,
        title: "Regions should not be placed inside code elements",
        messageFormat: "Remove the region",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A region inside a method or property body hides part of it. Replaces StyleCop SA1123.",
        helpLinkUri: HelpBase + DiagnosticIds.NoRegionsInCodeElements + "/");

    public static readonly DiagnosticDescriptor InheritDocumentation = new(
        id: DiagnosticIds.InheritDocumentation,
        title: "Overrides and implementations should inherit their documentation",
        messageFormat: "Add '/// <inheritdoc/>' to '{0}'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An override or interface implementation without documentation gets '/// <inheritdoc/>', which "
            + "takes the documentation from the member it overrides or implements. Replaces StyleCop SA1600 for these "
            + "members; other missing documentation isn't reported, since only a person can write it.",
        helpLinkUri: HelpBase + DiagnosticIds.InheritDocumentation + "/");

    public static readonly DiagnosticDescriptor DocumentationSlashesInComment = new(
        id: DiagnosticIds.DocumentationSlashesInComment,
        title: "Single-line comments should not use documentation style slashes",
        messageFormat: "Use '//' for a comment that doesn't document a member",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'///' starts documentation; a comment inside code uses '//'. Replaces StyleCop SA1626.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationSlashesInComment + "/");

    public static readonly DiagnosticDescriptor DocumentationEndsWithPeriod = new(
        id: DiagnosticIds.DocumentationEndsWithPeriod,
        title: "Documentation text should end with a period",
        messageFormat: "End the documentation text with a period",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Summary, remarks, parameter, return value and exception text are sentences. Replaces StyleCop SA1629.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationEndsWithPeriod + "/");

    public static readonly DiagnosticDescriptor PropertySummaryWording = new(
        id: DiagnosticIds.PropertySummaryWording,
        title: "Property summary documentation should match accessors",
        messageFormat: "Begin the summary with '{0}'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property's summary begins with 'Gets or sets', 'Gets' or 'Sets' (plus 'a value indicating whether' "
            + "for a bool), matching its accessors. Replaces StyleCop SA1623.",
        helpLinkUri: HelpBase + DiagnosticIds.PropertySummaryWording + "/");

    public static readonly DiagnosticDescriptor PropertySummaryRestrictedSetter = new(
        id: DiagnosticIds.PropertySummaryRestrictedSetter,
        title: "Property summary documentation should omit accessor with restricted access",
        messageFormat: "Begin the summary with '{0}': other code can't use the setter",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property with a private or otherwise restricted setter is documented as 'Gets', not 'Gets or sets'. "
            + "Replaces StyleCop SA1624.",
        helpLinkUri: HelpBase + DiagnosticIds.PropertySummaryRestrictedSetter + "/");

    public static readonly DiagnosticDescriptor ConstructorSummary = new(
        id: DiagnosticIds.ConstructorSummary,
        title: "Constructor summary documentation should begin with standard text",
        messageFormat: "Begin the constructor's summary with 'Initializes a new instance of ...'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A constructor's summary begins with 'Initializes a new instance of the <see cref=\"T\"/> class.' "
            + "(or 'Initializes static members of' for a static constructor). Replaces StyleCop SA1642.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstructorSummary + "/");

    public static readonly DiagnosticDescriptor DestructorSummary = new(
        id: DiagnosticIds.DestructorSummary,
        title: "Destructor summary documentation should begin with standard text",
        messageFormat: "Begin the finalizer's summary with 'Finalizes an instance of ...'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A finalizer's summary begins with 'Finalizes an instance of the <see cref=\"T\"/> class.' "
            + "Replaces StyleCop SA1643.",
        helpLinkUri: HelpBase + DiagnosticIds.DestructorSummary + "/");

    public static readonly DiagnosticDescriptor VoidReturnDocumented = new(
        id: DiagnosticIds.VoidReturnDocumented,
        title: "Void return value should not be documented",
        messageFormat: "Remove the <returns> documentation: the method returns void",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A method or delegate that returns void has no return value to document. Replaces StyleCop SA1617.",
        helpLinkUri: HelpBase + DiagnosticIds.VoidReturnDocumented + "/");

    public static readonly DiagnosticDescriptor PlaceholderElement = new(
        id: DiagnosticIds.PlaceholderElement,
        title: "Do not use placeholder elements",
        messageFormat: "Remove the <placeholder> tags",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'<placeholder>' marks generated documentation that still needs a review; the text stays, the "
            + "tags go. Replaces StyleCop SA1651.",
        helpLinkUri: HelpBase + DiagnosticIds.PlaceholderElement + "/");

    public static readonly DiagnosticDescriptor EmptyRemarks = new(
        id: DiagnosticIds.EmptyRemarks,
        title: "Documentation text should not be empty",
        messageFormat: "Remove the empty <remarks>",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty '<remarks>' element says nothing. Replaces StyleCop SA1627.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyRemarks + "/");

    public static readonly DiagnosticDescriptor ParameterTagsMatch = new(
        id: DiagnosticIds.ParameterTagsMatch,
        title: "Element parameter documentation should match element parameters",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'<param>' tags document the parameters that exist, in their order. Replaces StyleCop SA1612.",
        helpLinkUri: HelpBase + DiagnosticIds.ParameterTagsMatch + "/");

    public static readonly DiagnosticDescriptor ParameterTagHasName = new(
        id: DiagnosticIds.ParameterTagHasName,
        title: "Element parameter documentation should declare parameter name",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every '<param>' tag has a 'name'. Reported when the name is certain: one unnamed tag and one "
            + "undocumented parameter, or every tag unnamed with one per parameter. Replaces StyleCop SA1613.",
        helpLinkUri: HelpBase + DiagnosticIds.ParameterTagHasName + "/");

    public static readonly DiagnosticDescriptor TypeParameterTagsMatch = new(
        id: DiagnosticIds.TypeParameterTagsMatch,
        title: "Generic type parameter documentation should match type parameters",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'<typeparam>' tags document the type parameters that exist, in their order. Replaces StyleCop SA1620.",
        helpLinkUri: HelpBase + DiagnosticIds.TypeParameterTagsMatch + "/");

    public static readonly DiagnosticDescriptor TypeParameterTagHasName = new(
        id: DiagnosticIds.TypeParameterTagHasName,
        title: "Generic type parameter documentation should declare parameter name",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every '<typeparam>' tag has a 'name'. Reported when the name is certain, like BRO1612. Replaces "
            + "StyleCop SA1621.",
        helpLinkUri: HelpBase + DiagnosticIds.TypeParameterTagHasName + "/");

    public static readonly DiagnosticDescriptor FileHeader = new(
        id: DiagnosticIds.FileHeader,
        title: "File should have the XML copyright header",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The file starts with StyleCop's XML header: a '<copyright>' tag with the file name, the company and the "
            + "copyright text from stylebro_file_header_company/_copyright. Does nothing until stylebro_file_header_company is set. "
            + "Replaces StyleCop SA1633 (XML header), SA1634, SA1635, SA1636, SA1637, SA1638, SA1640 and SA1641.",
        helpLinkUri: HelpBase + DiagnosticIds.FileHeader + "/");

    public static readonly DiagnosticDescriptor SummaryLayout = new(
        id: DiagnosticIds.SummaryLayout,
        title: "Write the summary's tags consistently on their own lines or on the text's line",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A '<summary>' has its tags on lines of their own (stylebro_summary_layout = multi_line, the default), or, "
            + "with single_line_when_fits, is written on one line when its text is one line, fits within max_line_length and "
            + "has no '<para>', '<code>' or '<list>'. Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.SummaryLayout + "/");

    public static readonly DiagnosticDescriptor GenericCrefBraces = new(
        id: DiagnosticIds.GenericCrefBraces,
        title: "Write a cref's type arguments in braces",
        messageFormat: "Write the type arguments of '{0}' in braces",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A cref names a generic type or member with braces, 'List{T}', not with escaped angle brackets, "
            + "'List&lt;T&gt;'. Not a StyleCop rule (StyleCop's proposed SA1653, issue #758).",
        helpLinkUri: HelpBase + DiagnosticIds.GenericCrefBraces + "/");

    public static readonly DiagnosticDescriptor LangwordElement = new(
        id: DiagnosticIds.LangwordElement,
        title: "Write a C# keyword in documentation as <see langword=\"...\"/>",
        messageFormat: "Use <see langword=\"{0}\"/>",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A C# keyword alone in '<c>' ('<c>null</c>') is written '<see langword=\"null\"/>', which documentation "
            + "tools link to the keyword's page. Not a StyleCop rule (Meziantou MA0154).",
        helpLinkUri: HelpBase + DiagnosticIds.LangwordElement + "/");

    public static readonly DiagnosticDescriptor DocumentationElementOrder = new(
        id: DiagnosticIds.DocumentationElementOrder,
        title: "Put the documentation elements in the standard order",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The top-level elements of a documentation comment come in the order summary, typeparam, param, returns, "
            + "value, exception, remarks, example, seealso. Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationElementOrder + "/");

    public static readonly DiagnosticDescriptor TrailingComma = new(
        id: DiagnosticIds.TrailingComma,
        title: "Use a trailing comma in multi-line initializers",
        messageFormat: "{0}",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A trailing comma after the last item keeps diffs to one line when items are added "
            + "(stylebro_trailing_comma = omit removes trailing commas instead). Replaces StyleCop SA1413.",
        helpLinkUri: HelpBase + DiagnosticIds.TrailingComma + "/");

    public static readonly DiagnosticDescriptor BlankLineBeforeOpenBrace = new(
        id: DiagnosticIds.BlankLineBeforeOpenBrace,
        title: "Opening braces should not be preceded by a blank line",
        messageFormat: "Remove the blank line before '{{'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An opening brace on its own line directly follows the line it belongs to. Replaces StyleCop SA1509.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeOpenBrace + "/");

    public static readonly DiagnosticDescriptor BlankLineBeforeChainedBlock = new(
        id: DiagnosticIds.BlankLineBeforeChainedBlock,
        title: "Chained blocks should not be preceded by a blank line",
        messageFormat: "Remove the blank line before '{0}'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'else', 'catch' and 'finally' directly follow the block they continue. Replaces StyleCop SA1510.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeChainedBlock + "/");

    public static readonly DiagnosticDescriptor BlankLineAfterOpenBrace = new(
        id: DiagnosticIds.BlankLineAfterOpenBrace,
        title: "Opening braces should not be followed by a blank line",
        messageFormat: "Remove the blank line after '{{'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The first line inside a block directly follows its opening brace. Replaces StyleCop SA1505.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterOpenBrace + "/");

    public static readonly DiagnosticDescriptor BlankLineBeforeComment = new(
        id: DiagnosticIds.BlankLineBeforeComment,
        title: "Single-line comments should be preceded by a blank line",
        messageFormat: "Add a blank line before the comment",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comment that introduces the code below it is separated from the code above. Replaces StyleCop SA1515.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeComment + "/");

    public static readonly DiagnosticDescriptor ElementsSeparatedByBlankLine = new(
        id: DiagnosticIds.ElementsSeparatedByBlankLine,
        title: "Elements should be separated by a blank line",
        messageFormat: "Add a blank line before this element",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Members, types and namespaces are separated by a blank line; consecutive fields may stay together. "
            + "Replaces StyleCop SA1516.",
        helpLinkUri: HelpBase + DiagnosticIds.ElementsSeparatedByBlankLine + "/");

    public static readonly DiagnosticDescriptor BlankLineAfterComment = new(
        id: DiagnosticIds.BlankLineAfterComment,
        title: "Single-line comments should not be followed by a blank line",
        messageFormat: "Remove the blank line after the comment",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comment sits directly on the code it describes. Replaces StyleCop SA1512.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterComment + "/");

    public static readonly DiagnosticDescriptor BlankLinesAtEndOfFile = new(
        id: DiagnosticIds.BlankLinesAtEndOfFile,
        title: "Files should not end with blank lines",
        messageFormat: "Remove the blank lines at the end of the file",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A file ends with its last line of code, followed by at most one line break. Replaces StyleCop SA1518.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLinesAtEndOfFile + "/");

    public static readonly DiagnosticDescriptor SingleLineStatementBlock = new(
        id: DiagnosticIds.SingleLineStatementBlock,
        title: "A block should not be on a single line",
        messageFormat: "Put the block on separate lines",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'if (x) { return; }' becomes a block with its braces and each statement on their own lines. Lambda "
            + "bodies are left alone. Replaces StyleCop SA1501.",
        helpLinkUri: HelpBase + DiagnosticIds.SingleLineStatementBlock + "/");

    public static readonly DiagnosticDescriptor SingleLineElement = new(
        id: DiagnosticIds.SingleLineElement,
        title: "An element should not be on a single line",
        messageFormat: "Put '{0}' on separate lines",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types, namespaces, method bodies and accessor lists with bodies aren't written on one line. Auto-properties "
            + "('{ get; set; }') and single-line accessors inside a multi-line property are fine. Replaces StyleCop SA1502.",
        helpLinkUri: HelpBase + DiagnosticIds.SingleLineElement + "/");

    public static readonly DiagnosticDescriptor AccessorsConsistentLayout = new(
        id: DiagnosticIds.AccessorsConsistentLayout,
        title: "Accessors should all be single-line or all multi-line",
        messageFormat: "Lay out all accessors the same way: all on one line each, or all on several lines",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When every accessor has a block body, they are either all single-line or all multi-line. The fix puts "
            + "one-statement bodies on one line, or expands the single-line ones. Replaces StyleCop SA1504.",
        helpLinkUri: HelpBase + DiagnosticIds.AccessorsConsistentLayout + "/");

    public static readonly DiagnosticDescriptor PropertyAccessorOrder = new(
        id: DiagnosticIds.PropertyAccessorOrder,
        title: "Property accessors should follow order",
        messageFormat: "Put the 'get' accessor before the '{0}' accessor",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'get' comes before 'set' or 'init'. The fix swaps the accessors with their comments. Replaces StyleCop SA1212.",
        helpLinkUri: HelpBase + DiagnosticIds.PropertyAccessorOrder + "/");

    public static readonly DiagnosticDescriptor EventAccessorOrder = new(
        id: DiagnosticIds.EventAccessorOrder,
        title: "Event accessors should follow order",
        messageFormat: "Put the 'add' accessor before the 'remove' accessor",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'add' comes before 'remove'. The fix swaps the accessors with their comments. Replaces StyleCop SA1213.",
        helpLinkUri: HelpBase + DiagnosticIds.EventAccessorOrder + "/");

    public static readonly DiagnosticDescriptor CombinedFields = new(
        id: DiagnosticIds.CombinedFields,
        title: "Do not combine fields",
        messageFormat: "Declare '{0}' in a declaration of its own",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "One field (or event field) per declaration. The fix gives every field its own declaration with the same "
            + "attributes, modifiers, type and documentation. Replaces StyleCop SA1132.",
        helpLinkUri: HelpBase + DiagnosticIds.CombinedFields + "/");

    public static readonly DiagnosticDescriptor NullableShorthand = new(
        id: DiagnosticIds.NullableShorthand,
        title: "Use shorthand for nullable types",
        messageFormat: "Use '{0}?' instead of 'Nullable<{0}>'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'int?' instead of 'Nullable<int>' or 'System.Nullable<int>'. Replaces StyleCop SA1125.",
        helpLinkUri: HelpBase + DiagnosticIds.NullableShorthand + "/");

    public static readonly DiagnosticDescriptor LiteralSuffix = new(
        id: DiagnosticIds.LiteralSuffix,
        title: "Use literal suffix notation instead of casting",
        messageFormat: "Use '{0}' instead of the cast",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'1L' instead of '(long)1', when the literal has exactly the cast's value. Replaces StyleCop SA1139.",
        helpLinkUri: HelpBase + DiagnosticIds.LiteralSuffix + "/");

    public static readonly DiagnosticDescriptor TupleSyntax = new(
        id: DiagnosticIds.TupleSyntax,
        title: "Use tuple syntax",
        messageFormat: "Use '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'(int, string)' instead of 'ValueTuple<int, string>', and '(1, \"a\")' instead of 'new ValueTuple<int, string>(1, \"a\")' "
            + "or 'ValueTuple.Create(1, \"a\")'. Replaces StyleCop SA1141.",
        helpLinkUri: HelpBase + DiagnosticIds.TupleSyntax + "/");

    public static readonly DiagnosticDescriptor TupleElementName = new(
        id: DiagnosticIds.TupleElementName,
        title: "Refer to tuple elements by name",
        messageFormat: "Use '{0}' instead of '{1}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'t.Name' instead of 't.Item1' when the tuple element has a name. Replaces StyleCop SA1142.",
        helpLinkUri: HelpBase + DiagnosticIds.TupleElementName + "/");

    public static readonly DiagnosticDescriptor LambdaSyntax = new(
        id: DiagnosticIds.LambdaSyntax,
        title: "Use lambda syntax",
        messageFormat: "Use a lambda instead of an anonymous method",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'(s, e) => { }' instead of 'delegate (object s, EventArgs e) { }', when the lambda binds the same way. "
            + "Replaces StyleCop SA1130.",
        helpLinkUri: HelpBase + DiagnosticIds.LambdaSyntax + "/");

    public static readonly DiagnosticDescriptor EmptyDelegateParentheses = new(
        id: DiagnosticIds.EmptyDelegateParentheses,
        title: "Remove delegate parenthesis when possible",
        messageFormat: "Remove the empty parentheses after 'delegate'",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'delegate { }' instead of 'delegate() { }', unless an overloaded call needs them. Where BRO1125 is on, it turns "
            + "the method into a lambda instead. Replaces StyleCop SA1410.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyDelegateParentheses + "/");

    public static readonly DiagnosticDescriptor QualifiedUsing = new(
        id: DiagnosticIds.QualifiedUsing,
        title: "Using directives should be qualified",
        messageFormat: "Use the fully qualified name '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A using directive inside a namespace names its namespace or type fully, so it doesn't depend on the "
            + "namespace it's in. Replaces StyleCop SA1135.",
        helpLinkUri: HelpBase + DiagnosticIds.QualifiedUsing + "/");

    public static readonly DiagnosticDescriptor QueryClauseBlankLine = new(
        id: DiagnosticIds.QueryClauseBlankLine,
        title: "Query clause should follow previous clause",
        messageFormat: "Remove the blank lines before the query clause",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between the clauses of a query expression. Replaces StyleCop SA1102.",
        helpLinkUri: HelpBase + DiagnosticIds.QueryClauseBlankLine + "/");

    public static readonly DiagnosticDescriptor QueryClausesOnSeparateLines = new(
        id: DiagnosticIds.QueryClausesOnSeparateLines,
        title: "Query clauses should be on separate lines or all on one line",
        messageFormat: "Put each query clause on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A query's clauses are all on one line or each on its own line. Replaces StyleCop SA1103.",
        helpLinkUri: HelpBase + DiagnosticIds.QueryClausesOnSeparateLines + "/");

    public static readonly DiagnosticDescriptor QueryClauseAfterMultiLineClause = new(
        id: DiagnosticIds.QueryClauseAfterMultiLineClause,
        title: "Query clause should begin on new line when previous clause spans multiple lines",
        messageFormat: "Start the query clause on a new line: the clause before it spans several lines",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A clause after a clause that spans several lines starts on its own line. Replaces StyleCop SA1104.",
        helpLinkUri: HelpBase + DiagnosticIds.QueryClauseAfterMultiLineClause + "/");

    public static readonly DiagnosticDescriptor MultiLineQueryClause = new(
        id: DiagnosticIds.MultiLineQueryClause,
        title: "Query clauses spanning multiple lines should begin on own line",
        messageFormat: "Start the query clause on its own line: it spans several lines",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A clause that spans several lines starts on its own line. Replaces StyleCop SA1105.",
        helpLinkUri: HelpBase + DiagnosticIds.MultiLineQueryClause + "/");

    public static readonly DiagnosticDescriptor AccessModifier = new(
        id: DiagnosticIds.AccessModifier,
        title: "Access modifier should be declared",
        messageFormat: "Declare the access modifier of '{0}' ({1})",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types and members say who can use them instead of relying on the default. Replaces StyleCop SA1400.",
        helpLinkUri: HelpBase + DiagnosticIds.AccessModifier + "/");

    public static readonly DiagnosticDescriptor PartialAccessModifier = new(
        id: DiagnosticIds.PartialAccessModifier,
        title: "Partial elements should declare an access modifier",
        messageFormat: "Declare the access modifier of '{0}' ({1}) on this part too",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every part of a partial type says its accessibility, so no part has to be found to know it. Replaces StyleCop SA1205.",
        helpLinkUri: HelpBase + DiagnosticIds.PartialAccessModifier + "/");

    public static readonly DiagnosticDescriptor UsingPlacement = new(
        id: DiagnosticIds.UsingPlacement,
        title: "Using directives should be placed correctly",
        messageFormat: "Move the using directive {0} the namespace",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using directives go inside or outside the namespace, as csharp_using_directive_placement says (nothing is "
            + "reported when it isn't set). The fix moves them only when every name in the file still means the same afterwards. "
            + "Replaces StyleCop SA1200.",
        helpLinkUri: HelpBase + DiagnosticIds.UsingPlacement + "/");

    public static readonly DiagnosticDescriptor UnnecessaryParentheses = new(
        id: DiagnosticIds.UnnecessaryParentheses,
        title: "Statement should not use unnecessary parenthesis",
        messageFormat: "Remove the unnecessary parentheses",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parentheses around a value, or around a whole expression in a statement, argument or initializer, add nothing. Replaces StyleCop SA1119.",
        helpLinkUri: HelpBase + DiagnosticIds.UnnecessaryParentheses + "/");

    // The '(' and ')' of a BRO1405 finding, so an IDE fades them (like StyleCop's SA1119_p): hidden, not configurable,
    // no fix of its own. Not in DiagnosticIds: no baseline, migration or documentation entry of its own.
    public static readonly DiagnosticDescriptor UnnecessaryParenthesesFade = new(
        id: DiagnosticIds.UnnecessaryParentheses + "_p",
        title: "Statement should not use unnecessary parenthesis",
        messageFormat: "Remove the unnecessary parentheses",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: FadeDescription,
        helpLinkUri: HelpBase + DiagnosticIds.UnnecessaryParentheses + "/",
        customTags: new[] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable });

    public static readonly DiagnosticDescriptor UnnecessaryPatternParentheses = new(
        id: DiagnosticIds.UnnecessaryPatternParentheses,
        title: "Patterns should not use unnecessary parentheses",
        messageFormat: "Remove the unnecessary parentheses",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parentheses around a pattern that parses the same without them ('x is (> 0)', 'o is (string s)', "
            + "'x is not (null)') add nothing. Ones that BRO1407 wants between 'and' and 'or' stay.",
        helpLinkUri: HelpBase + DiagnosticIds.UnnecessaryPatternParentheses + "/");

    // The same for BRO1410.
    public static readonly DiagnosticDescriptor UnnecessaryPatternParenthesesFade = new(
        id: DiagnosticIds.UnnecessaryPatternParentheses + "_p",
        title: "Patterns should not use unnecessary parentheses",
        messageFormat: "Remove the unnecessary parentheses",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: FadeDescription,
        helpLinkUri: HelpBase + DiagnosticIds.UnnecessaryPatternParentheses + "/",
        customTags: new[] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable });

    public static readonly DiagnosticDescriptor ArithmeticPrecedence = new(
        id: DiagnosticIds.ArithmeticPrecedence,
        title: "Arithmetic expressions should declare precedence",
        messageFormat: "Add parentheses to show the precedence",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An operation inside an arithmetic operation of another kind ('a + b * c', 'a * b % c', 'a << b + c') is in parentheses. Replaces StyleCop SA1407.",
        helpLinkUri: HelpBase + DiagnosticIds.ArithmeticPrecedence + "/");

    public static readonly DiagnosticDescriptor ConditionalPrecedence = new(
        id: DiagnosticIds.ConditionalPrecedence,
        title: "Conditional expressions should declare precedence",
        messageFormat: "Add parentheses to show the precedence",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'&&' and '||' (and the 'and'/'or' patterns) aren't mixed without parentheses. Replaces StyleCop SA1408.",
        helpLinkUri: HelpBase + DiagnosticIds.ConditionalPrecedence + "/");

    public static readonly DiagnosticDescriptor HungarianNotation = new(
        id: DiagnosticIds.HungarianNotation,
        title: "Field names should not use Hungarian notation",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Variable, parameter and field names don't start with a type prefix ('iCount' -> 'count'). Off by default, like StyleCop's. Replaces StyleCop SA1305."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.HungarianNotation + "/");

    public static readonly DiagnosticDescriptor TupleElementCasing = new(
        id: DiagnosticIds.TupleElementCasing,
        title: "Tuple element names should use correct casing",
        messageFormat: "Rename tuple element '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Element names in tuple types are PascalCase ('(int Count, string Name)'), or camelCase with stylebro_tuple_element_name_casing. Replaces StyleCop SA1316."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.TupleElementCasing + "/");

    public static readonly DiagnosticDescriptor BaseCall = new(
        id: DiagnosticIds.BaseCall,
        title: "Do not prefix calls with base unless local implementation exists",
        messageFormat: "'base.' isn't needed: the type has no member of its own with this name",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'base.' suggests the type overrides or hides the member. Replaces StyleCop SA1100.",
        helpLinkUri: HelpBase + DiagnosticIds.BaseCall + "/");

    public static readonly DiagnosticDescriptor EmbeddedComment = new(
        id: DiagnosticIds.EmbeddedComment,
        title: "Block statements should not contain embedded comments",
        messageFormat: "Move the comment into the block",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comment between a statement's header and its '{' belongs inside the block. Replaces StyleCop SA1108.",
        helpLinkUri: HelpBase + DiagnosticIds.EmbeddedComment + "/");

    public static readonly DiagnosticDescriptor DeclarationComment = new(
        id: DiagnosticIds.DeclarationComment,
        title: "Declarations should not contain embedded comments",
        messageFormat: "Move the comment into the declaration's body",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comment between the header of a type, namespace, member, accessor or local function and its '{' belongs "
            + "inside the body. Not a StyleCop rule (proposed in StyleCop issue #605; BRO1132 does the same for statements).",
        helpLinkUri: HelpBase + DiagnosticIds.DeclarationComment + "/");

    public static readonly DiagnosticDescriptor LiteralSuffixCase = new(
        id: DiagnosticIds.LiteralSuffixCase,
        title: "Integer literal suffixes should be upper case",
        messageFormat: "Use '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'1L' instead of '1l', which looks like '11'; 'U' and 'UL' likewise. Real suffixes ('f', 'd', 'm') aren't "
            + "checked; stylebro_upper_case_literal_suffixes = l_only checks only 'l'. Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.LiteralSuffixCase + "/");

    public static readonly DiagnosticDescriptor NullCheckStyle = new(
        id: DiagnosticIds.NullCheckStyle,
        title: "Check for null in one form",
        messageFormat: "Use '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Null checks use 'is null' and 'is not null' (stylebro_null_check_style = pattern_matching, the default) or "
            + "'== null' and '!= null' (equality_operator). Checks whose meaning would change, such as a user-defined '==', "
            + "aren't reported. Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.NullCheckStyle + "/");

    public static readonly DiagnosticDescriptor DirectiveSpacing = new(
        id: DiagnosticIds.DirectiveSpacing,
        title: "Preprocessor keywords should not be preceded by a space",
        messageFormat: "Remove the space between '#' and '{0}'",
        category: "Spacing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'#if', not '# if'. Replaces StyleCop SA1006.",
        helpLinkUri: HelpBase + DiagnosticIds.DirectiveSpacing + "/");

    public static readonly DiagnosticDescriptor EmptyAttributeParentheses = new(
        id: DiagnosticIds.EmptyAttributeParentheses,
        title: "Attribute constructor should not use unnecessary parenthesis",
        messageFormat: "Remove the empty parentheses",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'[Obsolete]' instead of '[Obsolete()]'. Replaces StyleCop SA1411.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyAttributeParentheses + "/");

    public static readonly DiagnosticDescriptor DocumentationLineSpace = new(
        id: DiagnosticIds.DocumentationLineSpace,
        title: "Documentation lines should begin with single space",
        messageFormat: "Put one space after '///'",
        category: "Spacing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'/// text' instead of '///text' or '///   text'. Lines inside <code> are left alone. Replaces StyleCop SA1004.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationLineSpace + "/");

    public static readonly DiagnosticDescriptor EmptyListOnOneLine = new(
        id: DiagnosticIds.EmptyListOnOneLine,
        title: "Closing parenthesis should be on line of opening parenthesis",
        messageFormat: "Put ')' right after '('",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty parameter or argument list is written '()', not split over lines. Replaces StyleCop SA1112.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyListOnOneLine + "/");

    public static readonly DiagnosticDescriptor CommaOnItemLine = new(
        id: DiagnosticIds.CommaOnItemLine,
        title: "Comma should be on the same line as previous parameter",
        messageFormat: "Put the comma at the end of the line before",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comma in a parameter or argument list ends the line of the item before it instead of starting the next. Replaces StyleCop SA1113.",
        helpLinkUri: HelpBase + DiagnosticIds.CommaOnItemLine + "/");

    public static readonly DiagnosticDescriptor FirstItemFollowsOpening = new(
        id: DiagnosticIds.FirstItemFollowsOpening,
        title: "Parameter list should follow declaration",
        messageFormat: "Remove the blank lines before the first item",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between the opening parenthesis and the first parameter or argument. Replaces StyleCop SA1114.",
        helpLinkUri: HelpBase + DiagnosticIds.FirstItemFollowsOpening + "/");

    public static readonly DiagnosticDescriptor ItemFollowsComma = new(
        id: DiagnosticIds.ItemFollowsComma,
        title: "Parameter should follow comma",
        messageFormat: "Remove the blank lines before this item",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between a comma and the next parameter or argument. Replaces StyleCop SA1115.",
        helpLinkUri: HelpBase + DiagnosticIds.ItemFollowsComma + "/");

    public static readonly DiagnosticDescriptor EmptyComment = new(
        id: DiagnosticIds.EmptyComment,
        title: "Comments should contain text",
        messageFormat: "Remove the empty comment",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty '//' or '/* */' at the start or end of a comment (or on its own) is removed. Empty lines between comment paragraphs are fine. Replaces StyleCop SA1120.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyComment + "/");

    public static readonly DiagnosticDescriptor EnumValueOnOwnLine = new(
        id: DiagnosticIds.EnumValueOnOwnLine,
        title: "Enum values should be on separate lines",
        messageFormat: "Put '{0}' on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each enum value starts its own line. Replaces StyleCop SA1136.",
        helpLinkUri: HelpBase + DiagnosticIds.EnumValueOnOwnLine + "/");

    public static readonly DiagnosticDescriptor BlankLineAfterDocumentation = new(
        id: DiagnosticIds.BlankLineAfterDocumentation,
        title: "Element documentation headers should not be followed by blank line",
        messageFormat: "Remove the blank lines between the documentation and the element",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A documentation comment sits directly on the element it documents. Replaces StyleCop SA1506.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterDocumentation + "/");

    public static readonly DiagnosticDescriptor BlankLineBeforeWhile = new(
        id: DiagnosticIds.BlankLineBeforeWhile,
        title: "While-do footer should not be preceded by blank line",
        messageFormat: "Remove the blank lines before 'while'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The 'while' of a 'do ... while' follows the block directly. Replaces StyleCop SA1511.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeWhile + "/");

    public static readonly DiagnosticDescriptor BracesOmitted = new(
        id: DiagnosticIds.BracesOmitted,
        title: "Braces should not be omitted",
        messageFormat: "Put the statement in braces",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The child statement of an if, else, loop, using, lock or fixed statement is a block. Replaces StyleCop SA1503.",
        helpLinkUri: HelpBase + DiagnosticIds.BracesOmitted + "/");

    public static readonly DiagnosticDescriptor BracesMultiLine = new(
        id: DiagnosticIds.BracesMultiLine,
        title: "Braces should not be omitted from multi-line child statement",
        messageFormat: "Put the multi-line statement in braces",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A child statement that spans several lines is a block. Replaces StyleCop SA1519.",
        helpLinkUri: HelpBase + DiagnosticIds.BracesMultiLine + "/");

    public static readonly DiagnosticDescriptor BracesConsistent = new(
        id: DiagnosticIds.BracesConsistent,
        title: "Use braces consistently",
        messageFormat: "Put the statement in braces like the other clauses of the if statement",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When one clause of an if/else chain has braces, all of them do. Replaces StyleCop SA1520.",
        helpLinkUri: HelpBase + DiagnosticIds.BracesConsistent + "/");

    public static readonly DiagnosticDescriptor MultipleBlankLines = new(
        id: DiagnosticIds.MultipleBlankLines,
        title: "Code should not contain multiple blank lines in a row",
        messageFormat: "Keep one blank line",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Two or more blank lines in a row become one. Replaces StyleCop SA1507.",
        helpLinkUri: HelpBase + DiagnosticIds.MultipleBlankLines + "/");

    public static readonly DiagnosticDescriptor BlankLineBeforeCloseBrace = new(
        id: DiagnosticIds.BlankLineBeforeCloseBrace,
        title: "Closing braces should not be preceded by blank line",
        messageFormat: "Remove the blank lines before the closing brace",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A closing brace directly follows the code above it. Replaces StyleCop SA1508.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeCloseBrace + "/");

    public static readonly DiagnosticDescriptor BlankLineAfterCloseBrace = new(
        id: DiagnosticIds.BlankLineAfterCloseBrace,
        title: "Closing brace should be followed by blank line",
        messageFormat: "Add a blank line after the closing brace",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A statement after a block is separated from it by a blank line. Replaces StyleCop SA1513.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterCloseBrace + "/");

    public static readonly DiagnosticDescriptor BlankLineBeforeDocumentation = new(
        id: DiagnosticIds.BlankLineBeforeDocumentation,
        title: "Element documentation header should be preceded by blank line",
        messageFormat: "Add a blank line before the documentation",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A documentation comment is separated from the code above it by a blank line, except after an opening brace. Replaces StyleCop SA1514.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeDocumentation + "/");

    public static readonly DiagnosticDescriptor OperatorPlacement = new(
        id: DiagnosticIds.OperatorPlacement,
        title: "Place the operator consistently when an expression wraps",
        messageFormat: "Put '{0}' at the {1} of the line",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When the line breaks next to a binary operator or the conditional operator's '?' or ':', the operator "
            + "goes at the beginning of the next line (dotnet_style_operator_placement_when_wrapping = beginning_of_line, the "
            + "default) or at the end of the broken line (end_of_line). Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.OperatorPlacement + "/");

    public static readonly DiagnosticDescriptor ArrowPlacement = new(
        id: DiagnosticIds.ArrowPlacement,
        title: "Place '=>' consistently when an expression body wraps",
        messageFormat: "Put '{0}' at the {1} of the line",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When the line breaks next to the '=>' of an expression body or a switch expression arm, the '=>' goes "
            + "at the end of the broken line (stylebro_arrow_placement_when_wrapping = end_of_line, the default) or at the "
            + "beginning of the next line (beginning_of_line). Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.ArrowPlacement + "/");

    public static readonly DiagnosticDescriptor EqualsPlacement = new(
        id: DiagnosticIds.EqualsPlacement,
        title: "Place '=' consistently when an assignment wraps",
        messageFormat: "Put '{0}' at the {1} of the line",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When the line breaks next to the '=' of an assignment or an initializer, the '=' goes at the end of "
            + "the broken line (stylebro_equals_placement_when_wrapping = end_of_line, the default) or at the beginning of "
            + "the next line (beginning_of_line). Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.EqualsPlacement + "/");

    public static readonly DiagnosticDescriptor CallChainLayout = new(
        id: DiagnosticIds.CallChainLayout,
        title: "Each call of a split call chain starts its own line",
        messageFormat: "Start '{0}' on its own line, like the chain's other calls",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a member access chain is split over several lines (a '.' or '?.' of the chain starts a line), "
            + "every call after the chain's first line starts its own line. The first line may hold any number of calls, "
            + "and a member that only leads to the next call ('.WriteTo.Sink(x)') stays with it. Not a StyleCop rule.",
        helpLinkUri: HelpBase + DiagnosticIds.CallChainLayout + "/");

    public static readonly DiagnosticDescriptor BlankLineAfterAttributes = new(
        id: DiagnosticIds.BlankLineAfterAttributes,
        title: "Attributes should not be followed by a blank line",
        messageFormat: "Remove the blank line after the attribute",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between an attribute list and the element it applies to, or the element's next attribute "
            + "list. Not a StyleCop rule: proposed in StyleCop issue #738 and never implemented there.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterAttributes + "/");

    public static readonly DiagnosticDescriptor ConditionalLayout = new(
        id: DiagnosticIds.ConditionalLayout,
        title: "A split conditional expression has the condition, '?' and ':' parts on their own lines",
        messageFormat: "Break the line {0} '{1}': the conditional expression is split, so each part starts its own line",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A conditional expression is on one line, or the condition, the '? a' part and the ': b' part each start "
            + "their own line, with '?' and ':' on the side dotnet_style_operator_placement_when_wrapping asks for (the "
            + "beginning of the line by default). Not a StyleCop rule (proposed in StyleCop's issue #651).",
        helpLinkUri: HelpBase + DiagnosticIds.ConditionalLayout + "/");

    public static readonly DiagnosticDescriptor ElseAfterJump = new(
        id: DiagnosticIds.ElseAfterJump,
        title: "No 'else' after a branch that ends in a jump",
        messageFormat: "Remove 'else': the 'if' branch ends in a jump, so the 'else' body can follow the 'if' statement",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "When the 'if' branch ends in return, throw, break, continue, goto or yield break, the 'else' is removed "
            + "and its body follows the 'if' statement, one level less indented. Off by default (a matter of taste). Not a "
            + "StyleCop rule (Meziantou MA0071, Roslynator RCS1211).",
        helpLinkUri: HelpBase + DiagnosticIds.ElseAfterJump + "/");

    public static readonly DiagnosticDescriptor BlankLineBetweenSwitchSections = new(
        id: DiagnosticIds.BlankLineBetweenSwitchSections,
        title: "Blank line between switch sections",
        messageFormat: "{0} between the switch sections",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Switch sections are separated by a blank line (stylebro_blank_line_between_switch_sections = include, "
            + "the default), or not (omit), or not after a section that ends in a block (omit_after_block). Not a StyleCop "
            + "rule (Roslynator RCS0061).",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBetweenSwitchSections + "/");

    public static readonly DiagnosticDescriptor LambdaParentheses = new(
        id: DiagnosticIds.LambdaParentheses,
        title: "A lambda's single parameter should not be in parentheses",
        messageFormat: "Remove the parentheses around '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'x => x' instead of '(x) => x' when the lambda has one parameter without a type, modifier, attribute or "
            + "default value. Not a StyleCop rule (StyleCop issue #762).",
        helpLinkUri: HelpBase + DiagnosticIds.LambdaParentheses + "/");

    public static readonly DiagnosticDescriptor RedundantJump = new(
        id: DiagnosticIds.RedundantJump,
        title: "Remove a redundant 'return;' or 'yield break;'",
        messageFormat: "Remove the redundant '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A 'return;' as the last statement of a method's, accessor's, constructor's, local function's or "
            + "lambda's body, and a 'yield break;' as the last statement of an iterator's body, do nothing. Not a StyleCop "
            + "rule (StyleCop issue #760; Roslynator RCS1134).",
        helpLinkUri: HelpBase + DiagnosticIds.RedundantJump + "/");

    public static readonly DiagnosticDescriptor UnneededStringPrefix = new(
        id: DiagnosticIds.UnneededStringPrefix,
        title: "A string literal should be a plain string when nothing needs more",
        messageFormat: "Write this string as {0}",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'$' without interpolations, '@' on text without a backslash, quote or line break, and a single-line raw "
            + "string without a backslash or quote: the plain string says the same. Not a StyleCop rule (Roslynator "
            + "RCS1214, RCS1192, RCS1262).",
        helpLinkUri: HelpBase + DiagnosticIds.UnneededStringPrefix + "/");

    public static readonly DiagnosticDescriptor ElseIf = new(
        id: DiagnosticIds.ElseIf,
        title: "Write 'else if' on one line",
        messageFormat: "Write 'else if'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An 'if' that follows 'else' on the next line, or is the only statement of the else's block, becomes "
            + "'else if'. Not a StyleCop rule (Roslynator RCS0041, RCS1006).",
        helpLinkUri: HelpBase + DiagnosticIds.ElseIf + "/");

    public static readonly DiagnosticDescriptor EmptyRecordBody = new(
        id: DiagnosticIds.EmptyRecordBody,
        title: "A record with an empty body should end with ';'",
        messageFormat: "Replace the empty body of '{0}' with ';'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'record R(int X);' instead of 'record R(int X) { }'. Not a StyleCop rule (Roslynator RCS1251, "
            + "Meziantou MA0206).",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyRecordBody + "/");

    public static readonly DiagnosticDescriptor ContextualKeyword = new(
        id: DiagnosticIds.ContextualKeyword,
        title: "Escape identifiers that C# 14 reads as keywords",
        messageFormat: "C# 14 reads '{0}' here as a keyword: write '@{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'@field' for a member or local named 'field' used in a property accessor, '@extension' for a type named "
            + "'extension' (and members of that type), '@partial' for a method returning a type named 'partial': C# 14 reads "
            + "these as keywords, which changes what the code means or breaks the build. '@' means the same in every C# "
            + "version. Not a StyleCop rule (Sonar S8367, S8368, S8380 report it without a fix).",
        helpLinkUri: HelpBase + DiagnosticIds.ContextualKeyword + "/");

    public static readonly DiagnosticDescriptor ObjectCreationParentheses = new(
        id: DiagnosticIds.ObjectCreationParentheses,
        title: "Object creation with an initializer: parentheses in one style",
        messageFormat: "{0} the empty parentheses",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'new List<int> { 1 }' (stylebro_object_creation_parentheses = omit, the default) or "
            + "'new List<int>() { 1 }' (include) when the creation has an initializer and no arguments. Not a StyleCop rule "
            + "(Roslynator RCS1050).",
        helpLinkUri: HelpBase + DiagnosticIds.ObjectCreationParentheses + "/");

    public static readonly DiagnosticDescriptor CombinedLocals = new(
        id: DiagnosticIds.CombinedLocals,
        title: "Do not combine local variables",
        messageFormat: "Declare '{0}' in a declaration of its own",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "One local per declaration: 'int a = 1, b;' becomes two declarations, like BRO1114 for fields. Not "
            + "'using' declarations or 'for' initializers. Not a StyleCop rule (Roslynator RCS1081, Sonar S1659).",
        helpLinkUri: HelpBase + DiagnosticIds.CombinedLocals + "/");

    public static readonly DiagnosticDescriptor RedundantBaseType = new(
        id: DiagnosticIds.RedundantBaseType,
        title: "Remove a redundant base type",
        messageFormat: "Remove the redundant base type '{0}'",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'enum E : int' and 'class C : object' say what the declaration means without them. Not a StyleCop rule "
            + "(Roslynator RCS1042, Sonar S1939).",
        helpLinkUri: HelpBase + DiagnosticIds.RedundantBaseType + "/");

    public static readonly DiagnosticDescriptor InternalTypePublicMethod = new(
        id: DiagnosticIds.InternalTypePublicMethod,
        title: "Methods of internal types should be internal, not public",
        messageFormat: "'{0}' can't be seen outside the assembly: declare it internal",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "An ordinary method declared public in a type that is internal (or private, or nested in one) is internal "
            + "in effect; 'internal' says so. Off by default. Only methods: properties, constructors and anything an interface, "
            + "a base type, an attribute or a convention needs stay public, because reflection and serializers see public "
            + "members only (StyleCop issue #2981, never implemented)."
            + KeptNote,
        helpLinkUri: HelpBase + DiagnosticIds.InternalTypePublicMethod + "/");

    public static readonly DiagnosticDescriptor RedundantNullForgiving = new(
        id: DiagnosticIds.RedundantNullForgiving,
        title: "Remove a redundant null-forgiving operator",
        messageFormat: "Remove the '!': the compiler already knows the value isn't null here",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "A '!' whose operand's flow state is already not-null hides nothing. Off by default. Not reported: "
            + "'null!' and 'default!', type parameters and types with type arguments, '#nullable' changing the project's "
            + "setting, and projects with several target frameworks. Not a StyleCop rule (Sonar S8969, the SDK's IDE0370).",
        helpLinkUri: HelpBase + DiagnosticIds.RedundantNullForgiving + "/");

    public static readonly DiagnosticDescriptor HasValueNullCheck = new(
        id: DiagnosticIds.HasValueNullCheck,
        title: "Check a nullable value type for null instead of calling HasValue",
        messageFormat: "Use '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "'x.HasValue' becomes 'x is not null' and '!x.HasValue' 'x is null' (or '!= null' / '== null', following "
            + "stylebro_null_check_style), so every null check has one form. Off by default. Not a StyleCop rule (Meziantou "
            + "MA0171).",
        helpLinkUri: HelpBase + DiagnosticIds.HasValueNullCheck + "/");

    public static readonly DiagnosticDescriptor NestedIf = new(
        id: DiagnosticIds.NestedIf,
        title: "Merge an 'if' into the enclosing 'if'",
        messageFormat: "Merge this 'if' into the enclosing one",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An 'if' that is the only statement of an enclosing 'if', neither with an 'else', joins it: "
            + "'if (a && b)'. Not a StyleCop rule (Sonar S1066).",
        helpLinkUri: HelpBase + DiagnosticIds.NestedIf + "/");

    public static readonly DiagnosticDescriptor WhereBeforeTerminal = new(
        id: DiagnosticIds.WhereBeforeTerminal,
        title: "Pass the predicate to the LINQ call instead of calling Where first",
        messageFormat: "Use '{0}' with the predicate instead of 'Where(...).{0}()'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'source.Where(p).Count()' becomes 'source.Count(p)'; also Any, LongCount, First, FirstOrDefault, Last, "
            + "LastOrDefault, Single and SingleOrDefault, for System.Linq's Enumerable and Queryable. Not a StyleCop rule "
            + "(Sonar S2971).",
        helpLinkUri: HelpBase + DiagnosticIds.WhereBeforeTerminal + "/");

    public static readonly DiagnosticDescriptor ParamsArray = new(
        id: DiagnosticIds.ParamsArray,
        title: "Pass the elements, not an array, to a params parameter",
        messageFormat: "Pass the elements instead of creating an array for the params parameter",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'M(x, new[] { a, b })' becomes 'M(x, a, b)' where the call still binds to the same method. Not a "
            + "StyleCop rule (Sonar S3878).",
        helpLinkUri: HelpBase + DiagnosticIds.ParamsArray + "/");

    public static readonly DiagnosticDescriptor EmptyTypeBody = new(
        id: DiagnosticIds.EmptyTypeBody,
        title: "A class, struct or interface with an empty body should end with ';'",
        messageFormat: "Replace the empty body of '{0}' with ';'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "'class Marker;' instead of 'class Marker { }' (C# 12). Off by default. Not reported below C# 12, or in a "
            + "project with several target frameworks unless every one defaults to C# 12 or LangVersion is set. Not a StyleCop "
            + "rule (Meziantou MA0206).",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyTypeBody + "/");

    public static readonly DiagnosticDescriptor RecordClassKeyword = new(
        id: DiagnosticIds.RecordClassKeyword,
        title: "Write 'record' without 'class'",
        messageFormat: "Remove 'class' from the declaration of record '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'record R' instead of 'record class R': a record is a class unless it says 'struct'. Not a StyleCop rule "
            + "(Meziantou MA0174).",
        helpLinkUri: HelpBase + DiagnosticIds.RecordClassKeyword + "/");

    public static readonly DiagnosticDescriptor AutoAccessorsOnOneLine = new(
        id: DiagnosticIds.AutoAccessorsOnOneLine,
        title: "Auto-accessors should be on one line",
        messageFormat: "Put the auto-accessors on the declaration's line",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A list of auto-accessors only ('get;', 'private set;', 'init;') spread over several lines goes on the "
            + "declaration's line: 'public int Simple { get; set; }'. Not a StyleCop rule (Roslynator RCS0042).",
        helpLinkUri: HelpBase + DiagnosticIds.AutoAccessorsOnOneLine + "/");

    private const string HelpBase = "https://bisforboman.github.io/stylebro/rules/";

    private const string FadeDescription = "Fades the parentheses of a finding in an IDE. Not configurable: reported only where its rule is on.";
}
