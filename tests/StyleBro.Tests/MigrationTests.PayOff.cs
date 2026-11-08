using StyleBro.Analyzers.Baseline;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Backlog 086: 'bro baseline --pay-off &lt;folder or file&gt;' (the format run is a stand-in here).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void PayOff_TakesOutTheFolderEntries_FormatsTheirFiles()
    {
        PayOffRepository();
        string[]? formatted = null;
        string? during = null;

        var output = Capture(() => Assert.Equal(0, BaselineCommand.PayOff(["src"], root, args =>
        {
            (formatted, during) = (args, File.ReadAllText(Path.Combine(root, Baseline.FileName)));
            Write("src/A.cs", "class A\n{\n    string a = string.Empty;\n}\n");
            Write("src/B.cs", "class B\n{\n    string b = string.Empty;\n}\n");
            return 0;
        })));

        Assert.Equal([FormatCommand.FilesOption, Path.Combine(root, "src", "A.cs"), Path.Combine(root, "src", "B.cs")], formatted);
        Assert.Equal(["lib/C.cs", "src.Tests/D.cs"], Baseline.Parse(during!).Entries.Keys.Select(k => k.Path));
        Assert.Equal(["lib/C.cs", "src.Tests/D.cs"], Baseline.Parse(File.ReadAllText(Path.Combine(root, Baseline.FileName))).Entries.Keys.Select(k => k.Path));
        Assert.Contains("Paying off 3 baselined findings in 2 files (src): formatting them with their baseline entries taken out.", output, StringComparison.Ordinal);
        Assert.Contains("Baseline: paid off 3 of 3 baselined findings in src.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PayOff_KeptFindings_StayInTheBaseline()
    {
        PayOffRepository();

        // Format fixes A and keeps B's finding, which it puts back into the baseline (as after any clean run).
        var output = Capture(() => Assert.Equal(0, BaselineCommand.PayOff(["src/"], root, _ =>
        {
            Write("src/A.cs", "class A\n{\n    string a = string.Empty;\n}\n");
            File.AppendAllText(Path.Combine(root, Baseline.FileName), Entry("src/B.cs", "string b = \"\";", 1));
            return 0;
        })));

        Assert.Equal(["lib/C.cs", "src.Tests/D.cs", "src/B.cs"], Baseline.Parse(File.ReadAllText(Path.Combine(root, Baseline.FileName))).Entries.Keys.Select(k => k.Path).Order(StringComparer.Ordinal));
        Assert.Contains("Baseline: paid off 2 of 3 baselined findings in src/; 1 kept on purpose stays in the baseline (the reasons are above).", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PayOff_AnUnfinishedRun_PutsBackTheEntriesOfTheLinesItLeft()
    {
        PayOffRepository();

        // Exit code 2 (still changing after 3 runs): A was fixed, B's line is still there and its entry goes back.
        var output = Capture(() => Assert.Equal(2, BaselineCommand.PayOff(["src/A.cs", "src/B.cs"], root, _ =>
        {
            Write("src/A.cs", "class A\n{\n    string a = string.Empty;\n}\n");
            return 2;
        })));

        var entries = Baseline.Parse(File.ReadAllText(Path.Combine(root, Baseline.FileName)));
        Assert.Equal(["lib/C.cs", "src.Tests/D.cs", "src/B.cs"], entries.Entries.Keys.Select(k => k.Path).Order(StringComparer.Ordinal));
        Assert.Equal(2, entries.Allowed(new Baseline.Key("BRO1106", "src/B.cs", Baseline.Fingerprint("string b = \"\";"))));
        Assert.Contains("Baseline: paid off 1 of 3 baselined findings in src/A.cs, src/B.cs; the run didn't end clean (exit code 2), so the entries for the 2 findings on lines it left are back in the baseline.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void PayOff_ABaselineTheProps_Name()
    {
        // eng/stylebro.baseline, named in Directory.Build.props without a condition (where StyleBroBaseline=none can't reach).
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <StyleBroBaseline>$(MSBuildThisFileDirectory)eng/stylebro.baseline</StyleBroBaseline>\n  </PropertyGroup>\n</Project>\n");
        Write("src/A.cs", "class A\n{\n    string a = \"\";\n}\n");
        Write("eng/" + Baseline.FileName, Baseline.Header + Entry("../src/A.cs", "string a = \"\";", 1));
        string[]? formatted = null;

        Capture(() => BaselineCommand.PayOff(["src"], root, args =>
        {
            formatted = args;
            return 0;
        }));

        Assert.Equal([FormatCommand.FilesOption, Path.Combine(root, "src", "A.cs")], formatted);
        Assert.Empty(Baseline.Parse(File.ReadAllText(Path.Combine(root, "eng", Baseline.FileName))).Entries);
    }

    [Fact]
    public void PayOff_NothingToDo_AndUsageErrors()
    {
        PayOffRepository();
        var before = File.ReadAllText(Path.Combine(root, Baseline.FileName));
        Write("docs/readme.md", "docs\n");
        int Fail(string[] args) => throw new InvalidOperationException("format ran");

        Assert.Contains("Nothing to pay off: no baseline has entries for docs.", Capture(() => Assert.Equal(0, BaselineCommand.PayOff(["docs"], root, Fail))), StringComparison.Ordinal);
        Assert.Equal(1, BaselineCommand.PayOff(["missing"], root, Fail));
        Assert.Equal(1, BaselineCommand.PayOff([], root, Fail));
        var error = Console.Error;
        using var writer = new StringWriter();
        Console.SetError(writer);
        try
        {
            Assert.Equal(1, BaselineCommand.Run([BaselineCommand.PayOffOption, "src", "--project", "App.sln"]));
        }
        finally
        {
            Console.SetError(error);
        }

        Assert.StartsWith("--pay-off takes folders and files, not --project", writer.ToString(), StringComparison.Ordinal);
        Assert.Equal(before, File.ReadAllText(Path.Combine(root, Baseline.FileName)));

        // Entries for files that are gone: removed, and nothing to format.
        File.AppendAllText(Path.Combine(root, Baseline.FileName), Entry("old/Gone.cs", "x", 2));
        Write("old/.keep", string.Empty);
        Assert.Contains("Baseline: removed 1 entry for files that are gone (old).", Capture(() => Assert.Equal(0, BaselineCommand.PayOff(["old"], root, Fail))), StringComparison.Ordinal);
        Assert.Equal(Baseline.Parse(before).Entries, Baseline.Parse(File.ReadAllText(Path.Combine(root, Baseline.FileName))).Entries);
    }

    private static string Entry(string path, string line, int count) => $"BRO1106\t{path}\t{Baseline.Fingerprint(line)}\t{count}\n";

    /// <summary>Four files with a baselined BRO1106 each (B's line twice): src/, and lib/ and src.Tests/ next to it.</summary>
    private void PayOffRepository()
    {
        Write("src/A.cs", "class A\n{\n    string a = \"\";\n}\n");
        Write("src/B.cs", "class B\n{\n    string b = \"\";\n}\n");
        Write("lib/C.cs", "class C\n{\n    string c = \"\";\n}\n");
        Write("src.Tests/D.cs", "class D\n{\n    string d = \"\";\n}\n");
        Write(Baseline.FileName, Baseline.Header + Entry("src/A.cs", "string a = \"\";", 1) + Entry("src/B.cs", "string b = \"\";", 2) + Entry("lib/C.cs", "string c = \"\";", 1)
            + Entry("src.Tests/D.cs", "string d = \"\";", 1));
    }
}
