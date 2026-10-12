using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the twelfth trial (the starter kit, RestSharp, sourcegit) found in init's conventions.</summary>
public sealed partial class MigrationTests
{
    [Theory]
    [InlineData(new[] { 513, 1203 }, "false", "insert_final_newline = unset")]   // the starter kit: mixed, each file stays
    [InlineData(new[] { 2, 40 }, "false", "insert_final_newline = true")]
    [InlineData(new[] { 40, 2 }, "true", "insert_final_newline = false")]
    [InlineData(new[] { 1, 40 }, "true", null)]                                // the key's form has the share: it stays
    [InlineData(new[] { 0, 2 }, "false", null)]                                // too few
    public void ContradictedFileSetting_FilesDecideAFinalNewlineTheyDontFollow(int[] files, string setting, string? expected)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.FinalNewlineKey] = files;

        var result = Conventions.ContradictedFileSetting(counts, new Dictionary<string, string> { ["insert_final_newline"] = setting }, Conventions.FinalNewlineKey);

        Assert.Equal(expected, result?.Line);
    }

    [Fact]
    public void Conventions_CountFinalNewlinesAndTrailingWhitespacePerFile()
    {
        var counts = Conventions.NewCounts();
        foreach (var text in new[] { "class A\n{\n}\n", "class B\r\n{\r\n}", "class C  \n{\n}\n", string.Empty })
        {
            Conventions.CountFile(text, counts);
        }

        Assert.Equal(new[] { 1, 2 }, counts[Conventions.FinalNewlineKey]);
        Assert.Equal(new[] { 2, 1 }, counts[Conventions.TrailingWhitespaceKey]);
    }

    [Fact]
    public void Init_FinalNewlineTheFilesDontFollow_IsUnsetForCSharpFiles()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ninsert_final_newline = false\ntrim_trailing_whitespace = false\n");
        Write("A0.cs", "class A0\n{\n}\n");
        Write("A1.cs", "class A1\n{\n}\n");
        Write("A2.cs", "class A2\n{\n}");
        Write("A3.cs", "class A3  \n{\n}");

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        Assert.Contains("  note     Final line breaks: your .editorconfig says insert_final_newline = false, but 2 of 4 C# files end with a line break", output, StringComparison.Ordinal);
        Assert.Contains("\ninsert_final_newline = unset\n", output, StringComparison.Ordinal);
        Assert.Contains("  note     Trailing whitespace: your .editorconfig says trim_trailing_whitespace = false, but 'dotnet format' doesn't read that key", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_FinalNewlineAndCharsetTheFilesDontFollow_FollowTheFiles()
    {
        Write(".editorconfig", "root = true\n[*]\ninsert_final_newline = false\ncharset = utf-8-bom\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("insert_final_newline = true", result.Lines);
        Assert.Contains("charset = utf-8", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("Final line breaks: your .editorconfig says insert_final_newline = false, but 3 of 3 C# files end with a line break", StringComparison.Ordinal));
    }

    [Fact]
    public void AlignmentKeys_AreReSharpersColumnAlignment()
    {
        var own = new Dictionary<string, string>
        {
            ["resharper_int_align_variables"] = "true",
            ["resharper_csharp_int_align_fields"] = "true",
            ["resharper_int_align_properties"] = "false",
            ["resharper_csharp_int_align_comments"] = "true",          // IDE0055 leaves a trailing comment's spaces
            ["resharper_align_multiline_binary_expressions_chain"] = "true",
        };

        Assert.Equal(new[] { "resharper_csharp_int_align_fields", "resharper_int_align_variables" }, Conventions.AlignmentKeys(own));
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none", Conventions.AlignmentLines(own)!.Value.Lines);

        own["dotnet_diagnostic.IDE0055.severity"] = "warning";      // the team's own severity stays
        Assert.Null(Conventions.AlignmentLines(own));
    }

    [Fact]
    public void Init_ColumnAlignment_TurnsIDE0055Off()
    {
        // RestSharp: 'var doc      = CreateAttributesXml();' became 'var doc = CreateAttributesXml();' in 171 files.
        Write(".editorconfig", "root = true\n[*]\nresharper_int_align_variables = true\nresharper_int_align_assignments = true\n");
        Write("A.cs", "class A\n{\n    void M()\n    {\n        var a      = 1;\n        var longer = 2;\n    }\n}\n");

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        Assert.Contains("  off      column alignment (resharper_int_align_assignments, resharper_int_align_variables = true) -> IDE0055 is off", output, StringComparison.Ordinal);
        Assert.Contains("\n# init: column alignment: your .editorconfig aligns code in columns", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.IDE0055.severity = none\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = warning", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_CountAutoPropertiesOnOneLineOrSeveral()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class A
            {
                public int One { get; set; }
                public int Two { get; private set; } = 2;
                public int Three
                {
                    get;
                    set;
                }
                public int Body { get => 1; }
                public int Four
                {
                    get; init;
                }
            }
            """),
            counts);

        Assert.Equal(new[] { 2, 2 }, counts[Conventions.AutoAccessorsKey]);
    }

    [Theory]
    [InlineData(new[] { 732, 306 }, "dotnet_diagnostic.BRO1527.severity = none")]   // sourcegit: 70 %, mixed
    [InlineData(new[] { 2, 30 }, "dotnet_diagnostic.BRO1527.severity = none")]
    [InlineData(new[] { 30, 2 }, null)]
    public void Conventions_AutoPropertyLayout_TurnsBRO1527Off(int[] count, string? expected)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.AutoAccessorsKey] = count;

        var lines = Conventions.Decide(counts, new Dictionary<string, string>()).Lines.Where(l => !l.StartsWith('#') && !l.StartsWith("dotnet_sort", StringComparison.Ordinal)).ToList();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, lines);
    }

    [Fact]
    public void Conventions_CountRegionsInsideMembers()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class Tests
            {
                #region Helpers
                void Test()
                {
                    #region Arrange
                    var a = 1;
                    #endregion

                    #region Act
                    a++;
                    #endregion
                }
                #endregion
            }
            """),
            counts);

        Assert.Equal(new[] { 0, 2 }, counts[Conventions.RegionsKey]);
    }

    [Theory]
    [InlineData(114, true)]    // the starter kit's '#region Arrange/Act/Assert' in 38 tests
    [InlineData(3, true)]
    [InlineData(2, false)]     // too few
    public void Conventions_RegionsInsideMembers_TurnBRO1113Off(int regions, bool off)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.RegionsKey] = new[] { 0, regions };

        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());

        Assert.Equal(off, lines.Contains("dotnet_diagnostic.BRO1113.severity = none"));
        if (off)
        {
            Assert.Contains(report, l => l == $"  off      '#region's inside members: {regions} '#region's -> BRO1113 is off");
        }
    }

    [Fact]
    public void Conventions_CountOneLineJumps()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class A
            {
                int M(int x)
                {
                    while (true)
                    {
                        if (x > 1) break;
                        if (x > 2) continue;
                        if (x > 3) { return 1; }
                        if (x > 4)
                            return 2;
                        if (x > 5) x++;
                    }
                }
            }
            """),
            counts);

        // Braceless on the 'if' line: break, continue. In braces: 'return 1'. 'return 2' is below its 'if' (not a one-line jump).
        Assert.Equal(new[] { 1, 2 }, counts[Conventions.SingleLineJumpsKey]);
    }

    [Theory]
    [InlineData(new[] { 515, 169 }, true)]   // the starter kit: 3 are enough to keep them
    [InlineData(new[] { 40, 3 }, true)]
    [InlineData(new[] { 40, 2 }, false)]
    public void Conventions_OneLineJumps_AreKept(int[] count, bool kept)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.SingleLineJumpsKey] = count;

        var lines = Conventions.Decide(counts, new Dictionary<string, string>()).Lines;

        Assert.Equal(kept, lines.Contains("stylebro_allow_single_line_jump_statements = true"));
    }

    [Fact]
    public void Conventions_UnenforcedKeyWithMixedCode_TurnsTheRuleOff()
    {
        // The starter kit: beginning_of_line, unenforced, 486 at the start and 285 at the end; BRO1520 moved 285.
        var counts = Conventions.NewCounts();
        counts["dotnet_style_operator_placement_when_wrapping"] = new[] { 486, 285 };
        var key = new Dictionary<string, string> { ["dotnet_style_operator_placement_when_wrapping"] = "set in .editorconfig (beginning_of_line)" };
        var repository = new Dictionary<string, string> { ["dotnet_style_operator_placement_when_wrapping"] = "beginning_of_line" };

        var (lines, report) = Conventions.Decide(counts, key, repository: repository);

        Assert.Contains("dotnet_diagnostic.BRO1520.severity = none", lines);
        Assert.Contains(report, l => l.EndsWith(".editorconfig says beginning_of_line, unenforced, code mixed: BRO1520 is off", StringComparison.Ordinal));

        // The repository's own BRO1520 severity wins; a clear majority for the key leaves it in charge.
        Assert.DoesNotContain("dotnet_diagnostic.BRO1520.severity = none", Conventions.Decide(counts, key, k => k == "dotnet_diagnostic.BRO1520.severity", repository).Lines);
        counts["dotnet_style_operator_placement_when_wrapping"] = new[] { 760, 11 };
        Assert.DoesNotContain("dotnet_diagnostic.BRO1520.severity = none", Conventions.Decide(counts, key, repository: repository).Lines);
    }

    [Theory]
    [InlineData("false", new[] { 158, 76, 782, 104 }, 262)]   // the starter kit: System first only and unsorted move
    [InlineData("true", new[] { 158, 76, 782, 104 }, 180)]
    [InlineData("false", new[] { 0, 50, 50, 2 }, null)]       // too few would move
    public void UnsortedUnderKey_NamesTheKeyAsTheCause(string value, int[] files, int? moved)
    {
        var note = Conventions.UnsortedUnderKey(files, new Dictionary<string, string> { [Conventions.SortUsingsKey] = value });

        Assert.Equal(moved is null, note is null);
        if (moved is not null)
        {
            Assert.Contains($"your .editorconfig sets dotnet_sort_system_directives_first = {value}", note, StringComparison.Ordinal);
            Assert.Contains($": {moved} of {files.Sum()} files aren't in that order", note, StringComparison.Ordinal);
        }

        Assert.Null(Conventions.UnsortedUnderKey(files, new Dictionary<string, string>()));
    }

    [Fact]
    public void Init_Again_KeepsARuleTurnedOffAgainstTheRepositorysKey()
    {
        // sourcegit: 'format' moved the count from 263 to 264 operators, and the second init rewrote BRO1520's line.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_style_operator_placement_when_wrapping = beginning_of_line\n");
        Write("A.cs", "class A\n{\n    bool M(bool a, bool b) =>\n        a &&\n        b &&\n        a &&\n        b;\n}\n");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.BRO1520.severity = none", first, StringComparison.Ordinal);

        Write("B.cs", "class B\n{\n    bool M(bool a, bool b) =>\n        a &&\n        b;\n}\n");
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))).Replace("\r\n", "\n");

        Assert.Contains("-> kept from the last init (BRO1520 off; .editorconfig says beginning_of_line)", output, StringComparison.Ordinal);
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Init_WithAllCodeUnderARootFolder_WritesNoRootEditorConfig()
    {
        // The starter kit: all C# under src/ (root = true); the root .editorconfig init created was read by nothing.
        Write("src/.editorconfig", "root = true\n[*.cs]\nindent_size = 4\n");
        Write("src/A.cs", "class A\n{\n}\n");
        Write("templates/Template.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("No C# code outside src/, whose .editorconfig says root = true: no root .editorconfig is written.", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
        Assert.Contains(Migration.BeginMarker, File.ReadAllText(Path.Combine(root, "src", ".editorconfig")), StringComparison.Ordinal);
    }

    [Fact]
    public void PropsFiles_LeaveOutProjectsWithoutCode()
    {
        // The starter kit's template pack project has no C#: a root Directory.Build.props just for it was read by nothing.
        Write("src/Directory.Build.props", "<Project />");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/App/A.cs", "class A { }");
        Write("templates/Template.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal(new[] { "src/Directory.Build.props" }, Migration.PropsFiles(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')));
    }

    [Theory]
    [InlineData("unset", false)]
    [InlineData("Copyright A.", true)]
    public void Init_FileHeaderLine_OnlyWithATemplate(string template, bool expected)
    {
        Write(".editorconfig", $"root = true\n[*.cs]\nfile_header_template = {template}\n");
        Write("A.cs", "class A\n{\n}\n");

        var output = Capture(() => InitCommand.Run(new[] { root }));

        Assert.Equal(expected, output.Contains("The file header is IDE0073's", StringComparison.Ordinal));
    }
}
