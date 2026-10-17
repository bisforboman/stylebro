using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers.Baseline;

namespace StyleBro.Analyzers;

/// <summary>
/// Hides StyleBro's findings in source generator output: 'dotnet format' can neither fix nor report a generated
/// document (so 'stylebro-migrate baseline' can't record it), and the generator writes it again on every build.
/// Roslyn treats a file as generated only by its name (.g.cs, .designer.cs, ...) or an auto-generated header, which
/// generators don't have to write (RestSharp's ReadOnlyRestClientOptions.cs: about 950 warnings across 6 frameworks).
/// The compiler puts every generated file under the project's intermediate output folder (obj/), or under
/// CompilerGeneratedFilesOutputPath when EmitCompilerGeneratedFiles is on; the package's build targets pass both
/// (<see cref="Property"/>, '|'-separated). Without them, a path with an 'obj' folder counts. The list also names the files
/// a T4 template writes (a Compile item with DependentUpon '*.tt' or AutoGen True; owner's choice 2026-10-17, trial 28:
/// ZLogger's MessageTemplate.Format.cs): the template writes them again, so they're generated code too, and the built-in
/// rules' findings (<see cref="BaselineSuppressor.SdkIds"/>) are hidden there as well, since 'stylebro-migrate format'
/// leaves such files alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratedFileSuppressor : DiagnosticSuppressor
{
    /// <summary>The generated file folders (and T4 templates' files), from the package's build targets.</summary>
    public const string Property = "build_property.StyleBroGeneratedFolders";

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = BaselineSuppressor.BroIds().Concat(BaselineSuppressor.SdkIds)
        .Distinct(StringComparer.Ordinal)
        .ToImmutableDictionary(id => id, id => new SuppressionDescriptor("GENERATED_" + id, id, "In a file a source generator or a T4 template wrote"));

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.ToImmutableArray();

    /// <summary>Whether <paramref name="path"/> is one of the files or in one of the folders or, without them, in an 'obj' folder.</summary>
    public static bool IsGeneratedFile(string path, string? folders)
    {
        var roots = PackageFileSuppressor.Roots(folders);
        return roots.Count > 0
            ? PackageFileSuppressor.IsUnder(path, roots) || roots.Contains(PackageFileSuppressor.Normalize(path), StringComparer.OrdinalIgnoreCase)
            : PackageFileSuppressor.Normalize(path).IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Property, out var folders);

        // Decided once per file.
        var generatedFiles = new Dictionary<SyntaxTree, bool>();
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Location.SourceTree is not { } tree || !Descriptors.TryGetValue(diagnostic.Id, out var descriptor))
            {
                continue;
            }

            if (!generatedFiles.TryGetValue(tree, out var isGenerated))
            {
                generatedFiles[tree] = isGenerated = IsGeneratedFile(tree.FilePath, folders);
            }

            if (isGenerated)
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }
}
