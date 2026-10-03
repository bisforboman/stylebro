using Microsoft.CodeAnalysis;

namespace StyleBro.Analyzers;

internal static class Descriptors
{
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

    public static readonly DiagnosticDescriptor ParameterTagHasName = new(
        id: DiagnosticIds.ParameterTagHasName,
        title: "Element parameter documentation should declare parameter name",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every '<param>' tag has a 'name'. Reported when the name is certain: one unnamed tag and one "
            + "undocumented parameter, or every tag unnamed with one per parameter. Replaces StyleCop SA1613.",
        helpLinkUri: HelpBase + DiagnosticIds.ParameterTagHasName + ".md");

    public static readonly DiagnosticDescriptor TypeParameterTagsMatch = new(
        id: DiagnosticIds.TypeParameterTagsMatch,
        title: "Generic type parameter documentation should match type parameters",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'<typeparam>' tags document the type parameters that exist, in their order. Replaces StyleCop SA1620.",
        helpLinkUri: HelpBase + DiagnosticIds.TypeParameterTagsMatch + ".md");

    public static readonly DiagnosticDescriptor TypeParameterTagHasName = new(
        id: DiagnosticIds.TypeParameterTagHasName,
        title: "Generic type parameter documentation should declare parameter name",
        messageFormat: "{0}",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every '<typeparam>' tag has a 'name'. Reported when the name is certain, like BRO1612. Replaces "
            + "StyleCop SA1621.",
        helpLinkUri: HelpBase + DiagnosticIds.TypeParameterTagHasName + ".md");

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
        helpLinkUri: HelpBase + DiagnosticIds.FileHeader + ".md");

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

    public static readonly DiagnosticDescriptor PropertyAccessorOrder = new(
        id: DiagnosticIds.PropertyAccessorOrder,
        title: "Property accessors should follow order",
        messageFormat: "Put the 'get' accessor before the '{0}' accessor",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'get' comes before 'set' or 'init'. The fix swaps the accessors with their comments. Replaces StyleCop SA1212.",
        helpLinkUri: HelpBase + DiagnosticIds.PropertyAccessorOrder + ".md");
    public static readonly DiagnosticDescriptor EventAccessorOrder = new(
        id: DiagnosticIds.EventAccessorOrder,
        title: "Event accessors should follow order",
        messageFormat: "Put the 'add' accessor before the 'remove' accessor",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'add' comes before 'remove'. The fix swaps the accessors with their comments. Replaces StyleCop SA1213.",
        helpLinkUri: HelpBase + DiagnosticIds.EventAccessorOrder + ".md");

    public static readonly DiagnosticDescriptor CombinedFields = new(
        id: DiagnosticIds.CombinedFields,
        title: "Do not combine fields",
        messageFormat: "Declare '{0}' in a declaration of its own",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "One field (or event field) per declaration. The fix gives every field its own declaration with the same "
            + "attributes, modifiers, type and documentation. Replaces StyleCop SA1132.",
        helpLinkUri: HelpBase + DiagnosticIds.CombinedFields + ".md");

    public static readonly DiagnosticDescriptor NullableShorthand = new(
        id: DiagnosticIds.NullableShorthand,
        title: "Use shorthand for nullable types",
        messageFormat: "Use '{0}?' instead of 'Nullable<{0}>'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'int?' instead of 'Nullable<int>' or 'System.Nullable<int>'. Replaces StyleCop SA1125.",
        helpLinkUri: HelpBase + DiagnosticIds.NullableShorthand + ".md");

    public static readonly DiagnosticDescriptor LiteralSuffix = new(
        id: DiagnosticIds.LiteralSuffix,
        title: "Use literal suffix notation instead of casting",
        messageFormat: "Use '{0}' instead of the cast",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'1L' instead of '(long)1', when the literal has exactly the cast's value. Replaces StyleCop SA1139.",
        helpLinkUri: HelpBase + DiagnosticIds.LiteralSuffix + ".md");

    public static readonly DiagnosticDescriptor TupleSyntax = new(
        id: DiagnosticIds.TupleSyntax,
        title: "Use tuple syntax",
        messageFormat: "Use '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'(int, string)' instead of 'ValueTuple<int, string>', and '(1, \"a\")' instead of 'new ValueTuple<int, string>(1, \"a\")' "
            + "or 'ValueTuple.Create(1, \"a\")'. Replaces StyleCop SA1141.",
        helpLinkUri: HelpBase + DiagnosticIds.TupleSyntax + ".md");

    public static readonly DiagnosticDescriptor TupleElementName = new(
        id: DiagnosticIds.TupleElementName,
        title: "Refer to tuple elements by name",
        messageFormat: "Use '{0}' instead of '{1}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'t.Name' instead of 't.Item1' when the tuple element has a name. Replaces StyleCop SA1142.",
        helpLinkUri: HelpBase + DiagnosticIds.TupleElementName + ".md");

    public static readonly DiagnosticDescriptor LambdaSyntax = new(
        id: DiagnosticIds.LambdaSyntax,
        title: "Use lambda syntax",
        messageFormat: "Use a lambda instead of an anonymous method",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'(s, e) => { }' instead of 'delegate (object s, EventArgs e) { }', when the lambda binds the same way. "
            + "Replaces StyleCop SA1130.",
        helpLinkUri: HelpBase + DiagnosticIds.LambdaSyntax + ".md");

    public static readonly DiagnosticDescriptor EmptyDelegateParentheses = new(
        id: DiagnosticIds.EmptyDelegateParentheses,
        title: "Remove delegate parenthesis when possible",
        messageFormat: "Remove the empty parentheses after 'delegate'",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'delegate { }' instead of 'delegate() { }', unless an overloaded call needs them. Where BRO1125 is on, it turns "
            + "the method into a lambda instead. Replaces StyleCop SA1410.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyDelegateParentheses + ".md");

    public static readonly DiagnosticDescriptor QualifiedUsing = new(
        id: DiagnosticIds.QualifiedUsing,
        title: "Using directives should be qualified",
        messageFormat: "Use the fully qualified name '{0}'",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A using directive inside a namespace names its namespace or type fully, so it doesn't depend on the "
            + "namespace it's in. Replaces StyleCop SA1135.",
        helpLinkUri: HelpBase + DiagnosticIds.QualifiedUsing + ".md");

    public static readonly DiagnosticDescriptor QueryClauseBlankLine = new(
        id: DiagnosticIds.QueryClauseBlankLine,
        title: "Query clause should follow previous clause",
        messageFormat: "Remove the blank lines before the query clause",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between the clauses of a query expression. Replaces StyleCop SA1102.",
        helpLinkUri: HelpBase + DiagnosticIds.QueryClauseBlankLine + ".md");

    public static readonly DiagnosticDescriptor QueryClausesOnSeparateLines = new(
        id: DiagnosticIds.QueryClausesOnSeparateLines,
        title: "Query clauses should be on separate lines or all on one line",
        messageFormat: "Put each query clause on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A query's clauses are all on one line or each on its own line. Replaces StyleCop SA1103.",
        helpLinkUri: HelpBase + DiagnosticIds.QueryClausesOnSeparateLines + ".md");

    public static readonly DiagnosticDescriptor QueryClauseAfterMultiLineClause = new(
        id: DiagnosticIds.QueryClauseAfterMultiLineClause,
        title: "Query clause should begin on new line when previous clause spans multiple lines",
        messageFormat: "Start the query clause on a new line: the clause before it spans several lines",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A clause after a clause that spans several lines starts on its own line. Replaces StyleCop SA1104.",
        helpLinkUri: HelpBase + DiagnosticIds.QueryClauseAfterMultiLineClause + ".md");

    public static readonly DiagnosticDescriptor MultiLineQueryClause = new(
        id: DiagnosticIds.MultiLineQueryClause,
        title: "Query clauses spanning multiple lines should begin on own line",
        messageFormat: "Start the query clause on its own line: it spans several lines",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A clause that spans several lines starts on its own line. Replaces StyleCop SA1105.",
        helpLinkUri: HelpBase + DiagnosticIds.MultiLineQueryClause + ".md");

    public static readonly DiagnosticDescriptor AccessModifier = new(
        id: DiagnosticIds.AccessModifier,
        title: "Access modifier should be declared",
        messageFormat: "Declare the access modifier of '{0}' ({1})",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types and members say who can use them instead of relying on the default. Replaces StyleCop SA1400.",
        helpLinkUri: HelpBase + DiagnosticIds.AccessModifier + ".md");

    public static readonly DiagnosticDescriptor PartialAccessModifier = new(
        id: DiagnosticIds.PartialAccessModifier,
        title: "Partial elements should declare an access modifier",
        messageFormat: "Declare the access modifier of '{0}' ({1}) on this part too",
        category: "Ordering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Every part of a partial type says its accessibility, so no part has to be found to know it. Replaces StyleCop SA1205.",
        helpLinkUri: HelpBase + DiagnosticIds.PartialAccessModifier + ".md");

    public static readonly DiagnosticDescriptor BaseCall = new(
        id: DiagnosticIds.BaseCall,
        title: "Do not prefix calls with base unless local implementation exists",
        messageFormat: "Use 'this' instead of 'base': the type has no member of its own with this name",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'base.' suggests the type overrides or hides the member. Replaces StyleCop SA1100.",
        helpLinkUri: HelpBase + DiagnosticIds.BaseCall + ".md");

    public static readonly DiagnosticDescriptor DirectiveSpacing = new(
        id: DiagnosticIds.DirectiveSpacing,
        title: "Preprocessor keywords should not be preceded by a space",
        messageFormat: "Remove the space between '#' and '{0}'",
        category: "Spacing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'#if', not '# if'. Replaces StyleCop SA1006.",
        helpLinkUri: HelpBase + DiagnosticIds.DirectiveSpacing + ".md");

    public static readonly DiagnosticDescriptor EmptyAttributeParentheses = new(
        id: DiagnosticIds.EmptyAttributeParentheses,
        title: "Attribute constructor should not use unnecessary parenthesis",
        messageFormat: "Remove the empty parentheses",
        category: "Maintainability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'[Obsolete]' instead of '[Obsolete()]'. Replaces StyleCop SA1411.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyAttributeParentheses + ".md");

    public static readonly DiagnosticDescriptor DocumentationLineSpace = new(
        id: DiagnosticIds.DocumentationLineSpace,
        title: "Documentation lines should begin with single space",
        messageFormat: "Put one space after '///'",
        category: "Spacing",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "'/// text' instead of '///text' or '///   text'. Lines inside <code> are left alone. Replaces StyleCop SA1004.",
        helpLinkUri: HelpBase + DiagnosticIds.DocumentationLineSpace + ".md");

    public static readonly DiagnosticDescriptor EmptyListOnOneLine = new(
        id: DiagnosticIds.EmptyListOnOneLine,
        title: "Closing parenthesis should be on line of opening parenthesis",
        messageFormat: "Put ')' right after '('",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty parameter or argument list is written '()', not split over lines. Replaces StyleCop SA1112.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyListOnOneLine + ".md");

    public static readonly DiagnosticDescriptor CommaOnItemLine = new(
        id: DiagnosticIds.CommaOnItemLine,
        title: "Comma should be on the same line as previous parameter",
        messageFormat: "Put the comma at the end of the line before",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A comma in a parameter or argument list ends the line of the item before it instead of starting the next. Replaces StyleCop SA1113.",
        helpLinkUri: HelpBase + DiagnosticIds.CommaOnItemLine + ".md");

    public static readonly DiagnosticDescriptor FirstItemFollowsOpening = new(
        id: DiagnosticIds.FirstItemFollowsOpening,
        title: "Parameter list should follow declaration",
        messageFormat: "Remove the blank lines before the first item",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between the opening parenthesis and the first parameter or argument. Replaces StyleCop SA1114.",
        helpLinkUri: HelpBase + DiagnosticIds.FirstItemFollowsOpening + ".md");

    public static readonly DiagnosticDescriptor ItemFollowsComma = new(
        id: DiagnosticIds.ItemFollowsComma,
        title: "Parameter should follow comma",
        messageFormat: "Remove the blank lines before this item",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No blank line between a comma and the next parameter or argument. Replaces StyleCop SA1115.",
        helpLinkUri: HelpBase + DiagnosticIds.ItemFollowsComma + ".md");

    public static readonly DiagnosticDescriptor EmptyComment = new(
        id: DiagnosticIds.EmptyComment,
        title: "Comments should contain text",
        messageFormat: "Remove the empty comment",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty '//' or '/* */' at the start or end of a comment (or on its own) is removed. Empty lines between comment paragraphs are fine. Replaces StyleCop SA1120.",
        helpLinkUri: HelpBase + DiagnosticIds.EmptyComment + ".md");

    public static readonly DiagnosticDescriptor EnumValueOnOwnLine = new(
        id: DiagnosticIds.EnumValueOnOwnLine,
        title: "Enum values should be on separate lines",
        messageFormat: "Put '{0}' on its own line",
        category: "Readability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each enum value starts its own line. Replaces StyleCop SA1136.",
        helpLinkUri: HelpBase + DiagnosticIds.EnumValueOnOwnLine + ".md");

    public static readonly DiagnosticDescriptor BlankLineAfterDocumentation = new(
        id: DiagnosticIds.BlankLineAfterDocumentation,
        title: "Element documentation headers should not be followed by blank line",
        messageFormat: "Remove the blank lines between the documentation and the element",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A documentation comment sits directly on the element it documents. Replaces StyleCop SA1506.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineAfterDocumentation + ".md");

    public static readonly DiagnosticDescriptor BlankLineBeforeWhile = new(
        id: DiagnosticIds.BlankLineBeforeWhile,
        title: "While-do footer should not be preceded by blank line",
        messageFormat: "Remove the blank lines before 'while'",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The 'while' of a 'do ... while' follows the block directly. Replaces StyleCop SA1511.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeWhile + ".md");

    public static readonly DiagnosticDescriptor BracesOmitted = new(
        id: DiagnosticIds.BracesOmitted,
        title: "Braces should not be omitted",
        messageFormat: "Put the statement in braces",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The child statement of an if, else, loop, using, lock or fixed statement is a block. Replaces StyleCop SA1503.",
        helpLinkUri: HelpBase + DiagnosticIds.BracesOmitted + ".md");

    public static readonly DiagnosticDescriptor BracesMultiLine = new(
        id: DiagnosticIds.BracesMultiLine,
        title: "Braces should not be omitted from multi-line child statement",
        messageFormat: "Put the multi-line statement in braces",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A child statement that spans several lines is a block. Replaces StyleCop SA1519.",
        helpLinkUri: HelpBase + DiagnosticIds.BracesMultiLine + ".md");

    public static readonly DiagnosticDescriptor BracesConsistent = new(
        id: DiagnosticIds.BracesConsistent,
        title: "Use braces consistently",
        messageFormat: "Put the statement in braces like the other clauses of the if statement",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When one clause of an if/else chain has braces, all of them do. Replaces StyleCop SA1520.",
        helpLinkUri: HelpBase + DiagnosticIds.BracesConsistent + ".md");

    public static readonly DiagnosticDescriptor BlankLineBeforeDocumentation = new(
        id: DiagnosticIds.BlankLineBeforeDocumentation,
        title: "Element documentation header should be preceded by blank line",
        messageFormat: "Add a blank line before the documentation",
        category: "Layout",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A documentation comment is separated from the code above it by a blank line, except after an opening brace. Replaces StyleCop SA1514.",
        helpLinkUri: HelpBase + DiagnosticIds.BlankLineBeforeDocumentation + ".md");

    private const string HelpBase = "https://github.com/bisforboman/stylebro/blob/main/docs/rules/";
}
