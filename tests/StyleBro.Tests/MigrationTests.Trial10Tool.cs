using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the tenth trial for the tool (actions/runner, openmcdf, GameDemo, CleanArchitecture).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_StyleBroLoads_WhenItsRestoreOutputHasThePackage_OrAPackagesConfigDllExists()
    {
        // actions/runner: format skipped the restore, the SDK-style projects' restore output had no StyleBro.Analyzers, and
        // 'dotnet format' said "clean" with ~3,000 BRO errors left for the build.
        Write("A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("A/obj/project.assets.json", "{ \"libraries\": { \"StyleBro.Analyzers/0.6.0\": {} } }");
        Write("B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("B/obj/project.assets.json", "{ \"libraries\": { \"Serilog/4.0.0\": {} } }");
        Assert.True(FormatCommand.LoadsStyleBro(Path.Combine(root, "A/A.csproj"), Path.Combine(root, "A/obj/project.assets.json")));
        Assert.False(FormatCommand.LoadsStyleBro(Path.Combine(root, "B/B.csproj"), Path.Combine(root, "B/obj/project.assets.json")));
        Assert.False(FormatCommand.LoadsStyleBro(Path.Combine(root, "B/B.csproj"), string.Empty));

        // Before the first restore ProjectAssetsFile is empty (openmcdf: the restore's props set it): the folder decides.
        Assert.Equal(Path.Combine("C:/r/artifacts/obj/A/", "project.assets.json"), FormatCommand.ParseProject("""{ "Properties": { "ProjectAssetsFile": "", "MSBuildProjectExtensionsPath": "C:/r/artifacts/obj/A/" } }""").AssetsFile);
        Assert.Equal("C:/r/A/obj/project.assets.json", FormatCommand.ParseProject("""{ "Properties": { "ProjectAssetsFile": "C:/r/A/obj/project.assets.json", "MSBuildProjectExtensionsPath": "C:/x/" } }""").AssetsFile);

        // packages.config: the Analyzer item counts once its DLL is there (restored).
        Write("Old/Old.csproj", "<Project ToolsVersion=\"15.0\">\n  <ItemGroup>\n    <Analyzer Include=\"..\\packages\\StyleBro.Analyzers.0.6.0\\analyzers\\dotnet\\cs\\StyleBro.Analyzers.dll\" />\n  </ItemGroup>\n</Project>\n");
        Assert.False(FormatCommand.LoadsStyleBro(Path.Combine(root, "Old/Old.csproj"), string.Empty));
        Write("packages/StyleBro.Analyzers.0.6.0/analyzers/dotnet/cs/StyleBro.Analyzers.dll", string.Empty);
        Assert.True(FormatCommand.LoadsStyleBro(Path.Combine(root, "Old/Old.csproj"), string.Empty));
    }

    [Fact]
    public void Format_WithoutStyleBroAnywhere_Stops_AndNamesTheProjectsItMissesOtherwise()
    {
        Assert.Null(FormatCommand.NotLoadedMessage(Array.Empty<string>(), all: false));
        Assert.StartsWith(FormatCommand.NotLoadedHeader, FormatCommand.NotLoadedMessage(new[] { "A/A.csproj" }, all: true), StringComparison.Ordinal);
        var partly = FormatCommand.NotLoadedMessage(new[] { "src/Runner.Service/Windows/RunnerService.csproj" }, all: false)!;
        Assert.StartsWith(FormatCommand.PartlyLoadedHeader + " src/Runner.Service/Windows/RunnerService.csproj has no StyleBro.Analyzers", partly, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WithoutStyleBroRestored_FailsInsteadOfReportingClean()
    {
        // A real evaluation (no restore): nothing in the restore output, so no 'dotnet format' run and exit code 1.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n");
        var lines = new List<string>();

        Assert.Equal(1, FormatCommand.Run(new[] { Path.Combine(root, "App/App.csproj"), "--no-restore", FormatCommand.OnceOption }, lines.Add));
        Assert.Contains(lines, l => l.StartsWith(FormatCommand.NotLoadedHeader, StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.StartsWith("Run ", StringComparison.Ordinal));
    }

    [Fact]
    public void Format_OldStyleProjects_GetThePackagesConfigRestoreOnlyWhenTheyHaveIt()
    {
        var packageReference = FormatCommand.OldStyleNote(new[] { "src/Runner.Service/Windows/RunnerService.csproj" }, packagesConfig: false);
        Assert.Contains("restoring the SDK-style projects only", packageReference, StringComparison.Ordinal);
        Assert.DoesNotContain("packages.config", packageReference, StringComparison.Ordinal);
        Assert.Contains("msbuild -t:restore (", packageReference, StringComparison.Ordinal);
        Assert.Contains("-p:RestorePackagesConfig=true", FormatCommand.OldStyleNote(new[] { "Old/Old.csproj" }, packagesConfig: true), StringComparison.Ordinal);

        Write("Old/Old.csproj", "<Project ToolsVersion=\"15.0\" />");
        Assert.False(FormatCommand.UsesPackagesConfig(Path.Combine(root, "Old/Old.csproj")));
        Write("Old/packages.config", "<packages />");
        Assert.True(FormatCommand.UsesPackagesConfig(Path.Combine(root, "Old/Old.csproj")));
    }

    [Fact]
    public void Preview_OldStyleProjectsWithoutPackagesConfig_AreNotRefused()
    {
        // actions/runner: RunnerService.csproj has no packages.config, yet --diff refused with the packages.config explanation.
        Write("Old/Old.csproj", "<?xml version=\"1.0\"?>\n<Project ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" />\n");
        Write("New/New.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("App.sln", string.Empty);

        var error = CaptureError(() => Capture(() => Assert.Throws<InvalidOperationException>(() => PreviewCommand.Run("format", new[] { root, "--diff" }, (_, _) => throw new InvalidOperationException("format ran")))));
        Assert.DoesNotContain("packages.config", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_ASolutionFilter_IsReadFromItsJson()
    {
        // openmcdf: 'dotnet sln X.slnf list' fails, so a .slnf got no projects: no per-framework runs, no warning.
        Write("build/Cross.slnf", "{\n  \"solution\": {\n    \"path\": \"..\\\\OpenMcdf.slnx\",\n    \"projects\": [ \"OpenMcdf\\\\OpenMcdf.csproj\", \"Tests/Tests.csproj\", \"Docs/Docs.vbproj\" ]\n  }\n}\n");

        var (solution, projects) = FormatCommand.ReadSolutionFilter(Path.Combine(root, "build/Cross.slnf"));

        Assert.Equal(Path.Combine(root, "OpenMcdf.slnx"), solution);
        Assert.Equal(new[] { "OpenMcdf\\OpenMcdf.csproj", "Tests/Tests.csproj", "Docs/Docs.vbproj" }, projects);
        Assert.True(FormatCommand.IsSolutionFilter("X.SLNF"));
    }

    [Fact]
    public void Format_AWorkspaceWithoutProjects_Stops()
    {
        // 'dotnet sln Empty.slnf list' fails ("Project `Docs/Docs.vbproj` could not be found in the solution"): read from the JSON.
        Write("OpenMcdf.slnx", "<Solution>\n  <Project Path=\"Docs/Docs.vbproj\" />\n</Solution>\n");
        Write("Docs/Docs.vbproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Empty.slnf", "{ \"solution\": { \"path\": \"OpenMcdf.slnx\", \"projects\": [ \"Docs/Docs.vbproj\" ] } }");
        var lines = new List<string>();

        Assert.Equal(1, FormatCommand.Run(new[] { Path.Combine(root, "Empty.slnf"), FormatCommand.OnceOption }, lines.Add));
        Assert.Contains("No C# project in Empty.slnf, so there's nothing to format. Name a solution or project with C# projects.", lines);
    }

    [Fact]
    public void Format_AProjectMSBuildCantEvaluate_ShowsMSBuildsError()
    {
        // GameDemo: an invalid NuGet.Config; ParseProject's JsonReaderException hid MSBuild's message behind a stack trace.
        Write("Bad/Bad.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>");
        var code = 0;

        var error = CaptureError(() => Capture(() => code = Program.Main(new[] { "format", Path.Combine(root, "Bad/Bad.csproj"), "--no-restore" })));

        Assert.Equal(1, code);
        Assert.Contains("'dotnet msbuild ", error, StringComparison.Ordinal);
        Assert.Contains("MSB4025", error, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_ARestoreSkipsNuGetsAudit_AndVersionPrintsTheVersion()
    {
        // CleanArchitecture: a vulnerable test package (NU1903 under TreatWarningsAsErrors) failed every format run.
        Assert.Contains("-p:NuGetAudit=false", FormatCommand.RestoreOptions);
        Assert.Contains("NU1901-NU1904", PreviewCommand.NoRestoreIgnored, StringComparison.Ordinal);

        var output = Capture(() => Assert.Equal(0, Program.Main(new[] { "--version" })));
        Assert.Matches(@"^\d+\.\d+\.\d+", output.Trim());
        Assert.DoesNotContain("+", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_AReferenceInAFileImportedByProjectExtension_IsNamed()
    {
        // openmcdf: Directory.Build.props imports 'Directory$(MSBuildProjectExtension).props', which references StyleCop.
        Write("Directory.Build.props", "<Project>\n  <Import Project=\"Directory$(MSBuildProjectExtension).props\" Condition=\"Exists('Directory$(MSBuildProjectExtension).props')\" />\n</Project>\n");
        Write("Directory.csproj.props", "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" />\n  </ItemGroup>\n</Project>\n");
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal(new[] { "Directory.csproj.props" }, StyleCopSetup.Read(root).ReferencedIn);
    }
}
