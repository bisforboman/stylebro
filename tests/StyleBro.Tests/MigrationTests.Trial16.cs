using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the sixteenth trial (tusdotnet, markdig, SimplCommerce, AspNet.Security.OAuth.Providers) found in the tool.</summary>
public sealed partial class MigrationTests
{
    private const string SourceTools = "{ \"version\": 1, \"isRoot\": true, \"tools\": { \"csharpier\": { \"version\": \"1.3.0\" } } }\n";

    [Fact]
    public void CSharpier_IsFoundWhereTheSolutionIs_AndCoversTheRepositoryWhenAllCodeIsThere()
    {
        // tusdotnet: the solution and .config/dotnet-tools.json are under Source/, and init from the root missed CSharpier.
        Write("Source/.config/dotnet-tools.json", SourceTools);
        Write("Source/tusdotnet.slnx", "<Solution />");
        Write("Source/tusdotnet/tusdotnet.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Source/tusdotnet/A.cs", "class A\n{\n}\n");

        Assert.Equal(("Source/.config/dotnet-tools.json", string.Empty), Conventions.CSharpierScope(root));

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var config = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("# init: CSharpier formats this repository (Source/.config/dotnet-tools.json):", config, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none\n", config, StringComparison.Ordinal);
        Assert.DoesNotContain("[Source/", config, StringComparison.Ordinal);
    }

    [Fact]
    public void CSharpier_InAFolderWithoutAProject_DoesNotCount()
    {
        Write("docs/.config/dotnet-tools.json", SourceTools);
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        Assert.Null(Conventions.CSharpier(root));
    }

    [Fact]
    public void CSharpier_FormattingOneFolder_GetsThatFoldersSection()
    {
        Write("Source/.csharpierrc.yaml", "printWidth: 110\n");
        Write("Source/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Source/Lib/A.cs", "class A\n{\n}\n");
        Write("Tools/Gen/Gen.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Tools/Gen/B.cs", "class B\n{\n}\n");

        Assert.Equal(("Source/.csharpierrc.yaml", "Source"), Conventions.CSharpierScope(root));

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))).Replace("\r\n", "\n");
        var config = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        var section = config.Substring(config.IndexOf("[Source/**.cs]\n", StringComparison.Ordinal));
        Assert.Contains("# init: CSharpier formats Source/ (Source/.csharpierrc.yaml):", section, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1107.severity = none\n", section, StringComparison.Ordinal);
        Assert.Contains("stylebro_layout_print_width = 110\n", section, StringComparison.Ordinal);

        // The rest is StyleBro's to format.
        var main = config.Substring(0, config.IndexOf("[Source/**.cs]", StringComparison.Ordinal));
        Assert.DoesNotContain("stylebro_layout_by", main, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = warning", main, StringComparison.Ordinal);
        Assert.Contains("  off      CSharpier in Source/ (Source/.csharpierrc.yaml) -> IDE0055 and", output, StringComparison.Ordinal);

        // The migration the same way.
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Equal("Source/**.cs", result.CSharpierSection?.Section);
        Assert.DoesNotContain("stylebro_layout_by = csharpier", result.Lines);
        Assert.Contains("[Source/**.cs]", Migration.Render(Migration.Plan(StyleCopSetup.Read(root), root, result)[".editorconfig"]), StringComparison.Ordinal);
    }

    [Fact]
    public void Init_WithCSharpier_WritesNoUsingSortKey()
    {
        // tusdotnet: init wrote dotnet_sort_system_directives_first = true, and 'dotnet format' and CSharpier swapped
        // 'AspNetCore_netcoreapp3._0_TestApp' and 'AspNetCore_netcoreapp3_0_TestApp' back and forth.
        Write(".csharpierrc", "{}\n");
        for (var i = 0; i < 12; i++)
        {
            Write($"F{i}.cs", $"using System;\nusing Alpha;\n\nclass F{i}\n{{\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))).Replace("\r\n", "\n");
        var config = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        Assert.DoesNotContain("dotnet_sort_system_directives_first", config, StringComparison.Ordinal);
        Assert.Contains("  set      using directives: 12 of 12 files sorted", output, StringComparison.Ordinal);
        Assert.Contains("-> CSharpier sorts them", output, StringComparison.Ordinal);

        // StyleCop's SA1208/SA1210 don't bring the keys back in the migration either.
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("dotnet_sort_system_directives_first", StringComparison.Ordinal));
    }

    [Fact]
    public void NewFiles_GetTheLineEndingsOfTheFilesAroundThem()
    {
        // tusdotnet (CRLF checkout): init's new Source/Directory.Build.props had LF, and CSharpier's check rejected it.
        Write("Source/tusdotnet.sln", "Microsoft Visual Studio Solution File\r\n# Visual Studio\r\n");
        Write("Source/run-tests.ps1", "dotnet test\r\n");
        var props = Path.Combine(root, "Source", "Directory.Build.props");

        PreviewCommand.EditFile(props, _ => "<Project>\n</Project>\n");

        Assert.Equal("<Project>\r\n</Project>\r\n", File.ReadAllText(props));
        Assert.Equal("\n", PreviewCommand.FolderLineEnding(Path.Combine(root, "Other", "New.props")) ?? "\n");
    }

    [Fact]
    public void AddPackage_WithCentralPackagesOnlyBelowTheProps_WritesTheConditionalPair()
    {
        // markdig: no Directory.Build.props, src/Directory.Packages.props turns central package management on for src/ only;
        // the root props with a versioned reference failed the restore of all 8 projects (NU1008).
        Write("src/Directory.Packages.props", "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n  </PropertyGroup>\n  <ItemGroup>\n    <PackageVersion Include=\"NUnit\" Version=\"4.6.1\" />\n  </ItemGroup>\n</Project>\n");
        Write("src/Markdig/Markdig.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("tools/Diff/Diff.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var added = PreviewCommand.AddPackage(root);

        Assert.Contains("(version in src/Directory.Packages.props)", added, StringComparison.Ordinal);
        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" Condition=\"'$(ManagePackageVersionsCentrally)' == 'true'\" />", props, StringComparison.Ordinal);
        Assert.Matches("<PackageReference Include=\"StyleBro.Analyzers\" Version=\"[^\"]+\" PrivateAssets=\"all\" Condition=\"'\\$\\(ManagePackageVersionsCentrally\\)' != 'true'\" />", props);
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\"", File.ReadAllText(Path.Combine(root, "src", "Directory.Packages.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void AddPackageVersion_SkipsConditionalItemGroups()
    {
        // markdig: the version landed in the ItemGroup for net462/netstandard2.0, and the other frameworks failed (NU1010). A
        // conditional item isn't a neighbour either.
        const string Packages = "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"NUnit\" Version=\"4\" />\n    <PackageVersion Include=\"Swift\" Version=\"1\" Condition=\"'$(X)' == 'y'\" />\n  </ItemGroup>\n  <ItemGroup Condition=\" '$(TargetFramework)' == 'net462'\">\n    <PackageVersion Include=\"System.Memory\" Version=\"4.6.3\" />\n  </ItemGroup>\n  <ItemGroup>\n    <PackageVersion Include=\"Zed\" Version=\"1\" />\n  </ItemGroup>\n</Project>\n";

        Assert.Equal(
            Packages.Replace("    <PackageVersion Include=\"Zed\"", "    <PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />\n    <PackageVersion Include=\"Zed\"", StringComparison.Ordinal),
            PreviewCommand.AddPackageVersion(Packages, "<PackageVersion Include=\"StyleBro.Analyzers\" Version=\"1\" />"));
    }

    [Fact]
    public void Preview_LeavesPackagesConfigProjectsOut_WhenOthersRemain()
    {
        // tusdotnet: one Owin_net452 test site with packages.config refused the whole preview.
        Write("App.slnx", "<Solution />");
        Write("Old/Old.csproj", OldProject);
        Write("Old/packages.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<packages>\n</packages>\n");
        Write("New/New.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("New/C.cs", "class C\n{\n}\n");
        PreviewCommand.MakeRepository(root);
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        int Format(string[] args, Action<string> log)
        {
            var report = Array.IndexOf(args, "--report");
            if (report >= 0)
            {
                Directory.CreateDirectory(args[report + 1]);
                File.WriteAllText(Path.Combine(args[report + 1], "format-report.json"), "[]");
            }

            log($"{FormatCommand.IncompleteMarker} 'dotnet format' skipped Old (references didn't load), so those files weren't formatted.");
            return 0;
        }

        var output = Capture(() => Assert.Equal(0, PreviewCommand.Run("init", new[] { root, "--diff=" + patch }, Format)));

        Assert.Contains("packages.config projects left out of the preview: Old/Old.csproj", output, StringComparison.Ordinal);
        Assert.Contains("Note: packages.config projects left out", output, StringComparison.Ordinal);

        // Trial 17: left out on purpose, so no "To include them: run 'dotnet restore'" (it can't restore them).
        Assert.DoesNotContain("WARNING", output, StringComparison.Ordinal);
        Assert.True(PreviewCommand.OnlyLeftOut("Incomplete: 'dotnet format' skipped Old (references didn't load)", ["Old/Old.csproj"]));
        Assert.False(PreviewCommand.OnlyLeftOut("Incomplete: 'dotnet format' skipped New, Old (references didn't load)", ["Old/Old.csproj"]));

        // Only packages.config projects: nothing to preview.
        File.Delete(Path.Combine(root, "New", "New.csproj"));
        var error = CaptureError(() => Assert.Equal(1, PreviewCommand.Run("init", new[] { root, "--diff=" + patch }, Format)));
        Assert.Contains("--diff doesn't work with packages.config projects (Old/Old.csproj)", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_CountDeclarationsWithSeveralAttributes_CombinedOrOnePerList()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            [A, B]
            class C
            {
                [A]
                [B]
                void M([A, B] int p) { }

                [A]
                [return: B]
                int N() => 0;

                [A]
                int O() => 0;

                [A, B]
                [C]
                int P;
            }
            """),
            counts);

        // Combined: class C, P; separate: M. (N's attributes have two targets, O has one, the parameter is BRO1102's exception.)
        Assert.Equal(new[] { 1, 2 }, counts["dotnet_diagnostic.BRO1102.severity"]);
    }

    [Theory]
    [InlineData(76, 42, "dotnet_diagnostic.BRO1102.severity = none")] // tusdotnet: mixed
    [InlineData(2, 40, "dotnet_diagnostic.BRO1102.severity = none")]  // mostly combined
    [InlineData(40, 2, null)]                                          // mostly separate
    [InlineData(0, 2, null)]                                           // too few
    public void Conventions_CombinedAttributes_AreFollowed(int separate, int combined, string? expected)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { ["dotnet_diagnostic.BRO1102.severity"] = new[] { separate, combined } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, lines);
    }

    [Fact]
    public void Conventions_OneLineJumps_SayWhySomeAreEnough()
    {
        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [Conventions.SingleLineJumpsKey] = new[] { 350, 3 } }, new Dictionary<string, string>());

        Assert.Contains("# init: one-line jumps ('if (x) return;'): allowed, because your code has some (3 of 353 places on the 'if' line without braces; any 3 keep them allowed: without it BRO1514 would add braces to each)", lines);
        Assert.Contains(report, r => r.EndsWith("-> true (3 are enough)", StringComparison.Ordinal));
    }
}
