using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 27's findings in 'stylebro-migrate format', verify and the baseline.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Verify_CountsTheFindingsOfAMultiTargetedProjectsBuild()
    {
        // CliFx: a multi-targeted build tags its lines '[...\Lib.csproj::TargetFramework=net8.0]', and every finding was dropped.
        var project = Path.Combine(root, "Lib", "Lib.csproj");
        var file = Path.Combine(root, "Lib", "R.cs");
        var said = new List<string>();
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        var line = FormatCommand.UncheckedLine(root, said.Add, (_, _) => (1, [$"{file}(8,9): error BRO1002: Add a space after '//' [{project}::TargetFramework=net8.0]"]));

        Assert.Equal("  Lib/R.cs(8,9): BRO1002: Add a space after '//'", said[2]);
        Assert.StartsWith("Not clean: 1 finding in code only another configuration compiles", line, StringComparison.Ordinal);
        Assert.Equal(project, FormatCommand.BuildLineProject($"{project}::TargetFramework=net8.0"));
        Assert.Equal(project, FormatCommand.BuildLineProject(project));
    }

    [Fact]
    public void Verify_IsNotClean_WhenTheBuildFailsWithoutFindingsOfItsOwn()
    {
        // CliFx: the referenced generator project's errors (or a compile error) failed the build, which read as "no findings".
        var project = Path.Combine(root, "Lib", "Lib.csproj");
        var gen = Path.Combine(root, "Gen", "Gen.csproj");
        var said = new List<string>();
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        var line = FormatCommand.UncheckedLine(root, said.Add, (_, _) => (1, [$"{Path.Combine(root, "Gen", "G.cs")}(1,1): error CS8370: Feature 'file-scoped namespace' is not available in C# 7.3. [{gen}]"]));

        Assert.Equal("Release build of Lib: no findings.", said[1]);
        Assert.Equal("Release build of Lib failed, first error:", said[2]);
        Assert.Equal("  Gen/G.cs(1,1): CS8370: Feature 'file-scoped namespace' is not available in C# 7.3.", said[3]);
        Assert.StartsWith("Not clean: the build of Lib in Release failed (above)", line, StringComparison.Ordinal);

        // A failed build without any error line says its exit code; a build that succeeds is clean.
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        Assert.NotNull(FormatCommand.UncheckedLine(root, said.Add, (_, _) => (1, ["MSBUILD : error MSB1009: Project file does not exist."])));
        Assert.Equal("Release build of Lib failed (exit code 1).", said[^1]);
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        Assert.Null(FormatCommand.UncheckedLine(root, said.Add, (_, _) => (0, [$"{Path.Combine(root, "Gen", "G.cs")}(1,1): warning CS1591: Missing XML comment [{gen}]"])));
    }

    [Fact]
    public void Format_RunsTheSolutionsPlatformsLikeConfigurations()
    {
        // PalCalc: 'Release|x64' code ('$(Configuration)|$(Platform)' == 'Release|x64' defines HANDLE_ERRORS) was never run.
        Write("App.sln", "Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|x64 = Debug|x64\n\t\tRelease|x64 = Release|x64\n\tEndGlobalSection\nEndGlobal\n");
        Assert.Equal(["Debug|x64", "Release|x64"], FormatCommand.SolutionConfigurations(Path.Combine(root, "App.sln")));
        Write("Mixed.sln", "Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Mixed Platforms = Debug|Mixed Platforms\n\t\tRelease|Any CPU = Release|Any CPU\n\tEndGlobalSection\nEndGlobal\n");
        Assert.Equal(["Debug", "Release"], FormatCommand.SolutionConfigurations(Path.Combine(root, "Mixed.sln")));
        Write("App.slnx", "<Solution>\n  <Configurations>\n    <Platform Name=\"x64\" />\n  </Configurations>\n</Solution>\n");
        Assert.Equal(["Debug|x64", "Release|x64"], FormatCommand.SolutionConfigurations(Path.Combine(root, "App.slnx")));

        // A platform-only solution: the default configuration in its platform is another one too (a run only where it parses differently).
        Assert.Equal(["Debug|x64", "Release|x64"], FormatCommand.OtherConfigurations("Debug;Release", "Debug", ["Debug|x64", "Release|x64"]));
        Assert.Equal(["Release", "Debug|x64", "Release|x64"], FormatCommand.OtherConfigurations("Debug;Release", "Debug", ["Debug", "Release", "Debug|x64", "Release|x64"]));

        Assert.Equal(("Release", "x64"), FormatCommand.SplitConfiguration("Release|x64"));
        Assert.Equal(("Release", (string?)null), FormatCommand.SplitConfiguration("Release"));
        var environment = new Dictionary<string, string?>();
        FormatCommand.SetConfiguration(environment, "Release|x64");
        Assert.Equal("Release", environment["Configuration"]);
        Assert.Equal("x64", environment["Platform"]);
        environment.Clear();
        FormatCommand.SetConfiguration(environment, "Release");
        Assert.False(environment.ContainsKey("Platform"));
    }
}
