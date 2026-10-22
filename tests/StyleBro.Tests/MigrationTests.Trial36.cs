using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the thirty-sixth trial (C-Sharp-Algorithms, SharpZipLib, NodaTime, WireMock.Net) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Preview_WithAGapInTheLastRun_SaysNotVerified_AndExitsWith3()
    {
        // SharpZipLib: format held IDE0049 back (SDK 10.0.113 ignores the multi-target guard) and said "Not verified" with
        // exit code 3, but the preview of the same steps said "clean after 1 run" and exited with 0.
        // (Since 2026-10-22 a held-back SDK rule is only a note; a project that didn't load still counts.)
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "class C { int b; int a; }\n");
        PreviewCommand.MakeRepository(root);
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        var gap = FormatCommand.Gap("Tests (net8.0)", "'dotnet format' couldn't load it (references didn't load)");
        var runs = 0;
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

            // Only the last run's gaps count: the first one's go when a later run doesn't say them.
            if (++runs == 1)
            {
                log(FormatCommand.Gap("Lib", "'dotnet format' couldn't load it (references didn't load)"));
            }

            log(gap);
            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C { int a; int b; }\n");
            return 0;
        }

        var output = Capture(() => Assert.Equal(FormatCommand.NotVerifiedExitCode, PreviewCommand.Run("init", new[] { root, "--diff=" + patch }, Format)));

        Assert.Equal(2, runs);
        Assert.Contains("Format: 1 file, 1 reported change, nothing left to change after 1 run, but not verified (below)", output, StringComparison.Ordinal);
        Assert.Contains(gap, output, StringComparison.Ordinal);
        Assert.DoesNotContain("Not checked: Lib", output, StringComparison.Ordinal);
        Assert.Contains(FormatCommand.NotVerifiedLine(1), output, StringComparison.Ordinal);
        Assert.DoesNotContain("clean after", output, StringComparison.Ordinal);
        DeleteFolder(Path.GetDirectoryName(patch)!);
    }

    [Fact]
    public void Preview_WithOnlyAHeldBackSdkRule_ListsIt_AndIsClean()
    {
        // Owner 2026-10-22 ("Note it, don't fail"): the preview ends like format, the held-back IDE0049 listed, clean, exit 0.
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "class C { int b; int a; }\n");
        PreviewCommand.MakeRepository(root);
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        var heldBack = FormatCommand.HeldBackGap(["IDE0049"]);
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

            log(heldBack);
            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C { int a; int b; }\n");
            return 0;
        }

        var output = Capture(() => Assert.Equal(0, PreviewCommand.Run("init", new[] { root, "--diff=" + patch }, Format)));

        Assert.Contains("clean after 1 run", output, StringComparison.Ordinal);
        Assert.Contains(heldBack, output, StringComparison.Ordinal);
        Assert.DoesNotContain(FormatCommand.NotVerifiedMarker, output, StringComparison.Ordinal);
        DeleteFolder(Path.GetDirectoryName(patch)!);
    }

    [Fact]
    public void FormatSummary_NotVerified_SaysSoInsteadOfClean()
    {
        var rules = new Dictionary<string, SortedDictionary<string, int>>();
        Assert.Contains("nothing left to change after 1 run, but not verified (below)", PreviewCommand.FormatSummary(3, 5, rules, [true, false], notVerified: true), StringComparison.Ordinal);
        Assert.Contains("nothing to change, but not verified (below)", PreviewCommand.FormatSummary(0, 0, rules, [false], notVerified: true), StringComparison.Ordinal);
        Assert.Contains("clean after 1 run", PreviewCommand.FormatSummary(3, 5, rules, [true, false]), StringComparison.Ordinal);
    }

    [Fact]
    public void WorkingRoot_HoldsTheRepositoryFilesTheProjectsCompileFromAboveIt()
    {
        // NodaTime: src/NodaTime.slnx lists src/NodaTime.Tools.Common, which compiles ..\..\lib\commandline\*.cs. format worked
        // from src/, 'dotnet format --include' can't name ../lib/, and CommandLine.cs was never formatted (134 TWAE errors).
        Write("src/All.slnx", "<Solution>\n  <Project Path=\"Tools/Tools.csproj\" />\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>\n");
        Write("src/Tools/Tools.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <Compile Include=\"..\\..\\lib\\commandline\\*.cs\" />\n  </ItemGroup>\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <Compile Include=\"../Shared/S.cs;$(Outside)/X.cs;..\\..\\missing\\*.cs\" />\n  </ItemGroup>\n</Project>\n");
        Write("src/Shared/S.cs", "class S\n{\n}\n");
        Write("lib/commandline/CommandLine.cs", "class CommandLine\n{\n}\n");
        Write("src/Inside/Inside.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <!-- <Compile Include=\"..\\..\\lib\\commandline\\*.cs\" /> -->\n  </ItemGroup>\n</Project>\n");
        Write("src/Only.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n  <Project Path=\"Inside/Inside.csproj\" />\n</Solution>\n");

        // Without a repository nothing tells whose files they are: the solution's folder, as before.
        Assert.Equal(Path.Combine(root, "src"), FormatCommand.WorkingRoot(Path.Combine(root, "src", "All.slnx")));

        PreviewCommand.MakeRepository(root);
        Assert.Equal(root, FormatCommand.WorkingRoot(Path.Combine(root, "src", "All.slnx")));
        Assert.Equal(root, FormatCommand.WorkingRoot(Path.Combine(root, "src", "Tools", "Tools.csproj")));
        Assert.Equal([Path.Combine(root, "lib", "commandline")], FormatCommand.LinkedFolders(Path.Combine(root, "src", "Tools", "Tools.csproj"), root));

        // A link inside the solution's folder (or one through a property, to a folder that doesn't exist, or commented out) changes nothing.
        Assert.Equal(Path.Combine(root, "src"), FormatCommand.WorkingRoot(Path.Combine(root, "src", "Only.slnx")));
        Assert.Equal(Path.Combine(root, "src"), FormatCommand.WorkingRoot(Path.Combine(root, "src", "Lib", "Lib.csproj")));
        Assert.Empty(FormatCommand.LinkedFolders(Path.Combine(root, "src", "Inside", "Inside.csproj"), root));
    }

    [Fact]
    public void LinkedFolders_OutsideTheRepository_AreNotTheRepositorys()
    {
        // A sibling repository's files (Speckle) stay someone else's code: format leaves them alone, from the same root as before.
        Write("repo/src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <Compile Include=\"..\\..\\..\\other\\*.cs\" />\n  </ItemGroup>\n</Project>\n");
        Write("other/O.cs", "class O\n{\n}\n");
        var repository = Path.Combine(root, "repo");
        PreviewCommand.MakeRepository(repository);
        Assert.Empty(FormatCommand.LinkedFolders(Path.Combine(repository, "src", "App", "App.csproj"), repository));
        Assert.Equal(Path.Combine(repository, "src", "App"), FormatCommand.WorkingRoot(Path.Combine(repository, "src", "App", "App.csproj")));
    }

    [Fact]
    public void BuildCheck_AProjectAFailedOneKeptFromCompiling_IsNotVerified()
    {
        // Spot repro: Lib fails with warnings as errors (CS0168, the untouched code too), App references it, so MSBuild never
        // compiles App and says nothing about it. --build said "Clean." with exit code 0.
        var solution = Path.Combine(root, "Dep.slnx");
        File.WriteAllText(solution, "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n  <Project Path=\"App/App.csproj\" />\n  <Project Path=\"Other/Other.csproj\" />\n</Solution>\n");
        var lib = Path.Combine(root, "Lib", "Lib.csproj");
        (int, IReadOnlyList<string>) Build(string folder, string workspace, string configuration) =>
            (1, [$"{Path.Combine(folder, "Lib", "L.cs")}(7,13): error CS0168: The variable 'unused' is declared but never used [{lib}]", "  Other -> /tmp/out/Other.dll"]);

        var lines = new List<string>();
        var line = FormatCommand.BuildCheckLine(root, solution, lines.Add, Build, () => string.Empty, () => ["Lib", "App", "Other"]);

        Assert.Contains("Debug: no findings, no new errors (1 error the untouched code has too, not counted).", lines);
        Assert.Contains("  Didn't compile: App (a project it needs failed).", lines);
        Assert.StartsWith(FormatCommand.NotVerifiedMarker + " the Debug (App), Release (App) builds didn't compile the projects named", line, StringComparison.Ordinal);

        // Every project compiled (its own error in the code counts: the compiler ran its analyzers): nothing to say.
        Assert.Null(FormatCommand.BuildCheckLine(root, solution, _ => { }, Build, () => string.Empty, () => ["Lib", "Other"]));
    }

    [Fact]
    public void Uncompiled_CountsOutputLinesAndCompilerMessages_NotMSBuilds()
    {
        var root2 = Path.Combine(root, "r");
        IReadOnlyList<string> output =
        [
            "  A -> /out/A.dll",
            $"{Path.Combine(root2, "B", "B.cs")}(1,1): error CS0168: unused [{Path.Combine(root2, "B", "B.csproj")}::TargetFramework=net8.0]",
            $"/sdk/Microsoft.Common.CurrentVersion.targets(1259,5): error MSB3644: The reference assemblies were not found [{Path.Combine(root2, "C", "C.csproj")}]",
            $"/sdk/Microsoft.NET.Sdk.targets(10,5): error NETSDK1147: To build this project, the following workloads must be installed: ios [{Path.Combine(root2, "D", "D.csproj")}]",
            $"{Path.Combine(root2, "E", "E.csproj")} : error NU1101: Unable to find package X [{Path.Combine(root2, "E", "E.csproj")}]",
        ];
        var messages = FormatCommand.BuildMessages(output, root2);
        Assert.Equal(["C", "D", "E", "F"], FormatCommand.Uncompiled(output, messages, ["A", "B", "C", "D", "E", "F"]));
    }

    [Fact]
    public void Files_AFileLinkedThroughAProperty_SharesItsProjectsRun()
    {
        // Trial 8's case (Files_ALinkedFile_IsFormattedInTheProjectThatCompilesIt) with the link through a property in its folder, which
        // LinkedFolders doesn't read: the project's folder stays its working root, so the linked file is a group of its own
        // with the same project, and both share one run of that project.
        Write("App/App.csproj", "<Project>\n  <PropertyGroup>\n    <SharedDir>Shared</SharedDir>\n  </PropertyGroup>\n  <ItemGroup>\n    <Compile Include=\"..\\$(SharedDir)\\S.cs\" Link=\"S.cs\" />\n  </ItemGroup>\n</Project>\n");
        Write("App/A.cs", "class A { }\n");
        Write("Shared/S.cs", "class S { }\n");
        Git("init", "-q");
        var calls = new List<string[]>();

        var log = new List<string>();
        var code = FormatCommand.Run(["--files", Path.Combine(root, "Shared/S.cs"), Path.Combine(root, "App/A.cs"), "--once"], log.Add, (args, _) =>
        {
            calls.Add(args);
            return 0;
        });

        Assert.True(code == 0, string.Join("\n", log));
        Assert.Equal([$"{Path.Combine(root, "App", "App.csproj")} {FormatCommand.WorkingFolderOption} {root} --include Shared/S.cs App/A.cs"], calls.Select(c => string.Join(" ", c)));
    }
}
