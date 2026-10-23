using System.Diagnostics;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>
/// "Close the gap first" (owner's decisions 2026-10-20): format and check say "Not verified" (exit code 3) with a list of
/// what they didn't check the way the build does, never "Clean"; 'bro check --build' builds every configuration with
/// warnings as errors and compares with a copy of HEAD.
/// </summary>
[Collection(FormatEnvironmentCollection.Name)]
public sealed class FormatCoverageTests : IDisposable
{
    private readonly List<string> folders = [];

    public void Dispose() => folders.ForEach(f =>
    {
        foreach (var file in Directory.EnumerateFiles(f, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal); // git's read-only object files
        }

        Directory.Delete(f, recursive: true);
    });

    [Fact]
    public void Verify_WithAGap_IsNotVerified_NotClean()
    {
        // MimeKit, PalCalc, EpubReader: a part the runs never checked, and the verdict said "Clean".
        var root = NewFolder();
        var gap = FormatCommand.Gap("App.Android/App.Android.csproj", "it needs Microsoft.Android.Sdk, which this machine doesn't have installed");
        var lines = new List<string>();

        var code = FormatCommand.Verify(["--verify-no-changes"], root, lines.Add, (_, output) =>
        {
            output?.Invoke(gap);
            return 0;
        });

        Assert.Equal(FormatCommand.NotVerifiedExitCode, code);
        Assert.Equal([gap, FormatCommand.NotVerifiedLine(1)], lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("Clean", StringComparison.Ordinal));

        // Without one: clean, as before.
        lines.Clear();
        Assert.Equal(0, FormatCommand.Verify(["--verify-no-changes"], root, lines.Add, (_, _) => 0));
        Assert.Equal(["Clean."], lines);
    }

    [Fact]
    public void Verify_WithFindings_StaysNotClean_AndListsTheGaps()
    {
        var root = NewFolder();
        var gap = FormatCommand.Gap("xunit.analyzers' rules", "no Fix All");
        var lines = new List<string>();

        // An error of its own (a failed restore): its code, the gaps said too.
        Assert.Equal(1, FormatCommand.Verify(["--verify-no-changes"], root, lines.Add, (_, output) =>
        {
            output?.Invoke(gap);
            return 1;
        }));
        Assert.Contains(gap, lines);
        Assert.Equal(1, FormatCommand.WithGaps(1, [gap]));
        Assert.Equal(FormatCommand.NotCleanExitCode, FormatCommand.WithGaps(FormatCommand.NotCleanExitCode, [gap]));
        Assert.Equal(0, FormatCommand.WithGaps(0, []));
    }

    [Fact]
    public void Format_WithAGap_SaysNotEverythingWasChecked_AndExits3()
    {
        var root = NewFolder();
        var gap = FormatCommand.Gap("Lib (netstandard1.3)", "'dotnet format' couldn't load it (references didn't load)");
        var lines = new List<string>();

        var code = FormatCommand.Run([root, FormatCommand.OnceOption], lines.Add, (_, output) =>
        {
            output?.Invoke(gap);
            output?.Invoke(gap); // each run's 'dotnet format' says it: listed once
            return 0;
        });

        Assert.Equal(FormatCommand.NotVerifiedExitCode, code);
        Assert.Contains("Run 1: 0 files changed, but not everything was checked (below).", lines);
        Assert.Single(lines, gap);
        Assert.Equal(FormatCommand.NotVerifiedLine(1), lines[^1]);
        Assert.True(lines.IndexOf(gap) < lines.Count - 1);
        Assert.DoesNotContain(lines, l => l.EndsWith(", clean.", StringComparison.Ordinal));
    }

