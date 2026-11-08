using StyleBro.Analyzers;
using StyleBro.Analyzers.Baseline;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Backlog 085: bro adds 'stylebro.baseline merge=union' to the repository's .gitattributes when it writes a baseline.</summary>
public sealed class GitAttributesTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("bro-attributes-").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public void AddsTheLineOnce_AtTheRepositoryRoot()
    {
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        var baseline = Path.Combine(root, "eng", Baseline.FileName);

        var said = GitAttributes.AddBaselineLine(root, baseline);

        Assert.Equal("Baseline: .gitattributes: added 'stylebro.baseline merge=union', so git keeps both branches' entries when it merges them (https://bisforboman.github.io/stylebro/baseline/#merging-branches).", said);
        var text = File.ReadAllText(Path.Combine(root, ".gitattributes"));
        Assert.EndsWith("\nstylebro.baseline merge=union\n", text, StringComparison.Ordinal);
        Assert.StartsWith("# StyleBro: ", text, StringComparison.Ordinal);
        Assert.Null(GitAttributes.AddBaselineLine(root, baseline));
        Assert.Equal(text, File.ReadAllText(Path.Combine(root, ".gitattributes")));
    }

    [Fact]
    public void KeepsTheFile_AndItsLineEndings()
    {
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        var file = Path.Combine(root, ".gitattributes");
        File.WriteAllText(file, "* text=auto\r\n*.sh text eol=lf");

        GitAttributes.AddBaselineLine(root, Path.Combine(root, Baseline.FileName));

        var text = File.ReadAllText(file);
        Assert.StartsWith("* text=auto\r\n*.sh text eol=lf\r\n# StyleBro: ", text, StringComparison.Ordinal);
        Assert.EndsWith("\r\nstylebro.baseline merge=union\r\n", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("stylebro.baseline -merge\n")]
    [InlineData("*.cs text\n/eng/stylebro.baseline merge=binary\n")]
    [InlineData("  **/stylebro.baseline\tmerge=union\n")]
    public void LeavesAFileThatNamesTheBaseline(string text)
    {
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        File.WriteAllText(Path.Combine(root, ".gitattributes"), text);

        Assert.Null(GitAttributes.AddBaselineLine(root, Path.Combine(root, Baseline.FileName)));
        Assert.Equal(text, File.ReadAllText(Path.Combine(root, ".gitattributes")));
    }

    [Theory]
    [InlineData("# stylebro.baseline merge=union\n")]
    [InlineData("#stylebro.baseline merge=union\n")]
    [InlineData("*.baseline -text\n")]
    [InlineData("stylebro.baseline.previous binary\n")]
    public void OtherLines_DontNameTheBaseline(string text) => Assert.False(GitAttributes.NamesBaseline(text));

    [Fact]
    public void NothingOutsideARepository()
    {
        Assert.Null(GitAttributes.AddBaselineLine(root, Path.Combine(root, Baseline.FileName)));
        Assert.False(File.Exists(Path.Combine(root, ".gitattributes")));
    }

    [Fact]
    public void FormatsBaseline_AddsTheLine()
    {
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(Path.Combine(root, "src"));
        File.WriteAllText(Path.Combine(root, "src", "C.cs"), "class C\n{\n    private int Count;\n}\n");
        var kept = new[] { new KeptFinding(Path.Combine(root, "src", "C.cs"), 3, 17, "BRO1303", KeptReason.NameInString, "Rename 'Count' to 'count'") };

        var lines = FormatCommand.BaselineKept(root, kept)!.Split('\n');

        Assert.Equal(2, lines.Length);
        Assert.StartsWith("Baseline: stylebro.baseline: added 1 kept finding", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("Baseline: .gitattributes: added 'stylebro.baseline merge=union'", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void GitMergesTwoBranchesBaselines()
    {
        // Each branch adds an entry next to the other's and one changes a shared entry's count: without the attribute
        // that's a conflict; with it git keeps both sides' lines, and the shared entry takes the larger count.
        Git("init", "-q", "-b", "main");
        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
        File.WriteAllText(Path.Combine(root, ".gitattributes"), GitAttributes.Append(null));
        var file = Path.Combine(root, Baseline.FileName);
        string Entry(string path, int count) => $"BRO1106\t{path}\t0000000000000001\t{count}\n";
        File.WriteAllText(file, Baseline.Header + Entry("src/A.cs", 1) + Entry("src/C.cs", 1));
        Git("add", "-A");
        Git("commit", "-q", "-m", "base");
        Git("checkout", "-q", "-b", "other");
        File.WriteAllText(file, Baseline.Header + Entry("src/A.cs", 1) + Entry("src/B1.cs", 1) + Entry("src/C.cs", 2));
        Git("commit", "-q", "-am", "other");
        Git("checkout", "-q", "main");
        File.WriteAllText(file, Baseline.Header + Entry("src/A.cs", 1) + Entry("src/B2.cs", 1) + Entry("src/C.cs", 1));
        Git("commit", "-q", "-am", "main");

        Git("merge", "-q", "--no-edit", "other");

        var merged = Baseline.Parse(File.ReadAllText(file));
        Assert.Equal(["src/A.cs", "src/B1.cs", "src/B2.cs", "src/C.cs"], merged.Entries.Keys.Select(k => k.Path).Order(StringComparer.Ordinal));
        Assert.Equal(2, merged.Allowed(new Baseline.Key("BRO1106", "src/C.cs", "0000000000000001")));
    }

    private void Git(params string[] args)
    {
        var (code, output) = PreviewCommand.Git(root, args);
        Assert.True(code == 0, $"git {string.Join(' ', args)}: {output}");
    }
}
