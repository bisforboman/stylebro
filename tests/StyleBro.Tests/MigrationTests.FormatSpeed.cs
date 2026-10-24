using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Phase 0 of docs/proposals/format-speed.md: the restore skip, verify from the clean record, generator builds together, --timings.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void ACopysPaths_MoveToTheRepositorys_AndBack()
    {
        // check's kept-findings copy runs in the repository's host (--engine host): what the copy's run hands it, and what it prints.
        var sep = Path.DirectorySeparatorChar;
        var copy = $"{sep}tmp{sep}stylebro-verify-1{sep}";
        var repository = $"{sep}src{sep}repo{sep}";
        Assert.Equal($"{repository}Lib{sep}A.cs(1,1): warning BRO1001", FormatCommand.MovePaths($"{copy}Lib{sep}A.cs(1,1): warning BRO1001", copy, repository));
        Assert.Equal(repository.TrimEnd(sep), FormatCommand.MovePaths(copy.TrimEnd(sep), copy, repository));
        Assert.Equal("--include", FormatCommand.MovePaths("--include", copy, repository));
        Assert.Equal($"{sep}tmp{sep}stylebro-verify-10{sep}A.cs", FormatCommand.MovePaths($"{sep}tmp{sep}stylebro-verify-10{sep}A.cs", copy, repository));
    }

    [Fact]
    public void ARunsFiles_AreNamedByTheProcessAndTheirContent()
    {
        // --engine host keys a configuration's workspace by them: run 2 of a command found a new path and loaded it again.
        var first = FormatCommand.RunFile("stylebro-format-", ".targets", "<Project />");
        Assert.Equal(first, FormatCommand.RunFile("stylebro-format-", ".targets", "<Project />"));
        Assert.NotEqual(first, FormatCommand.RunFile("stylebro-format-", ".targets", "<Project></Project>"));
        Assert.StartsWith(Path.Combine(Path.GetTempPath(), $"stylebro-format-{Environment.ProcessId}-"), first, StringComparison.Ordinal);
        Assert.EndsWith(".targets", first, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_SkipsTheRestore_OnlyWhenItsOutputIsCurrent()
    {
        // PalCalc: 'dotnet restore' of an already restored solution took 17.5 s before every format and verify.
        var (project, assets, cache, package) = RestoredProject();
        List<(string Project, string AssetsFile)> projects = [(project, assets)];
        var stamps = Path.Combine(root, "stamps");

        Assert.True(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        Assert.False(FormatCommand.RestoreIsCurrent(root, [], stamps)); // nothing to tell by

        // An input newer than the output: the project file, a props file above the project, a nuget.config.
        foreach (var input in new[] { project, Path.Combine(root, "Directory.Build.props"), Path.Combine(root, "nuget.config"), Path.Combine(root, "src", "global.json") })
        {
            Write(Path.GetRelativePath(root, input), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.SetLastWriteTimeUtc(input, DateTime.UtcNow.AddMinutes(2));
            Assert.False(FormatCommand.RestoreIsCurrent(root, projects, stamps));
            File.SetLastWriteTimeUtc(input, DateTime.UtcNow.AddMinutes(-10));
        }

        // Not restored with StyleBro.Analyzers, a failed restore, a package file gone, no assets.
        var listed = File.ReadAllText(assets);
        File.WriteAllText(assets, listed.Replace("StyleBro.Analyzers", "Other.Package", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(assets, DateTime.UtcNow.AddMinutes(1));
        Assert.False(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        File.WriteAllText(assets, listed);
        File.SetLastWriteTimeUtc(assets, DateTime.UtcNow.AddMinutes(1));
        var succeeded = File.ReadAllText(cache);
        File.WriteAllText(cache, succeeded.Replace("true", "false", StringComparison.Ordinal));
        File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(1));
        Assert.False(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        File.WriteAllText(cache, succeeded);
        File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(1));
        Assert.True(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        File.Move(package, package + ".gone");
        Assert.False(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        File.Move(package + ".gone", package);
        Assert.False(FormatCommand.RestoreIsCurrent(root, [(project, Path.Combine(root, "src", "obj", "missing.json"))], stamps));
        Assert.False(FormatCommand.RestoreIsCurrent(root, [(project, string.Empty)], stamps));

        // --restore restores anyway, --no-restore never; old-style projects aren't restored by format, so they don't count.
        Assert.True(FormatCommand.SkipsRestore(noRestore: false, forceRestore: false, () => true));
        Assert.False(FormatCommand.SkipsRestore(noRestore: false, forceRestore: true, () => true));
        Assert.False(FormatCommand.SkipsRestore(noRestore: true, forceRestore: false, () => true));
        Assert.False(FormatCommand.SkipsRestore(noRestore: false, forceRestore: false, () => false));
        Write("src/Old.csproj", "<Project ToolsVersion=\"15.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"></Project>");
        Assert.Equal([project], FormatCommand.RestoredProjects([(project, assets), (Path.Combine(root, "src", "Old.csproj"), string.Empty)]).Select(p => p.Project));
    }

    [Fact]
    public void Format_RestoreInputs_IncludeTheSettingsAboveTheRepository()
    {
        // Directory.Build.props, Directory.Packages.props and nuget.config above the repository take part in its restore too.
        Write("Directory.Packages.props", "<Project />");
        Write("repo/.git/HEAD", "ref: refs/heads/main");
        Write("repo/src/App.csproj", "<Project />");
        Write("repo/src/obj/App.csproj.nuget.g.props", "<Project />");
        Write("repo/src/Code.cs", "class C { }");
        Write("repo/NuGet.Config", "<configuration />");

        var inputs = FormatCommand.RestoreInputs(Path.Combine(root, "repo", "src")).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).ToList();

        Assert.Contains("Directory.Packages.props", inputs);
        Assert.Contains("repo/src/App.csproj", inputs);
        Assert.Contains("repo/NuGet.Config", inputs);
        Assert.DoesNotContain("repo/src/obj/App.csproj.nuget.g.props", inputs);
        Assert.DoesNotContain("repo/src/Code.cs", inputs);
    }

    [Fact]
    public void Verify_RunsTheChecks_AfterARunThatChangedLineEndingsOnly()
    {
        // Its check was of the files before the run changed them: what verify's checks find now isn't known.
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X { }\n");
        FormatCommand.Run([root], _ => { }, (_, _) =>
        {
            FormatCommand.MarkCheckedClean(root);
            File.WriteAllText(Path.Combine(root, "src", "A", "X.cs"), "class X { }\n", new System.Text.UTF8Encoding(true));
            return 0;
        });

        Assert.NotNull(FormatCommand.LoadRecord(root, [root]));
        Assert.False(FormatCommand.LoadRecord(root, [root])!.Checked);
    }

    [Fact]
    public void Format_SkipsTheRestore_AfterItsOwnNoOpRestore()
    {
        // A restore that changes nothing doesn't write its output again: after a props edit it stayed older than the props, and
        // every later run restored again. format's own restore of the same output counts.
        var (project, assets, cache, _) = RestoredProject();
        List<(string Project, string AssetsFile)> projects = [(project, assets)];
        var stamps = Path.Combine(root, "stamps");
        File.SetLastWriteTimeUtc(assets, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(-5));
        Write("Directory.Build.props", "<Project />");
        File.SetLastWriteTimeUtc(Path.Combine(root, "Directory.Build.props"), DateTime.UtcNow.AddMinutes(-1));
        Assert.False(FormatCommand.RestoreIsCurrent(root, projects, stamps));

        FormatCommand.SaveRestoreStamp(root, [assets], "src/App.csproj: NU1603: A newer version was resolved.", stamps);

        Assert.True(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        Assert.Equal("src/App.csproj: NU1603: A newer version was resolved.", FormatCommand.SkippedRestoreWarning(root, projects, stamps));

        // Written since (a restore format didn't make): the stamp no longer speaks for it.
        File.SetLastWriteTimeUtc(assets, DateTime.UtcNow.AddMinutes(-4));
        Assert.False(FormatCommand.RestoreIsCurrent(root, projects, stamps));
        Assert.Null(FormatCommand.SkippedRestoreWarning(root, projects, stamps));
    }

    [Fact]
    public void Format_SaysTheSkippedRestoresWarning_FromTheAssetsLogs()
    {
        // A no-op 'dotnet restore' prints the warnings the assets file keeps; 'format' named the first in its workspace note.
        var (project, assets, _, _) = RestoredProject();
        var text = File.ReadAllText(assets).Replace("\"logs\": []", $"\"logs\": [ {{ \"code\": \"NU1603\", \"level\": \"Warning\", \"message\": \"A newer version was resolved.\", \"projectPath\": {System.Text.Json.JsonSerializer.Serialize(project)} }} ]", StringComparison.Ordinal);
        File.WriteAllText(assets, text);

        Assert.Equal("src/App.csproj: NU1603: A newer version was resolved.", FormatCommand.SkippedRestoreWarning(root, [(project, assets)], Path.Combine(root, "stamps")));
    }

    [Fact]
    public void Format_RestoreOptions_AreFormatsOwn()
    {
        // --restore and --timings say how to run, like --no-restore: the record of a clean run doesn't depend on them.
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X { }");
        Assert.Equal(FormatCommand.StateKey(root, [root]), FormatCommand.StateKey(root, [root, FormatCommand.RestoreOption, FormatCommand.TimingsOption, "--no-restore"]));
        Assert.NotEqual(FormatCommand.StateKey(root, [root]), FormatCommand.StateKey(root, [root, "--all"]));
    }

    [Fact]
    public void Verify_RecordKey_HoldsTheSdkAndPackageVersions()
    {
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/obj/project.assets.json", "{ \"libraries\": { \"StyleBro.Analyzers/1.0.0\": { } } }");

        Assert.Equal("sdk 10.0.100; StyleBro.Analyzers 1.0.0", FormatCommand.Versions(root, _ => "10.0.100\n"));
        Assert.NotEqual(FormatCommand.Versions(root, _ => "10.0.100"), FormatCommand.Versions(root, _ => "10.0.200"));
        Write("src/A/obj/project.assets.json", "{ \"libraries\": { \"StyleBro.Analyzers/1.1.0\": { } } }");
        Assert.Equal("sdk 10.0.100; StyleBro.Analyzers 1.1.0", FormatCommand.Versions(root, _ => "10.0.100"));
        Assert.Equal("sdk unknown; StyleBro.Analyzers 1.1.0", FormatCommand.Versions(root, _ => throw new ToolException("no dotnet")));

        // In the record's key: another package version (restored, so in obj/, which the file hashes leave out) is another state.
        var key = FormatCommand.StateKey(root, [root]);
        Write("src/A/obj/project.assets.json", "{ \"libraries\": { \"StyleBro.Analyzers/1.2.0\": { } } }");
        Assert.NotEqual(key, FormatCommand.StateKey(root, [root]));
    }

    [Fact]
    public void Verify_AnswersFromTheRecord_AfterACleanCheckedFormat()
    {
        // PalCalc: --verify-no-changes right after a clean 'format' took 191 s to find what that format's last check had found.
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X { }");
        var formatted = new List<string>();
        Assert.Equal(0, FormatCommand.Run([root], formatted.Add, (args, _) =>
        {
            FormatCommand.MarkCheckedClean(root); // every run's check was clean
            return 0;
        }));
        Assert.Contains("Run 1: 0 files changed, clean.", formatted);

        var lines = new List<string>();
        var code = FormatCommand.Run([root, "--verify-no-changes"], lines.Add, (_, _) => throw new InvalidOperationException("verify ran 'dotnet format'"));

        Assert.Equal(0, code);
        Assert.StartsWith(FormatCommand.FromRecordHeader, lines[0], StringComparison.Ordinal);
        Assert.Equal("Clean.", lines[^1]);

        // Another state: the checks run.
        Write("src/A/X.cs", "class X { int a; }");
        var ran = false;
        Assert.Equal(0, FormatCommand.Run([root, "--verify-no-changes"], _ => { }, (_, _) =>
        {
            ran = true;
            return 0;
        }));
        Assert.True(ran);
    }

    [Fact]
    public void Verify_RunsTheChecks_WhenTheCleanRunDidntCheckEveryRun()
    {
        // A first run of one plain 'dotnet format' (no check) that changed nothing, or a check that loaded only some projects
        // ("Incomplete"): verify can't know what its checks find.
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X { }");
        foreach (var marked in new[] { false, true })
        {
            FormatCommand.Run([root], _ => { }, (_, output) =>
            {
                if (marked)
                {
                    FormatCommand.MarkCheckedClean(root);
                    output?.Invoke("Incomplete: B wasn't loaded in any run.");
                }

                return 0;
            });

            var ran = false;
            FormatCommand.Run([root, "--verify-no-changes"], _ => { }, (_, _) =>
            {
                ran = true;
                return 0;
            });
            Assert.True(ran);
        }
    }

    [Fact]
    public void Verify_FromTheRecord_ListsTheKeptFindingsItsCheckReports_AndBuildsTheGeneratorConfigurations()
    {
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X\n{\n    int a;\n}\n");
        var kept = new KeptFinding(Path.Combine(root, "src", "A", "X.cs"), 3, 9, "BRO1306", KeptReason.NameInString, "Rename 'a' to 'A'");
        var project = Path.Combine(root, "src", "A", "A.csproj");
        string[] formatArgs = [root, "--no-restore", FormatCommand.CheckFirstOption];
        var verdict = FormatCommand.CleanVerdict(root, [kept]);
        FormatCommand.SaveKept(root, formatArgs, [kept], verdict, [(project, "Release")], "Configurations: A: it takes an in-solution source generator ...");

        var record = FormatCommand.LoadRecord(root, [root, "--verify-no-changes"])!;
        Assert.True(record.Checked);
        Assert.Equal([kept.ToString()], record.Kept.Select(k => k.ToString()));
        Assert.Equal(new List<(string, string)> { (project, "Release") }, record.Builds);
        Assert.Equal(verdict, record.Verdict);
        Assert.Equal(kept.ToString(), FormatCommand.LoadKept(root, [root])!.Single().ToString()); // the old reader still reads it

        var lines = new List<string>();
        var built = new List<string>();
        Assert.Equal(0, FormatCommand.Verify([root, "--verify-no-changes"], root, lines.Add, (_, _) => throw new InvalidOperationException("ran")));
        Assert.Equal("Configurations: A: it takes an in-solution source generator ...", lines[1]);
        Assert.Equal(verdict, lines.Skip(2));
        Assert.StartsWith("Clean: 1 finding kept on purpose", verdict[0], StringComparison.Ordinal);

        // The build is the check of the generated code's configuration: it still runs.
        Assert.Null(FormatCommand.UncheckedLine(root, lines.Add, (p, c) =>
        {
            built.Add($"{Path.GetFileName(p)} {c}");
            return (0, []);
        }));
        Assert.Equal(["A.csproj Release"], built);

        // A record without a check of every run (a plain first run) only names the kept findings.
        FormatCommand.SaveKept(root, formatArgs, [kept]);
        Assert.False(FormatCommand.LoadRecord(root, [root])!.Checked);
        Assert.Null(FormatCommand.VerifyFromRecord([root, "--verify-no-changes"], root, _ => { }));
    }

    [Fact]
    public void Verify_BuildsTheGeneratorConfigurationsTogether_AndAloneWhenThatFails()
    {
        // CliFx: two '--no-incremental' Release builds one after another, 99 s of a 160 s verify, rebuilding the same references.
        var app = Path.Combine(root, "src", "App", "App.csproj");
        var tests = Path.Combine(root, "src", "Tests", "Tests.csproj");
        var appFile = Path.Combine(root, "src", "App", "A.cs");
        var output = new List<string>
        {
            $"{appFile}(3,1): warning BRO1002: Add a space after '//' [{app}]",
            $"{Path.Combine(root, "src", "Tests", "T.cs")}(1,1): warning CS1591: Missing XML comment [{tests}]",
        };
        var together = new List<string>();
        var alone = new List<string>();
        (int, IReadOnlyList<string>) Alone(string p, string c)
        {
            alone.Add(Path.GetFileName(p));
            return (0, output.Where(l => l.EndsWith($"[{p}]", StringComparison.Ordinal)).ToList());
        }

        var said = new List<string>();
        FormatCommand.RecordUnchecked(root, [(app, "Release"), (tests, "Release"), (app, "Staging")]);
        var line = FormatCommand.UncheckedLine(root, said.Add, Alone, (ps, c) =>
        {
            together.Add($"{string.Join(",", ps.Select(Path.GetFileName))} {c}");
            return (0, output);
        });

        Assert.Equal(["App.csproj,Tests.csproj Release"], together);
        Assert.Equal(["App.csproj"], alone); // Staging: one project, built as before
        Assert.Contains("Release build of App: 1 finding:", said);
        Assert.Contains("Release build of Tests: no findings.", said);
        Assert.StartsWith("Not clean: 2 findings in code only another configuration compiles", line, StringComparison.Ordinal);

        // A failed build doesn't tell whose: each builds alone, as before.
        alone.Clear();
        FormatCommand.RecordUnchecked(root, [(app, "Release"), (tests, "Release")]);
        FormatCommand.UncheckedLine(root, said.Add, Alone, (_, _) => (1, ["MSBUILD : error MSB1009: Project file does not exist."]));
        Assert.Equal(["App.csproj", "Tests.csproj"], alone);
    }

    [Fact]
    public void Verify_SplitsABuildOfSeveralProjects_IntoEachProjectsOwnOutput()
    {
        var lib = Path.Combine(root, "Lib", "Lib.csproj");
        var app = Path.Combine(root, "App", "App.csproj");
        var other = Path.Combine(root, "Other", "Other.csproj");
        string[] output =
        [
            $"x.cs(1,1): warning BRO1002: x [{app}::TargetFramework=net8.0]",
            $"y.cs(1,1): warning CS0168: y [{lib}]",
            $"z.cs(1,1): error CS0103: z [{other}]",
            "Build FAILED.",
        ];

        Assert.Equal([output[0], output[3]], FormatCommand.OwnOutput(app, output));
        Assert.Equal([output[1], output[3]], FormatCommand.OwnOutput(lib, output));
        Assert.Equal([Path.GetFullPath(app), Path.GetFullPath(lib)], FormatCommand.ProjectGraph(app, p => p == Path.GetFullPath(app) ? [lib] : []));

        Assert.Equal(["Release: App.csproj, Lib.csproj", "Debug: App.csproj"], FormatCommand.BuildGroups([(app, "Release"), (lib, "Release"), (app, "Debug"), (app, "Release")]).Select(g => $"{g.Configuration}: {string.Join(", ", g.Projects.Select(Path.GetFileName))}"));
        var text = FormatCommand.TogetherProject([app, "a&b.csproj"]);
        Assert.Contains($"<StyleBroCheck Include=\"{app}\" />", text, StringComparison.Ordinal);
        Assert.Contains("<StyleBroCheck Include=\"a&amp;b.csproj\" />", text, StringComparison.Ordinal);
        Assert.Contains("Targets=\"Rebuild\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_PlansTheViewsOfTheConfigurationRuns_AtOnce()
    {
        // PalCalc: 11 evaluations one after another (~3 s each) for the projects its Release runs reach through references.
        var references = new Dictionary<string, string[]> { [Path.GetFullPath("/r/App.csproj")] = [Path.GetFullPath("/r/Lib.csproj")], [Path.GetFullPath("/r/Lib.csproj")] = [Path.GetFullPath("/r/Core.csproj")] };
        IEnumerable<string> References(string p) => references.TryGetValue(Path.GetFullPath(p), out var r) ? r : [];

        var views = FormatCommand.RunViews([("net8.0", "Release", ["/r/App.csproj"]), ("net8.0", "Release", ["/r/Lib.csproj"]), (string.Empty, "Staging", ["/r/Core.csproj"])], References);

        Assert.Equal(
            ["App net8.0 Release", "Core  Staging", "Core net8.0 Release", "Lib net8.0 Release"],
            views.Select(v => $"{Path.GetFileNameWithoutExtension(v.Project)} {v.Framework} {v.Configuration}").Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Format_Timings_ListsEachPhase()
    {
        Write("src/A/A.csproj", "<Project />");
        Write("src/A/X.cs", "class X { }");
        var lines = new List<string>();
        string[]? passed = null;

        FormatCommand.Run([root, FormatCommand.TimingsOption, FormatCommand.OnceOption], lines.Add, (args, _) =>
        {
            passed = args;
            return 0;
        });

        Assert.DoesNotContain(FormatCommand.TimingsOption, passed!);
        var at = lines.FindIndex(l => l.StartsWith("Timings", StringComparison.Ordinal));
        Assert.True(at > 0);
        Assert.Contains(lines.Skip(at), l => l.TrimStart().StartsWith("total", StringComparison.Ordinal) && l.EndsWith(" s", StringComparison.Ordinal));
        Assert.Contains(lines.Skip(at), l => l.TrimStart().StartsWith("run 1", StringComparison.Ordinal));

        // Off by default: nothing measured, nothing said.
        lines.Clear();
        FormatCommand.Run([root, FormatCommand.OnceOption], lines.Add, (_, _) => 0);
        Assert.DoesNotContain(lines, l => l.StartsWith("Timings", StringComparison.Ordinal));
    }

    /// <summary>A project under src/ restored with StyleBro.Analyzers, its output newer than its inputs (project, assets, cache, the package's file).</summary>
    private (string Project, string Assets, string Cache, string Package) RestoredProject()
    {
        Write("src/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("packages/stylebro.analyzers/1.0.0/stylebro.analyzers.1.0.0.nupkg.sha512", "x");
        var project = Path.Combine(root, "src", "App.csproj");
        var package = Path.Combine(root, "packages", "stylebro.analyzers", "1.0.0", "stylebro.analyzers.1.0.0.nupkg.sha512");
        Write("src/obj/project.assets.json", "{ \"libraries\": { \"StyleBro.Analyzers/1.0.0\": { } }, \"logs\": [] }");
        Write("src/obj/project.nuget.cache", $"{{ \"version\": 2, \"success\": true, \"expectedPackageFiles\": [ {System.Text.Json.JsonSerializer.Serialize(package)} ] }}");
        var assets = Path.Combine(root, "src", "obj", "project.assets.json");
        var cache = Path.Combine(root, "src", "obj", "project.nuget.cache");
        File.SetLastWriteTimeUtc(project, DateTime.UtcNow.AddMinutes(-10));
        File.SetLastWriteTimeUtc(assets, DateTime.UtcNow.AddMinutes(1));
        File.SetLastWriteTimeUtc(cache, DateTime.UtcNow.AddMinutes(1));
        return (project, assets, cache, package);
    }
}
