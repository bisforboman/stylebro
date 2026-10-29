using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the twenty-sixth trial (Serde, NLog, Bencodex, Lua) found in migrate and init.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Init_TurnsBro1519Off_WhenStatementsAfterBlocksAreMixed()
    {
        // Serde: 365 statements after a block's '}' after a blank line, 543 right below it; NLog 1417 to 480 (74.7 %).
        foreach (var mixed in new[] { new[] { 365, 543 }, [1417, 480] })
        {
            var counts = Conventions.NewCounts();
            counts["dotnet_diagnostic.BRO1519.severity"] = mixed;
            var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());
            Assert.Contains("dotnet_diagnostic.BRO1519.severity = none", lines);
            Assert.Contains(report, l => l.Contains("statements after a block's '}':", StringComparison.Ordinal) && l.EndsWith("-> BRO1519 is off", StringComparison.Ordinal));
        }

        // 75 % with the blank line: BRO1519 stays; 75 % without it: off. A severity the team set stays theirs.
        var clear = Conventions.NewCounts();
        clear["dotnet_diagnostic.BRO1519.severity"] = [75, 25];
        Assert.DoesNotContain("dotnet_diagnostic.BRO1519.severity = none", Conventions.Decide(clear, new Dictionary<string, string>()).Lines);
        clear["dotnet_diagnostic.BRO1519.severity"] = [25, 75];
        Assert.Contains("dotnet_diagnostic.BRO1519.severity = none", Conventions.Decide(clear, new Dictionary<string, string>()).Lines);
        clear["dotnet_diagnostic.BRO1519.severity"] = [50, 50];
        Assert.DoesNotContain("dotnet_diagnostic.BRO1519.severity = none", Conventions.Decide(clear, new Dictionary<string, string>(), key => key == "dotnet_diagnostic.BRO1519.severity").Lines);

        // With BRO1505 off too, the off line and its comment come once.
        clear[Conventions.All.Single(c => c.Off == "dotnet_diagnostic.BRO1505.severity = none").Key] = [10, 10];
        var both = Conventions.Decide(clear, new Dictionary<string, string>());
        Assert.Single(both.Lines, l => l == "dotnet_diagnostic.BRO1519.severity = none");
        Assert.DoesNotContain(both.Lines, l => l.Contains("would still add a blank line after a member's '}'", StringComparison.Ordinal));
        Assert.DoesNotContain(both.Report, l => l.Contains("is off too", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("<Project>\n  <PropertyGroup></PropertyGroup>\n</Project>\n", "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n  </PropertyGroup>\n</Project>\n")]
    [InlineData("<Project>\r\n  <!-- x -->\r\n  <PropertyGroup></PropertyGroup>\r\n</Project>\r\n", "<Project>\r\n  <!-- x -->\r\n  <PropertyGroup>\r\n    <StyleBroPreset>1.0</StyleBroPreset>\r\n  </PropertyGroup>\r\n</Project>\r\n")]
    [InlineData("<Project>\n  <PropertyGroup><A>1</A></PropertyGroup>\n</Project>\n", "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n    <A>1</A></PropertyGroup>\n</Project>\n")]
    [InlineData("<Project>\n  <PropertyGroup>\n    <A>1</A>\n  </PropertyGroup>\n</Project>\n", "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n    <A>1</A>\n  </PropertyGroup>\n</Project>\n")]
    [InlineData("<Project Sdk=\"X\" />\n", "<Project Sdk=\"X\">\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n  </PropertyGroup>\n</Project>\n")]
    public void SetPreset_WritesWellFormedXml(string props, string expected)
    {
        // Serde's perf/bench/Directory.Build.props had an empty '<PropertyGroup></PropertyGroup>', and CSharpier's check rejected
        // '<StyleBroPreset>1.0</StyleBroPreset></PropertyGroup>'.
        var result = Migration.SetPreset(props, "1.0", "c")!;
        Assert.Equal(expected, result);
        System.Xml.Linq.XDocument.Parse(result);
    }

    [Fact]
    public void AddItem_PutsTheGroupOnLinesOfItsOwn()
    {
        Assert.Equal("<Project Sdk=\"X\">\n  <ItemGroup>\n    <A />\n  </ItemGroup>\n</Project>\n", PreviewCommand.AddItem("<Project Sdk=\"X\" />\n", "<A />"));
        Assert.Equal("<Project Sdk=\"X\">\n  <ItemGroup>\n    <A />\n  </ItemGroup>\n</Project>\n", PreviewCommand.AddItem("<Project Sdk=\"X\"></Project>\n", "<A />"));
    }

    [Fact]
    public void Init_CountsOnlyCompiledFiles()
    {
        // Serde: 441 Verify snapshots ('*.verified.cs') under a project that lists its own Compile items turned BRO1401 off.
        const string Commas = "class C{0}\n{{\n    int[] a = new[]\n    {{\n        1,\n        2,\n    }};\n}}\n";
        const string NoCommas = "class S{0}\n{{\n    int[] a = new[]\n    {{\n        1,\n        2\n    }};\n}}\n";
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n    <EnableDefaultItems>false</EnableDefaultItems>\n  </PropertyGroup>\n  <ItemGroup>\n    <Compile Include=\"*.cs\" />\n  </ItemGroup>\n</Project>\n");
        for (var i = 0; i < 4; i++)
        {
            Write($"src/Lib/C{i}.cs", string.Format(System.Globalization.CultureInfo.InvariantCulture, Commas, i));
            Write($"src/Lib/test_output/S{i}.verified.cs", string.Format(System.Globalization.CultureInfo.InvariantCulture, NoCommas, i));
            Write($"src/Lib/other/O{i}.cs", string.Format(System.Globalization.CultureInfo.InvariantCulture, NoCommas, i + 10));
        }

        var files = Migration.OwnCSharpFiles(root).Select(f => Path.GetFileName(f)).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(["C0.cs", "C1.cs", "C2.cs", "C3.cs"], files);
        Assert.True(Migration.IsSnapshot("A.verified.cs"));
        Assert.True(Migration.IsSnapshot("A.received.cs"));
        Assert.False(Migration.IsSnapshot("Verified.cs"));
        Assert.Equal([4, 0], Conventions.Count(root)["stylebro_trailing_comma"]);

        // Without the explicit items every file below the project counts, except the snapshots.
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Assert.Equal(12, Migration.OwnCSharpFiles(root).Count());
        Assert.Equal([4, 4], Conventions.Count(root)["stylebro_trailing_comma"]);
    }

    [Fact]
    public void Init_DoesNotAllowOneLineJumps_WhenBracesAreEnforced()
    {
        Assert.Equal("IDE0011 = warning", InitCommand.BracesEnforced(new Dictionary<string, string> { ["dotnet_diagnostic.IDE0011.severity"] = "warning", ["csharp_prefer_braces"] = "true:silent" }));
        Assert.Equal("IDE0011 = error", InitCommand.BracesEnforced(new Dictionary<string, string> { ["csharp_prefer_braces"] = "true:error" }));
        Assert.Equal("IDE0011 = warning", InitCommand.BracesEnforced(new Dictionary<string, string> { ["dotnet_diagnostic.IDE0011.severity"] = "warning" }));
        Assert.Null(InitCommand.BracesEnforced(new Dictionary<string, string> { ["dotnet_diagnostic.IDE0011.severity"] = "warning", ["csharp_prefer_braces"] = "when_multiline" }));
        Assert.Null(InitCommand.BracesEnforced(new Dictionary<string, string> { ["dotnet_diagnostic.IDE0011.severity"] = "suggestion", ["csharp_prefer_braces"] = "true" }));
        Assert.Null(InitCommand.BracesEnforced(new Dictionary<string, string>()));

        // The template's braces with six braceless jumps of twelve: nothing allows them.
        var code = string.Concat(Enumerable.Range(0, 6).Select(i => $"    int A{i}(int x)\n    {{\n        if (x > 0) return 1;\n        if (x < 0)\n        {{\n            return 2;\n        }}\n\n        return 0;\n    }}\n\n"));
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("App/C.cs", $"class C\n{{\n{code}}}\n");
        var own = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["dotnet_diagnostic.IDE0011.severity"] = "warning", ["csharp_prefer_braces"] = "true:silent" };
        var (lines, report, _, _) = InitCommand.Detect(root, own, []);
        Assert.DoesNotContain(lines, l => l.StartsWith(Conventions.SingleLineJumpsKey, StringComparison.Ordinal));
        Assert.Contains(report, l => l.Contains("one-line jumps", StringComparison.Ordinal) && l.Contains("your .editorconfig enforces braces (IDE0011 = warning)", StringComparison.Ordinal));
        var allowed = InitCommand.Detect(root, new Dictionary<string, string>(), []).Lines;
        Assert.Contains($"{Conventions.SingleLineJumpsKey} = true", allowed);

        // An earlier init's 'true' goes too.
        Assert.DoesNotContain(InitCommand.Detect(root, own, [], allowed).Lines, l => l.StartsWith(Conventions.SingleLineJumpsKey, StringComparison.Ordinal));
    }

    [Fact]
    public void Init_WritesSCamelCase_ForPrefixedStatics()
    {
        // Serde: 19 of 22 private static readonly fields with 's_': BRO1307 went off instead of keeping the prefix.
        var counts = Conventions.NewCounts();
        counts[Conventions.StaticPrefixKey] = [3, 19];
        counts["private static readonly fields' prefix"] = [3, 19];
        counts[Conventions.StaticCasingKey] = [0, 10, 0];
        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.Contains("stylebro_private_static_field_naming = s_camelCase", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("stylebro_private_mutable_static_field_naming", StringComparison.Ordinal));
        Assert.DoesNotContain("dotnet_diagnostic.BRO1307.severity = none", lines);
        Assert.Single(lines, l => l.StartsWith("stylebro_private_static_field_naming", StringComparison.Ordinal));
        Assert.Contains(report, l => l.Contains("private static fields: 19 of 22 with an 's_' prefix -> s_camelCase", StringComparison.Ordinal));

        // Mutable ones too; a kind that is mixed keeps the old answer: BRO1307 off.
        counts[Conventions.StaticPrefixKey] = [3, 29];
        Assert.Contains("stylebro_private_mutable_static_field_naming = s_camelCase", Conventions.Decide(counts, new Dictionary<string, string>()).Lines);
        counts[Conventions.StaticPrefixKey] = [6, 30];
        counts["private static readonly fields' prefix"] = [0, 24];
        lines = Conventions.Decide(counts, new Dictionary<string, string>()).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1307.severity = none", lines);
        Assert.DoesNotContain(lines, l => l.Contains("s_camelCase", StringComparison.Ordinal) && !l.StartsWith('#'));

        // Counted per kind.
        counts = Conventions.NewCounts();
        Conventions.Count(CSharpSyntaxTree.ParseText("class C { static readonly int s_a = 1; static int s_b; static readonly int C1 = 2; }"), counts);
        Assert.Equal([1, 2], counts[Conventions.StaticPrefixKey]);
        Assert.Equal([1, 1], counts["private static readonly fields' prefix"]);
    }

    [Fact]
    public void Init_KeepsTheSCamelCaseLines_OnASecondRun()
    {
        var code = string.Concat(Enumerable.Range(0, 12).Select(i => $"    private static readonly int s_value{i} = {i};\n"));
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("App/C.cs", $"class C\n{{\n{code}}}\n");
        var first = InitCommand.Detect(root, new Dictionary<string, string>(), []).Lines;
        Assert.Contains("stylebro_private_static_field_naming = s_camelCase", first);

        // Counted again on other code, the kept lines stay as they were, once.
        Write("App/C.cs", "class C\n{\n    private static readonly int Value = 1;\n}\n");
        var second = InitCommand.Detect(root, new Dictionary<string, string>(), [], first).Lines;
        Assert.Equal(string.Join("|", first), string.Join("|", second));
        Assert.Single(second, l => l == "stylebro_private_static_field_naming = s_camelCase");
    }

    [Fact]
    public void ReadOrNull_SkipsALockedFile()
    {
        Write("a.log", "x");
        Write("b.txt", "y\r\n");
        using (new FileStream(Path.Combine(root, "a.log"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Null(Migration.ReadOrNull(Path.Combine(root, "a.log")));
            Assert.Equal("\r\n", PreviewCommand.FolderLineEnding(Path.Combine(root, "new.props")));
        }

        Assert.Equal("x", Migration.ReadOrNull(Path.Combine(root, "a.log")));
    }

    [Fact]
    public void TheReport_LeavesOutHeaderRules_WhenSa1633IsOff()
    {
        // Bencodex: SA1633 None, and SA1634-SA1641 were listed as not enforced ("StyleCop wanted 'PlaceholderCompany'").
        Write("App/App.csproj", StyleCopProject);
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.SA1633.severity = none\n");
        var output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root })));
        Assert.DoesNotContain("SA1634", output);
        Assert.DoesNotContain("SA1641", output);
        Assert.DoesNotContain("PlaceholderCompany", output);
        Assert.Contains("SA1636", Migration.CannotFire(StyleCopSetup.Read(root)));
    }

    [Fact]
    public void PackageHints_MentionTheRootProps_WhenEveryProjectReferencesStyleCop()
    {
        Write("A/A.csproj", StyleCopProject);
        Write("B/B.csproj", StyleCopProject);
        Assert.Contains(Program.PackageHints(StyleCopSetup.Read(root), root), l => l.Contains("StyleBro.Analyzers goes there (or once in a Directory.Build.props at the root, which every project imports)", StringComparison.Ordinal));

        Write("C/C.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Assert.DoesNotContain(Program.PackageHints(StyleCopSetup.Read(root), root), l => l.Contains("or once in", StringComparison.Ordinal));
    }

    [Fact]
    public void DocOrder_NamesTheElementsOutOfOrder()
    {
        var counts = Conventions.NewCounts();
        var comment = "/// <summary>S.</summary>\n/// <returns>R.</returns>\n/// <param name=\"x\">X.</param>\nint M(int x) => x;\n";
        Conventions.Count(CSharpSyntaxTree.ParseText($"class C\n{{\n{string.Concat(Enumerable.Repeat(comment, 1))}}}\n"), counts);
        Assert.Equal("doc element order (param, returns)", Conventions.DocOrderWhat(counts));
        Assert.Equal("doc element order", Conventions.DocOrderWhat(Conventions.NewCounts()));
    }

    [Fact]
    public void Migrate_StyleSharpsIsNull_IsBro1133WithPatternMatching()
    {
        // SST1149 is on by default: BRO1103 turned 'null == who' into 'who == null', which SST1149 reported.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleSharp.Analyzers\" Version=\"5.1.2\" /></ItemGroup></Project>");
        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1133.severity = warning", lines);
        Assert.Contains("stylebro_null_check_style = pattern_matching", lines);

        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.SST1149.severity = none\n");
        lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1133.severity = none", lines);
        Assert.DoesNotContain("stylebro_null_check_style = pattern_matching", lines);

        // Not listed as a rule that goes with StyleSharp.
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.SST1149.severity = warning\n");
        Assert.DoesNotContain("SST1149", Program.StyleSharpOnly(StyleCopSetup.Read(root)));
    }
}
