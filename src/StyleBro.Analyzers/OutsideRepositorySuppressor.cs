using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers.Baseline;

namespace StyleBro.Analyzers;

/// <summary>
/// Hides StyleBro's findings, and those of the built-in rules its preset and 'bro init' turn on
/// (<see cref="BaselineSuppressor.SdkIds"/>), in files outside the project's git repository: a project that compiles
/// '..\..\outside\Shared.cs' from a sibling folder (trial 27). 'bro format' leaves such files alone (someone
/// else's code, like a submodule's), so without this its check said clean while the build failed on them. The package's
/// build targets pass the repository's folder (<see cref="Property"/>: the nearest folder above the project with a '.git'
/// folder or file); without it, or with 'none', nothing is hidden. Unlike <see cref="PackageFileSuppressor"/>, which hides
/// only BRO findings (a package's file is compiled from the cache, where only StyleBro's own suggestions are noise), the
/// built-in rules' are hidden too: 'format' doesn't touch the file for them either, so they would fail the build the same way.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OutsideRepositorySuppressor : DiagnosticSuppressor
{
    /// <summary>The repository's folder, from the package's build targets.</summary>
    public const string Property = "build_property.StyleBroRepositoryRoot";

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = BaselineSuppressor.BroIds().Concat(BaselineSuppressor.SdkIds)
        .Distinct(StringComparer.Ordinal)
        .ToImmutableDictionary(id => id, id => new SuppressionDescriptor("OUTSIDE_" + id, id, "In a file outside the project's git repository"));

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.ToImmutableArray();

    /// <summary>Whether <paramref name="path"/> (a full path) is outside <paramref name="root"/>, the repository's folder.</summary>
    public static bool IsOutside(string path, string? root)
    {
        var roots = PackageFileSuppressor.Roots(root);
        return roots.Count == 1 && !roots[0].Equals("none", StringComparison.OrdinalIgnoreCase)
            && IsFullPath(PackageFileSuppressor.Normalize(path)) && !PackageFileSuppressor.IsUnder(path, roots);
    }

    // Text only, the same on every OS: Path.IsPathRooted on Linux doesn't take a drive letter.
    private static bool IsFullPath(string path) =>
        path.StartsWith("/", StringComparison.Ordinal) || (path.Length > 2 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '/');

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Property, out var root))
        {
            return;
        }

        // Decided once per file.
        var outside = new Dictionary<SyntaxTree, bool>();
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Location.SourceTree is not { } tree || !Descriptors.TryGetValue(diagnostic.Id, out var descriptor))
            {
                continue;
            }

            if (!outside.TryGetValue(tree, out var isOutside))
            {
                outside[tree] = isOutside = IsOutside(tree.FilePath, root);
            }

            if (isOutside)
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }
}
