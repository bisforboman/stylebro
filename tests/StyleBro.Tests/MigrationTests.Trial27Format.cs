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

    [Fact]
    public void Baseline_GoesWhereEachProjectsBuildReadsIt()
    {
        // <StyleBroBaseline> in src/Directory.Build.props (evaluated: the package's targets resolve it) named eng/'s, and
        // 'baseline' and format's kept findings wrote the root's.
        var app = Path.Combine(root, "src", "App");
        Assert.Equal((true, Path.Combine(root, "eng", "stylebro.baseline")), FormatCommand.BaselineOf(Path.Combine(root, "src") + "\\..\\eng\\stylebro.baseline", app));
        Assert.Equal((true, Path.Combine(app, "eng", "stylebro.baseline")), FormatCommand.BaselineOf("eng/stylebro.baseline", app));
        Assert.Equal((true, (string?)null), FormatCommand.BaselineOf(" none ", app));
        Assert.Equal((false, (string?)null), FormatCommand.BaselineOf(string.Empty, app));

        var eng = Path.Combine(root, "eng", "stylebro.baseline");
        var top = Path.Combine(root, "stylebro.baseline");
        var file = Path.Combine(app, "A.cs");
        var shared = Path.Combine(root, "Shared.cs");
        var projects = new Dictionary<string, (string? File, bool Off, HashSet<string> Compile)>
        {
            [Path.Combine(app, "App.csproj")] = (eng, false, [file, shared]),
            [Path.Combine(root, "tests", "Tests.csproj")] = (top, false, [Path.Combine(root, "tests", "T.cs"), shared]),
            [Path.Combine(root, "tools", "Tool.csproj")] = (null, true, [Path.Combine(root, "tools", "X.cs")]),
        };
        var groups = FormatCommand.BaselineGroups(projects, new[] { "A", "S", "X", "N" }.Zip([file, shared, Path.Combine(root, "tools", "X.cs"), Path.Combine(root, "None.cs")]), top);
        Assert.Equal([(eng, "A,S"), (top, "S,N")], groups.Select(g => (g.File, string.Join(",", g.Items))));
        Assert.Equal([eng, top], FormatCommand.BaselinesOf(projects, shared, null));

        var different = FormatCommand.DifferentBaselinesLine(root, projects);
        Assert.Equal("Baseline: the projects read different baselines (none: Tool; eng/stylebro.baseline: App; stylebro.baseline: Tests): each gets the findings in the files its projects compile.", different);
        Assert.Null(FormatCommand.DifferentBaselinesLine(root, new Dictionary<string, (string? File, bool Off, HashSet<string> Compile)> { ["A"] = (eng, false, []), ["B"] = (eng, false, []) }));

        // The 'none' message names the cause that applies.
        Assert.Equal("Nothing written: <StyleBroBaseline>none</StyleBroBaseline> turns the baseline off.", BaselineCommand.OffLine(off: true));
        Assert.Contains("MSBuild properties", BaselineCommand.OffLine(off: false), StringComparison.Ordinal);
        Write("props/Directory.Build.props", "<Project><PropertyGroup><StyleBroBaseline>none</StyleBroBaseline></PropertyGroup></Project>");
        Assert.True(FormatCommand.NamedBaseline(Path.Combine(root, "props")).Off);
        Write("props/Directory.Build.props", "<Project><PropertyGroup><StyleBroBaseline>$(Eng)\\stylebro.baseline</StyleBroBaseline></PropertyGroup></Project>");
        Assert.Equal((true, (string?)null, false), FormatCommand.NamedBaseline(Path.Combine(root, "props")));
    }

    [Fact]
    public void Baseline_AddsOnlyTheFilesInItsFolder_AndItsProjectsFiles()
    {
        // 'baseline Tests.slnx' from tests/ added entries for the src/ files the tests compile.
        Write("tests/T.cs", "class T { string s = \"\"; }\n");
        Write("src/A.cs", "class A { string s = \"\"; }\n");
        string Document(string path) => $$"""{ "FilePath": {{System.Text.Json.JsonSerializer.Serialize(path)}}, "FileChanges": [ { "LineNumber": 1, "CharNumber": 22, "DiagnosticId": "BRO1106" } ] }""";
        var report = "[" + Document(Path.Combine(root, "tests", "T.cs")) + "," + Document(Path.Combine(root, "src", "A.cs")) + "]";

        var result = BaselineCommand.Build(Path.Combine(root, "tests"), report, root);
        Assert.Equal(["tests/T.cs"], result.Baseline.Entries.Keys.Select(k => k.Path));
        Assert.Equal(1, result.Outside);

        // Only the files a baseline's projects compile.
        Assert.Empty(BaselineCommand.Build(root, report, root, f => false).Baseline.Entries);
        Assert.Equal(2, BaselineCommand.Build(root, report, root).Baseline.Count);
    }

    [Fact]
    public void Format_PlansNoRunsForAPackagesFiles()
    {
        // PolyShim's '#if !POLYSHIM_INCLUDE_COVERAGE', Nullable's '#if DEBUG': Release runs and shared-file parts for code format
        // never changes, and only once the restore had put the files into the build.
        Write("packages/nullable/1.3.1/.nupkg.metadata", "{}");
        Write("packages/nullable/1.3.1/contentFiles/cs/net40/Nullable/A.cs", "#if DEBUG\nclass A { }\n#endif\n");
        Write("src/B.cs", "#if DEBUG\nclass B { }\n#endif\n");
        Assert.True(FormatCommand.IsPackageFile(Path.Combine(root, "packages", "nullable", "1.3.1", "contentFiles", "cs", "net40", "Nullable", "A.cs")));
        Assert.Empty(FormatCommand.TestedSymbols(Path.Combine(root, "packages", "nullable", "1.3.1", "contentFiles", "cs", "net40", "Nullable", "A.cs")));
        Assert.False(FormatCommand.IsPackageFile(Path.Combine(root, "src", "B.cs")));
        Assert.Equal(["DEBUG"], FormatCommand.TestedSymbols(Path.Combine(root, "src", "B.cs")));

        // And they're not "outside the repository" in any packages folder (the restore's NUGET_PACKAGES, not this process's).
        var repository = Path.Combine(root, "repo");
        Assert.Empty(FormatCommand.OutsideRepository([Path.Combine(root, "packages", "nullable", "1.3.1", "contentFiles", "cs", "net40", "Nullable", "A.cs")], repository, Path.Combine(root, "other")));
        Assert.Equal([Path.Combine(root, "src", "B.cs")], FormatCommand.OutsideRepository([Path.Combine(root, "src", "B.cs")], repository, Path.Combine(root, "other")));
    }

    [Fact]
    public void NotBuiltFolders_ReadASlnxsBuildFalse()
    {
        // io-abstractions' Pipeline/Build.csproj: '<Build Project="false" />', never restored: "Without StyleBro".
        Write("App.slnx", "<Solution>\n  <Project Path=\"src/A/A.csproj\" />\n  <Folder Name=\"/Pipeline/\">\n    <Project Path=\"Pipeline/Build.csproj\">\n      <Build Project=\"false\" />\n    </Project>\n    <Project Path=\"tools/T/T.csproj\">\n      <Build Solution=\"Debug|*\" Project=\"false\" />\n    </Project>\n  </Folder>\n</Solution>\n");
        string[] projects = [Path.Combine(root, "src", "A", "A.csproj"), Path.Combine(root, "Pipeline", "Build.csproj"), Path.Combine(root, "tools", "T", "T.csproj")];
        Assert.Equal(["Pipeline/"], FormatCommand.NotBuiltFolders(Path.Combine(root, "App.slnx"), projects, root));
    }

    [Fact]
    public void IfBodies_KeepOnlyTheIndentation_AndTheirOtherSpacingIsFixed()
    {
        // b-1: 'builder.Append(v+1);' is a kept body: its indentation stays, 'v + 1' was never written and verify said clean.
        var before = IfDebugBody.Replace("        Log();\n        Log();", "        Log(1+2);\n        Log();", StringComparison.Ordinal);
        var after = before.Replace("        Log(1+2);", "            Log(1 + 2);", StringComparison.Ordinal);
        Assert.Equal(before.Replace("Log(1+2)", "Log(1 + 2)", StringComparison.Ordinal), IfBodies.Keep(before, [7], after));

        var file = Path.Combine(root, "C.cs");
        var protectedLines = new Dictionary<string, (string Text, List<int> Lines)>(StringComparer.OrdinalIgnoreCase) { [file] = (before, [7]) };
        string Line(int line, int column, string message) => $"{file}({line},{column}): error WHITESPACE: Fix whitespace formatting. {message} [{root}/A.csproj]";
        Assert.True(IfBodies.IsKeptFinding(Line(8, 9, "Insert '\\s\\s\\s\\s'."), protectedLines));
        Assert.False(IfBodies.IsKeptFinding(Line(8, 14, "Insert '\\s'."), protectedLines));
    }

    [Fact]
    public void DocIndentation_RewritesTheLinesTheWhitespacePassLeaves()
    {
        // PalCalc: the first '///' line has spaces, the others and the member's line tabs at the same column (tab_width 4).
        const string Text = "namespace Lib\n{\n    public class C\n    {\n        /// <summary>\n\t\t/// Text.\n\t\t/// </summary>\n\t\tpublic int P { get; set; }\n    }\n}\n";
        Assert.Equal([5, 6, 7], DocIndentation.Lines(Text, useTabs: false, tabWidth: 4));
        Assert.Equal(Text.Replace("\t\t", "        ", StringComparison.Ordinal), DocIndentation.Fix(Text, useTabs: false, tabWidth: 4));

        // With tabs asked for: the first line is the one written otherwise, which the whitespace pass rewrites whole.
        Assert.Empty(DocIndentation.Lines(Text, useTabs: true, tabWidth: 4));
        var tabs = Text.Replace("        ///", "\t\t///", StringComparison.Ordinal).Replace("\t\t/// Text.", "        /// Text.", StringComparison.Ordinal);
        Assert.Equal([5], DocIndentation.Lines(tabs, useTabs: true, tabWidth: 4));

        // Another column is the whitespace pass's (tab_width 8: two tabs are 16 columns), and so are other lines, the rest of a
        // line and its line break.
        Assert.Empty(DocIndentation.Lines(Text, useTabs: false, tabWidth: 8));
        const string Other = "class C\n{\n    /// <summary>\r\n\t/// Text.  \r\n    /// </summary>\r\n\t[Obsolete] int P;\n\tint Q;\n}\n";
        Assert.Equal([3, 5], DocIndentation.Lines(Other, useTabs: false, tabWidth: 4));
        Assert.Equal("class C\n{\n    /// <summary>\r\n    /// Text.  \r\n    /// </summary>\r\n    [Obsolete] int P;\n\tint Q;\n}\n", DocIndentation.Fix(Other, useTabs: false, tabWidth: 4));

        // A string that looks like one isn't one.
        Assert.Empty(DocIndentation.Lines("class C\n{\n    string s = @\"\n    /// a\n\t/// b\";\n}\n", useTabs: false, tabWidth: 4));
        Assert.StartsWith("Lib/C.cs(6,1): warning IDE0055: Fix formatting: indent this line with spaces", DocIndentation.FindingLines([("Lib/C.cs", [6], false, 4)])[0], StringComparison.Ordinal);
    }
}
