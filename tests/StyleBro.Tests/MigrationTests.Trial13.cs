using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the thirteenth trial for the tool (DynamicData, Spectre.Console, eShop).</summary>
public sealed partial class MigrationTests
{
    private const string ConditionalPackages = """
        <Project>
          <ItemGroup Condition="!$(IsTestProject)">
            <GlobalPackageReference Include="Roslynator.Analyzers" Version="4.15.0" />
            <GlobalPackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void AConditionalStyleCopReference_IsSettledByMSBuild()
    {
        // DynamicData: StyleCop only where '!$(IsTestProject) AND !$(IsBenchmarkProject)'. The text counted its tests, and
        // --write changed 360 test and benchmark files.
        Write("src/Directory.Packages.props", ConditionalPackages);
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/Lib/A.cs", "class A { }\n");

        var setup = StyleCopSetup.Read(root, project => !project.Contains("Tests", StringComparison.Ordinal));
        Assert.Equal(["src/Lib"], setup.Folders);
        Assert.Equal(["src/Lib.Tests"], setup.FoldersWithout);
        Assert.False(setup.NotInstalled);
        Assert.Empty(setup.Warnings);

        // MSBuild failing: the text's answer (every project), and the report says so.
        setup = StyleCopSetup.Read(root, _ => null);
        Assert.Null(setup.Folders);
        Assert.Contains(setup.Warnings, w => w.Contains("MSBuild couldn't evaluate src/Lib.Tests/Lib.Tests.csproj, src/Lib/Lib.csproj", StringComparison.Ordinal));

        // An unconditional reference: MSBuild still decides, with several projects (Nerdbank.Streams, AnUnquotedGetPathOfFileAbove_IsFollowed).
        Write("src/Directory.Packages.props", "<Project><ItemGroup><GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        Assert.Null(StyleCopSetup.Read(root, _ => true).Folders);
    }

    [Fact]
    public void EvaluateStyleCop_AppliesTheConditions()
    {
        Write("Directory.Build.props", "<Project><PropertyGroup><IsTestProject>$(MSBuildProjectName.Contains('Tests'))</IsTestProject></PropertyGroup></Project>");
        Write("Directory.Packages.props", ConditionalPackages);
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        Write("Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        Write("Broken/Broken.csproj", "<Project><Import Project=\"missing.props\" /></Project>");

        Assert.True(StyleCopSetup.EvaluateStyleCop(Path.Combine(root, "Lib", "Lib.csproj")));
        Assert.False(StyleCopSetup.EvaluateStyleCop(Path.Combine(root, "Lib.Tests", "Lib.Tests.csproj")));
        Assert.Null(StyleCopSetup.EvaluateStyleCop(Path.Combine(root, "Broken", "Broken.csproj")));
        Assert.True(StyleCopSetup.IsStyleCopItem("StyleCop.Analyzers"));
        Assert.True(StyleCopSetup.IsStyleCopItem(@"..\packages\StyleCop.Analyzers.1.1.118\analyzers\dotnet\cs\StyleCop.Analyzers.dll"));
        Assert.False(StyleCopSetup.IsStyleCopItem("StyleCop.Analyzers.Extra"));
    }

    [Theory]
    [InlineData("<Project><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" /></ItemGroup></Project>", false)]
    [InlineData("<Project><ItemGroup Condition=\"'$(X)' == ''\"><PackageReference Include=\"StyleCop.Analyzers\" /></ItemGroup></Project>", true)]
    [InlineData("<Project><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Condition=\"'$(X)' == ''\" /></ItemGroup></Project>", true)]
    [InlineData("<Project><Choose><When Condition=\"true\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" /></ItemGroup></When></Choose></Project>", true)]
    [InlineData("<Project><ItemGroup Condition=\"x\"><PackageReference Include=\"Other\" /></ItemGroup><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" /></ItemGroup></Project>", false)]
    [InlineData("<Project><ItemGroup Condition=\"x\"><PackageReference Include=\"Other\" /></ItemGroup></Project>", false)]
    [InlineData("<Project><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" ></Project>", true)]
    public void ConditionalReference_LooksAtTheItemAndItsParents(string text, bool expected) =>
        Assert.Equal(expected, StyleCopSetup.ConditionalReference(text));

    [Fact]
    public void ThePackageSwap_KeepsStyleCopsCondition()
    {
        // DynamicData: the preview put StyleBro into a new, unconditional ItemGroup, so the tests got it.
        var result = PreviewCommand.AddNextToStyleCop(ConditionalPackages, "<GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" />");
        Assert.Contains("    <GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" />\n    <GlobalPackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" />\n  </ItemGroup>", result, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(result, "<ItemGroup"));

        // A Condition on the item itself goes along; a reference in a comment isn't the one.
        var itemCondition = "<Project>\n  <!-- <PackageReference Include=\"StyleCop.Analyzers\" /> -->\n  <ItemGroup>\n    <PackageReference Include=\"StyleCop.Analyzers\" Condition=\"'$(X)' != 'true'\">\n      <PrivateAssets>all</PrivateAssets>\n    </PackageReference>\n  </ItemGroup>\n</Project>\n";
        result = PreviewCommand.AddNextToStyleCop(itemCondition, "<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />");
        Assert.Contains("    </PackageReference>\n    <PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" Condition=\"'$(X)' != 'true'\" />\n  </ItemGroup>", result, StringComparison.Ordinal);

        // Unconditional: an ItemGroup of its own, as before.
        var plain = "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleCop.Analyzers\" />\n  </ItemGroup>\n</Project>\n";
        Assert.Equal(PreviewCommand.AddItem(plain, "<X />"), PreviewCommand.AddNextToStyleCop(plain, "<X />"));
    }

    [Fact]
    public void ARootTrueFolderWithoutStyleCop_GetsNoBlock()
    {
        // DynamicData's benchmarks: their own 'root = true' .editorconfig, no StyleCop, and the whole block anyway.
        Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 4\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        Write("src/Lib/A.cs", "class A { }\n");
        Write("src/Bench/Bench.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/Bench/.editorconfig", "root = true\n[*.cs]\nindent_size = 4\n");
        Write("src/Bench/B.cs", "class B { }\n");

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);
        var plan = Migration.Plan(setup, root, result);

        Assert.Equal([".editorconfig"], plan.Keys);
        Assert.Contains("src/Bench/.editorconfig says root = true, and StyleCop didn't run there: no settings there.", result.Notes);
    }

    [Fact]
    public void StyleCopSettingsWithoutThePackage_AreLeftovers()
    {
        // Spectre.Console dropped StyleCop and kept 'dotnet_diagnostic.sa1101.severity = none' lines: init refused, and the
        // migration counted every rule they don't mention as on (BRO1008 moved usings into namespaces in 9 files).
        Write("src/.editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.sa1101.severity = none\n");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/App/A.cs", "namespace N;\n\nclass A { }\n");

        Assert.True(StyleCopSetup.Read(root, _ => false).NotInstalled);
        Assert.False(StyleCopSetup.IsInstalled(root, _ => false));
        Assert.True(StyleCopSetup.IsInstalled(root, _ => true)); // a reference only MSBuild sees
        var setup = StyleCopSetup.Read(root, _ => false);
        Assert.False(setup.For(Assert.Single(setup.Scopes)).IsOn("SA1101")); // lower-case ids count

        var output = Capture(() => Assert.Equal(1, Program.Migrate([root, "--write"])));
        Assert.Contains("StyleCop.Analyzers (or StyleSharp.Analyzers) isn't referenced by any project, so StyleCop doesn't run here: its settings are leftovers (src", output, StringComparison.Ordinal);
        Assert.Contains("Nothing to migrate, nothing written", output, StringComparison.Ordinal);
        Assert.Contains("Run 'bro init' instead", output, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));

        output = Capture(() => Assert.Equal(0, InitCommand.Run([root])));
        Assert.Contains("its settings are leftovers", output, StringComparison.Ordinal);
        Assert.Contains("init ignores them and follows the code's own conventions", output, StringComparison.Ordinal);

        // After the package swap the reference is gone on purpose: not leftovers.
        Write(".editorconfig", $"root = true\n{Migration.BeginMarker}\n[*.cs]\n{Migration.VersionPrefix}1.1.118 (x)\n{Migration.EndMarker}\n");
        Assert.True(StyleCopSetup.IsInstalled(root, _ => false));
        Assert.False(StyleCopSetup.Read(root, _ => false).NotInstalled);
    }

    [Fact]
    public void LowerCaseStyleCopIds_AreReadEverywhere()
    {
        Write("a.ruleset", "<RuleSet Name=\"x\" ToolsVersion=\"10.0\"><Rules AnalyzerId=\"StyleCop.Analyzers\" RuleNamespace=\"StyleCop.Analyzers\"><Rule Id=\"sa1633\" Action=\"None\" /></Rules></RuleSet>");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><CodeAnalysisRuleSet>../a.ruleset</CodeAnalysisRuleSet></PropertyGroup><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");

        Assert.False(StyleCopSetup.Read(root).IsOn("SA1633"));
        Assert.Contains("a.ruleset (StyleCop rules)", StyleCopSetup.Evidence(root));

        var replacements = new Dictionary<string, SortedSet<string>> { ["SA1623"] = new(StringComparer.Ordinal) { "BRO1604" } };
        Write("App/B.cs", "#pragma warning disable sa1623\nclass B { }\n");
        Write("App/App2.csproj", "<Project><PropertyGroup><NoWarn>sa1623</NoWarn></PropertyGroup></Project>");
        Assert.Equal((2, 2, 0, 0), Program.RewriteSuppressions(root, replacements, write: true));
        Assert.Contains("#pragma warning disable sa1623, BRO1604", File.ReadAllText(Path.Combine(root, "App", "B.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Suppressions_SkipGeneratedCode_AndRulesFormattingIgnores()
    {
        // Spectre.Console: its checked-in source generator output got BRO pragmas, and SA1028 pragmas got IDE0055, which
        // doesn't stop the whitespace pass.
        var replacements = new Dictionary<string, SortedSet<string>>
        {
            ["SA1028"] = new(StringComparer.Ordinal) { "IDE0055" },
            ["SA1623"] = new(StringComparer.Ordinal) { "BRO1604" },
            ["SA1310"] = new(StringComparer.Ordinal) { "BRO1308" },
        };
        const string Pragma = "#pragma warning disable SA1623\nclass B { }\n";
        Write("src/App/Generated/Gen.Lock/Gen.Lock.Generator/Lock.cs", Pragma);
        Write("src/App/C.g.cs", Pragma);
        Write("src/App/Mine.cs", Pragma);
        Write("src/App/Spaces.cs", "#pragma warning disable SA1028\nclass S { }\n");
        Write("src/App/Native.cs", "using System.Diagnostics.CodeAnalysis;\nclass N\n{\n    [SuppressMessage(\"StyleCop.CSharp.NamingRules\", \"SA1310:Field names should not contain underscore\")]\n    private const int STD_OUTPUT_HANDLE = -11;\n}\n");

        Assert.Equal((2, 2, 1, 0), Program.RewriteSuppressions(root, replacements, write: true));
        Assert.Equal(Pragma, File.ReadAllText(Path.Combine(root, "src/App/Generated/Gen.Lock/Gen.Lock.Generator/Lock.cs")));
        Assert.Equal(Pragma, File.ReadAllText(Path.Combine(root, "src/App/C.g.cs")));
        Assert.Contains("SA1623, BRO1604", File.ReadAllText(Path.Combine(root, "src/App/Mine.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("IDE0055", File.ReadAllText(Path.Combine(root, "src/App/Spaces.cs")), StringComparison.Ordinal);

        // BRO1308 is the one rule a const like STD_OUTPUT_HANDLE gets (FieldNames.GetRename: underscore before casing).
        Assert.Contains("[SuppressMessage(\"StyleBro\", \"BRO1308\")]", File.ReadAllText(Path.Combine(root, "src/App/Native.cs")), StringComparison.Ordinal);

        Assert.True(Migration.IsGeneratedOrVendored("src/App/Generated/Gen.Lock/Gen.Lock.Generator/Lock.cs", string.Empty));
        Assert.False(Migration.IsGeneratedOrVendored("src/App/Generated/Models/User.cs", string.Empty));
        Assert.False(Migration.IsGeneratedOrVendored("src/App/App.Core/X.cs", string.Empty));
        Assert.False(Migration.IsGeneratedOrVendored("src/App/Generated/Gen/Other/X.cs", string.Empty));
    }

    [Fact]
    public void TheMigrationReport_SaysWhichFilesFormatConverts()
    {
        // DynamicData: 794 of 847 LF with 'end_of_line = crlf'; the note said 'format' left endings alone, and it turned the
        // 53 CRLF files into LF.
        var counts = Conventions.NewCounts();
        counts[Conventions.EndOfLineKey] = [794, 53];
        var note = Conventions.ContradictedEnding(counts, new Dictionary<string, string> { ["end_of_line"] = "crlf" })!.Value.Note;
        Assert.Contains("end_of_line = lf is written for C# files and 'format' converts the other 53 (CRLF) to LF where it formats them", note, StringComparison.Ordinal);

        counts[Conventions.EndOfLineKey] = [12, 0];
        Assert.Contains("'format' leaves the endings alone", Conventions.ContradictedEnding(counts, new Dictionary<string, string> { ["end_of_line"] = "crlf" })!.Value.Note, StringComparison.Ordinal);
    }

    [Fact]
    public void TheFieldStyle_FollowsSA1309WhereTheCodeIs()
    {
        // Spectre.Console: SA1309 on at the root (StyleCop's default), none in src/.editorconfig, all the code under src/.
        Write("src/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1309.severity = none\n");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        Write("src/App/A.cs", "class A\n{\n    private int _a;\n    private int _b;\n    private int c;\n}\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Equal("_camelCase", result.FieldStyle);
        Assert.Contains(result.Notes, n => n.Contains("SA1309 is off in src/, where all the code is, so BRO1303 uses '_camelCase'", StringComparison.Ordinal));

        // Code outside it too: the root's value decides, and the note names the other.
        Write("tools/T.cs", "class T { }\n");
        Write("tools/T.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Equal("camelCase", result.FieldStyle);
        Assert.Contains(result.Notes, n => n.Contains("SA1309 is on (off in src/), so BRO1303 uses 'camelCase'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("net4.7", "net47")]
    [InlineData("net4.7.2", "net472")]
    [InlineData("NET4.8", "net48")]
    [InlineData("net47", "net47")]
    [InlineData("net6", "net6.0")]
    public void DottedFrameworkSpellings_ShareARun(string framework, string expected) =>
        Assert.Equal(expected, FormatCommand.NormalizeFramework(framework));

    [Theory]
    [InlineData(1, "summary", "1 summary")]
    [InlineData(2, "summary", "2 summaries")]
    [InlineData(2, "day", "2 days")]
    [InlineData(2, "class", "2 classes")]
    [InlineData(3, "file", "3 files")]
    public void Plural_FollowsEnglish(int count, string noun, string expected) =>
        Assert.Equal(expected, Conventions.Plural(count, noun));

    [Fact]
    public void SeveralWorkspaces_TheMessageNamesEachChoice()
    {
        // eShop: eShop.slnx next to one eShop.Web.slnf.
        Write("eShop.slnx", "<Solution />");
        Write("eShop.Web.slnf", "{ \"solution\": { \"path\": \"eShop.slnx\", \"projects\": [] } }");

        var message = BaselineCommand.NoWorkspace(root, "'bro format <file>'");
        Assert.Contains("eShop.slnx, eShop.Web.slnf", message, StringComparison.Ordinal);
        Assert.Contains("'bro format eShop.slnx' or 'bro format eShop.Web.slnf' (--project <file> works too)", message, StringComparison.Ordinal);
        Assert.DoesNotContain("works too", BaselineCommand.NoWorkspace(root, "--project <file>"), StringComparison.Ordinal);
    }
}
