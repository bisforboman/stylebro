using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers.Baseline;

namespace StyleBro.Analyzers;

/// <summary>
/// Hides StyleBro's findings in files that come from NuGet packages: source packages compile their files from the
/// package folder ('contentFiles', or a package's build targets adding them), where nobody can fix them, and 'dotnet
/// format' would edit the shared package cache (Spectre.Console: BRO1306 and BRO1133 in wcwidth.sources). The package's
/// build targets pass the restore's package folders (<see cref="Property"/>, '|'-separated); without them, a path with a
/// 'contentFiles' folder counts. Not the project directory: shared source linked from elsewhere in the repository
/// (OpenTelemetry's src/Shared) is the user's own code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PackageFileSuppressor : DiagnosticSuppressor
{
    /// <summary>The package folders, from the package's build targets (NuGetPackageFolders).</summary>
    public const string Property = "build_property.StyleBroPackageFolders";

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = BaselineSuppressor.BroIds()
        .ToImmutableDictionary(id => id, id => new SuppressionDescriptor("PACKAGE_" + id, id, "In a file from a NuGet package"));

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.ToImmutableArray();

    /// <summary>Whether <paramref name="path"/> is in one of the package folders or, without them, in a 'contentFiles' folder.</summary>
    public static bool IsPackageFile(string path, string? folders)
    {
        var file = path.Replace('\\', '/');
        var roots = (folders ?? string.Empty).Split('|').Select(f => f.Trim().Replace('\\', '/').TrimEnd('/')).Where(f => f.Length > 0).ToList();
        return roots.Count > 0
            ? roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
            : file.IndexOf("/contentFiles/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Property, out var folders);

        // Decided once per file.
        var packageFiles = new Dictionary<SyntaxTree, bool>();
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Location.SourceTree is not { } tree || !Descriptors.TryGetValue(diagnostic.Id, out var descriptor))
            {
                continue;
            }

            if (!packageFiles.TryGetValue(tree, out var isPackageFile))
            {
                packageFiles[tree] = isPackageFile = IsPackageFile(tree.FilePath, folders);
            }

            if (isPackageFile)
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }
}