    [Fact]
    public void Format_NamesTheFilesEachRunUndoes()
    {
        // Trial 48, Pidgin: the SDK's style pass flipped two 'using static' lines in 5 files on every run, also without
        // StyleBro; format said "please report it" (owner's choice 2026-10-23: name the files, stay Not clean).
        var root = NewFolder();
        var flips = Path.Combine(root, "Flips.cs");
        var settles = Path.Combine(root, "Settles.cs");
        File.WriteAllText(flips, "using static P;\nusing static P<char>;\n");
        File.WriteAllText(settles, "class S { }\n");
        var lines = new List<string>();
        var runs = 0;

        var code = FormatCommand.Run([root], lines.Add, (_, _) =>
        {
            runs++;
            var text = File.ReadAllText(flips);
            File.WriteAllText(flips, text.StartsWith("using static P;", StringComparison.Ordinal) ? "using static P<char>;\nusing static P;\n" : "using static P;\nusing static P<char>;\n");
            File.WriteAllText(settles, $"class S {{ int x{runs}; }}\n");
            return 0;
        });

        Assert.Equal(FormatCommand.NotCleanExitCode, code);
        Assert.Equal(FormatCommand.UndoneLine(["Flips.cs"]), lines[^1]);
        Assert.DoesNotContain(lines, l => l.Contains("please report it.", StringComparison.Ordinal) && l.Contains("still changing", StringComparison.Ordinal));

        // 'bro check' says the same where those files are all it would change, with the content 'format' left them in.
        Assert.Equal(["Flips.cs"], FormatCommand.RecordedUndone(root, [flips]));
        Assert.Null(FormatCommand.RecordedUndone(root, [flips, settles]));
        File.AppendAllText(flips, "// edited\n");
        Assert.Null(FormatCommand.RecordedUndone(root, [flips]));

        // A file that keeps changing to new text isn't undone: the old line stays.
        File.WriteAllText(flips, "using static P;\n");
        lines.Clear();
        Assert.Equal(FormatCommand.NotCleanExitCode, FormatCommand.Run([root], lines.Add, (_, _) =>
        {
            File.AppendAllText(flips, "// more\n");
            return 0;
        }));
        Assert.StartsWith("Not clean: still changing after 3 runs.", lines[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void HeldBackSdkRules_AreListed_ButLeaveTheVerdictClean()
    {
        // Owner 2026-10-22 ("Note it, don't fail"): SDK 10.0.1xx's 'dotnet format' ignores the multi-target guard, so IDE0049 was
        // held back in every multi-targeted repository and nothing could end clean there.
        var root = NewFolder();
        var heldBack = FormatCommand.HeldBackGap(["IDE0049"]);
        var lines = new List<string>();

        Assert.Equal(0, FormatCommand.Verify(["--verify-no-changes"], root, lines.Add, (_, output) =>
        {
            output?.Invoke(heldBack);
            return 0;
        }));
        Assert.Equal([heldBack, "Clean."], lines);

        lines.Clear();
        Assert.Equal(0, FormatCommand.Run([root, FormatCommand.OnceOption], lines.Add, (_, output) =>
        {
            output?.Invoke(heldBack);
            return 0;
        }));
        Assert.Contains("Run 1: 0 files changed, clean.", lines);
        Assert.Single(lines, heldBack);
        Assert.DoesNotContain(lines, l => l.StartsWith(FormatCommand.NotVerifiedMarker, StringComparison.Ordinal));

        // With a gap that counts, it's still "Not verified", counting that one only.
        var gap = FormatCommand.Gap("Lib", "'dotnet format' couldn't load it (references didn't load)");
        lines.Clear();
        Assert.Equal(FormatCommand.NotVerifiedExitCode, FormatCommand.Verify(["--verify-no-changes"], root, lines.Add, (_, output) =>
        {
            output?.Invoke(heldBack);
            output?.Invoke(gap);
            return 0;
        }));
        Assert.Equal([heldBack, gap, FormatCommand.NotVerifiedLine(1)], lines);
        Assert.Equal(0, FormatCommand.WithGaps(0, [heldBack]));
        Assert.Equal(FormatCommand.NotVerifiedExitCode, FormatCommand.WithGaps(0, [heldBack, gap]));
        Assert.Equal([gap], FormatCommand.Unverified([heldBack, gap]));
    }

    [Fact]
    public void SkippedProjects_AreGaps_WholeOrForSomeFrameworks()
    {
        var skipped = new Dictionary<string, SortedSet<string>> { ["Lib"] = new(["netstandard1.3"]), ["Tests"] = new([string.Empty]), ["All"] = new(["net10.0", "net8.0"]) };
        var frameworks = new Dictionary<string, List<string>> { ["Lib"] = ["netstandard1.3", "netstandard2.0"], ["All"] = ["net8.0", "net10.0"] };

        Assert.Equal(
            [
                "Not checked: All: 'dotnet format' couldn't load it (references didn't load)",
                "Not checked: Lib (netstandard1.3): 'dotnet format' couldn't load it (references didn't load), so the code only that framework compiles",
                "Not checked: Tests: 'dotnet format' couldn't load it (references didn't load)",
            ],
            FormatCommand.SkippedGaps(skipped, frameworks));
        Assert.Equal(["Not checked: App/App.csproj: it needs Microsoft.iOS.Sdk, which this machine doesn't have installed"], FormatCommand.WorkloadGaps([("App/App.csproj", ["Microsoft.iOS.Sdk"])]));
        Assert.Equal("A, B, C and 2 more", FormatCommand.ShortList(["A", "B", "C", "D", "E"]));
    }

    [Fact]
    public void OtherConfigurations_PastTheMost_AreKnown()
    {
        // The most it runs (4) is a gap past it: the build of those configurations isn't checked.
        string[] solution = ["Debug", "Release", "Debug|x64", "Release|x64", "Debug|ARM64", "Release|ARM64"];
        Assert.Equal(FormatCommand.MaxConfigurations, FormatCommand.OtherConfigurations("Debug;Release", "Debug", solution, "AnyCPU;x64;ARM64").Count);
        Assert.Equal(["Release|ARM64"], FormatCommand.OtherConfigurations("Debug;Release", "Debug", solution, "AnyCPU;x64;ARM64", int.MaxValue).Skip(FormatCommand.MaxConfigurations));
    }

    [Fact]
    public void BuildMessages_AreCountedWithoutTheirLines()
    {
        var root = NewFolder();
        var file = Path.Combine(root, "App", "A.cs");
        var project = Path.Combine(root, "App", "App.csproj");
        string[] output =
        [
            $"{file}(3,1): error BRO1002: Add a space after '//' [{project}::TargetFramework=net8.0]",
            $"{file}(3,1): error BRO1002: Add a space after '//' [{project}::TargetFramework=net8.0]", // the summary repeats it
            $"{file}(9,5): error CS0618: 'X' is obsolete [{project}]",
            $"{project} : error NU1701: Package 'Old' was restored using '.NETFramework' [{project}]",
            "Build FAILED.",
        ];

        var messages = FormatCommand.BuildMessages(output, root);

        Assert.Equal(3, messages.Count);
        Assert.Equal("  App/A.cs(3,1): BRO1002: Add a space after '//' (net8.0)", messages[0].Shown);
        Assert.Equal("NU1701", messages[2].Id);

        // The fix moved the obsolete call two lines down: the same error, not a new one; the BRO finding always counts.
        var untouched = FormatCommand.BuildMessages([$"{file}(7,5): error CS0618: 'X' is obsolete [{project}]", $"{project} : error NU1701: Package 'Old' was restored using '.NETFramework' [{project}]"], root);
        var (findings, newErrors, old) = FormatCommand.CompareBuild(messages, untouched, new HashSet<string>(["BRO1002"]));
        Assert.Single(findings);
        Assert.Empty(newErrors);
        Assert.Equal(2, old);

        // Without the untouched build (no git): every error counts.
        Assert.Equal(2, FormatCommand.CompareBuild(messages, null, new HashSet<string>(["BRO1002"])).New.Count);
    }

    [Fact]
    public void BuildCheck_BuildsEveryConfiguration_AndComparesWithTheUntouchedCode()
    {
        var root = NewFolder();
        var untouchedRoot = NewFolder();
        var solution = Path.Combine(root, "App.sln");
        File.WriteAllText(solution, "Global\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\n\t\tDebug|Any CPU = Debug|Any CPU\n\t\tRelease|x64 = Release|x64\n\tEndGlobalSection\nEndGlobal\n");
        var built = new List<(string Folder, string Configuration)>();
        (int, IReadOnlyList<string>) Build(string folder, string workspace, string configuration)
        {
            built.Add((folder, configuration));
            var file = Path.Combine(folder, "A.cs");
            return folder == untouchedRoot
                ? (1, [$"{file}(1,1): error CS0618: 'X' is obsolete"])
                : configuration == "Release|x64" ? (1, [$"{file}(1,1): error CS0618: 'X' is obsolete", $"{file}(5,1): error CS0103: The name 'Y' does not exist"]) : (1, [$"{file}(1,1): error CS0618: 'X' is obsolete"]);
        }

        var lines = new List<string>();
        var line = FormatCommand.BuildCheckLine(root, solution, lines.Add, Build, () => untouchedRoot);

        Assert.Equal([(root, "Debug"), (untouchedRoot, "Debug"), (root, "Release|x64"), (untouchedRoot, "Release|x64")], built);
        Assert.Contains("Debug: no findings, no new errors (1 error the untouched code has too, not counted).", lines);
        Assert.Contains("Release|x64: 1 new error (1 error the untouched code has too, not counted):", lines);
        Assert.Contains("  A.cs(5,1): CS0103: The name 'Y' does not exist", lines);
        Assert.Equal("Not clean: the Release|x64 build failed where the untouched code's didn't (above).", line);

        // The same code as HEAD (""): no second build, and errors other than format's rules are the code's own.
        built.Clear();
        Assert.Null(FormatCommand.BuildCheckLine(root, solution, _ => { }, Build, () => string.Empty));
        Assert.Equal([(root, "Debug"), (root, "Release|x64")], built);

        // A finding of format's rules always counts.
        var finding = FormatCommand.BuildCheckLine(root, solution, _ => { }, (folder, _, _) => (1, [$"{Path.Combine(folder, "A.cs")}(1,1): error BRO1002: Add a space after '//'"]), () => string.Empty);
        Assert.StartsWith("Not clean: the builds report 2 findings of format's rules", finding, StringComparison.Ordinal);

        // Failed like the untouched code's (sqlite-net: NETSDK1147 at the restore, nothing compiled): not verified, not clean.
        var both = FormatCommand.BuildCheckLine(root, solution, _ => { }, (folder, _, _) => (1, [$"{Path.Combine(folder, "A.csproj")} : error NETSDK1147: To build this project, the following workloads must be installed: ios"]), () => untouchedRoot);
        Assert.StartsWith(FormatCommand.NotVerifiedMarker + " the Debug, Release|x64 builds failed, as the untouched code's do", both, StringComparison.Ordinal);

        // Failed without a line that explains it, like the untouched code's: not verified either.
        Assert.StartsWith(FormatCommand.NotVerifiedMarker, FormatCommand.BuildCheckLine(root, solution, _ => { }, (_, _, _) => (1, []), () => string.Empty), StringComparison.Ordinal);

        // No git: a failed build no line explains counts.
        Assert.Equal("Not clean: the Debug, Release|x64 builds failed (above).", FormatCommand.BuildCheckLine(root, solution, _ => { }, (_, _, _) => (1, []), () => null));
    }

    [Fact]
    public void TheUntouchedCopy_IsHeads()
    {
        var root = NewFolder();
        Git(root, "init", "-q");
        File.WriteAllText(Path.Combine(root, "A.cs"), "class A { }\n");
        File.WriteAllText(Path.Combine(root, "Gone.cs"), "class Gone { }\n");
        Git(root, "add", ".");
        Git(root, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "x");
        Assert.Equal([], FormatCommand.ChangedSinceHead(root));

        // What format changed, deleted and added.
        File.WriteAllText(Path.Combine(root, "A.cs"), "class A\n{\n}\n");
        File.Delete(Path.Combine(root, "Gone.cs"));
        File.WriteAllText(Path.Combine(root, "New.cs"), "class New { }\n");
        File.WriteAllText(Path.Combine(root, "Staged.cs"), "class Staged { }\n");
        Git(root, "add", "Staged.cs");
        var changed = FormatCommand.ChangedSinceHead(root)!;
        Assert.Equal([("A.cs", true), ("Gone.cs", true), ("New.cs", false), ("Staged.cs", false)], changed.OrderBy(c => c.Path, StringComparer.Ordinal));

        var copy = NewFolder();
        File.WriteAllText(Path.Combine(copy, "A.cs"), "class A\n{\n}\n");
        File.WriteAllText(Path.Combine(copy, "New.cs"), "class New { }\n");
        FormatCommand.ToHead(root, copy, changed);
        Assert.Equal("class A { }\n", File.ReadAllText(Path.Combine(copy, "A.cs")));
        Assert.Equal("class Gone { }\n", File.ReadAllText(Path.Combine(copy, "Gone.cs")));
        Assert.False(File.Exists(Path.Combine(copy, "New.cs")));

        // Not a repository: no comparison.
        Assert.Null(FormatCommand.ChangedSinceHead(NewFolder()));
    }

    [Fact]
    public void Build_GoesWithAWholeCheck()
    {
        var root = NewFolder();
        var lines = new List<string>();
        Assert.Equal(1, FormatCommand.Run([root, FormatCommand.BuildOption], lines.Add, (_, _) => 0));
        Assert.Equal(1, FormatCommand.Run([root, "--verify-no-changes", FormatCommand.BuildOption, FormatCommand.FilesOption, "A.cs"], lines.Add, (_, _) => 0));
        Assert.All(lines, l => Assert.StartsWith(FormatCommand.BuildOption, l, StringComparison.Ordinal));
    }

    private static void Git(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true };
        arguments.ToList().ForEach(start.ArgumentList.Add);
        using var process = Process.Start(start)!;
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
    }

    private string NewFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "stylebro-coverage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        folders.Add(folder);
        return folder;
    }
}
