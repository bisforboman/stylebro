using Microsoft.CodeAnalysis;

namespace StyleBro.Analyzers;

internal static class Descriptors
{
    private const string HelpBase = "https://github.com/bisforboman/stylebro/blob/main/docs/rules/";

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
        helpLinkUri: HelpBase + DiagnosticIds.MemberOrdering + ".md");

    public static readonly DiagnosticDescriptor CommentSpacing = new(
        id: DiagnosticIds.CommentSpacing,
        title: "Single-line comments should begin with a space",
        messageFormat: "Add a space after '//'",
        category: "Spacing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'// note' instead of '//note'. Commented-out code ('////') and separators ('//--') are left alone. "
            + "Replaces StyleCop SA1005.",
        helpLinkUri: HelpBase + DiagnosticIds.CommentSpacing + ".md");

    public static readonly DiagnosticDescriptor ConstructorInitializerLine = new(
        id: DiagnosticIds.ConstructorInitializerLine,
        title: "Constructor initializers should be on their own line",
        messageFormat: "Put ': {0}(...)' on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "': base(...)' and ': this(...)' go on the line after the constructor's parameters, one level "
            + "deeper. Replaces StyleCop SA1128.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstructorInitializerLine + ".md");

    public static readonly DiagnosticDescriptor EmptyStatement = new(
        id: DiagnosticIds.EmptyStatement,
        title: "Code should not contain empty statements",
        messageFormat: "Remove the empty statement",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A stray ';' in a block, or after the closing brace of a type or namespace, does nothing. "
            + "Replaces StyleCop SA1106.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyStatement + ".md");

    public static readonly DiagnosticDescriptor CombinedAttributes = new(
        id: DiagnosticIds.CombinedAttributes,
        title: "Each attribute should be in its own brackets",
        messageFormat: "Put '{0}' in its own brackets",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Writes '[A, B]' as '[A]' and '[B]'. Replaces StyleCop SA1133.",
        helpLinkUri: HelpBase + DiagnosticIds.CombinedAttributes + ".md");

    public static readonly DiagnosticDescriptor ConstantOnLeft = new(
        id: DiagnosticIds.ConstantOnLeft,
        title: "Constants should be on the right-hand side of comparisons",
        messageFormat: "Put '{0}' on the right-hand side of the comparison",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'x == 1' reads more naturally than '1 == x'. Replaces StyleCop SA1131.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstantOnLeft + ".md");

    public static readonly DiagnosticDescriptor DefaultValueConstructor = new(
        id: DiagnosticIds.DefaultValueConstructor,
        title: "Use default instead of a value type's default constructor",
        messageFormat: "Use '{0}' instead",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'new int()' creates the default value; 'default(int)' says so. Replaces StyleCop SA1129.",
        helpLinkUri: HelpBase + DiagnosticIds.DefaultValueConstructor + ".md");

    public static readonly DiagnosticDescriptor EmptyString = new(
        id: DiagnosticIds.EmptyString,
        title: "Use string.Empty for empty strings",
        messageFormat: "Use 'string.Empty' instead of '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'string.Empty' instead of \"\", except where C# requires a constant. Replaces StyleCop SA1122.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyString + ".md");

    public static readonly DiagnosticDescriptor SplitParametersStartOnNewLine = new(
        id: DiagnosticIds.SplitParametersStartOnNewLine,
        title: "Split parameters should start on the line after the declaration",
        messageFormat: "Move the first item to the line after the opening parenthesis",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When parameters or arguments span several lines, the first one starts on its own line too. "
            + "Replaces StyleCop SA1116.",
        helpLinkUri: HelpBase + DiagnosticIds.SplitParametersStartOnNewLine + ".md");

    public static readonly DiagnosticDescriptor ParametersOnSameOrSeparateLines = new(
        id: DiagnosticIds.ParametersOnSameOrSeparateLines,
        title: "Parameters should be on the same line or on separate lines",
        messageFormat: "Put the items all on one line or each on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters and arguments are either all on one line or each on its own line. Replaces StyleCop SA1117.",
        helpLinkUri: HelpBase + DiagnosticIds.ParametersOnSameOrSeparateLines + ".md");

    public static readonly DiagnosticDescriptor VariableCasing = new(
        id: DiagnosticIds.VariableCasing,
        title: "Variable names should begin with a lower-case letter",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Local variables and query range variables are camelCase: 'total', not 'Total' or '_total'. "
            + "Replaces StyleCop SA1312.",
        helpLinkUri: HelpBase + DiagnosticIds.VariableCasing + ".md");

    public static readonly DiagnosticDescriptor ParameterCasing = new(
        id: DiagnosticIds.ParameterCasing,
        title: "Parameter names should begin with a lower-case letter",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Parameters are camelCase: 'count', not 'Count' or '_count'. The fix also renames named arguments "
            + "and the parameters of overrides and implementations. Replaces StyleCop SA1313.",
        helpLinkUri: HelpBase + DiagnosticIds.ParameterCasing + ".md");

    public static readonly DiagnosticDescriptor PrivateFieldNaming = new(
        id: DiagnosticIds.PrivateFieldNaming,
        title: "Private field names should be camelCase",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Private fields are 'count', or '_count' with stylebro_private_field_naming = _camelCase. "
            + "Replaces StyleCop SA1306 and SA1309 for private fields.",
        helpLinkUri: HelpBase + DiagnosticIds.PrivateFieldNaming + ".md");

    public static readonly DiagnosticDescriptor InterfacePrefix = new(
        id: DiagnosticIds.InterfacePrefix,
        title: "Interface names should begin with I",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Interfaces are named 'IShape', not 'Shape'. Replaces StyleCop SA1302.",
        helpLinkUri: HelpBase + DiagnosticIds.InterfacePrefix + ".md");

    public static readonly DiagnosticDescriptor TypeParameterPrefix = new(
        id: DiagnosticIds.TypeParameterPrefix,
        title: "Type parameter names should begin with T",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Type parameters are named 'T' or 'TItem', not 'Item'. Replaces StyleCop SA1314.",
        helpLinkUri: HelpBase + DiagnosticIds.TypeParameterPrefix + ".md");

    public static readonly DiagnosticDescriptor FieldPascalCase = new(
        id: DiagnosticIds.FieldPascalCase,
        title: "Constant, static readonly and non-private field names should be PascalCase",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Constants, static readonly fields and public or internal fields are 'MaxCount', not 'maxCount' or "
            + "'_maxCount'. Replaces StyleCop SA1303, SA1311, SA1307 and SA1304.",
        helpLinkUri: HelpBase + DiagnosticIds.FieldPascalCase + ".md");

    public static readonly DiagnosticDescriptor FieldPrefix = new(
        id: DiagnosticIds.FieldPrefix,
        title: "Field names should not begin with a prefix",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fields aren't named 'm_count', 's_count' or 't_count'. Replaces StyleCop SA1308.",
        helpLinkUri: HelpBase + DiagnosticIds.FieldPrefix + ".md");

    public static readonly DiagnosticDescriptor FieldUnderscore = new(
        id: DiagnosticIds.FieldUnderscore,
        title: "Field names should not contain an underscore",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fields are 'maxValue' or 'MaxValue', not 'max_value' or 'MAX_VALUE'. Replaces StyleCop SA1310.",
        helpLinkUri: HelpBase + DiagnosticIds.FieldUnderscore + ".md");

    public static readonly DiagnosticDescriptor ElementPascalCase = new(
        id: DiagnosticIds.ElementPascalCase,
        title: "Element names should begin with an upper-case letter",
        messageFormat: "Rename '{0}' to '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types, methods, properties, events, enum members and local functions are PascalCase. "
            + "Replaces StyleCop SA1300 (except for namespaces).",
        helpLinkUri: HelpBase + DiagnosticIds.ElementPascalCase + ".md");

    public static readonly DiagnosticDescriptor OpenParenthesisOnNameLine = new(
        id: DiagnosticIds.OpenParenthesisOnNameLine,
        title: "Opening parenthesis or bracket should be on the declaration line",
        messageFormat: "Move '{0}' to the end of the previous line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'Method(' starts the list on the line of the name, not on the next line. Replaces StyleCop SA1110.",
        helpLinkUri: HelpBase + DiagnosticIds.OpenParenthesisOnNameLine + ".md");

    public static readonly DiagnosticDescriptor CloseParenthesisOnLastItemLine = new(
        id: DiagnosticIds.CloseParenthesisOnLastItemLine,
        title: "Closing parenthesis or bracket should be on the line of the last item",
        messageFormat: "Move '{0}' to the end of the last item",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The list ends where its last item ends, not on a line of its own. Replaces StyleCop SA1111.",
        helpLinkUri: HelpBase + DiagnosticIds.CloseParenthesisOnLastItemLine + ".md");

    public static readonly DiagnosticDescriptor ConstraintOnOwnLine = new(
        id: DiagnosticIds.ConstraintOnOwnLine,
        title: "Generic type constraints should be on their own line",
        messageFormat: "Move the constraint on '{0}' to its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each 'where' clause starts its own line, one level deeper than the declaration. Replaces StyleCop SA1127.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstraintOnOwnLine + ".md");

    public static readonly DiagnosticDescriptor NoRegions = new(
        id: DiagnosticIds.NoRegions,
        title: "Do not use regions",
        messageFormat: "Remove the region",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'#region' hides code instead of organizing it. Replaces StyleCop SA1124 (regions between members "
            + "and types; BRO1113 covers regions inside members).",
        helpLinkUri: HelpBase + DiagnosticIds.NoRegions + ".md");

    public static readonly DiagnosticDescriptor NoRegionsInCodeElements = new(
        id: DiagnosticIds.NoRegionsInCodeElements,
        title: "Regions should not be placed inside code elements",
        messageFormat: "Remove the region",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A region inside a method or property body hides part of it. Replaces StyleCop SA1123.",
        helpLinkUri: HelpBase + DiagnosticIds.NoRegionsInCodeElements + ".md");

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
        helpLinkUri: HelpBase + DiagnosticIds.InheritDocumentation + ".md");

    public static readonly DiagnosticDescriptor DocumentationSlashesInComment = new(
        id: DiagnosticIds.DocumentationSlashesInComment,
        title: "Single-line comments should not use documentation style slashes",
        messageFormat: "Use '//' for a comment that doesn't document a member",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'///' starts documentation; a comment inside code uses '//'. Replaces StyleCop SA1626.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationSlashesInComment + ".md");

    public static readonly DiagnosticDescriptor DocumentationEndsWithPeriod = new(
        id: DiagnosticIds.DocumentationEndsWithPeriod,
        title: "Documentation text should end with a period",
        messageFormat: "End the documentation text with a period",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Summary, remarks, parameter, return value and exception text are sentences. Replaces StyleCop SA1629.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationEndsWithPeriod + ".md");

    public static readonly DiagnosticDescriptor PropertySummaryWording = new(
        id: DiagnosticIds.PropertySummaryWording,
        title: "Property summary documentation should match accessors",
        messageFormat: "Begin the summary with '{0}'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property's summary begins with 'Gets or sets', 'Gets' or 'Sets' (plus 'a value indicating whether' "
            + "for a bool), matching its accessors. Replaces StyleCop SA1623.",
        helpLinkUri: HelpBase + DiagnosticIds.PropertySummaryWording + ".md");

    public static readonly DiagnosticDescriptor PropertySummaryRestrictedSetter = new(
        id: DiagnosticIds.PropertySummaryRestrictedSetter,
        title: "Property summary documentation should omit accessor with restricted access",
        messageFormat: "Begin the summary with '{0}': other code can't use the setter",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property with a private or otherwise restricted setter is documented as 'Gets', not 'Gets or sets'. "
            + "Replaces StyleCop SA1624.",
        helpLinkUri: HelpBase + DiagnosticIds.PropertySummaryRestrictedSetter + ".md");

    public static readonly DiagnosticDescriptor ConstructorSummary = new(
        id: DiagnosticIds.ConstructorSummary,
        title: "Constructor summary documentation should begin with standard text",
        messageFormat: "Begin the constructor's summary with 'Initializes a new instance of ...'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A constructor's summary begins with 'Initializes a new instance of the <see cref=\"T\"/> class.' "
            + "(or 'Initializes static members of' for a static constructor). Replaces StyleCop SA1642.",
        helpLinkUri: HelpBase + DiagnosticIds.ConstructorSummary + ".md");

    public static readonly DiagnosticDescriptor DestructorSummary = new(
        id: DiagnosticIds.DestructorSummary,
        title: "Destructor summary documentation should begin with standard text",
        messageFormat: "Begin the finalizer's summary with 'Finalizes an instance of ...'",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A finalizer's summary begins with 'Finalizes an instance of the <see cref=\"T\"/> class.' "
            + "Replaces StyleCop SA1643.",
        helpLinkUri: HelpBase + DiagnosticIds.DestructorSummary + ".md");

    public static readonly DiagnosticDescriptor VoidReturnDocumented = new(
        id: DiagnosticIds.VoidReturnDocumented,
        title: "Void return value should not be documented",
        messageFormat: "Remove the <returns> documentation: the method returns void",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A method or delegate that returns void has no return value to document. Replaces StyleCop SA1617.",
        helpLinkUri: HelpBase + DiagnosticIds.VoidReturnDocumented + ".md");

    public static readonly DiagnosticDescriptor PlaceholderElement = new(
        id: DiagnosticIds.PlaceholderElement,
        title: "Do not use placeholder elements",
        messageFormat: "Remove the <placeholder> tags",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'<placeholder>' marks generated documentation that still needs a review; the text stays, the "
            + "tags go. Replaces StyleCop SA1651.",
        helpLinkUri: HelpBase + DiagnosticIds.PlaceholderElement + ".md");

    public static readonly DiagnosticDescriptor EmptyRemarks = new(
        id: DiagnosticIds.EmptyRemarks,
        title: "Documentation text should not be empty",
        messageFormat: "Remove the empty <remarks>",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty '<remarks>' element says nothing. Replaces StyleCop SA1627.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyRemarks + ".md");

    public static readonly DiagnosticDescriptor ParameterTagsMatch = new(
        id: DiagnosticIds.ParameterTagsMatch,
        title: "Element parameter documentation should match element parameters",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'<param>' tags document the parameters that exist, in their order. Replaces StyleCop SA1612.",
        helpLinkUri: HelpBase + DiagnosticIds.ParameterTagsMatch + ".md");

    public static readonly DiagnosticDescriptor TrailingComma = new(
        id: DiagnosticIds.TrailingComma,
        title: "Use a trailing comma in multi-line initializers",
        messageFormat: "Add a trailing comma",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A trailing comma after the last item keeps diffs to one line when items are added. "
            + "Replaces StyleCop SA1413.",
        helpLinkUri: HelpBase + DiagnosticIds.TrailingComma + ".md");

    public static readonly DiagnosticDescriptor BlankLineBeforeOpenBrace = new(
        id: DiagnosticIds.BlankLineBeforeOpenBrace,
        title: "Opening braces should not be preceded by a blank line",
        messageFormat: "Remove the blank line before '{{'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An opening brace on its own line directly follows the line it belongs to. Replaces StyleCop SA1509.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeOpenBrace + ".md");

    public static readonly DiagnosticDescriptor BlankLineBeforeChainedBlock = new(
        id: DiagnosticIds.BlankLineBeforeChainedBlock,
        title: "Chained blocks should not be preceded by a blank line",
        messageFormat: "Remove the blank line before '{0}'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'else', 'catch' and 'finally' directly follow the block they continue. Replaces StyleCop SA1510.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeChainedBlock + ".md");

    public static readonly DiagnosticDescriptor BlankLineAfterOpenBrace = new(
        id: DiagnosticIds.BlankLineAfterOpenBrace,
        title: "Opening braces should not be followed by a blank line",
        messageFormat: "Remove the blank line after '{{'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The first line inside a block directly follows its opening brace. Replaces StyleCop SA1505.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterOpenBrace + ".md");

    public static readonly DiagnosticDescriptor BlankLineBeforeComment = new(
        id: DiagnosticIds.BlankLineBeforeComment,
        title: "Single-line comments should be preceded by a blank line",
        messageFormat: "Add a blank line before the comment",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comment that introduces the code below it is separated from the code above. Replaces StyleCop SA1515.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeComment + ".md");

    public static readonly DiagnosticDescriptor ElementsSeparatedByBlankLine = new(
        id: DiagnosticIds.ElementsSeparatedByBlankLine,
        title: "Elements should be separated by a blank line",
        messageFormat: "Add a blank line before this element",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Members, types and namespaces are separated by a blank line; consecutive fields may stay together. "
            + "Replaces StyleCop SA1516.",
        helpLinkUri: HelpBase + DiagnosticIds.ElementsSeparatedByBlankLine + ".md");

    public static readonly DiagnosticDescriptor BlankLineAfterComment = new(
        id: DiagnosticIds.BlankLineAfterComment,
        title: "Single-line comments should not be followed by a blank line",
        messageFormat: "Remove the blank line after the comment",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comment sits directly on the code it describes. Replaces StyleCop SA1512.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterComment + ".md");

    public static readonly DiagnosticDescriptor BlankLinesAtEndOfFile = new(
        id: DiagnosticIds.BlankLinesAtEndOfFile,
        title: "Files should not end with blank lines",
        messageFormat: "Remove the blank lines at the end of the file",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A file ends with its last line of code, followed by at most one line break. Replaces StyleCop SA1518.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLinesAtEndOfFile + ".md");

    public static readonly DiagnosticDescriptor SingleLineStatementBlock = new(
        id: DiagnosticIds.SingleLineStatementBlock,
        title: "A block should not be on a single line",
        messageFormat: "Put the block on separate lines",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'if (x) { return; }' becomes a block with its braces and each statement on their own lines. Lambda "
            + "bodies are left alone. Replaces StyleCop SA1501.",
        helpLinkUri: HelpBase + DiagnosticIds.SingleLineStatementBlock + ".md");

    public static readonly DiagnosticDescriptor SingleLineElement = new(
        id: DiagnosticIds.SingleLineElement,
        title: "An element should not be on a single line",
        messageFormat: "Put '{0}' on separate lines",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types, namespaces, method bodies and accessor lists with bodies aren't written on one line. Auto-properties "
            + "('{ get; set; }') and single-line accessors inside a multi-line property are fine. Replaces StyleCop SA1502.",
        helpLinkUri: HelpBase + DiagnosticIds.SingleLineElement + ".md");

    public static readonly DiagnosticDescriptor AccessorsConsistentLayout = new(
        id: DiagnosticIds.AccessorsConsistentLayout,
        title: "Accessors should all be single-line or all multi-line",
        messageFormat: "Lay out all accessors the same way: all on one line each, or all on several lines",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When every accessor has a block body, they are either all single-line or all multi-line. The fix puts "
            + "one-statement bodies on one line, or expands the single-line ones. Replaces StyleCop SA1504.",
        helpLinkUri: HelpBase + DiagnosticIds.AccessorsConsistentLayout + ".md");
}
