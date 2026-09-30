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
            + "Run 'dotnet format analyzers --diagnostics BRO1001' to fix a whole solution.",
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
}
