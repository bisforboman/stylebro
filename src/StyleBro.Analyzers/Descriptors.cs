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
}
