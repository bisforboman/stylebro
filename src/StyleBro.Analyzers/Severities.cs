using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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

    /// <summary>
    /// The severity a file's settings give an id (also an SDK rule's, 'IDE1006'), or null when nothing sets it. With
    /// <paramref name="options"/> (the file's) also a bulk one, the id's first: 'dotnet_analyzer_diagnostic.category-X.severity'
    /// for <paramref name="category"/>, then 'dotnet_analyzer_diagnostic.severity' (trial 19: 'category-Style.severity =
    /// warning' enforced IDE0042, which an id-only check missed). Bulk settings skip rules that are off by default.
    /// </summary>
    public static ReportDiagnostic? Configured(CompilationOptions? compilationOptions, SyntaxTree tree, string id, CancellationToken cancellationToken, AnalyzerConfigOptions? options = null, string? category = null) =>
        compilationOptions?.SyntaxTreeOptionsProvider is { } provider
            && (provider.TryGetDiagnosticValue(tree, id, cancellationToken, out var severity) || provider.TryGetGlobalDiagnosticValue(id, cancellationToken, out severity))
            ? severity
            : compilationOptions is not null && compilationOptions.SpecificDiagnosticOptions.TryGetValue(id, out severity) && severity != ReportDiagnostic.Default
            ? severity
            : options is null ? null
            : Parse(category is null ? null : Get(options, $"dotnet_analyzer_diagnostic.category-{category}.severity"))
                ?? Parse(Get(options, "dotnet_analyzer_diagnostic.severity"));

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

    private static string? Get(AnalyzerConfigOptions options, string key) => options.TryGetValue(key, out var value) ? value : null;

    private static ReportDiagnostic? Parse(string? severity) => severity?.Trim().ToLowerInvariant() switch
    {
        "none" => ReportDiagnostic.Suppress,
        "silent" or "hidden" => ReportDiagnostic.Hidden,
        "suggestion" or "info" => ReportDiagnostic.Info,
        "warning" => ReportDiagnostic.Warn,
        "error" => ReportDiagnostic.Error,
        _ => null,
    };
}
