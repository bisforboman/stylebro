using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fourteenth trial (Bogus, Kavita, EntityGraphQL) found in init, baseline and format.</summary>
public sealed partial class MigrationTests
{
    private const string IdeOff = "dotnet_diagnostic.IDE0055.severity = none";

    [Fact]
    public void FileSettings_TheNearestEditorConfigThatSetsAKeyDecides()
    {
        // Bogus: the root's [*.cs] says true, Source/.editorconfig's [*] (not root = true) false, and the nearer one wins.
        Write(".editorconfig", "root = true\n[*.cs]\ninsert_final_newline = true\ncharset = utf-8\n# BEGIN stylebro-migrate\n[*.cs]\ncharset = utf-8-bom\n# END stylebro-migrate\n");
        Write("Source/.editorconfig", "[*]\ninsert_final_newline = false\nend_of_line = crlf\n[*.cs]\nend_of_line = lf\n");
        Write("Own/.editorconfig", "root = true\n[*.cs]\ninsert_final_newline = false\n");
        Write("A.cs", "class A\n{\n}\n");
        Write("Source/B.cs", "class B\n{\n}");
        Write("Source/Sub/C.cs", "class C\n{\n}");
        Write("Own/D.cs", "class D\n{\n}");

        var groups = Conventions.FileSettings(root);

        string? Source(string key, string value) => groups.Single(g => g.Key == key && g.Value == value).Source;
        Assert.Equal(string.Empty, Source(Conventions.FinalNewlineKey, "true"));
        Assert.Equal("Source", Source(Conventions.FinalNewlineKey, "false"));
        Assert.Equal(2, groups.Single(g => g.Key == Conventions.FinalNewlineKey && g.Value == "false").Counts[Conventions.FinalNewlineKey][0]);

        // A later section in the nearer file wins; the stylestylebro-migrate block isn't the repository's; a 'root = true' folder is its own.
        Assert.Equal("Source", Source(Conventions.EndOfLineKey, "lf"));
        Assert.DoesNotContain(groups, g => g.Value is "crlf" or "utf-8-bom");
        Assert.Equal(3, groups.Single(g => g.Key == Conventions.CharsetKey && g.Value == "utf-8").Counts[Conventions.CharsetKey].Sum());
        Assert.Null(groups.Single(g => g.Key == Conventions.TrailingWhitespaceKey).Source);
    }

