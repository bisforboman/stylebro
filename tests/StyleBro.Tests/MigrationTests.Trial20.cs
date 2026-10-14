using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the 20th trial found in init: opt-out folders, IDE0055's own severity, enforced rules by category, new conventions.</summary>
public sealed partial class MigrationTests
{
    private const string OptOutFolder = "# top-most EditorConfig file\nroot = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = none\n";

    // coverlet: 12 'root = true' folders with 'dotnet_analyzer_diagnostic.severity = none' ("ensure no rules are enabled for these
    // asset source files"); init wrote ~119 rules at warning there, format re-indented the samples and 49 tests failed.
    [Fact]
    public void Init_TurnsRulesOffById_WhereAFolderTurnsEveryAnalyzerOff()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("App/C.cs", "class C\n{\n}\n");
        Write("Samples/.editorconfig", OptOutFolder + "dotnet_diagnostic.BRO1002.severity = warning\n");
        Write("Samples/S.cs", "class S {\n  int x;\n}\n");
        Write("tests/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = silent\n");
        Write("tests/T.cs", "class T\n{\n}\n");
        Assert.StartsWith("Not clean", FormatCommand.HiddenLine(root), StringComparison.Ordinal);

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var samples = File.ReadAllText(Path.Combine(root, "Samples", ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = none\n", samples, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none\n", samples, StringComparison.Ordinal);
        Assert.DoesNotContain("= warning\n", samples.Substring(samples.IndexOf(Migration.BeginMarker, StringComparison.Ordinal)), StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1002.severity = warning\n", samples, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1002.severity = none", samples, StringComparison.Ordinal);
        Assert.Contains("Turned StyleBro's rules and the built-in ones off by id in Samples/.editorconfig", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Conventions in the code under Samples/", output, StringComparison.Ordinal);

        // The nested folder without 'root = true' turns off what the root's block turns on too (IDE0049 = warning there).
        var tests = File.ReadAllText(Path.Combine(root, "tests", ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains(InitCommand.OptOutComment, tests, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0049.severity = none\n", tests, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1527.severity = none\n", tests, StringComparison.Ordinal);
        Assert.Contains(InitCommand.WhitespaceBegin, tests, StringComparison.Ordinal);

        // 'format' leaves both folders out of its whitespace pass, says clean, and a second init changes nothing.
        Assert.Equal(["Samples/", "tests/"], FormatCommand.WhitespaceExclusions(root));
        Assert.Null(FormatCommand.HiddenLine(root));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(samples, File.ReadAllText(Path.Combine(root, "Samples", ".editorconfig")).Replace("\r\n", "\n"));
        Assert.Equal(tests, File.ReadAllText(Path.Combine(root, "tests", ".editorconfig")).Replace("\r\n", "\n"));
    }

    [Fact]
    public void NestedHiddenFromFormat_AFolderBelowAnOptOutFolder_NeedsNothingOfItsOwn()
    {
        Write(".editorconfig", "root = true\n");
        Write("samples/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = none\n");
        Write("samples/a/.editorconfig", "[*.cs]\nindent_size = 2\n");

        var hidden = InitCommand.NestedHiddenFromFormat(root, new Dictionary<string, string> { ["dotnet_diagnostic.CA1000.severity"] = "warning", ["dotnet_diagnostic.BRO1143.severity"] = "warning" }, withBlocks: false, ["dotnet_diagnostic.BRO1143.severity"]);

        Assert.Equal(["samples"], hidden.Keys);
        Assert.Contains("dotnet_diagnostic.BRO1143.severity = none", hidden["samples"]);
        Assert.DoesNotContain("dotnet_diagnostic.CA1000.severity = none", hidden["samples"]);
    }

    // MoreLINQ sets IDE0055 = suggestion; init wrote warning, while the report said it kept theirs.
    [Fact]
    public void Init_KeepsTheRepositorysOwnIde0055Severity()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.IDE0055.severity = suggestion\n");
        Write("A.cs", "class A\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.DoesNotContain("IDE0055.severity = warning", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
        Assert.Contains(".editorconfig sets IDE0055 (suggestion): kept", output, StringComparison.Ordinal);
    }

    // MoreLINQ: 'dotnet_style_qualification_for_field = true' with 'category-Style.severity = warning'; init wrote IDE0009 = none.
    [Theory]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = warning\n", true)]
    [InlineData("dotnet_analyzer_diagnostic.severity = suggestion\n", true)]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = silent\n", false)]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = warning\ndotnet_diagnostic.IDE0009.severity = none\n", false)]
    public void RequiresThisQualification_ReadsCategoryAndBulkSeverities(string severities, bool required)
    {
        var settings = (severities + "dotnet_style_qualification_for_field = true\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToDictionary(l => l.Split('=')[0].Trim(), l => l.Split('=')[1].Trim());

        Assert.Equal(required, InitCommand.RequiresThisQualification(settings));
    }

    // One-line methods ('{ a(); } else return b;') counted as 'else on the } line' and turned the code K&R.
    [Fact]
    public void Conventions_ElseAfterAOneLineBlock_DoesNotCount()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class A
            {
                int F(bool x) { if (x) { return 1; } else return 2; }

                void G() { try { F(true); } catch { } finally { } }

                int H(bool x)
                {
                    if (x)
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
            }
            """),
            counts);

        Assert.Equal([1, 0], counts["csharp_new_line_before_else"]);
    }

    // MoreLINQ: a blank line between the doc comment and its element in 324 of 491 places; BRO1511 removed them in 115 files.
    // Altinn: primary-constructor parameters named like fields ('_logger'), and camelCase tuple elements.
    [Fact]
    public void Conventions_CountDocBlankLines_PrimaryParameters_AndTupleElements()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class A(int _logger, int _clock, string name, int Upper)
            {
                /// <summary>One.</summary>

                public void B() { }

                /// <summary>Two.</summary>
                public (int count, string Name) C() => default;

                /// <summary>Three.</summary>
                // A comment between.

                public void D() { }
            }

            record R(int _skipped);
            """),
            counts);

        Assert.Equal([1, 1], counts["dotnet_diagnostic.BRO1511.severity"]);
        Assert.Equal([1, 2], counts["dotnet_diagnostic.BRO1302.severity"]);
        Assert.Equal([1, 1], counts["stylebro_tuple_element_name_casing"]);
    }

