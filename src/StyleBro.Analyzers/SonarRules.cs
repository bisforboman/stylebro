using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers;

/// <summary>
/// Whether one of Sonar's rules is on for a file, for fixes that must not write what it reports. A severity configured
/// for its id (.editorconfig, a global config, a ruleset) decides; otherwise the default of the SonarAnalyzer.CSharp
/// package the project uses, whose analyzer paths the package's build targets pass as <see cref="Property"/>.
/// </summary>
internal static class SonarRules
{
    /// <summary>The SonarAnalyzer.CSharp analyzer paths ('|'-separated), set by the package's build targets.</summary>
    public const string Property = "build_property.StyleBroSonar";

    // On in the package's defaults ("Sonar way") before 10.0, probed with the real package: on in 9.19.0.84025 and
    // 9.32.0.97167, off in 10.3.0.106239, 10.15.0.120848, 10.25.0.139117, 10.34.0.3385 and 10.35.0.4138.
    private static readonly string[] OnBeforeVersion10 = ["S6602", "S6603", "S6605"];

    public static bool IsOn(SemanticModel model, AnalyzerConfigOptions options, string id, CancellationToken cancellationToken)
    {
        var byDefault = Array.IndexOf(OnBeforeVersion10, id) >= 0
            && options.TryGetValue(Property, out var paths)
            && GetMajorVersion(paths) is < 10;
        return Severities.IsOn(model.Compilation.Options, model.SyntaxTree, id, cancellationToken, enabledByDefault: byDefault);
    }

    /// <summary>
    /// The package's major version from an analyzer path in the NuGet package folder
    /// ('.../sonaranalyzer.csharp/9.19.0.84025/analyzers/SonarAnalyzer.CSharp.dll'), or null (no package, another path).
    /// </summary>
    internal static int? GetMajorVersion(string paths)
    {
        foreach (var path in paths.Split('|'))
        {
            var parts = path.Split('/', '\\');
            for (var i = 0; i + 1 < parts.Length; i++)
            {
                if (string.Equals(parts[i], "sonaranalyzer.csharp", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[i + 1].Split('.')[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var major))
                {
                    return major;
                }
            }
        }

        return null;
    }
}
