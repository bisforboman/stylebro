using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 25's findings in 'bro format', verify, the preview and baseline: other configurations, --files, CSharpier.</summary>
public sealed partial class MigrationTests
{
    [Theory]
    [InlineData("NET8_0_OR_GREATER", true)]
    [InlineData("NET", true)]
    [InlineData("NET48", true)]
    [InlineData("NETSTANDARD2_0", true)]
    [InlineData("NETFRAMEWORK", true)]
    [InlineData("NETCOREAPP", true)]
    [InlineData("WINDOWS10_0_17763_0_OR_GREATER", true)]
    [InlineData("DEBUG", false)]
    [InlineData("RELEASE", false)]
    [InlineData("TRACE", false)]
    [InlineData("NETSOMETHING_CUSTOM", false)]
    [InlineData("IS_SHARED_NET", false)]
    public void Format_TellsFrameworkSymbolsFromConfigurationOnes(string symbol, bool framework)
    {
        Assert.Equal(framework, FormatCommand.IsFrameworkSymbol(symbol));
    }

    [Fact]
    public void Format_ReadsTheConfigurationsASolutionBuilds()
    {
        Write("App.sln", "Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|Any CPU = Release|Any CPU\n\t\tRelease|x64 = Release|x64\n\t\tCI|Any CPU = CI|Any CPU\n\tEndGlobalSection\nEndGlobal\n");
        Assert.Equal(["Debug", "Release", "Release|x64", "CI"], FormatCommand.SolutionConfigurations(Path.Combine(root, "App.sln"))); // trial 27: a platform of its own

        // A .slnx lists them only when it has others than Debug and Release.
        Write("App.slnx", "<Solution>\n  <Project Path=\"src/A/A.csproj\" />\n</Solution>\n");
        Assert.Equal(["Debug", "Release"], FormatCommand.SolutionConfigurations(Path.Combine(root, "App.slnx")));
        Write("App.slnx", "<Solution>\n  <Configurations>\n    <BuildType Name=\"Debug\" />\n    <BuildType Name=\"Staging\" />\n  </Configurations>\n</Solution>\n");
        Assert.Equal(["Debug", "Staging"], FormatCommand.SolutionConfigurations(Path.Combine(root, "App.slnx")));
        Assert.Null(FormatCommand.SolutionConfigurations(Path.Combine(root, "src", "A", "A.csproj")));
    }

    [Fact]
    public void Format_RunsAProjectsOtherConfigurations_ThatTheSolutionBuilds()
    {
        Assert.Equal(["Release"], FormatCommand.OtherConfigurations("Debug;Release", "Debug", null));
        Assert.Equal(["Release"], FormatCommand.OtherConfigurations(string.Empty, string.Empty, null)); // the SDK's defaults
        Assert.Equal(["Debug"], FormatCommand.OtherConfigurations("Debug;Release", "Release", null)); // CI's Configuration=Release
        Assert.Equal(["Release"], FormatCommand.OtherConfigurations("Debug;Release;Profile", "Debug", ["Debug", "Release"]));
        Assert.Equal(4, FormatCommand.OtherConfigurations("Debug;A;B;C;D;E", "Debug", null).Count); // within reason
    }

    [Fact]
    public void Format_AddsAConfigurationRun_OnlyWhereTheCodeParsesDifferently()
    {
        // Flow Launcher's Win32.cs has '#if !DEBUG' code: format ran Debug only, the Release build with warnings as errors failed.
        // A tests DEBUG, B tests a symbol only Release defines the same as Debug, C has no configurations of its own.
        var runs = new List<(string Framework, List<string> Projects)> { ("net8.0", ["A", "B", "C"]), ("net48", ["A"]) };
        string? Key(string project, string framework, string configuration) => project switch
        {
            "A" => framework + (configuration == "Release" ? "|release" : "|debug"),
            "B" => framework + "|same",
            _ => null,
        };
        var added = FormatCommand.ConfigurationRuns(runs, p => p == "C" ? Array.Empty<string>() : new[] { "Release" }, Key);
        Assert.Equal(["net8.0 Release A", "net48 Release A"], added.Select(r => $"{r.Framework} {r.Configuration} {string.Join(",", r.Projects)}"));

        // Two frameworks that compile the same in Release (same key): one run.
        string? Merged(string project, string framework, string configuration) => configuration == "Release" ? "release" : framework;
        Assert.Single(FormatCommand.ConfigurationRuns(runs, p => p == "A" ? new[] { "Release" } : Array.Empty<string>(), Merged));

        Assert.Equal(
            "Configurations: runs for Release (A) too: their '#if' code (DEBUG, RELEASE, ...) compiles differently there, and the build of every configuration gets the same fixes and checks.",
            FormatCommand.ConfigurationsLine(added));
        Assert.Null(FormatCommand.ConfigurationsLine([]));
    }

