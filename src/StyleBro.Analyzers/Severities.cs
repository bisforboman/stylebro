using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace StyleBro.Analyzers;

/// <summary>
/// Whether another StyleBro rule is on for a file, for fixes that must produce what that rule wants. Severity keys
/// (dotnet_diagnostic.X.severity) aren't in AnalyzerConfigOptions; they come from the compilation options.
/// </summary>
internal static class Severities
{
    // Each StyleBro rule's own default, from its descriptor: an unconfigured rule that is off by default (BRO1143,
    // BRO1310, ...) must count as off, also where a fix only gets an 'isOn' callback (BRO1139 asking about BRO1143 treated
    // it as on in every project that never configured it).
    private static readonly Dictionary<string, bool> Defaults = typeof(Descriptors)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => f.GetValue(null))
        .OfType<DiagnosticDescriptor>()
        .GroupBy(d => d.Id)
        .ToDictionary(g => g.Key, g => g.First().IsEnabledByDefault);

    public static bool IsOn(CompilationOptions? compilationOptions, SyntaxTree tree, string id, CancellationToken cancellationToken, bool? enabledByDefault = null)
    {
        var byDefault = enabledByDefault ?? (!Defaults.TryGetValue(id, out var known) || known);
        var severity = ReportDiagnostic.Default;
        if (compilationOptions?.SyntaxTreeOptionsProvider is { } provider
            && (provider.TryGetDiagnosticValue(tree, id, cancellationToken, out severity) || provider.TryGetGlobalDiagnosticValue(id, cancellationToken, out severity)))
        {
            return severity is not (ReportDiagnostic.Suppress or ReportDiagnostic.Hidden);
        }

        if (compilationOptions is null || !compilationOptions.SpecificDiagnosticOptions.TryGetValue(id, out severity))
        {
            return byDefault;
        }

        return severity switch
        {
            ReportDiagnostic.Suppress or ReportDiagnostic.Hidden => false,
            ReportDiagnostic.Default => byDefault,
            _ => true,
        };
    }
}
