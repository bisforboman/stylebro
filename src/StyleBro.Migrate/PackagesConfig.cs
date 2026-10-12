using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StyleBro.Migrate;

/// <summary>
/// StyleBro.Analyzers in a packages.config project, the way NuGet's packages.config install in Visual Studio writes it
/// (owner's decision 2026-10-11, docs/decisions.md): an entry in packages.config, an Analyzer item per DLL and an import
/// of the package's build targets (preset, baseline), guarded like NuGet's. A PackageReference instead switched such a
/// project to PackageReference restore, and its packages.config packages weren't restored any more
/// (xbox-live-developer-tools: a fresh clone didn't build).
/// </summary>
internal static class PackagesConfig
{
    /// <summary>
    /// The condition a PackageReference in a Directory.Build.props gets when packages.config projects import it: they keep
    /// packages.config restore. An SDK-style project (UsingMicrosoftNETSdk, set before Directory.Build.props) always gets it:
    /// NuGet ignores a stray packages.config there (Mapster.EFCore).
    /// </summary>
    public const string NotPackagesConfig = @"'$(UsingMicrosoftNETSdk)' == 'true' or !Exists('$(MSBuildProjectDirectory)\packages.config')";

    /// <summary>The package's analyzer DLLs, in the order NuGet lists them.</summary>
    private static readonly string[] Dlls = { "StyleBro.Analyzers.dll", "StyleBro.CodeFixes.dll" };

    /// <summary>The old-style projects (.csproj) with a packages.config next to them (SDK-style ones ignore it).</summary>
    public static List<string> Projects(IEnumerable<string> files) =>
        files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && FormatCommand.UsesPackagesConfig(f))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Installs StyleBro.Analyzers <paramref name="version"/> into a packages.config project (its packages.config and the project file).</summary>
    public static void Install(string project, string version)
    {
        var text = File.ReadAllText(project);
        var folder = $@"{PackagesFolder(project, text)}\StyleBro.Analyzers.{version}";
        var framework = Regex.Match(text, @"<TargetFrameworkVersion>\s*v(?<v>[\d.]+)\s*<") is { Success: true } m ? "net" + m.Groups["v"].Value.Replace(".", string.Empty) : null;
        PreviewCommand.EditFile(Path.Combine(Path.GetDirectoryName(project)!, "packages.config"), config => AddEntry(config!, version, framework));
        PreviewCommand.EditFile(project, csproj => AddToProject(csproj!, folder));
    }

    /// <summary>
    /// The packages folder relative to the project ('..\..\packages'): from a path the project already points into, else
    /// the folder next to the nearest solution above, else '..\packages'.
    /// </summary>
    public static string PackagesFolder(string project, string text)
    {
        if (Regex.Match(text, @"""(?<folder>(?:\.\.[\\/])+packages)[\\/]", RegexOptions.IgnoreCase) is { Success: true } used)
        {
            return used.Groups["folder"].Value.Replace('/', '\\');
        }

        for (var folder = Path.GetDirectoryName(Path.GetFullPath(project)); folder is not null; folder = Path.GetDirectoryName(folder))
        {
            if (Directory.EnumerateFiles(folder, "*.sln").Any())
            {
                return Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(project))!, Path.Combine(folder, "packages")).Replace('/', '\\');
            }
        }

        return @"..\packages";
    }

    /// <summary>packages.config with the StyleBro.Analyzers entry, in id order like NuGet keeps it (unchanged when it's there).</summary>
    public static string AddEntry(string config, string version, string? framework)
    {
        if (Regex.IsMatch(config, @"<package\b[^>]*\bid=""StyleBro\.Analyzers""", RegexOptions.IgnoreCase))
        {
            return config;
        }

        var newLine = config.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var entry = $"<package id=\"StyleBro.Analyzers\" version=\"{version}\"{(framework is null ? string.Empty : $" targetFramework=\"{framework}\"")} developmentDependency=\"true\" />";
        var packages = Regex.Matches(config, @"^(?<indent>[ \t]*)<package\b[^>]*\bid=""(?<id>[^""]+)""[^>]*>", RegexOptions.Multiline);
        if (packages.Count == 0)
        {
            var end = config.LastIndexOf("</packages>", StringComparison.OrdinalIgnoreCase);
            return end < 0 ? config : config.Insert(end, "  " + entry + newLine);
        }

        var next = packages.FirstOrDefault(p => string.Compare(p.Groups["id"].Value, "StyleBro.Analyzers", StringComparison.OrdinalIgnoreCase) > 0);
        var indent = packages[0].Groups["indent"].Value;
        return next is not null
            ? config.Insert(next.Index, indent + entry + newLine)
            : config.Insert(packages[^1].Index + packages[^1].Length, newLine + indent + entry);
    }

    /// <summary>
    /// The project with the Analyzer items (next to the other Analyzer items, else in an ItemGroup of their own before the
    /// C# targets import), the build targets import before '&lt;/Project&gt;', and its line in EnsureNuGetPackageBuildImports.
    /// Unchanged when it already points into a StyleBro.Analyzers package folder.
    /// </summary>
    public static string AddToProject(string project, string folder)
    {
        if (project.Contains(@"\StyleBro.Analyzers.", StringComparison.OrdinalIgnoreCase))
        {
            return project;
        }

        var newLine = project.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var analyzers = string.Concat(Dlls.Select(d => $"    <Analyzer Include=\"{folder}\\analyzers\\dotnet\\cs\\{d}\" />{newLine}"));
        var last = Regex.Matches(project, @"^[ \t]*<Analyzer\b[^>]*/>[ \t]*\r?\n", RegexOptions.Multiline).LastOrDefault();
        if (last is not null)
        {
            project = project.Insert(last.Index + last.Length, analyzers);
        }
        else
        {
            var at = Regex.Match(project, @"^[ \t]*<Import\b[^>]*Microsoft\.CSharp\.targets", RegexOptions.Multiline | RegexOptions.IgnoreCase) is { Success: true } import ? import.Index : project.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
            project = project.Insert(at, $"  <ItemGroup>{newLine}{analyzers}  </ItemGroup>{newLine}");
        }

        var targets = $@"{folder}\build\StyleBro.Analyzers.targets";
        var error = $"    <Error Condition=\"!Exists('{targets}')\" Text=\"$([System.String]::Format('$(ErrorText)', '{targets}'))\" />{newLine}";
        var ensure = Regex.Match(project, @"<Target\b[^>]*\bName=""EnsureNuGetPackageBuildImports""[^>]*>.*?(?=^[ \t]*</Target>)", RegexOptions.Singleline | RegexOptions.Multiline);
        var tail = $"  <Import Project=\"{targets}\" Condition=\"Exists('{targets}')\" />{newLine}";
        if (ensure.Success)
        {
            project = project.Insert(ensure.Index + ensure.Length, error);
        }
        else
        {
            tail += $"  <Target Name=\"EnsureNuGetPackageBuildImports\" BeforeTargets=\"PrepareForBuild\">{newLine}"
                + $"    <PropertyGroup>{newLine}"
                + $"      <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {{0}}.</ErrorText>{newLine}"
                + $"    </PropertyGroup>{newLine}"
                + error
                + $"  </Target>{newLine}";
        }

        var end = project.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
        return project.Insert(end, tail);
    }
}
