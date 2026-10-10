using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What a trial of 0.5.0-alpha.1 on LiteBus found in stylebro-migrate.</summary>
public sealed partial class MigrationTests
{
    private const string OnlyDocumentation = """
        root = false

        [*.cs]
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.DocumentationRules.severity = warning
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.LayoutRules.severity = none
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.MaintainabilityRules.severity = none
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.NamingRules.severity = none
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.OrderingRules.severity = none
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.ReadabilityRules.severity = none
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.SpacingRules.severity = none
        dotnet_analyzer_diagnostic.category-StyleCop.CSharp.SpecialRules.severity = none

        """;

    [Fact]
    public void Settings_OnlyWhereStyleCopRuns_LikeLiteBus()
    {
        // LiteBus: src/Directory.Build.props adds StyleCop (central package management in src/), src/.editorconfig turns
        // everything but documentation off, the analyzer project removes it; tests, samples and benchmarks have none.
        // The migration wrote StyleCop's defaults for the whole repository: 'this.' added and usings sorted in the tests.
        Write(".editorconfig", "root = true\n\n[*]\ncsharp_space_after_cast = true\n");
        Write("src/Directory.Build.props", """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="StyleCop.Analyzers">
                  <PrivateAssets>all</PrivateAssets>
                </PackageReference>
              </ItemGroup>
            </Project>
            """);
        Write("src/Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("src/.editorconfig", OnlyDocumentation);
        Write("tests/.editorconfig", OnlyDocumentation);
        Write("tests/Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("src/LiteBus.Core/LiteBus.Core.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/LiteBus.Core/Bus.cs", "class Bus\n{\n    private int _count;\n    private int _total;\n}\n");
        Write("src/LiteBus.Events/LiteBus.Events.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/LiteBus.Analyzers/LiteBus.Analyzers.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>
              <ItemGroup><PackageReference Remove="StyleCop.Analyzers" /></ItemGroup>
            </Project>
            """);
        Write("tests/LiteBus.Tests/LiteBus.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("tests/LiteBus.Tests/Test.cs", "class Test\n{\n    private int count;\n    private int total;\n    private int sum;\n}\n");
        Write("samples/Sample/Sample.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);
        var plan = Migration.Plan(setup, root, result);

        Assert.Equal(new[] { "src/LiteBus.Core", "src/LiteBus.Events" }, setup.Folders);
        Assert.Equal(new[] { "samples", "src/LiteBus.Analyzers", "tests" }, setup.FoldersWithout);
        Assert.Empty(setup.Scopes); // src/.editorconfig is the base now, tests/.editorconfig has no StyleCop to follow

        // The settings StyleCop had there: documentation only.
        Assert.False(setup.IsOn("SA1101"));
        Assert.False(setup.IsOn("SA1309"));
        Assert.True(setup.IsOn("SA1629"));
        Assert.Contains("dotnet_diagnostic.IDE0009.severity = none", result.Lines);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1603.severity = warning", result.Lines);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("dotnet_sort_system_directives_first", StringComparison.Ordinal));

        // The field style is judged where StyleCop runs, with its settings there (the trial: "SA1309 is on").
        Assert.Contains("Private fields where StyleCop runs: 2 named '_field', 0 named 'field'; SA1309 is off where StyleCop runs, so BRO1303 uses '_camelCase'.", result.Notes);

        // One block, in a section for those folders only.
        var file = Assert.Single(plan);
        Assert.Equal(".editorconfig", file.Key);
        Assert.Equal("src/{LiteBus.Core,LiteBus.Events}/**.cs", Assert.Single(file.Value).Section);
        var block = Migration.Render(file.Value);
        Assert.True(Applies(block, "src/LiteBus.Core/Bus.cs"));
        Assert.True(Applies(block, "src/LiteBus.Events/Deep/Handler.cs"));
        Assert.False(Applies(block, "src/LiteBus.Analyzers/Analyzer.cs"));
        Assert.False(Applies(block, "tests/LiteBus.Tests/Test.cs"));
        Assert.False(Applies(block, "samples/Sample/Program.cs"));

        // The report says where, and where the package swap goes.
        Assert.Equal(
            new[]
            {
                "StyleCop runs only in src/LiteBus.Core, src/LiteBus.Events: the settings apply there, with the rules StyleCop has on there.",
                "No StyleCop in samples, src/LiteBus.Analyzers, tests: no rule changes there. Once StyleCop is gone, 'stylebro-migrate init' can set those up.",
            },
            Program.ScopeReport(setup));
        var hints = Program.PackageHints(setup);
        Assert.StartsWith("StyleCop.Analyzers is referenced in src/Directory.Build.props: ", hints[0]);
        Assert.StartsWith("src/LiteBus.Analyzers/LiteBus.Analyzers.csproj removes the StyleCop.Analyzers reference", hints[1]);

        // --write leaves the folders without StyleCop alone.
        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));
        Assert.DoesNotContain(Migration.BeginMarker, File.ReadAllText(Path.Combine(root, "tests/.editorconfig")));
        Assert.DoesNotContain(Migration.BeginMarker, File.ReadAllText(Path.Combine(root, "src/.editorconfig")));
        Assert.Contains("[src/{LiteBus.Core,LiteBus.Events}/**.cs]\n# StyleBro rules", File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Settings_WhenEveryProjectOrNoneRunsStyleCop_ApplyEverywhere()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("tests/App.Tests/App.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Assert.Null(StyleCopSetup.Read(root).Folders);

        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        var setup = StyleCopSetup.Read(root);
        Assert.Null(setup.Folders);
        Assert.Equal(Migration.MainSection, Assert.Single(Migration.Plan(setup, root, Migration.Generate(setup, root))[".editorconfig"]).Section);
    }

    [Fact]
    public void AScopeAboveSomeStyleCopFolders_MovesToASectionForThem()
    {
        // StyleCop in lib/A, lib/B and app/C; lib/.editorconfig turns SA1101 off for lib's projects only.
        Write("lib/A/A.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("lib/B/B.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("lib/Tool/Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("app/C/C.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("lib/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\n");
        Write("lib/A/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1413.severity = none\n");

        var setup = StyleCopSetup.Read(root);

        Assert.Equal(new[] { "app", "lib/A", "lib/B" }, setup.Folders);
        Assert.True(setup.IsOn("SA1101")); // the base: lib's scope doesn't cover app/C
        Assert.Equal(
            new[] { (Path.Combine("lib", "A", ".editorconfig"), "*.cs"), (".editorconfig", "lib/{A,B}/**.cs") },
            setup.Scopes.Select(s => (s.File, s.Section)).OrderBy(s => s.Section, StringComparer.Ordinal));
        Assert.Equal(@"{a\[1\],b\,c}/**.cs", StyleCopSetup.SectionFor(new[] { "a[1]", "b,c" }));
    }

    [Fact]
    public void ScopesThatDontSortUsings_DontGetTheSortKeysFromTheMainBlock()
    {
        // 'dotnet format' sorts usings wherever a sort key is set, whatever its value: tests sorted although SA1208/SA1210 were off there.
        Write("tests/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1208.severity = none\ndotnet_diagnostic.SA1210.severity = none\n");
        Write("src/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\n");

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);
        var plan = Migration.Plan(setup, root, result);

        Assert.DoesNotContain(result.Lines, l => l.StartsWith("dotnet_sort_system_directives_first", StringComparison.Ordinal));
        Assert.Contains("dotnet_sort_system_directives_first = true", plan[Path.Combine("src", ".editorconfig")].Single().Lines);
        Assert.False(plan.TryGetValue(Path.Combine("tests", ".editorconfig"), out var tests) && tests.Any(b => b.Lines.Any(l => l.StartsWith("dotnet_sort", StringComparison.Ordinal))));
    }

    [Fact]
    public void ThePackage_GoesNextToStyleCop_WithItsCentralPackageManagement_LikeLiteBus()
    {
        // LiteBus: CPM is on through src/Directory.Build.props with src/Directory.Packages.props; a version-less reference in
        // src/Directory.Build.props and no PackageVersion failed the restore (NU1008 for the version on a reference).
        Write("src/Directory.Build.props", """
            <Project>
              <PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup>
              <ItemGroup><PackageReference Include="StyleCop.Analyzers" PrivateAssets="all" /></ItemGroup>
            </Project>
            """);
        Write("src/Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("src/Core/Core.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/Analyzers/Analyzers.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>
              <ItemGroup><PackageReference Remove="StyleCop.Analyzers" /></ItemGroup>
            </Project>
            """);
        Write("tests/Directory.Build.props", "<Project />");
        Write("tests/Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var added = PreviewCommand.AddPackage(root);

        Assert.NotNull(added);
        Assert.Contains("removed again in src/Analyzers/Analyzers.csproj", added);
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\" Version=", File.ReadAllText(Path.Combine(root, "src/Directory.Packages.props")));
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />", File.ReadAllText(Path.Combine(root, "src/Directory.Build.props")));
        Assert.Contains("<PackageReference Remove=\"StyleBro.Analyzers\" />", File.ReadAllText(Path.Combine(root, "src/Analyzers/Analyzers.csproj")));
        Assert.Equal("<Project />", File.ReadAllText(Path.Combine(root, "tests/Directory.Build.props")));
        Assert.False(File.Exists(Path.Combine(root, "Directory.Build.props")));

        // Without central package management for a file: the version on the reference.
        Assert.Null(PreviewCommand.CentralPackages(Path.Combine(root, "src/Analyzers/Analyzers.csproj"), root));
        Assert.Null(PreviewCommand.CentralPackages(Path.Combine(root, "tests/Directory.Build.props"), root));
        Assert.Equal(Path.Combine(root, "src", "Directory.Packages.props"), PreviewCommand.CentralPackages(Path.Combine(root, "src/Directory.Build.props"), root));
    }

    [Fact]
    public void Format_SkipsTheWhitespacePass_WhereTheSettingsTurnIDE0055Off()
    {
        Assert.False(FormatCommand.WhitespaceOff(root)); // nothing says so

        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.IDE0055.severity = none\n");
        Assert.False(FormatCommand.WhitespaceOff(root)); // not stylebro's settings

        Write(".editorconfig", Migration.Render(new[] { ("{src/A}/**.cs", new List<string> { "dotnet_diagnostic.IDE0055.severity = none" }) }));
        Assert.True(FormatCommand.WhitespaceOff(root));

        Write("src/B/.editorconfig", Migration.Render(new[] { ("*.cs", new List<string> { "dotnet_diagnostic.IDE0055.severity = warning" }) }));
        Assert.False(FormatCommand.WhitespaceOff(root));
    }

    [Fact]
    public void Format_NamesFilesWithOldMacLineEndings()
    {
        // LiteBus: every run added "\r\n" to such a file, and format gave up after 3 runs ("please report it").
        Write("Mac.cs", "class A\r{\r}\r");
        Write("Windows.cs", "class B\r\n{\r\n}\r\n");
        Write("Unix.cs", "class C\n{\n}\n");
        Write("sub/Stray.cs", "class D\n{\r}\n");

        Assert.Equal(new[] { "Mac.cs", "sub/Stray.cs" }, FormatCommand.OldMacLineEndings(root));
    }

    [Fact]
    public void Verify_AnUnknownOption_ShowsFormatsError_NotACopyFailure()
    {
        Write("C.cs", "class C { }\n");
        var lines = new List<string>();
        var runs = 0;

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes", "--bogus" }, lines.Add, (args, output) =>
        {
            runs++;
            output?.Invoke("Unrecognized command or argument '--bogus'.");
            return 1;
        });

        Assert.Equal(1, code);
        Assert.Equal(1, runs);
        Assert.Contains("Unrecognized command or argument '--bogus'.", lines);
        Assert.DoesNotContain(lines, l => l.Contains("formatting a copy", StringComparison.Ordinal));
    }

    [Fact]
    public void Format_ARestoreFailure_SaysHowToSeeWhy()
    {
        Assert.True(FormatCommand.IsRestoreFailure("Unhandled exception: System.Exception: Restore operation failed."));
        Assert.False(FormatCommand.IsRestoreFailure("Formatted code file 'A.cs'."));
        Assert.Contains("run 'dotnet restore' to see why", FormatCommand.RestoreHint);
        Assert.Contains("--no-restore", FormatCommand.RestoreHint);
    }

    /// <summary>Whether the .editorconfig text (at the root) applies to the file (relative), per Roslyn's own matching.</summary>
    private bool Applies(string editorConfig, string file)
    {
        var config = AnalyzerConfig.Parse("root = true\n" + editorConfig, Path.Combine(root, ".editorconfig"));
        var options = AnalyzerConfigSet.Create(ImmutableArray.Create(config)).GetOptionsForSourcePath(Path.Combine(root, file));
        return options.TreeOptions.ContainsKey("IDE0055") || options.AnalyzerOptions.ContainsKey("dotnet_diagnostic.ide0055.severity");
    }
}
