using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 26's findings in 'stylebro-migrate format', verify and the preview.</summary>
public sealed partial class MigrationTests
{
    private const string IfDebugBody = "class C\n{\n    void M(bool x)\n    {\n#if DEBUG\n        if (x)\n#endif\n        Log();\n        Log();\n    }\n\n    void Log() { }\n}\n";

    private const string ElseBody = "class C\n{\n    void M(bool c)\n    {\n#if NETFRAMEWORK\n        if (c)\n            A();\n        else\n#endif\n            B();\n    }\n\n    void A() { }\n\n    void B() { }\n}\n";

    [Fact]
    public void IfBodies_AreTheLinesWhoseBodyIfDecides()
    {
        // NLog: 'Log();' is the if's body where DEBUG is defined, a statement of its own where it isn't; the next 'Log();' is
        // a statement in both.
        Assert.Equal([7], IfBodies.Lines(IfDebugBody, [["DEBUG"], []]));
        Assert.Equal([9], IfBodies.Lines(ElseBody, [["NETFRAMEWORK"], []]));

        // One parse (every run defines the same), or no such shape: nothing.
        Assert.Empty(IfBodies.Lines(IfDebugBody, [["DEBUG"], ["DEBUG"]]));
        Assert.Empty(IfBodies.Lines("class C\n{\n    void M(bool x)\n    {\n#if DEBUG\n        if (x)\n#else\n        if (!x)\n#endif\n            Log();\n    }\n\n    void Log() { }\n}\n", [["DEBUG"], []]));

        // Stacked usings aren't indented: one under '#if' doesn't make the other a body.
        Assert.Empty(IfBodies.Lines("class C\n{\n    void M()\n    {\n#if X\n        using (A())\n#endif\n        using (A())\n        {\n        }\n    }\n\n    System.IDisposable A() => null;\n}\n", [["X"], []]));
        Assert.False(IfBodies.IsCandidate("class C\n{\n#if DEBUG\n    int x;\n#endif\n}\n"));
    }

    [Fact]
    public void IfBodies_KeepTheirIndentationAndLineBreak_WhereverTheRunMovedThem()
    {
        var before = IfDebugBody.Replace("\n", "\r\n", StringComparison.Ordinal);

        // A run indented the body (and wrote LF there) and added a line above it: the body gets its indentation and CRLF back,
        // the other changes stay.
        var after = before.Replace("        Log();\r\n        Log();", "            Log();\n        Log();", StringComparison.Ordinal).Replace("    void M(bool x)\r\n", "    // added\r\n    void M(bool x)\r\n", StringComparison.Ordinal);
        Assert.Equal(before.Replace("    void M(bool x)\r\n", "    // added\r\n    void M(bool x)\r\n", StringComparison.Ordinal), IfBodies.Keep(before, [7], after));

        // A run that changed its text leaves it.
        var renamed = before.Replace("        Log();\r\n        Log();", "            Write();\r\n        Log();", StringComparison.Ordinal);
        Assert.Equal(renamed, IfBodies.Keep(before, [7], renamed));
    }

    [Fact]
    public void IfBodies_AreFoundAgainByTheLinesAroundThem()
    {
        // NLog: a fix elsewhere added '{' and '}' lines; counting them found the wrong '{'.
        string[] old = ["void A()", "{", "#if X", "if (a)", "#endif", "{", "B();", "}", "}"];
        string[] now = ["void Z()", "{", "if (q)", "{", "Q();", "}", "}", "void A()", "{", "#if X", "if (a)", "#endif", "{", "B();", "}", "}"];
        Assert.Equal(12, IfBodies.Locate(old, 5, now));
        Assert.Equal(7, IfBodies.Locate(old, 0, now));

        // A line changed right next to it: the other side tells.
        string[] edited = ["void A()", "{", "#if X", "if (a)", "#endif", "{", "C();", "}", "}"];
        Assert.Equal(5, IfBodies.Locate(old, 5, edited));

        // Gone, or not told apart within the context: not found.
        Assert.Equal(-1, IfBodies.Locate(old, 6, edited));
        Assert.Equal(-1, IfBodies.Locate(["{"], 0, ["x", "{", "{", "x"]));
    }

