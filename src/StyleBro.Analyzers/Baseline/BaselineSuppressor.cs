using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Baseline;

/// <summary>
/// Hides the violations listed in <c>stylebro.baseline</c> (an AdditionalFile; the package adds the nearest one above
/// the project). Covers every StyleBro rule and the SDK rules 'stylebro-migrate init' or '--write' turns on. A suppressed
/// diagnostic is neither reported by the build nor fixed by 'dotnet format'.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BaselineSuppressor : DiagnosticSuppressor
{
    /// <summary>The SDK rules stylebro-migrate (init or --write) can turn on (MigrationTests checks this list).</summary>
    internal static readonly string[] SdkIds =
    {
        "IDE0003", "IDE0009", "IDE0011", "IDE0036", "IDE0040", "IDE0047", "IDE0048", "IDE0049", "IDE0055", "IDE0065", "IDE0073",
        "IDE2000", "IDE2002", "IDE2003",

        // init --modernize
        "IDE0016", "IDE0017", "IDE0018", "IDE0019", "IDE0020", "IDE0028", "IDE0029", "IDE0030", "IDE0031", "IDE0041", "IDE0054",
        "IDE0056", "IDE0057", "IDE0062", "IDE0074", "IDE0078", "IDE0083", "IDE0090", "IDE0150", "IDE0161", "IDE0170", "IDE0180",
        "IDE0240", "IDE0241", "IDE0250", "IDE0270", "IDE0300", "IDE0301", "IDE0302", "IDE0303", "IDE0304", "IDE0306", "IDE0330",
        "IDE0340", "IDE0360", "CA1510", "CA1511", "CA1512", "CA1513", "CA1825", "CA1829", "CA1834", "CA1847", "CA1850", "CA1864",
        "CA1865", "CA1872", "CA2249", "CA2263",
    };

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = CreateDescriptors();

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.ToImmutableArray();

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var file = context.Options.AdditionalFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f.Path), Baseline.FileName, StringComparison.OrdinalIgnoreCase));
        if (file?.GetText(context.CancellationToken) is not { } text)
        {
            return;
        }

        var baseline = Baseline.Parse(text.ToString());
        var directory = Path.GetDirectoryName(file.Path) ?? string.Empty;
        if (baseline.Count == 0)
        {
            return;
        }

        // Each entry covers as many violations of its rule on that line as its count, in source order.
        var used = new Dictionary<Baseline.Key, int>();
        foreach (var diagnostic in context.ReportedDiagnostics
            .Where(d => d.Location.IsInSource && Descriptors.ContainsKey(d.Id))
            .OrderBy(d => d.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(d => d.Location.SourceSpan.Start))
        {
            var tree = diagnostic.Location.SourceTree!;
            if (Baseline.RelativePath(directory, tree.FilePath) is not { } path)
            {
                continue;
            }

            var key = new Baseline.Key(diagnostic.Id, path, Baseline.Fingerprint(tree.GetText(context.CancellationToken), diagnostic.Location.SourceSpan.Start));
            used.TryGetValue(key, out var count);
            if (count < baseline.Allowed(key))
            {
                used[key] = count + 1;
                context.ReportSuppression(Suppression.Create(Descriptors[diagnostic.Id], diagnostic));
            }
        }
    }

    private static ImmutableDictionary<string, SuppressionDescriptor> CreateDescriptors()
    {
        // Every StyleBro rule id (the constants in DiagnosticIds), plus the SDK rules.
        var bro = typeof(DiagnosticIds).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);
        return bro.Concat(SdkIds).Distinct(StringComparer.Ordinal)
            .ToImmutableDictionary(id => id, id => new SuppressionDescriptor("BASELINE_" + id, id, "In stylebro.baseline"));
    }
}