    [Fact]
    public void Init_TurnsBro1302Off_WhenPrimaryParametersAreNamedLikeFields_AndKeepsCamelCaseTupleElements()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("A.cs", "class A(int _a, int _b, int _c)\n{\n    (int first, int second) F() => default;\n\n    (int third, int fourth) G() => default;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("\ndotnet_diagnostic.BRO1302.severity = none\n", text, StringComparison.Ordinal);
        Assert.Contains("BRO1302 has no switch for primary-constructor parameters alone", text, StringComparison.Ordinal);
        Assert.Contains("\nstylebro_tuple_element_name_casing = camelCase\n", text, StringComparison.Ordinal);
        Assert.Contains("primary-constructor parameters: 3 of 3 named like fields", output, StringComparison.Ordinal);
    }

    // The S4136 report line said 'same_access' while the root's NoWarn made the block write false.
    [Fact]
    public void Annotate_SaysWhereANoWarnResetsASonarOption()
    {
        var report = new List<string> { "  S4136 -> BRO1001 (stylebro_keep_overloads_together = same_access; ...)", "  S1066 -> BRO1149" };

        Suppressions.Annotate(report, [("*.cs", ["# S4136 in Directory.Build.props's <NoWarn>: StyleBro's own value there, the rule stays on", "stylebro_keep_overloads_together = false"])]);

        Assert.Equal("  S4136 -> BRO1001 keeps stylebro_keep_overloads_together = false (StyleBro's own value): Directory.Build.props's <NoWarn> has S4136", report[0]);
        Assert.Equal("  S1066 -> BRO1149", report[1]);

        report = ["  S4136 -> BRO1001 (same_access)"];
        Suppressions.Annotate(report, [("src/**.cs", ["# S4136 in src/Directory.Build.props's <NoWarn>: StyleBro's own value there, the rule stays on", "stylebro_keep_overloads_together = false"])]);
        Assert.Equal("  S4136 -> BRO1001 (same_access); stylebro_keep_overloads_together = false (StyleBro's own value) in src/**.cs: src/Directory.Build.props's <NoWarn> has S4136", report[0]);
    }

    // coverlet's Documentation/Examples and Altinn's test apps: projects no solution lists, without StyleBro, got a preset version.
    [Fact]
    public void Init_PinsThePreset_OnlyWhereTheSolutionsProjectsReadIt()
    {
        Write(".editorconfig", "root = true\n");
        Write("App.slnx", "<Solution>\n  <Project Path=\"src/App/App.csproj\" />\n</Solution>\n");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/App/C.cs", "class C\n{\n}\n");
        Write("Examples/Demo/Directory.Build.props", "<Project>\n</Project>\n");
        Write("Examples/Demo/Demo.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Examples/Demo/D.cs", "class D\n{\n}\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("<StyleBroPreset>", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("StyleBroPreset", File.ReadAllText(Path.Combine(root, "Examples", "Demo", "Directory.Build.props")), StringComparison.Ordinal);
    }

    // The nested block held 119 severities but was called 'stylebro-whitespace' and announced as "the whitespace settings".
    [Fact]
    public void Init_ReplacesALegacyWhitespaceBlock_AndSaysWhatItWrote()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("tests/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = suggestion\n\n" + InitCommand.LegacyWhitespaceBegin + " (old)\n[*.cs]\ndotnet_diagnostic.BRO1001.severity = warning\n" + InitCommand.LegacyWhitespaceEnd + "\n");
        Write("tests/T.cs", "class T\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, "tests", ".editorconfig"));
        Assert.DoesNotContain(InitCommand.LegacyWhitespaceBegin, text, StringComparison.Ordinal);
        Assert.Contains(InitCommand.WhitespaceBegin, text, StringComparison.Ordinal);
        Assert.Matches("Wrote [0-9]+ rule severities by id to tests/.editorconfig", output);
        Assert.Equal("Wrote the whitespace settings it decides to a/.editorconfig.", InitCommand.WroteLine("a/.editorconfig", InitCommand.WhitespaceBlock(["insert_final_newline = true"]), nested: true));
    }
}