    [Fact]
    public void IfBodies_TheirWhitespaceFindingsDontCount()
    {
        var file = Path.Combine(root, "C.cs");
        var protectedLines = new Dictionary<string, (string Text, List<int> Lines)>(StringComparer.OrdinalIgnoreCase) { [file] = (ElseBody, [9]) };
        string Line(int line, int column, string message) => $"{file}({line},{column}): error WHITESPACE: Fix whitespace formatting. {message} [{root}/A.csproj]";

        Assert.True(IfBodies.IsKeptFinding(Line(10, 13, "Delete 4 characters."), protectedLines));

        // The line break of the code line above the '#if' block (the one every parse has), or inside it: written by the same edit.
        Assert.True(IfBodies.IsKeptFinding(Line(4, 6, @"Replace 1 characters with '\r\n'."), protectedLines));
        Assert.True(IfBodies.IsKeptFinding(Line(8, 13, @"Replace 1 characters with '\r\n'."), protectedLines));

        // A finding that writes a line break indents the next line: not the body's then (D.cs, trial 26), but the body's from
        // the line above.
        Assert.False(IfBodies.IsKeptFinding(Line(10, 17, @"Replace 15 characters with '\r\n\s\s\s\s\s\s\s\s\s\s\s\s'."), protectedLines));
        Assert.True(IfBodies.IsKeptFinding(Line(9, 15, @"Replace 13 characters with '\r\n\s\s\s\s\s\s\s\s\s\s\s\s'."), protectedLines));

        // Other lines, files and rules count.
        Assert.False(IfBodies.IsKeptFinding(Line(3, 5, "Delete 4 characters."), protectedLines));
        Assert.False(IfBodies.IsKeptFinding(Line(4, 6, "Delete 4 characters."), protectedLines));
        Assert.False(IfBodies.IsKeptFinding($"{Path.Combine(root, "D.cs")}(10,13): error WHITESPACE: Fix whitespace formatting. Delete 4 characters.", protectedLines));
        Assert.False(IfBodies.IsKeptFinding($"{file}(10,13): warning BRO1514: Braces should not be omitted", protectedLines));

        // A check whose only findings don't count is clean.
        Assert.Equal(0, IfBodies.Without(FormatCommand.NotCleanExitCode, [Line(10, 13, "Delete 4 characters.")], l => IfBodies.IsKeptFinding(l, protectedLines)).Code);
        Assert.Equal(FormatCommand.NotCleanExitCode, IfBodies.Without(FormatCommand.NotCleanExitCode, [Line(10, 13, "Delete 4 characters."), Line(3, 5, "Delete 4 characters.")], l => IfBodies.IsKeptFinding(l, protectedLines)).Code);
        Assert.Equal(FormatCommand.NotCleanExitCode, IfBodies.Without(FormatCommand.NotCleanExitCode, ["Unhandled exception"], l => IfBodies.IsKeptFinding(l, protectedLines)).Code);
    }

    [Fact]
    public void IfBodies_AreFoundWithTheSymbolsOfTheRunsThatCompileThem()
    {
        Write("A/C.cs", IfDebugBody);
        Write("A/D.cs", ElseBody);
        var c = Path.Combine(root, "A", "C.cs");
        var d = Path.Combine(root, "A", "D.cs");
        var found = FormatCommand.IfBodyLines(root, [("net8.0", string.Empty, ["A"]), ("net8.0", "Release", ["A"])], (_, _, configuration) =>
            ("A.csproj", [c, d], configuration.Length == 0 ? ["DEBUG", "NETFRAMEWORK"] : ["NETFRAMEWORK"]));

        // D.cs's NETFRAMEWORK is defined in both runs: it parses one way only.
        Assert.Equal([c], found.Keys);
        Assert.Equal([7], found[c].Lines);
        Assert.StartsWith("Indentation kept: A/C.cs:8: '#if' decides", IfBodies.Line("Indentation kept:", root, found), StringComparison.Ordinal);

        // A run whose symbols aren't known doesn't count.
        Assert.Empty(FormatCommand.IfBodyLines(root, [("net8.0", string.Empty, ["A"]), ("net8.0", "Release", ["A"])], (_, _, configuration) => ("A.csproj", [c], configuration.Length == 0 ? ["DEBUG"] : null)));

        // Restore: the protected lines get their indentation back.
        File.WriteAllText(c, IfDebugBody.Replace("        Log();\n        Log();", "            Log();\n        Log();", StringComparison.Ordinal));
        IfBodies.Restore(found);
        Assert.Equal(IfDebugBody, File.ReadAllText(c));
    }

