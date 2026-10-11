using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the eighth trial for the tool (xbox-live-developer-tools, StabilityMatrix, Orleans).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void LineEndings_FilesGitConverts_OutvoteAFewCommittedWithAFixedEnding()
    {
        // xbox: 145 files 'i/lf w/crlf attr/text=auto', 3 committed as CRLF. 'end_of_line = crlf' from the 3 gave 12,593
        // ENDOFLINE errors in an LF checkout.
        Write(".gitattributes", "*.cs -text\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\r\n{{\r\n}}\r\n");
        }

        Git("init", "-q");
        Git("add", ".");
        Git("-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "x");
        Write(".gitattributes", "*.cs text=auto\n");
        for (var i = 0; i < 4; i++)
        {
            Write($"B{i}.cs", $"class B{i}\r\n{{\r\n}}\r\n");
        }

        Git("add", ".");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("end_of_line", StringComparison.Ordinal));
        Assert.Contains("Line endings: git converts 4 of 7 C# files on checkout, so end_of_line isn't set (each machine keeps its own ending; the 3 files committed with a fixed ending don't decide)", result.Notes);

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("  git      C# files' line endings: 3 of 3 CRLF -> git converts 4 of 7 C# files on checkout", output);
        Assert.DoesNotContain("end_of_line = ", output);
        Assert.DoesNotContain("note     line endings", output);

        // More files with a fixed ending than converted ones: those decide again.
        for (var i = 3; i < 8; i++)
        {
            Write($"A{i}.cs", $"class A{i}\r\n{{\r\n}}\r\n");
        }

        Assert.Contains("end_of_line = crlf", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Theory]
    [InlineData("CommandLine/App/packages.config", """<?xml version="1.0" encoding="utf-8"?><packages><package id="StyleCop.Analyzers" version="1.0.2" targetFramework="net462" developmentDependency="true" /></packages>""")]
    [InlineData("CommandLine/App/App.csproj", """<Project ToolsVersion="15.0"><ItemGroup><Analyzer Include="..\..\packages\StyleCop.Analyzers.1.0.2\analyzers\dotnet\cs\StyleCop.Analyzers.dll" /></ItemGroup></Project>""")]
    public void StyleCop10_FromPackagesConfigOrAHintPath_TurnsOffRulesItDoesntHave(string file, string content)
    {
        // xbox: SA1135, SA1136, SA1137, SA1139, SA1314 and SA1413 came on and gave 94 findings in StyleCop-clean code.
        Write(file, content);

        var setup = StyleCopSetup.Read(root);

        Assert.Equal("1.0.2", setup.Version);
        foreach (var id in new[] { "SA1135", "SA1136", "SA1137", "SA1139", "SA1314", "SA1413", "SA1141" })
        {
            Assert.False(setup.IsOn(id), id);
        }

        Assert.True(setup.IsOn("SA1642"));
        Assert.Null(StyleCopSetup.OldRules("1.2.0-beta.556"));
        Assert.Contains("SA1413", StyleCopSetup.OldRules("1.1.118")!);
    }

    [Fact]
    public void AddPackage_FollowsANestedPropsImportThroughAProperty()
    {
        // Orleans: src/, test/ and playground/Directory.Build.props import the root's through $(_ParentDirectoryBuildPropsPath).
        // The reference went into all four: NU1504 (duplicate PackageReference).
        const string Nested = """
            <Project>
              <PropertyGroup>
                <_ParentDirectoryBuildPropsPath Condition="'$(_DirectoryBuildPropsFile)' != ''">$([System.IO.Path]::Combine('..', '$(_DirectoryBuildPropsFile)'))</_ParentDirectoryBuildPropsPath>
              </PropertyGroup>
              <Import Project="$(_ParentDirectoryBuildPropsPath)" Condition="Exists('$(_ParentDirectoryBuildPropsPath)')"/>
            </Project>
            """;
        Write("Directory.Build.props", "<Project>\n</Project>\n");
        Write("src/Directory.Build.props", Nested);
        Write("test/Directory.Build.props", Nested);
        Write("src/A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("test/B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal(new[] { "Directory.Build.props" }, Migration.PropsFiles(root).Select(f => Path.GetRelativePath(root, f)));
        Assert.StartsWith("StyleBro.Analyzers ", PreviewCommand.AddPackage(root), StringComparison.Ordinal);
        Assert.Contains("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "src/Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBro.Analyzers", File.ReadAllText(Path.Combine(root, "test/Directory.Build.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void AddPackage_GivesProjectsWithoutCentralPackageManagementAVersion()
    {
        // StabilityMatrix: analyzers/*.csproj set ManagePackageVersionsCentrally to false; the version-less reference from
        // Directory.Build.props failed their restore (NU1015).
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");
        Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Avalonia\" Version=\"11.0.0\" />\n  </ItemGroup>\n</Project>\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("analyzers/Gen/Gen.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n</Project>\n");

        var added = PreviewCommand.AddPackage(root);

        Assert.Contains("with a version for analyzers/Gen/Gen.csproj", added, StringComparison.Ordinal);
        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" Condition=\"'$(ManagePackageVersionsCentrally)' == 'true'\" />", props, StringComparison.Ordinal);
        Assert.Matches("<PackageReference Include=\"StyleBro.Analyzers\" Version=\"[^\"]+\" PrivateAssets=\"all\" Condition=\"'\\$\\(ManagePackageVersionsCentrally\\)' != 'true'\" />", props);
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\"", File.ReadAllText(Path.Combine(root, "Directory.Packages.props")), StringComparison.Ordinal);
    }

    private void Git(params string[] arguments) => Assert.Equal(0, PreviewCommand.Git(root, arguments).Code);
}
