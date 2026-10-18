using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Issues reported on GitHub (#177, #179).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Issue177_SetPreset_ExpandsASelfClosingProjectBelowAComment()
    {
        var props = "<!-- Stops MSBuild from importing the root one. -->\n<Project />\n";
        var written = Migration.SetPreset(props, "1.0", "comment")!;
        Assert.StartsWith("<!-- Stops MSBuild from importing the root one. -->\n<Project>\n", written, StringComparison.Ordinal);
        Assert.EndsWith("</Project>\n", written, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(written, "<Project"));
    }

    [Fact]
    public void Issue177_Init_LeavesAPropsThatStopsTheRootImport_WhenNothingUnderItUsesStyleBro()
    {
        Write("Directory.Build.props", "<Project><ItemGroup><PackageReference Include=\"StyleBro.Analyzers\" Version=\"0.7.0-alpha.1\" PrivateAssets=\"all\" /></ItemGroup></Project>\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n");
        Write("App/A.cs", "class A { }\n");
        Write("sub/Sub.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n");
        Write("sub/B.cs", "class B { }\n");
        var sub = "<!-- Stops MSBuild from importing the root one. -->\n<Project />\n";
        Write("sub/Directory.Build.props", sub);

        Capture(() => InitCommand.Run(new[] { root, "--write" }));

        Assert.Equal(sub, File.ReadAllText(Path.Combine(root, "sub", "Directory.Build.props")));
        Assert.Contains($"<StyleBroPreset>{PresetVersions.Latest}</StyleBroPreset>", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void Issue179_Init_LeavesBRO1606On_ForConstructorSummariesInTheirOwnWords()
    {
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>\n");
        foreach (var name in new[] { "PA", "PB", "PC", "PD" })
        {
            Write($"App/{name}.cs", $"/// <summary>A thing {name}.</summary>\npublic sealed class {name}\n{{\n    /// <summary>Makes a thing at the origin.</summary>\n    public {name}() {{ }}\n}}\n");
        }

        Capture(() => InitCommand.Run(new[] { root, "--write" }));

        Assert.DoesNotContain("BRO1606", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
    }
}
