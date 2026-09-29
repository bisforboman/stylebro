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
}
