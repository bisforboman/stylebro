using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 28's findings in 'stylebro-migrate format', verify, the preview and the baseline.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_GivesOldStyleProjectsARun_NextToMultiTargetedOnes()
    {
        // EpubReader: WpfDemo (TargetFrameworkVersion, no TargetFramework) got no run, and verify said clean.
        var plan = FormatCommand.Plan(new Dictionary<string, string[]> { ["Multi.csproj"] = ["net8.0", "netstandard2.0"], ["Old.csproj"] = [] });

        Assert.Equal([string.Empty, "net8.0", "netstandard2.0"], plan.Select(p => p.Framework));
        Assert.Equal(["Old.csproj"], plan[0].Projects);
        Assert.Contains("(old-style projects, net8.0, netstandard2.0)", FormatCommand.MultiTargetedLine(plan, plan), StringComparison.Ordinal);

        // Without a multi-targeted project one plain run loads them all, as before.
        Assert.Empty(FormatCommand.Plan(new Dictionary<string, string[]> { ["A.csproj"] = ["net8.0"], ["Old.csproj"] = [] }));
    }

    [Fact]
    public void Format_RunsAnotherMergedFramework_WhenTheChosenOneWasSkipped()
    {
        // EpubReader: netstandard2.0 merged into netstandard1.3, which 'dotnet format' couldn't load: neither was checked.
        List<(string Framework, List<string> Projects)> all = [("net46", ["Lib"]), ("netstandard1.3", ["Lib"]), ("netstandard2.0", ["Lib"])];
        List<(string Framework, List<string> Projects)> chosen = [("net46", ["Lib"]), ("netstandard1.3", ["Lib"])];
        string? Key(string project, string framework) => framework.StartsWith("netstandard", StringComparison.Ordinal) ? "standard" : framework;
        var tried = new HashSet<(string, string)>();
        var skipped = new Dictionary<string, IReadOnlyCollection<string>> { ["Lib"] = ["netstandard1.3"] };

        Assert.Equal([("netstandard2.0", "Lib", "netstandard1.3")], FormatCommand.StandIns(all, chosen, skipped, Key, tried));

        // Each is tried once; a framework that compiles other code (net46) stands in for nothing.
        Assert.Empty(FormatCommand.StandIns(all, chosen, skipped, Key, tried));
        Assert.Empty(FormatCommand.StandIns(all, chosen, new Dictionary<string, IReadOnlyCollection<string>> { ["Lib"] = ["net46"] }, Key, new HashSet<(string, string)>()));

        // Skipped where it wasn't run for itself (a reference another project's run loaded): nothing to stand in for.
        Assert.Empty(FormatCommand.StandIns(all, [("net46", ["Lib"])], skipped, Key, new HashSet<(string, string)>()));

        // Unknown code (no key) has nothing to stand in.
        Assert.Empty(FormatCommand.StandIns(all, chosen, skipped, (_, _) => null, new HashSet<(string, string)>()));
    }

    [Fact]
    public void Verify_IsNotVerified_WhenAFrameworksCodeWentUnchecked()
    {
        // Trial 28: "Partly incomplete" (no other framework of that code loaded) was followed by "Clean".
        const string partly = "Partly incomplete: 'dotnet format' skipped Lib for netstandard1.3 (references didn't load): code only that framework compiles (inside #if) wasn't formatted.";
        Assert.True(FormatCommand.Unverified([partly]));
        Assert.True(FormatCommand.Unverified(["Incomplete: 'dotnet format' skipped Lib (references didn't load), so those files weren't formatted."]));
        Assert.False(FormatCommand.Unverified([partly, $"{FormatCommand.LikelyCause} they reference Helpers.fsproj, which 'dotnet format' can't load."]));
        Assert.False(FormatCommand.Unverified(["Clean."]));
        Assert.StartsWith(FormatCommand.PartlyIncompleteMarker, FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["Lib"] = new(["netstandard1.3"]) }, new Dictionary<string, List<string>> { ["Lib"] = ["net46", "netstandard1.3"] })[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_ShowsAFailedBuild_NextToItsFindings()
    {
        // ZLogger: 18 findings and "baseline them", but the Release build also failed on a compile error, which a baseline doesn't fix.
        var project = Path.Combine(root, "App", "App.csproj");
        var file = Path.Combine(root, "App", "Program.cs");
        var said = new List<string>();
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        var line = FormatCommand.UncheckedLine(root, said.Add, (_, _) => (1, [$"{file}(3,1): error BRO1002: Add a space after '//' [{project}]", $"{file}(5,9): error CS0103: The name 'X' does not exist in the current context [{project}]"]));

        Assert.Equal("Release build of App: 1 finding:", said[1]);
        Assert.Equal("Release build of App failed, first error:", said[3]);
        Assert.Equal("  App/Program.cs(5,9): CS0103: The name 'X' does not exist in the current context", said[4]);
        Assert.Contains("baselining doesn't fix that", line, StringComparison.Ordinal);

        // Failed only because the findings are errors (TreatWarningsAsErrors): the findings say it all.
        said.Clear();
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        line = FormatCommand.UncheckedLine(root, said.Add, (_, _) => (1, [$"{file}(3,1): error BRO1002: Add a space after '//' [{project}]"]));
        Assert.DoesNotContain(said, l => l.Contains("failed", StringComparison.Ordinal));
        Assert.StartsWith("Not clean: 1 finding in code only another configuration compiles", line, StringComparison.Ordinal);
        Assert.DoesNotContain("build", line!.Substring(line.IndexOf("(above)", StringComparison.Ordinal)), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_RunsTheConfigurationsWhoseCompileItemsDiffer()
    {
        // A Release-only file ('<Compile Remove>' unless Release) was never formatted or checked: no '#if', so no Release run.
        Assert.True(FormatCommand.PicksCompileByConfiguration("<ItemGroup Condition=\"'$(Configuration)' != 'Release'\">\n  <Compile Remove=\"Telemetry.Release.cs\" />\n</ItemGroup>"));
        Assert.True(FormatCommand.PicksCompileByConfiguration("<Compile Include=\"X.cs\" Condition=\"'$(Configuration)' == 'Debug'\" />"));
        Assert.True(FormatCommand.PicksCompileByConfiguration("<Choose><When Condition=\" '$(Configuration)|$(Platform)' == 'Release|x64' \"><ItemGroup><Compile Include=\"X.cs\" /></ItemGroup></When></Choose>"));
        Assert.False(FormatCommand.PicksCompileByConfiguration("<PropertyGroup Condition=\"'$(Configuration)' == 'Release'\"><Optimize>true</Optimize></PropertyGroup>\n<ItemGroup><Compile Include=\"X.cs\" /></ItemGroup>"));
        Assert.False(FormatCommand.PicksCompileByConfiguration("<ItemGroup Condition=\"'$(TargetFramework)' == 'net8.0'\"><Compile Remove=\"X.cs\" /></ItemGroup>"));

        // In the project file or a Directory.Build.props above it.
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.False(FormatCommand.CompileByConfiguration(Path.Combine(root, "src", "App", "App.csproj")));
        Write("src/Directory.Build.props", "<Project>\n  <ItemGroup Condition=\"'$(Configuration)' == 'Debug'\">\n    <Compile Include=\"../Shared/DebugOnly.cs\" />\n  </ItemGroup>\n</Project>\n");
        Assert.True(FormatCommand.CompileByConfiguration(Path.Combine(root, "src", "App", "App.csproj")));
    }

    [Fact]
    public void Verify_CountsDocCommentIndentation_OnlyWhereTheBuildReportsIt()
    {
        // Owner's decision 2026-10-17: with documentation generated the build reported none of the lines (spaces) or only some (tabs).
        Assert.True(FormatCommand.BuildReportsDocIndentation(string.Empty));
        Assert.True(FormatCommand.BuildReportsDocIndentation("false"));
        Assert.False(FormatCommand.BuildReportsDocIndentation("true"));
        Assert.False(FormatCommand.BuildReportsDocIndentation(" True "));
    }

    [Fact]
    public void Format_NamesTheRestoreWarning_ForWorkspaceWarnings()
    {
        // Text-Grab: "e.g. one in another language" was a guess; the restore's NU1603 was the cause.
        var output = new[]
        {
            $"{Path.Combine(root, "Package", "Package.wapproj")} : warning NU1503: Skipping restore for project. [{Path.Combine(root, "App.sln")}]",
            $"{Path.Combine(root, "App", "App.csproj")} : warning NU1603: A 2.3.5 depends on B (>= 2.1.1) but B 2.1.1 was not found. B 2.1.3 was resolved instead. [{Path.Combine(root, "App.sln")}]",
        };
        var warning = FormatCommand.RestoreWarning(root, output);
        Assert.Equal("App/App.csproj: NU1603: A 2.3.5 depends on B (>= 2.1.1) but B 2.1.1 was not found. B 2.1.3 was resolved instead.", warning);
        Assert.Null(FormatCommand.RestoreWarning(root, ["  Determining projects to restore...", "  All projects are up-to-date for restore."]));

        FormatCommand.ResetExplained();
        var line = FormatCommand.Explain("Warnings were encountered while loading the workspace. Set the verbosity option to the 'diagnostic' level to log warnings.", restoreWarning: warning);
        Assert.Contains("The restore warned too, likely the same: App/App.csproj: NU1603:", line, StringComparison.Ordinal);
        Assert.DoesNotContain("another language", line, StringComparison.Ordinal);
        FormatCommand.ResetExplained();
    }

    [Fact]
    public void Baseline_GoesNextToTheSolutionsProjects_WhicheverWayFormatRuns()
    {
        // Jackett: the preview (on the repository) wrote stylebro.baseline, 'format src/Jackett.sln' src/stylebro.baseline.
        Write("src/App.sln", "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"App\", \"App\\App.csproj\", \"{1}\"\nEndProject\n");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");

        Assert.Equal(Path.Combine(root, "src"), FormatCommand.NewBaselineFolder(root).TrimEnd(Path.DirectorySeparatorChar));
        Assert.Equal(Path.Combine(root, "src", "stylebro.baseline"), FormatCommand.BaselineFile(root));
        Assert.Equal(Path.Combine(root, "src", "stylebro.baseline"), FormatCommand.BaselineFile(Path.Combine(root, "src")));

        // One already at the root stays the one.
        Write("stylebro.baseline", string.Empty);
        Assert.Equal(Path.Combine(root, "stylebro.baseline"), FormatCommand.BaselineFile(root));
    }

    [Fact]
    public void Preview_KeepsEachFilesLineEnding()
    {
        // ZLogger: every file the preview's format changed was CRLF in the patch of an LF repository (no end_of_line).
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n    int  x;\n}\n");
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

            // The whitespace pass on Windows: the line it fixes gets CRLF, so the file mixes them.
            var file = Path.Combine(Path.GetDirectoryName(args[0])!, "C.cs");
            File.WriteAllText(file, File.ReadAllText(file).Replace("    int  x;\n", "    int x;\r\n", StringComparison.Ordinal));
            return 0;
        }

        Capture(() => Assert.Equal(0, PreviewCommand.Run("format", new[] { Path.Combine(root, "App/App.csproj"), "--diff=" + patch }, Format)));

        var text = File.ReadAllText(patch);
        Assert.Contains("+    int x;\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
    }
}
