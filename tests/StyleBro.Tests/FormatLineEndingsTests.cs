using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>
/// Issue #133: without end_of_line, the SDK's whitespace pass writes the OS's line ending into the line breaks it rewrites,
/// so a CRLF file got LF lines on Linux (and an LF file CRLF lines on Windows), and '--verify-no-changes' depended on the OS.
/// 'bro format' gives a file that used one ending throughout its own ending back.
/// </summary>
[Collection(FormatEnvironmentCollection.Name)]
public sealed class FormatLineEndingsTests : IDisposable
{
    private readonly List<string> folders = [];

    /// <summary>The folders <see cref="NewFolder"/> made, deleted after the test (hundreds were left in the temp folder).</summary>
    public void Dispose() => folders.ForEach(f => Directory.Delete(f, recursive: true));

    [Fact]
    public void Format_KeepsEachFilesOwnLineEnding()
    {
        var root = NewFolder(("Crlf.cs", "class A\r\n{\r\n}\r\n"), ("Lf.cs", "class B\n{\n}\n"), ("Converted.cs", "class C\r\n{\r\n}\r\n"), ("Untouched.cs", "class D\r\n{\r\n}\r\n"), (".editorconfig", "root = true\n[Converted.cs]\nend_of_line = lf\n"));
        var untouched = Path.Combine(root, "Untouched.cs");
        var written = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(untouched, written);

        var code = FormatCommand.Run(new[] { root, FormatCommand.OnceOption }, _ => { }, (args, _) =>
        {
            // The OS's ending in one rewritten line break each; one file converted completely (end_of_line set for it).
            File.WriteAllText(Path.Combine(root, "Crlf.cs"), "class A\r\n{\n}\r\n");
            File.WriteAllText(Path.Combine(root, "Lf.cs"), "class B\r\n{\n}\n");
            File.WriteAllText(Path.Combine(root, "Converted.cs"), "class C\n{\n}\n");
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Equal("class A\r\n{\r\n}\r\n", File.ReadAllText(Path.Combine(root, "Crlf.cs")));
        Assert.Equal("class B\n{\n}\n", File.ReadAllText(Path.Combine(root, "Lf.cs")));
        Assert.Equal("class C\n{\n}\n", File.ReadAllText(Path.Combine(root, "Converted.cs")));
        Assert.Equal(written, File.GetLastWriteTimeUtc(untouched)); // a file that still uses one ending isn't written
    }

    [Fact]
    public void Format_LeavesFilesThatMixedEndingsBefore()
    {
        var root = NewFolder(("Mixed.cs", "class A\r\n{\n}\r\n"));

        FormatCommand.Run(new[] { root, FormatCommand.OnceOption }, _ => { }, (args, _) =>
        {
            File.WriteAllText(Path.Combine(root, "Mixed.cs"), "class A\r\n{\n}\n");
            return 0;
        });

        Assert.Equal("class A\r\n{\n}\n", File.ReadAllText(Path.Combine(root, "Mixed.cs")));
    }

    [Fact]
    public void Verify_OnlyLineEndingsDiffer_IsClean()
    {
        var root = NewFolder(("C.cs", "class C\r\n{\r\n}\r\n"));
        var whitespace = $"{Path.Combine(root, "C.cs")}(1,8): error WHITESPACE: Fix whitespace formatting. Replace 2 characters with '\\n'.";
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke(whitespace);
                return 2;
            }

            // The run on the copy writes this OS's ending (here LF) into one line break.
            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C\n{\r\n}\r\n");
            return 0;
        });

        Assert.Equal(0, code);
        Assert.DoesNotContain(whitespace, lines);
        Assert.Contains("Clean: formatting would change nothing.", lines);
        Assert.Contains(lines, l => l.StartsWith("1 whitespace finding is only this OS's line ending", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_AFileEndOfLineConverts_IsNotClean()
    {
        // Trial 28: 'end_of_line = crlf' and an LF file: "Clean" (only this OS's line ending), while 'format' converted it.
        var root = NewFolder(("C.cs", "class C\n{\n}\n"), (".editorconfig", "root = true\n\n[*.cs]\nend_of_line = crlf\n"));
        var endOfLine = $"{Path.Combine(root, "C.cs")}(1,8): error ENDOFLINE: Fix end of line marker. Replace 1 characters with '\\r\\n'.";
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke(endOfLine);
                return 2;
            }

            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C\r\n{\r\n}\r\n");
            return 0;
        });