    [Fact]
    public void Format_FindsTheConfigurationsWhoseCodeDiffers_GeneratorProjectsToo()
    {
        // MimeKit's Benchmarks: one framework in <TargetFrameworks> (an outer evaluation without Compile items) and '#if DEBUG'.
        Write("App.slnx", "<Solution>\n  <Project Path=\"Bench/Bench.csproj\" />\n</Solution>\n");
        Write("Bench/Bench.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>net8.0</TargetFrameworks></PropertyGroup>\n</Project>\n");
        Write("Bench/Program.cs", "class P\n{\n#if DEBUG\n    int a;\n#else\n    int b;\n#endif\n}\n");
        Assert.Equal(["Release"], FormatCommand.WorkspaceConfigurations(Path.Combine(root, "App.slnx")));

        // A project that runs an in-solution source generator gets them too (trial 28, owner: "Fix it in a Release run"; format
        // builds the generator in Release first).
        Write("Gen/Gen.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup>\n</Project>\n");
        Write("Bench2/Bench2.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>net8.0</TargetFrameworks></PropertyGroup>\n  <ItemGroup><ProjectReference Include=\"../Gen/Gen.csproj\" OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" /></ItemGroup>\n</Project>\n");
        Write("Bench2/Program.cs", "class P\n{\n#if DEBUG\n    int a;\n#endif\n}\n");
        Write("App2.slnx", "<Solution>\n  <Project Path=\"Bench2/Bench2.csproj\" />\n  <Project Path=\"Gen/Gen.csproj\" />\n</Solution>\n");
        Assert.Equal(["Release"], FormatCommand.WorkspaceConfigurations(Path.Combine(root, "App2.slnx")));

        // Trial 28: no '#if', but a file only Release compiles.
        Write("Tel/Tel.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <ItemGroup Condition=\"'$(Configuration)' != 'Release'\"><Compile Remove=\"Telemetry.Release.cs\" /></ItemGroup>\n</Project>\n");
        Write("Tel/A.cs", "class A\n{\n}\n");
        Write("Tel/Telemetry.Release.cs", "class T\n{\n}\n");
        Write("App3.slnx", "<Solution>\n  <Project Path=\"Tel/Tel.csproj\" />\n</Solution>\n");
        Assert.Equal(["Release"], FormatCommand.WorkspaceConfigurations(Path.Combine(root, "App3.slnx")));
    }

    [Fact]
    public void Format_RunsTheFilesOfSeveralProjectsTogether_FromTheirCommonFolder()
    {
        var a = Path.Combine(root, "src", "Company.Module5");
        var b = Path.Combine(root, "src", "Company.Module9", "Inner");
        Assert.Equal(Path.Combine(root, "src"), FormatCommand.CommonFolder([a, b]));
        Assert.Equal(a, FormatCommand.CommonFolder([a, a]));
        Assert.Equal(Path.TrimEndingDirectorySeparator(root), FormatCommand.CommonFolder([a, Path.Combine(root, "tests")]));
    }

    [Fact]
    public void Format_PutsANewBaselineForFiles_WhereAWholeRunPutsIt()
    {
        // 'format --files' wrote stylebro.baseline next to the project; a whole run writes it at the repository's root.
        var project = Path.Combine(root, "src", "Lib");
        Directory.CreateDirectory(project);
        Assert.Equal(Path.Combine(root, Analyzers.Baseline.Baseline.FileName), FormatCommand.BaselineFile(project, root));
        Assert.Equal(Path.Combine(project, Analyzers.Baseline.Baseline.FileName), FormatCommand.BaselineFile(project));

        // An existing one above the project still wins.
        Write("src/stylebro.baseline", "# baseline\n");
        Assert.Equal(Path.Combine(root, "src", Analyzers.Baseline.Baseline.FileName), FormatCommand.BaselineFile(project, root));
    }

    [Fact]
    public void Format_SaysWhichCompiledFilesAreMissing()
    {
        var missing = new[] { Path.Combine(root, "..", "outside", "Gen.cs") };
        var line = FormatCommand.MissingCompileLine(root, missing.Select(Path.GetFullPath).ToList());
        Assert.StartsWith("Missing: 1 file the projects compile isn't there (../outside/Gen.cs): the code that uses it doesn't compile", line, StringComparison.Ordinal);
        Assert.Contains("IDE0005 (unused usings) is left out", line, StringComparison.Ordinal);

        // IDE0005 leaves the rules a run fixes; without a list of them it's excluded (an empty list would mean every rule).
        Assert.Equal(["--diagnostics", "BRO1001", "--severity", "warn"], FormatCommand.WithoutUnusedUsings(["--diagnostics", "BRO1001", "IDE0005", "--severity", "warn"]));
        Assert.Equal(["--severity", "warn", "--exclude-diagnostics", "IDE0005"], FormatCommand.WithoutUnusedUsings(["--severity", "warn"]));
        Assert.Equal(["--diagnostics", "IDE0005"], FormatCommand.WithoutUnusedUsings(["--diagnostics", "IDE0005"]));
    }

    [Fact]
    public void Preview_CopiesTheFilesProjectsCompileFromOutside_ToTheSameRelativePlace()
    {
        // Speckle compiles a sibling repository's files: the preview's copy didn't have them, and IDE0005 removed usings.
        var source = Path.Combine(root, "work", "speckle", "repo");
        Write("work/speckle/outside/Gen.cs", "namespace Outside;\n");
        var outside = new[] { Path.Combine(root, "work", "speckle", "outside", "Gen.cs") };
        var temp = Path.Combine(root, "temp");
        var copy = PreviewCommand.CopyLocation(temp, source, outside);
        Assert.Equal(Path.Combine(temp, "speckle", "repo"), copy);
        PreviewCommand.CopyOutside(source, copy, temp, outside);
        Assert.True(File.Exists(Path.Combine(temp, "speckle", "outside", "Gen.cs")));

        // Nothing from outside: the copy is the repository's folder in the temporary one, and a file that would land outside
        // the temporary folder isn't copied.
        Assert.Equal(Path.Combine(temp, "repo"), PreviewCommand.CopyLocation(temp, source, []));
        PreviewCommand.CopyOutside(source, temp, temp, outside);
        Assert.False(File.Exists(Path.Combine(root, "outside", "Gen.cs")));
    }

    [Fact]
    public void Format_FindsTheFilesAProjectCompilesFromOutsideTheRepository()
    {
        Write("outside/Gen.cs", "namespace Outside;\n");
        Write("repo/src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <ItemGroup><Compile Include=\"$(MSBuildThisFileDirectory)../../../outside/Gen.cs\" Link=\"Gen.cs\" /></ItemGroup>\n</Project>\n");
        Write("repo/src/Lib/User.cs", "namespace Lib;\n");
        Assert.Equal([Path.Combine(root, "outside", "Gen.cs")], FormatCommand.OutsideCompile(Path.Combine(root, "repo")));
    }

    [Fact]
    public void GeneratedFileNote_LeavesOutDesignersAndToolOutput_NoBuildWrites()
    {
        // Flow Launcher's Settings.Designer.cs (Visual Studio writes it from Settings.settings); MimeKit's *.g.cs from a tool in
        // tools/ someone runs by hand (its csproj names them in Compile items only).
        Write("Properties/Settings.settings", "<SettingsFile />\n");
        Write("Properties/Settings.Designer.cs", "// <auto-generated />\nclass S { }\n");
        Write("Utils/Table.g.cs", "class T { }\n");
        Assert.False(FormatCommand.IsGenerated(Path.Combine(root, "Properties", "Settings.Designer.cs")));
        Assert.False(FormatCommand.IsGenerated(Path.Combine(root, "Utils", "Table.g.cs"), "<Compile Update=\"Utils\\Table.g.cs\">"));
        Assert.True(FormatCommand.IsGenerated(Path.Combine(root, "Utils", "Table.g.cs"), "<Exec Command=\"gen > Utils/Table.g.cs\" />"));
        Assert.True(FormatCommand.IsGenerated(Path.Combine(root, "Utils", "Table.g.cs"), "dotnet run --project tools/Gen > Utils\\Table.g.cs"));

        // A source generator's output the build writes into the repository (EmitCompilerGeneratedFiles): no build file names it.
        Write("Lib/Generated/Gen/Gen.Generator/Model.g.cs", "class M { }\n");
        PreviewCommand.MakeRepository(root);
        const string Emit = "<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>\n<CompilerGeneratedFilesOutputPath>$(MSBuildProjectDirectory)/Generated</CompilerGeneratedFilesOutputPath>";
        Assert.True(FormatCommand.IsGenerated(Path.Combine(root, "Lib", "Generated", "Gen", "Gen.Generator", "Model.g.cs"), Emit));
        Assert.False(FormatCommand.IsGenerated(Path.Combine(root, "Utils", "Table.g.cs"), Emit));
    }

    [Fact]
    public void CSharpier_FormatsWholeFolders_WhereItWouldChangeNothingElse()
    {
        // Speckle: 242 files named one by one took ~20 s (each name costs an options lookup), the folders ~1 s.
        var tool = new CSharpierStep.Tool(root, ["csharpier"], true, [string.Empty], null);
        string Full(string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        var changed = new[] { Full("src/A/One.cs"), Full("src/A/Two.cs"), Full("src/B/Three.cs"), Full("tests/T.cs") };

        // nuget.config at the root isn't formatted: the root can't be given, its subfolders can.
        Assert.Equal([Full("src"), Full("tests")], CSharpierStep.Arguments(tool, root, changed, new HashSet<string>([Full("nuget.config")], StringComparer.OrdinalIgnoreCase)));

        // An unformatted file below: that folder is left to the file names; a changed file CSharpier would change anyway goes alone.
        Assert.Equal([Full("src/A"), Full("src/B/Three.cs"), Full("tests")], CSharpierStep.Arguments(tool, root, changed, new HashSet<string>([Full("src/B/Other.cs"), Full("src/B/Three.cs")], StringComparer.OrdinalIgnoreCase)));
        Assert.Equal([Full(string.Empty).TrimEnd(Path.DirectorySeparatorChar)], CSharpierStep.Arguments(tool, root, changed, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

        // Unknown (the check failed): the files.
        Assert.Equal(changed, CSharpierStep.Arguments(tool, root, changed, null));
        Assert.Equal(["./src/A.cs", "./nuget.config"], CSharpierStep.Failures(["Error ./src/A.cs - Was not formatted.", "  The file contained different line endings", "Error ./nuget.config - Was not formatted.", "Checked 2 files in 10ms."]));

        // The check: only C# files count (Speckle's nuget.config line endings made verify fail).
        Assert.Null(CSharpierStep.FailedCSharp(0, []));
        Assert.Null(CSharpierStep.FailedCSharp(1, ["Error ./nuget.config - Was not formatted."]));
        Assert.Equal(["src/A.cs"], CSharpierStep.FailedCSharp(1, ["Error ./nuget.config - Was not formatted.", "Error ./src/A.cs - Was not formatted."]));
        Assert.Equal([], CSharpierStep.FailedCSharp(1, ["Unhandled exception"]));
    }

    [Fact]
    public void Format_DeletesWhatAKilledRunLeftInTheTemporaryFolder()
    {
        var temp = Path.Combine(root, "temp");
        Write("temp/stylebro-format-old.targets", "<Project />\n");
        Write("temp/stylebro-names-old.tsv", "x\n");
        Write("temp/stylebro-format-report-old/format-report.json", "[]\n");
        Write("temp/other.txt", "keep\n");
        File.SetLastWriteTimeUtc(Path.Combine(temp, "stylebro-format-old.targets"), DateTime.UtcNow.AddDays(-2));
        File.SetLastWriteTimeUtc(Path.Combine(temp, "stylebro-names-old.tsv"), DateTime.UtcNow.AddDays(-2));
        File.SetLastWriteTimeUtc(Path.Combine(temp, "other.txt"), DateTime.UtcNow.AddDays(-2));
        Directory.SetLastWriteTimeUtc(Path.Combine(temp, "stylebro-format-report-old"), DateTime.UtcNow.AddDays(-2));
        Write("temp/stylebro-names-new.tsv", "x\n");

        FormatCommand.DeleteStaleTemp(temp, DateTime.UtcNow.AddDays(-1));
        Assert.Equal(["other.txt", "stylebro-names-new.tsv"], Directory.EnumerateFileSystemEntries(temp).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }
}