    [Fact]
    public void Init_WritesAWhitespaceDecisionInTheEditorConfigThatDecidesIt_AndKeepsIt()
    {
        // Bogus: the root block's 'insert_final_newline = unset' never reached Source/, whose own key said false.
        Write(".editorconfig", "root = true\n[*.cs]\ninsert_final_newline = true\n");
        Write("Source/.editorconfig", "[*]\ninsert_final_newline = false\n");
        for (var i = 0; i < 10; i++)
        {
            Write($"Source/A{i}.cs", $"class A{i}\n{{\n}}" + (i < 5 ? "\n" : string.Empty));
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))).Replace("\r\n", "\n");

        var nested = File.ReadAllText(Path.Combine(root, "Source", ".editorconfig"));
        Assert.Contains("Final line breaks: Source/.editorconfig says insert_final_newline = false, but 5 of 10 C# files end with a line break", output, StringComparison.Ordinal);
        Assert.Contains("Wrote the whitespace settings it decides to Source/.editorconfig.", output, StringComparison.Ordinal);
        Assert.StartsWith("[*]\ninsert_final_newline = false\n\n" + InitCommand.WhitespaceBegin, nested, StringComparison.Ordinal);
        Assert.Contains("[*.cs]\n# init: Final line breaks: Source/.editorconfig says", nested, StringComparison.Ordinal);
        Assert.Contains("\ninsert_final_newline = unset\n" + InitCommand.WhitespaceEnd, nested, StringComparison.Ordinal);
        Assert.DoesNotContain("insert_final_newline = unset", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);

        // After 'format' (every file without its line break, say) a second init keeps the decision.
        for (var i = 0; i < 5; i++)
        {
            Write($"Source/A{i}.cs", $"class A{i}\n{{\n}}");
        }

        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(nested, File.ReadAllText(Path.Combine(root, "Source", ".editorconfig")));
        Assert.Contains("Source/.editorconfig: insert_final_newline kept from the last init", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_KeepsTheRootsFinalNewlineDecision()
    {
        // Bogus's second init counted the files 'format' had stripped and flipped 'unset' to 'false'.
        Write(".editorconfig", "root = true\n[*.cs]\ninsert_final_newline = true\n");
        for (var i = 0; i < 10; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}" + (i < 5 ? "\n" : string.Empty));
        }

        Capture(() => InitCommand.Run(new[] { root, "--write" }));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("\ninsert_final_newline = unset\n", first, StringComparison.Ordinal);
        for (var i = 0; i < 5; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}");
        }

        var output = Capture(() => InitCommand.Run(new[] { root, "--write" }));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("insert_final_newline: kept from the last init (unset)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_LineEndingsANestedEditorConfigDecides_AreJudgedThere()
    {
        // The root block's end_of_line doesn't reach files a nearer .editorconfig sets it for: those files don't vote on it.
        Write("Source/.editorconfig", "[*]\nend_of_line = crlf\n");
        for (var i = 0; i < 10; i++)
        {
            Write($"Source/A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.DoesNotContain("end_of_line", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
        Assert.Contains("Line endings: Source/.editorconfig says end_of_line = crlf, but 10 of 10 C# files are LF", output, StringComparison.Ordinal);
        Assert.Contains("\nend_of_line = lf\n" + InitCommand.WhitespaceEnd, File.ReadAllText(Path.Combine(root, "Source", ".editorconfig")), StringComparison.Ordinal);
    }

    [Fact]
    public void EndingContradiction_NamesTheEditorConfigThatSaysIt()
    {
        var (lines, report) = Conventions.EndingContradiction("crlf", 200, "Source/.editorconfig")!.Value;

        Assert.StartsWith("# init: Line endings: Source/.editorconfig says end_of_line = crlf, but git checks out 200 C# files", lines[0], StringComparison.Ordinal);
        Assert.Equal(["end_of_line = unset", "dotnet_diagnostic.IDE0055.severity = suggestion"], lines.Skip(1));
        Assert.Contains("for C# files in Source/.editorconfig", report, StringComparison.Ordinal);
        Assert.Null(Conventions.EndingContradiction("crlf", 0, "Source/.editorconfig"));
        Assert.Null(Conventions.EndingContradiction("auto", 3));
    }

    [Theory]
    [InlineData(6, 4, true)]    // Bogus: 'foreach( var x in y )' in 65 % of the places
    [InlineData(10, 0, false)]  // clear: written
    [InlineData(0, 10, false)]  // the default
    public void Spacing_MixedCodeTurnsIde0055Off(int other, int standard, bool off)
    {
        var code = string.Concat(Enumerable.Range(0, other).Select(i => $"        if( x{i} ) {{ }}\n"))
            + string.Concat(Enumerable.Range(0, standard).Select(i => $"        if (y{i}) {{ }}\n"));
        var counts = Conventions.NewCounts();
        Conventions.Count(CSharpSyntaxTree.ParseText($"class C\n{{\n    void M()\n    {{\n{code}    }}\n}}\n"), counts);

        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());

        Assert.Equal(off, lines.Contains(IdeOff));
        Assert.Equal(other == 10, lines.Contains("csharp_space_after_keywords_in_control_flow_statements = false"));
        Assert.Equal(other == 10, lines.Contains("csharp_space_between_parentheses = control_flow_statements"));
        Assert.Contains(report, l => l.Contains("spacing, control-flow keywords", StringComparison.Ordinal));
        if (off)
        {
            Assert.Contains(lines, l => l.StartsWith(Conventions.SpacingComment + " your code mixes control-flow keywords (4 'if (x)', 6 'if(x)')", StringComparison.Ordinal));
        }

        // Another formatter's (or the team's) spacing is only reported.
        (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>(), spacing: "CSharpier formats it");
        Assert.DoesNotContain(IdeOff, lines);
        Assert.Equal(["  set      spacing (csharp_space_*): CSharpier formats it"], report.Where(l => l.Contains("spacing", StringComparison.Ordinal)));
    }

    [Fact]
    public void Spacing_CountsCastsCallsAndDeclarations()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(CSharpSyntaxTree.ParseText("class C\n{\n    int M( int a ) => (int) a + ( a * 2 ) + F( a) + F();\n}\n"), counts);

        int[] Of(int index) => counts[Conventions.SpacingCountKey(Conventions.Spacing[index])];
        Assert.Equal(new[] { 1, 0, 0 }, Of(3));  // '(int)': no spaces inside
        Assert.Equal(new[] { 0, 1, 0 }, Of(4));  // ') a': a space after the cast
        Assert.Equal(new[] { 0, 1, 0 }, Of(2));  // '( a * 2 )'
        Assert.Equal(new[] { 0, 0, 1 }, Of(5));  // 'F( a)' one-sided, 'F()' empty
        Assert.Equal(new[] { 0, 1, 0 }, Of(6));  // 'M( int a )'
    }

    [Fact]
    public void Init_KeepsTheSpacingDecision()
    {
        Write("A.cs", "class A\n{\n    void M(int x)\n    {\n" + string.Concat(Enumerable.Range(0, 5).Select(i => $"        if( x == {i} ) {{ }}\n        if (x == {i}) {{ }}\n")) + "    }\n}\n");

        Capture(() => InitCommand.Run(new[] { root, "--write" }));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains(IdeOff, first, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = warning", first, StringComparison.Ordinal);

        // 'format' doesn't change the spacing with IDE0055 off; counted again with the other form, the block stays.
        Write("A.cs", "class A\n{\n    void M(int x)\n    {\n" + string.Concat(Enumerable.Range(0, 10).Select(i => $"        if (x == {i}) {{ }}\n")) + "    }\n}\n");
        var output = Capture(() => InitCommand.Run(new[] { root, "--write" }));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("  set      spacing (csharp_space_*): kept from the last init", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Separation_CompactCodeTurnsBro1505AndBro1513Off()
    {
        // Kavita: members and doc comments without blank lines between them, and BRO1505/BRO1513 added them in 392 and 312 files.
        var members = string.Concat(Enumerable.Range(0, 6).Select(i => $"    /// <summary>M{i}.</summary>\n    void M{i}()\n    {{\n    }}\n"));
        var counts = Conventions.NewCounts();
        Conventions.Count(CSharpSyntaxTree.ParseText($"class C\n{{\n    int x;\n{members}}}\n"), counts);

        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());

        Assert.Contains("dotnet_diagnostic.BRO1505.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1513.severity = none", lines);
        Assert.Contains(report, l => l.Contains("neighbouring members: 6 of 6 without one -> BRO1505 is off", StringComparison.Ordinal));
        Assert.Contains(report, l => l.Contains("doc comments below code: 6 of 6 right below the code -> BRO1513 is off", StringComparison.Ordinal));

        // Separated code keeps both rules; the first doc comment after '{' isn't counted.
        counts = Conventions.NewCounts();
        Conventions.Count(CSharpSyntaxTree.ParseText($"class C\n{{\n{members.Replace("    }\n", "    }\n\n")}}}\n"), counts);
        (lines, _) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.DoesNotContain(lines, l => l.Contains("BRO1505", StringComparison.Ordinal) || l.Contains("BRO1513", StringComparison.Ordinal));
        Assert.Equal(new[] { 5, 0 }, counts["dotnet_diagnostic.BRO1513.severity"]);
    }

    [Fact]
    public void Init_TakesProject_AndKeepsTheBlockOrderOnASecondRun()
    {
        // EntityGraphQL: a second init swapped the using-order and BRO1306 lines.
        Write("Source/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        for (var i = 0; i < 4; i++)
        {
            Write($"Source/A{i}.cs", $"using System;\nusing System.IO;\n\nclass A{i}\n{{\n    private const int One{i} = 1;\n    private const int two{i} = 2;\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--project", "Source/App.csproj" })));
        Assert.Contains("To format it: 'bro format Source/App.csproj'.", output, StringComparison.Ordinal);
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.True(first.IndexOf("dotnet_sort_system_directives_first", StringComparison.Ordinal) < first.IndexOf("BRO1306", StringComparison.Ordinal));

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));

        Assert.Contains("--project is relative to", CaptureError(() => Assert.Equal(1, InitCommand.Run(new[] { root, "--project", "Missing.sln" }))), StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_MergesTheReportsOfThePasses()
    {
        var merged = BaselineCommand.MergeReports(["[{\"FilePath\":\"a.cs\",\"FileChanges\":[]}]", "[]", "[{\"FilePath\":\"b.cs\",\"FileChanges\":[]}]"]);

        Assert.Equal("[{\"FilePath\":\"a.cs\",\"FileChanges\":[]},{\"FilePath\":\"b.cs\",\"FileChanges\":[]}]", merged);
    }

    [Fact]
    public void NotBuiltFolders_AreTheProjectsNoConfigurationBuilds()
    {
        // Bogus's Nuke builder: listed in Bogus.sln, only an ActiveCfg line, never restored, and 'format' warned about it.
        Write("Source/Bogus.sln", """
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Bogus", "Bogus\Bogus.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Builder", "Builder\Builder.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Shared", "Bogus\Shared\Shared.csproj", "{33333333-3333-3333-3333-333333333333}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Lib", "Lib\Lib.csproj", "{55555555-5555-5555-5555-555555555555}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Parent", "Bogus\Parent.csproj", "{44444444-4444-4444-4444-444444444444}"
            EndProject
            Global
            	GlobalSection(ProjectConfigurationPlatforms) = postSolution
            		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
            		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{33333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{44444444-4444-4444-4444-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{55555555-5555-5555-5555-555555555555}.Release|Any CPU.ActiveCfg = Release|Any CPU
            		{55555555-5555-5555-5555-555555555555}.Release|Any CPU.Build.0 = Release|Any CPU
            	EndGlobalSection
            EndGlobal
            """);
        var source = Path.Combine(root, "Source");
        var projects = new[] { Path.Combine(source, "Bogus", "Bogus.csproj"), Path.Combine(source, "Builder", "Builder.csproj"), Path.Combine(source, "Bogus", "Shared", "Shared.csproj"), Path.Combine(source, "Bogus", "Parent.csproj"), Path.Combine(source, "Lib", "Lib.csproj") };

        // Parent isn't built, but Bogus (built) is in its folder: leaving the folder out would leave Bogus out.
        Assert.Equal(["Source/Bogus/Shared/", "Source/Builder/"], FormatCommand.NotBuiltFolders(Path.Combine(source, "Bogus.sln"), projects, root));
        Assert.Empty(FormatCommand.NotBuiltFolders(Path.Combine(source, "Bogus.slnx"), projects, root));
    }
}