        Assert.Equal(FormatCommand.NotCleanExitCode, code);
        Assert.Contains("Not clean: formatting would change 1 file. Run 'bro format'.", lines);
    }

    [Fact]
    public void Verify_AFormattingFindingInAnLfFile_IsNotLineEndingNoise()
    {
        // Backlog 079 (moq4): a finding the copy's run doesn't change, in a file that has only LF, was "only this OS's line ending"
        // (so a check before any format said Clean). It's a finding no fix changes. (On Windows the file would be the other kind.)
        var root = NewFolder(("C.cs", "class C\n{\n}\n"));
        var finding = $"{Path.Combine(root, "C.cs")}(1,8): warning IDE0055: Fix formatting";
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke(finding);
                return 2;
            }

            return 0;
        });

        Assert.Equal(OperatingSystem.IsWindows() ? 0 : FormatCommand.NotCleanExitCode, code);
        Assert.DoesNotContain(lines, l => l.Contains("only this OS's line ending", StringComparison.Ordinal) && !OperatingSystem.IsWindows());
        if (!OperatingSystem.IsWindows())
        {
            Assert.Contains(finding, lines);
            Assert.Contains(lines, l => l.StartsWith("Not clean: 1 finding above no fix changes", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Verify_AFileTheRawStringGuardKeptInTheCopy_IsNotVerifiedLikeFormat()
    {
        // Backlog 079: without a format record before it, a check said Clean for the kept raw string file (its IDE0055 spots were
        // "line endings"), where format says Not checked (exit 3). The copy's run names the file, as format's run does.
        var root = NewFolder(("R.cs", "class R\n{\n}\n"));
        var finding = $"{Path.Combine(root, "R.cs")}(1,8): warning IDE0055: Fix formatting";
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke(finding);
                return 2;
            }

            output?.Invoke($"Not checked: {Path.Combine(args[0], "R.cs")}: IDE0055 {FormatCommand.RawStringGapEnd}");
            return 0;
        });

        Assert.Equal(FormatCommand.NotVerifiedExitCode, code);
        Assert.Contains(lines, l => l.StartsWith("Not checked:", StringComparison.Ordinal) && l.Contains("R.cs", StringComparison.Ordinal) && !l.Contains("stylebro-verify-", StringComparison.Ordinal));
    }

    [Fact]
    public void SeparatePasses_SplitsThePlainRun_OnlyWithTheCodeStylePackage()
    {
        // Backlog 079: the analyzer pass must run apart from the style pass, to leave the IDE ids to the SDK's.
        Assert.Equal(["whitespace", "style", "analyzers"], FormatCommand.SeparatePasses([string.Empty], codeStyle: true));
        Assert.Equal([string.Empty], FormatCommand.SeparatePasses([string.Empty], codeStyle: false));
        Assert.Equal(["style", "analyzers"], FormatCommand.SeparatePasses(["style", "analyzers"], codeStyle: true));
    }

    [Theory]
    [InlineData("a\r\nb\r\n", false, true)]
    [InlineData("a\nb\n", false, false)]
    [InlineData("a\nb\n", true, true)]
    [InlineData("a\r\nb\r\n", true, false)]
    [InlineData("a\r\nb\n", true, true)]
    [InlineData("ab", false, false)]
    public void HasOtherLineEnding_IsWhatThisOsDoesntWrite(string text, bool windows, bool other)
    {
        var root = NewFolder(("F.cs", text));
        Assert.Equal(other, FormatCommand.HasOtherLineEnding(Path.Combine(root, "F.cs"), windows));
    }

    [Fact]
    public void HasOtherLineEnding_AMissingFile_HasNone() =>
        Assert.False(FormatCommand.HasOtherLineEnding(Path.Combine(Path.GetTempPath(), "stylebro-missing-" + Guid.NewGuid().ToString("N") + ".cs")));

    [Theory]
    [InlineData("a\r\nb\r\n", true)]
    [InlineData("a\nb\n", false)]
    [InlineData("a\r\nb\n", null)]
    [InlineData("a\rb\r\n", null)]
    [InlineData("ab", null)]
    public void Ending_IsOneLineEndingThroughout(string text, bool? crlf) =>
        Assert.Equal(crlf, FormatCommand.Ending(System.Text.Encoding.UTF8.GetBytes(text)));

    private string NewFolder(params (string Name, string Text)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), "stylebro-endings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        folders.Add(root);
        foreach (var (name, text) in files)
        {
            File.WriteAllText(Path.Combine(root, name), text);
        }

        return root;
    }
}
