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
}
