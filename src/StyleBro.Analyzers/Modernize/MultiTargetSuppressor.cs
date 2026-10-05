using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Modernize;

/// <summary>
/// The multi-target guard (docs/modernizing.md): hides a diagnostic of an SDK rule whose fix writes a newer API (tier C of
/// 'stylebro-migrate init --modernize') when the project targets several frameworks and one of them lacks that API. The
/// SDK rules check the API per target framework, so they report in the newer framework's compilation and 'dotnet format'
/// writes the fix into the file every framework shares. The package passes the framework list as
/// <c>build_property.StyleBroTargetFrameworks</c> (comma-separated: ';' starts a comment in the generated editorconfig).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultiTargetSuppressor : DiagnosticSuppressor
{
    /// <summary>The compiler-visible property with the project's target frameworks.</summary>
    public const string Property = "build_property.StyleBroTargetFrameworks";

    /// <summary>
    /// Rule -> the first .NET (Core) version and .NET Standard version (null: none) with every API the rule's fix can
    /// write. .NET Framework has none of them. Maintenance: add a row when tier C (modernize-rules.editorconfig) gets a rule
    /// (MigrationTests checks both lists match).
    /// </summary>
    // ponytail: hand-kept table; a minimum that's too high only hides a fix, one that's too low lets a fix break a build.
    internal static readonly ImmutableDictionary<string, (Version Core, Version? Standard)> Minimums = new Dictionary<string, (Version, Version?)>
    {
        ["CA1510"] = (new(6, 0), null),               // ArgumentNullException.ThrowIfNull
        ["CA1511"] = (new(7, 0), null),               // ArgumentException.ThrowIfNullOrEmpty
        ["CA1512"] = (new(8, 0), null),               // ArgumentOutOfRangeException.ThrowIfNegative, ...
        ["CA1513"] = (new(7, 0), null),               // ObjectDisposedException.ThrowIf
        ["CA1847"] = (new(2, 1), new(2, 1)),          // string.Contains(char)
        ["CA1850"] = (new(6, 0), null),               // static HashData (SHA*/MD5 .NET 5, HMAC* .NET 6)
        ["CA1864"] = (new(2, 0), new(2, 1)),          // Dictionary.TryAdd
        ["CA1865"] = (new(2, 1), new(2, 1)),          // StartsWith(char), EndsWith(char), IndexOf(char, StringComparison)
        ["CA1872"] = (new(9, 0), null),               // Convert.ToHexString (.NET 5), ToHexStringLower (.NET 9)
        ["CA2249"] = (new(2, 1), new(2, 1)),          // string.Contains(string, StringComparison)
        ["CA2263"] = (new(5, 0), null),               // generic overloads: Enum.Parse<T> (Core 2.0), Enum.GetValues<T> (.NET 5), ...
        ["IDE0056"] = (new(3, 0), new(2, 1)),         // System.Index
        ["IDE0057"] = (new(3, 0), new(2, 1)),         // System.Range
        ["IDE0330"] = (new(9, 0), null),              // System.Threading.Lock
    }.ToImmutableDictionary();

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = Minimums.Keys.ToImmutableDictionary(
        id => id,
        id => new SuppressionDescriptor("MULTITARGET_" + id, id, "A target framework of this project lacks the API the fix writes (StyleBro's multi-target guard)"));

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.ToImmutableArray();

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Property, out var value))
        {
            return;
        }

        var frameworks = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).Where(f => f.Length > 0).ToList();
        if (frameworks.Count < 2)
        {
            // Single target: the rule itself checks that the API exists.
            return;
        }

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (Descriptors.TryGetValue(diagnostic.Id, out var descriptor) && !frameworks.All(f => Has(f, Minimums[diagnostic.Id])))
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    /// <summary>Whether a target framework (net48, netstandard2.1, netcoreapp3.1, net8.0-windows, ...) reaches the minimum.</summary>
    internal static bool Has(string framework, (Version Core, Version? Standard) minimum)
    {
        var name = framework.ToLowerInvariant();
        var dash = name.IndexOf('-');
        if (dash >= 0)
        {
            name = name.Substring(0, dash);
        }

        if (name.StartsWith("netcoreapp", StringComparison.Ordinal))
        {
            return Version.TryParse(name.Substring(10), out var core) && core >= minimum.Core;
        }

        if (name.StartsWith("netstandard", StringComparison.Ordinal))
        {
            return minimum.Standard is { } standard && Version.TryParse(name.Substring(11), out var version) && version >= standard;
        }

        // net5.0 and later have a dot, so .NET Framework (net48) doesn't parse. Anything else is unknown: treated as lacking it.
        return name.StartsWith("net", StringComparison.Ordinal)
            && Version.TryParse(name.Substring(3), out var net) && net.Major >= 5 && net >= minimum.Core;
    }
}