    [Fact]
    public void IfBodies_Line_GroupsEachFilesLinesAsRanges()
    {
        var lines = new Dictionary<string, (string Text, List<int> Lines)>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(root, "B.cs")] = (string.Empty, [172, 173, 174, 176]),
            [Path.Combine(root, "A.cs")] = (string.Empty, [7]),
        };
        Assert.StartsWith("Indentation kept: A.cs:8, B.cs:173-175,177: ", IfBodies.Line("Indentation kept:", root, lines), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_DoesntSayWhitespaceRunsWhereItLeavesAFolderAlone()
    {
        // Bencodex: "Leaving alone: Bencodex.Benchmarks/" then "style and analyzer fixes still run there".
        Assert.Empty(FormatCommand.NotExcluded(["Bencodex.Benchmarks/"], ["Bencodex.Benchmarks/"]));
        Assert.Empty(FormatCommand.NotExcluded(["Bench/Sub/"], ["Bench"]));
        Assert.Equal(["Samples/"], FormatCommand.NotExcluded(["Bench/", "Samples/"], ["Bench/"]));
        Assert.Equal(["BenchOther/"], FormatCommand.NotExcluded(["BenchOther/"], ["Bench/"]));
    }

    [Fact]
    public void Files_OfASolutionInASubfolder_AreCheckedAgainstTheFolderOfItsProjects()
    {
        // NLog: 'format src/NLog.sln --files tests/...' said the test file "isn't under src".
        Write("src/App.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n  <Project Path=\"../tests/T/T.csproj\" />\n</Solution>\n");
        Write("src/Lib/Lib.csproj", "<Project />");
        Write("tests/T/T.csproj", "<Project />");
        Write("tests/T/X.cs", "class X { }\n");
        var errors = new List<string>();

        var groups = FormatCommand.GroupFiles(["tests/T/X.cs"], Path.Combine(root, "src", "App.slnx"), root, errors.Add);

        Assert.Empty(errors);
        var group = Assert.Single(groups!);
        Assert.Equal(root.TrimEnd(Path.DirectorySeparatorChar), group.Folder.TrimEnd(Path.DirectorySeparatorChar));
        Assert.Equal(["tests/T/X.cs"], group.Include);
    }

    [Fact]
    public void Format_ExplainsWorkspaceWarnings_WithTheCommandThatShowsThem()
    {
        FormatCommand.ResetExplained();
        var line = FormatCommand.Explain("Warnings were encountered while loading the workspace. Set the verbosity option to the 'diagnostic' level to log warnings.", "net6.0");
        Assert.Equal("'dotnet format' had warnings loading the workspace in its net6.0 run (a project or reference it couldn't load completely, e.g. one in another language); they don't stop the run. To see them, add '--verbosity diagnostic' to this 'stylebro-migrate format' command.", line);
        Assert.DoesNotContain("dotnet format <solution>", line, StringComparison.Ordinal);
        FormatCommand.ResetExplained();
    }

    [Fact]
    public void Format_LeavesFilesOutsideTheRepositoryAlone()
    {
        var repository = Path.Combine(root, "repo");
        Write("repo/src/A/A.cs", "class A { }\n");
        Write("outside/Shared.cs", "class Shared { }\n");
        var shared = Path.Combine(root, "outside", "Shared.cs");

        Write("packages/benchmark/contentFiles/cs/any/Config.g.cs", "class Config { }\n");
        var outside = FormatCommand.OutsideRepository([Path.Combine(repository, "src", "A", "A.cs"), shared, Path.Combine(root, "outside", "Missing.cs"), Path.Combine(root, "packages", "benchmark", "contentFiles", "cs", "any", "Config.g.cs")], repository, Path.Combine(root, "packages"));
        Assert.Equal([shared], outside);
        Assert.Equal("Leaving alone: ../outside/Shared.cs (outside the repository): a project compiles it, but it's someone else's code, like a git submodule's, so 'format' doesn't change it and its findings don't count here.", FormatCommand.OutsideRepositoryLine(repository, outside));
        Assert.Equal(Path.GetFullPath(shared), FormatCommand.FindingPath($"{shared}(1,1): warning BRO1509: Put 'X' on separate lines"));

        // Put back after a run that changed it.
        var said = new List<string>();
        using (new FormatCommand.UntouchedFiles(outside, "files outside the repository", said.Add))
        {
            File.WriteAllText(shared, "class Shared\n{\n}\n");
        }

        Assert.Equal("class Shared { }\n", File.ReadAllText(shared));
        Assert.StartsWith($"Put back: {shared}: a fix changed it, and 'format' leaves files outside the repository alone.", Assert.Single(said), StringComparison.Ordinal);
    }

    [Fact]
    public void CSharpier_TakesTheFilesWhoseLineEndingsArentEndOfLines()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\nend_of_line = lf\n\n[legacy/*.cs]\nend_of_line = unset\n");
        Write("A.cs", "class A\n{\n}\n");
        Write("B.cs", "class B\r\n{\r\n}\r\n");
        Write("C.cs", "class C\n{\r\n}\n");
        Write("legacy/D.cs", "class D\r\n{\r\n}\r\n");
        string Full(string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

        Assert.Equal([Full("B.cs"), Full("C.cs")], CSharpierStep.WrongEndings(root, new[] { "A.cs", "B.cs", "C.cs", "legacy/D.cs" }.Select(Full)));
        Assert.True(CSharpierStep.HasOtherEnding("a\nb\n", crlf: true));
        Assert.False(CSharpierStep.HasOtherEnding("a\r\nb\r\n", crlf: true));
        Assert.True(CSharpierStep.HasOtherEnding("a\rb", crlf: true));
        Assert.False(CSharpierStep.HasOtherEnding("a\nb", crlf: false));

        // verify counts what CSharpier says about the MSBuild files init edits, not about others.
        Assert.Equal(["perf/Bench/Directory.Build.props"], CSharpierStep.FailedCSharp(1, ["Error .\\perf\\Bench\\Directory.Build.props - Was not formatted."]));
        Assert.Null(CSharpierStep.FailedCSharp(1, ["Error ./src/App/App.csproj - Was not formatted."]));
    }

    [Fact]
    public void Verify_BuildsTheConfigurationsFormatSkipsForAGeneratorProject()
    {
        Assert.StartsWith("Configurations: UsesGen: its default configuration only", FormatCommand.GeneratorConfigurationsLine(["UsesGen"], verify: true), StringComparison.Ordinal);
        Assert.StartsWith("Configurations: A, B: their default configuration only", FormatCommand.GeneratorConfigurationsLine(["A", "B"], verify: false), StringComparison.Ordinal);

        var project = Path.Combine(root, "src", "UsesGen", "UsesGen.csproj");
        var greeter = Path.Combine(root, "src", "UsesGen", "Greeter.cs");
        var output = new[]
        {
            $"{greeter}(8,34): error BRO1509: Put 'Empty' on separate lines (https://bisforboman.github.io/stylebro/rules/BRO1509/) [{project}]",
            $"{greeter}(8,34): error BRO1509: Put 'Empty' on separate lines (https://bisforboman.github.io/stylebro/rules/BRO1509/) [{project}]",
            $"{greeter}(8,43): error BRO1106: Use 'string.Empty' instead of '\"\"' (https://bisforboman.github.io/stylebro/rules/BRO1106/) [{project}]",
            $"{greeter}(2,1): warning CS1591: Missing XML comment [{project}]",
            $"{Path.Combine(root, "src", "Gen", "G.cs")}(3,1): error BRO1509: Put 'G' on separate lines [{Path.Combine(root, "src", "Gen", "Gen.csproj")}]",
        };
        var said = new List<string>();
        var built = new List<string>();

        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        var line = FormatCommand.UncheckedLine(root, said.Add, (p, c) =>
        {
            built.Add($"{Path.GetFileName(p)} {c}");
            return output;
        });

        Assert.Equal(["UsesGen.csproj Release"], built);
        Assert.Equal("Not checked by format: UsesGen in Release (it takes an in-solution source generator): building it...", said[0]);
        Assert.Equal("Release build of UsesGen: 2 findings:", said[1]);
        Assert.Equal("  src/UsesGen/Greeter.cs(8,34): BRO1509: Put 'Empty' on separate lines (https://bisforboman.github.io/stylebro/rules/BRO1509/)", said[2]);
        Assert.Equal(4, said.Count);
        Assert.StartsWith("Not clean: 2 findings in code only another configuration compiles", line, StringComparison.Ordinal);

        // Once per command; a clean build is clean.
        Assert.Null(FormatCommand.UncheckedLine(root, said.Add, (_, _) => output));
        FormatCommand.RecordUnchecked(root, [(project, "Release")]);
        Assert.Null(FormatCommand.UncheckedLine(root, said.Add, (_, _) => []));
        Assert.Equal("Release build of UsesGen: no findings.", said[^1]);
    }

    [Fact]
    public void CSharpier_PutBack_NamesTheRulesWhoseEditsItUndoes()
    {
        Assert.StartsWith("Not clean: CSharpier puts back what BRO1519 changes in src/A.cs, so the two formatters undo each other there", CSharpierStep.PutBackLine(["src/A.cs"], ["BRO1519"]), StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1519.severity = none", CSharpierStep.PutBackLine(["src/A.cs"], ["BRO1519"]), StringComparison.Ordinal);
        Assert.DoesNotContain("IDE0036", CSharpierStep.PutBackLine(["src/A.cs"], ["BRO1519"]), StringComparison.Ordinal);
        Assert.StartsWith("Not clean: CSharpier puts back what BRO1001, BRO1519 change in", CSharpierStep.PutBackLine(["src/A.cs"], ["BRO1001", "BRO1519"]), StringComparison.Ordinal);

        // The check of those files: their names only, no report.
        Assert.Equal(["a", "--severity", "warn", "--include", "src/A.cs"], FormatCommand.WithInclude(["a", "--include", "x.cs", "y.cs", "--severity", "warn", "--report", "r"], ["src/A.cs"]));
    }

    [Fact]
    public void Preview_SaysNotClean_WhenCSharpierPutsBackWhatTheFixesChange()
    {
        var rules = new Dictionary<string, SortedDictionary<string, int>>();
        Assert.Contains("not clean: CSharpier puts back what the fixes change in 2 files", PreviewCommand.FormatSummary(69, 318, rules, [true, false], putBack: 2), StringComparison.Ordinal);
        Assert.Contains("clean after 1 run", PreviewCommand.FormatSummary(69, 318, rules, [true, false]), StringComparison.Ordinal);
        Assert.Contains("still changing after 3 runs", PreviewCommand.FormatSummary(69, 318, rules, [true, true, true], putBack: 2), StringComparison.Ordinal);
    }

    [Fact]
    public void Snapshot_EndingsAside_IgnoresLineEndingsAndAByteOrderMark()
    {
        Assert.Equal(FormatCommand.Hash("a\nb\n"u8.ToArray(), endingsAside: true), FormatCommand.Hash("﻿a\r\nb\r\n"u8.ToArray(), endingsAside: true));
        Assert.NotEqual(FormatCommand.Hash("a\nb\n"u8.ToArray()), FormatCommand.Hash("a\r\nb\r\n"u8.ToArray()));
        Assert.NotEqual(FormatCommand.Hash("a\nb\n"u8.ToArray(), endingsAside: true), FormatCommand.Hash("a\n b\n"u8.ToArray(), endingsAside: true));
    }
}
