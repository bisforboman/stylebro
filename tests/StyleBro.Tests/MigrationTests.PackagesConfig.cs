using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>StyleBro.Analyzers in packages.config projects, installed the way NuGet does it there (owner's decision 2026-10-11).</summary>
public sealed partial class MigrationTests
{
    private const string OldProject = """
        <?xml version="1.0" encoding="utf-8"?>
        <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <PropertyGroup>
            <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
          </PropertyGroup>
          <ItemGroup>
            <Reference Include="Newtonsoft.Json">
              <HintPath>..\packages\Newtonsoft.Json.13.0.1\lib\net45\Newtonsoft.Json.dll</HintPath>
            </Reference>
          </ItemGroup>
          <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
        </Project>

        """;

    [Fact]
    public void AddPackage_InstallsItThePackagesConfigWay_AndKeepsThePropsReferenceAway()
    {
        // xbox: a PackageReference from Directory.Build.props switched the packages.config projects to PackageReference
        // restore; a fresh clone didn't restore their packages.config packages and didn't build.
        Write("App.sln", string.Empty);
        Write("Old/Old.csproj", OldProject);
        Write("Old/packages.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<packages>\n  <package id=\"Newtonsoft.Json\" version=\"13.0.1\" targetFramework=\"net462\" />\n  <package id=\"System.Runtime\" version=\"4.3.0\" targetFramework=\"net462\" />\n</packages>\n");
        Write("New/New.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var added = PreviewCommand.AddPackage(root);

        Assert.Contains("the packages.config way to Old/Old.csproj", added, StringComparison.Ordinal);
        var version = File.ReadAllText(Path.Combine(root, "Old/packages.config")).Split("StyleBro.Analyzers\" version=\"")[1].Split('"')[0];
        Assert.Equal(
            $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<packages>\n  <package id=\"Newtonsoft.Json\" version=\"13.0.1\" targetFramework=\"net462\" />\n  <package id=\"StyleBro.Analyzers\" version=\"{version}\" targetFramework=\"net462\" developmentDependency=\"true\" />\n  <package id=\"System.Runtime\" version=\"4.3.0\" targetFramework=\"net462\" />\n</packages>\n",
            File.ReadAllText(Path.Combine(root, "Old/packages.config")));
        var project = File.ReadAllText(Path.Combine(root, "Old/Old.csproj"));
        var folder = $@"..\packages\StyleBro.Analyzers.{version}";
        Assert.Contains($"    <Analyzer Include=\"{folder}\\analyzers\\dotnet\\cs\\StyleBro.Analyzers.dll\" />\n    <Analyzer Include=\"{folder}\\analyzers\\dotnet\\cs\\StyleBro.CodeFixes.dll\" />\n  </ItemGroup>\n  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />", project, StringComparison.Ordinal);
        Assert.Contains($"  <Import Project=\"{folder}\\build\\StyleBro.Analyzers.targets\" Condition=\"Exists('{folder}\\build\\StyleBro.Analyzers.targets')\" />\n  <Target Name=\"EnsureNuGetPackageBuildImports\"", project, StringComparison.Ordinal);
        Assert.Contains($"    <Error Condition=\"!Exists('{folder}\\build\\StyleBro.Analyzers.targets')\"", project, StringComparison.Ordinal);
        Assert.Contains($"PrivateAssets=\"all\" Condition=\"{PackagesConfig.NotPackagesConfig}\" />", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);

        // Installed once: a second install changes nothing.
        PackagesConfig.Install(Path.Combine(root, "Old/Old.csproj"), version);
        Assert.Equal(project, File.ReadAllText(Path.Combine(root, "Old/Old.csproj")));
    }

    [Fact]
    public void PackagesConfig_GoesNextToTheProjectsOtherAnalyzers_AndIntoNuGetsTarget()
    {
        // Like StyleCop 1.0.2 in xbox: next to its Analyzer items, a line in EnsureNuGetPackageBuildImports.
        var project = "<Project>\r\n  <ItemGroup>\r\n    <Analyzer Include=\"..\\..\\packages\\Other.1.0\\analyzers\\dotnet\\cs\\Other.dll\" />\r\n  </ItemGroup>\r\n"
            + "  <Target Name=\"EnsureNuGetPackageBuildImports\" BeforeTargets=\"PrepareForBuild\">\r\n    <Error Condition=\"x\" />\r\n  </Target>\r\n</Project>\r\n";

        var result = PackagesConfig.AddToProject(project, @"..\..\packages\StyleBro.Analyzers.1.0.0");

        Assert.Contains("Other.dll\" />\r\n    <Analyzer Include=\"..\\..\\packages\\StyleBro.Analyzers.1.0.0\\analyzers\\dotnet\\cs\\StyleBro.Analyzers.dll\" />\r\n", result, StringComparison.Ordinal);
        Assert.Contains("    <Error Condition=\"x\" />\r\n    <Error Condition=\"!Exists('..\\..\\packages\\StyleBro.Analyzers.1.0.0\\build\\StyleBro.Analyzers.targets')\"", result, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(result, "EnsureNuGetPackageBuildImports"));
        Assert.EndsWith("Condition=\"Exists('..\\..\\packages\\StyleBro.Analyzers.1.0.0\\build\\StyleBro.Analyzers.targets')\" />\r\n</Project>\r\n", result, StringComparison.Ordinal);
        Assert.Equal(@"..\..\packages", PackagesConfig.PackagesFolder("x.csproj", project));

        // No path into the packages folder yet: the one next to the solution.
        Write("src/App/App.csproj", "<Project />");
        Write("App.sln", string.Empty);
        Assert.Equal(@"..\..\packages", PackagesConfig.PackagesFolder(Path.Combine(root, "src/App/App.csproj"), "<Project />"));
    }
}
