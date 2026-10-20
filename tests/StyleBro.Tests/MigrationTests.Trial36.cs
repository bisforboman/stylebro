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
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "class C { int b; int a; }\n");
        PreviewCommand.MakeRepository(root);
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        var gap = FormatCommand.Gap("IDE0049", "this SDK's 'dotnet format' ignores the multi-target guard, so these fixes are held back and not checked");
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
}
