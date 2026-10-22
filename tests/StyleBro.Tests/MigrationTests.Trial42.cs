using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the forty-second trial (N_m3u8DL-RE, sbom-tool, PolyShim, kiota-dotnet) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void WithEmptyTarget_RetriesOnlyAConditionErrorOfAQueryWithoutATarget()
    {
        const string error = "a.csproj(4,42): error MSB4184: The expression \"[MSBuild]::VersionGreaterThanOrEquals('', 4.5)\" cannot be evaluated.";
        Assert.Equal(["msbuild", "a.csproj", "-getProperty:X", "-target:InitializeSourceControlInformation"], FormatCommand.WithEmptyTarget(["msbuild", "a.csproj", "-getProperty:X"], error)!);
        Assert.Null(FormatCommand.WithEmptyTarget(["msbuild", "a.csproj", "-getProperty:X"], "error MSB4025: The project file could not be loaded."));
        Assert.Null(FormatCommand.WithEmptyTarget(["msbuild", "a.csproj", "-target:AddImplicitDefineConstants", "-getProperty:X"], error));
        Assert.Null(FormatCommand.WithEmptyTarget(["build", "a.csproj"], error));
    }

    [Fact]
    public void Capture_AQueryOfAProjectWithAConditionOnlyItsFrameworksCanEvaluate_ReadsTheProject()
    {
        // PolyShim: an item Condition comparing $(TargetFrameworkVersion) in a group for .NETFramework only. A query without a
        // target evaluates it on the outer evaluation of the multi-targeted project too, where the version is empty (MSB4184),
        // so init and format stopped at their first query; the build skips it.
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>net8.0;net48</TargetFrameworks></PropertyGroup>\n  <ItemGroup Condition=\"'$(TargetFrameworkIdentifier)' == '.NETFramework'\">\n    <Reference Include=\"System.Net.Http\" Condition=\"$([MSBuild]::VersionGreaterThanOrEquals($(TargetFrameworkVersion), '4.5'))\" />\n  </ItemGroup>\n</Project>\n");

        Assert.Contains("net8.0;net48", FormatCommand.Capture(root, "msbuild", Path.Combine(root, "Lib", "Lib.csproj"), "-getProperty:TargetFrameworks", "-getItem:Compile", "-nologo"));
    }

    [Theory]
    [InlineData("true", false)]
    [InlineData("", true)]
    public void PackageTargets_LeaveAFileBasedProgramWithoutStyleBrosAnalyzers(string fileBased, bool kept)
    {
        // PolyShim: its build runs GenerateSignatures.csx with 'dotnet run'; the file-based program imports the repository's
        // Directory.Build.props, StyleBro included, and failed on findings 'dotnet format' can't load the file to fix.
        var targets = Path.Combine(Presets.Root, "src", "StyleBro.Package", "build", "StyleBro.Analyzers.targets");
        Write("App/App.csproj", $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework><FileBasedProgram>{fileBased}</FileBasedProgram></PropertyGroup>\n  <ItemGroup>\n    <Analyzer Include=\"bro/StyleBro.Analyzers.dll\" NuGetPackageId=\"StyleBro.Analyzers\" />\n    <Analyzer Include=\"other/Other.dll\" NuGetPackageId=\"Other\" />\n  </ItemGroup>\n  <Import Project=\"{targets}\" />\n</Project>\n");

        var analyzers = FormatCommand.Capture(root, "msbuild", Path.Combine(root, "App", "App.csproj"), "-target:_StyleBroLeaveFileBasedPrograms", "-getItem:Analyzer", "-nologo");

        Assert.Equal(kept, analyzers.Contains("StyleBro.Analyzers.dll", StringComparison.Ordinal));
        Assert.Contains("Other.dll", analyzers, StringComparison.Ordinal);
    }

    [Fact]
    public void HeldBackGapIds_ReadsOnlyGapsOfHeldBackIdeFixes()
    {
        Assert.Equal(["IDE0049", "IDE0057"], FormatCommand.HeldBackGapIds([FormatCommand.Gap("IDE0049, IDE0057", FormatCommand.HeldBackReason)]));
        Assert.Null(FormatCommand.HeldBackGapIds([FormatCommand.Gap("IDE0049", FormatCommand.HeldBackReason), FormatCommand.Gap("Lib in Release", "it takes an in-solution source generator")]));
        Assert.Null(FormatCommand.HeldBackGapIds([]));
    }

    [Theory]
    [InlineData("true", "warning", true)]
    [InlineData("true", "suggestion", false)]
    [InlineData("false", "warning", false)]
    public void BuildEnforces_NeedsCodeStyleInTheBuildAndTheIdAtWarningEverywhere(string enforce, string severity, bool enforced)
    {
        // sbom-tool: EnforceCodeStyleInBuild and IDE0049 at warning, so --build's builds checked what format held back.
        Write(".editorconfig", $"root = true\n\n[*.cs]\ndotnet_diagnostic.IDE0049.severity = {severity}\n");
        Write("App/App.csproj", $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework><EnforceCodeStyleInBuild>{enforce}</EnforceCodeStyleInBuild></PropertyGroup>\n</Project>\n");
        Write("App/A.cs", "class A { }\n");

        Assert.Equal(enforced, FormatCommand.BuildEnforces(root, Path.Combine(root, "App", "App.csproj"), ["IDE0049"]));
    }

    [Fact]
    public void Preview_NamesTheConfigurationsAnInSolutionGeneratorKeepsFromTheRuns()
    {
        // kiota-dotnet: format said "Not checked: Microsoft.Kiota.Http.HttpClientLibrary in Release" (an in-solution generator),
        // the preview's plain runs didn't, so without another gap it would have said "clean after 1 run" and exited with 0.
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "class C { int b; int a; }\n");
        PreviewCommand.MakeRepository(root);
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        int Format(string[] args, Action<string> log)
        {
            var report = Array.IndexOf(args, "--report");
            if (report >= 0)
            {
                Directory.CreateDirectory(args[report + 1]);
                var c = Path.Combine(args[0], "C.cs").Replace("\\", "\\\\");
                File.WriteAllText(Path.Combine(args[report + 1], "format-report.json"), $$"""
                    [ { "FilePath": "{{c}}", "FileChanges": [ { "LineNumber": 1, "CharNumber": 11, "DiagnosticId": "BRO1001" } ] } ]
                    """);
            }

            FormatCommand.RememberGeneratorBuilds(args[0], [(Path.Combine(args[0], "Lib", "Lib.csproj"), "Release")]);
            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C { int a; int b; }\n");
            return 0;
        }

        var output = Capture(() => Assert.Equal(FormatCommand.NotVerifiedExitCode, PreviewCommand.Run("init", new[] { root, "--diff=" + patch }, Format)));

        Assert.Contains("Not checked: Lib in Release: it takes an in-solution source generator", output, StringComparison.Ordinal);
        Assert.Contains(FormatCommand.NotVerifiedLine(1), output, StringComparison.Ordinal);
        DeleteFolder(Path.GetDirectoryName(patch)!);
    }
}
