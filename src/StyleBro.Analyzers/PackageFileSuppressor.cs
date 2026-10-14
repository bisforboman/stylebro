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
/// (OpenTelemetry's src/Shared) is the user's own code. Files in the repository's git submodules too
/// (<see cref="SubmoduleProperty"/>): another repository's code a project compiles (SixLabors.Fonts' shared-infrastructure:
/// 88 warnings on code its own .editorconfig governs), which 'dotnet format' skips and nobody here can fix.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PackageFileSuppressor : DiagnosticSuppressor
{
    /// <summary>The package folders, from the package's build targets (NuGetPackageFolders).</summary>
    public const string Property = "build_property.StyleBroPackageFolders";

    /// <summary>The repository's git submodule folders, from the package's build targets (.gitmodules).</summary>
    public const string SubmoduleProperty = "build_property.StyleBroSubmoduleFolders";

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = BaselineSuppressor.BroIds()
        .ToImmutableDictionary(id => id, id => new SuppressionDescriptor("PACKAGE_" + id, id, "In a file from a NuGet package or a git submodule"));

    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = Descriptors.Values.ToImmutableArray();

    /// <summary>Whether <paramref name="path"/> is in one of the package folders or, without them, in a 'contentFiles' folder.</summary>
    public static bool IsPackageFile(string path, string? folders)
    {
        var roots = Roots(folders);
        return roots.Count > 0 ? IsUnder(path, roots) : Normalize(path).IndexOf("/contentFiles/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Whether <paramref name="path"/> is in one of the submodule folders ('|'-separated).</summary>
    public static bool IsSubmoduleFile(string path, string? folders) => IsUnder(path, Roots(folders));

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Property, out var folders);
        context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(SubmoduleProperty, out var submodules);

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
                packageFiles[tree] = isPackageFile = IsPackageFile(tree.FilePath, folders) || IsSubmoduleFile(tree.FilePath, submodules);
            }

            if (isPackageFile)
            {
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    /// <summary>The '|'-separated folders, normalized (<see cref="Normalize"/>), without a trailing '/'.</summary>
    internal static List<string> Roots(string? folders) =>
        (folders ?? string.Empty).Split('|').Select(f => Normalize(f.Trim()).TrimEnd('/')).Where(f => f.Length > 0).ToList();

    /// <summary>Whether <paramref name="path"/>, normalized, is below one of <paramref name="roots"/>.</summary>
    internal static bool IsUnder(string path, List<string> roots)
    {
        var file = Normalize(path);
        return roots.Any(root => file.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <paramref name="path"/> with '/' separators and its '.' and '..' segments resolved: the compiler keeps a file's path as
    /// MSBuild wrote it ('src/App/../../ext/lib/Helper.cs' for a Compile item '..\..\ext\lib\*.cs', trial 19), which didn't
    /// start with the submodule's folder. Text only: an analyzer doesn't touch the file system.
    /// </summary>
    internal static string Normalize(string path)
    {
        var file = path.Replace('\\', '/');
        if (file.IndexOf("/.", StringComparison.Ordinal) < 0)
        {
            return file;
        }

        var parts = new List<string>();
        foreach (var part in file.Split('/'))
        {
            if (part == ".")
            {
                continue;
            }

            if (part == ".." && parts.Count > 1 && parts[parts.Count - 1] is not (".." or ""))
            {
                parts.RemoveAt(parts.Count - 1);
                continue;
            }

            parts.Add(part);
        }

        return string.Join("/", parts);
    }
}
