using System.Threading;
using Microsoft.CodeAnalysis;

namespace StyleBro.Analyzers;

/// <summary>
/// Whether another StyleBro rule is on for a file, for fixes that must produce what that rule wants. Severity keys
/// (dotnet_diagnostic.X.severity) aren't in AnalyzerConfigOptions; they come from the compilation options.
/// </summary>
internal static class Severities
{
    public static bool IsOn(CompilationOptions? compilationOptions, SyntaxTree tree, string id, CancellationToken cancellationToken)
    {
        var severity = ReportDiagnostic.Default;
        if (compilationOptions?.SyntaxTreeOptionsProvider is { } provider
            && (provider.TryGetDiagnosticValue(tree, id, cancellationToken, out severity) || provider.TryGetGlobalDiagnosticValue(id, cancellationToken, out severity)))
        {
            return severity is not (ReportDiagnostic.Suppress or ReportDiagnostic.Hidden);
        }

        return compilationOptions is null || !compilationOptions.SpecificDiagnosticOptions.TryGetValue(id, out severity)
            || severity is not (ReportDiagnostic.Suppress or ReportDiagnostic.Hidden);
    }
}
