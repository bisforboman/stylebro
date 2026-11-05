using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the sixty-eighth trial (CacheManager, BedrockFramework, sendgrid-csharp, Avro) found in the tool.</summary>
public sealed partial class MigrationTests
{
    // CacheManager.Tests: <TargetFrameworks> over three lines (a commented-out net481 and ";net8.0") evaluates to "net8.0\n    ",
    // and 'dotnet format' printed "Required references did not load for CacheManager.Tests(net8.0\n    ) or referenced project".
    // Read line by line (or with a '.' that stops at the line break) the skip went unseen: the project's analyzers never ran and
    // format, check and the --json report said Clean while the build reported 460 findings in it.
    [Fact]
    public void Format_SeesASkippedProject_WhoseFrameworkHasALineBreak()
    {
        const string multiLine = "Required references did not load for CacheManager.Tests(net8.0\n    ) or referenced project. Run `dotnet restore` prior to formatting.";
        Assert.Equal("CacheManager.Tests(net8.0)", FormatCommand.SkippedProject(multiLine));
        Assert.Equal("CacheManager.Tests(net8.0)", FormatCommand.SkippedProject(multiLine.Replace("\n", "\r\n", StringComparison.Ordinal)));
        Assert.Equal(("CacheManager.Tests", "net8.0"), FormatCommand.SkippedFor(FormatCommand.SkippedProject(multiLine)!, "net8.0"));

        // A name with a space in it keeps it.
        Assert.Equal("My Lib", FormatCommand.SkippedProject("Required references did not load for My Lib or referenced project."));

        // 'dotnet format' as a process (--engine dotnet-format) writes the lines one by one: put back together before they are read.
        var joiner = new FormatCommand.SkipLineJoiner();
        var read = new List<string>();
        foreach (var line in multiLine.Split('\n').Prepend("  Determining projects to restore...").Append("Formatted code file 'A.cs'."))
        {
            if (joiner.Add(line) is { } whole)
            {
                read.Add(whole);
            }
        }

        Assert.Null(joiner.Flush());
        Assert.Equal(3, read.Count);
        Assert.Equal("CacheManager.Tests(net8.0)", FormatCommand.SkippedProject(read[1]));
        Assert.Equal("Formatted code file 'A.cs'.", read[2]);
    }

    [Fact]
    public void SkipLineJoiner_KeepsALineThatNeverCompletes_AndOtherLinesAsTheyAre()
    {
        var joiner = new FormatCommand.SkipLineJoiner();
        Assert.Equal("Warnings were encountered while loading the workspace.", joiner.Add("Warnings were encountered while loading the workspace."));
        Assert.Null(joiner.Add("Required references did not load for Lib(net8.0"));
        Assert.Equal("Required references did not load for Lib(net8.0", joiner.Flush());
        Assert.Null(joiner.Flush());

        // A message that goes on and on is let through after a few lines, not held for the rest of the run.
        Assert.Null(joiner.Add("Required references did not load for Lib(a"));
        Assert.Null(joiner.Add("b"));
        Assert.Null(joiner.Add("c"));
        Assert.Null(joiner.Add("d"));
        Assert.Equal("Required references did not load for Lib(abcde", joiner.Add("e"));
        Assert.Equal("next", joiner.Add("next"));
    }

    // `bro check <solution> --report` (no path): check adds --verify-no-changes after it, which 'dotnet format' took for the
    // report folder, so the "check" ran without it and formatted 85 files.
    [Theory]
    [InlineData(new[] { "App.sln", "--report" }, true)]
    [InlineData(new[] { "App.sln", "--report", "--verify-no-changes" }, true)]
    [InlineData(new[] { "App.sln", "--report", "out", "--verify-no-changes" }, false)]
    [InlineData(new[] { "App.sln", "--verify-no-changes" }, false)]
    public void ReportWithoutAPath_IsRefused(string[] args, bool refused)
    {
        Assert.Equal(refused, FormatCommand.ReportWithoutValue(args));
        if (refused)
        {
            var said = new List<string>();
            Assert.Equal(1, FormatCommand.Run(args, said.Add));
            Assert.Contains("--report needs", Assert.Single(said));
        }
    }

    // The old engine formatted CacheManager.Tests and the host did not (it was in no run): the injected targets file cuts the
    // white space out of <TargetFrameworks> for bro's runs, so the project loads as 'Lib(net8.0)'.
    [Fact]
    public void RunTargets_CutWhiteSpaceOutOfTargetFrameworks()
    {
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFrameworks>\n      <!--net481-->;net8.0\n    </TargetFrameworks>\n  </PropertyGroup>\n</Project>\n");
        Write("select.targets", FormatCommand.SelectFrameworkTargets);
        var project = Path.Combine(root, "App", "App.csproj");
        string Frameworks(bool hook) => FormatCommand.EvaluatedProperty(project, "TargetFrameworks");

        Assert.Contains("\n", Frameworks(false)); // as written
        var json = FormatCommand.Capture(root, ["msbuild", project, $"-property:BeforeMicrosoftNETSdkTargets={Path.Combine(root, "select.targets")}", "-getProperty:TargetFrameworks", "-nologo"]);
        Assert.Equal(";net8.0", json.Trim());
    }
}
