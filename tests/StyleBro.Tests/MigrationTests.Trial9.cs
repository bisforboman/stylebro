using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the ninth trial (tfplan2md, kubernetes-client, metadata-extractor) found in the migration and init.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void CategorySeverity_DoesNotTurnOnRulesThatAreOffByDefault()
    {
        // tfplan2md: 'category-StyleCop.CSharp.NamingRules.severity = warning' never ran SA1305 in the build, yet the migration
        // turned BRO1310 on, which renamed 'spCount' to 'count' in 6 StyleCop-clean files.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_analyzer_diagnostic.category-StyleCop.CSharp.NamingRules.severity = error\n");

        var setup = StyleCopSetup.Read(root);

        Assert.Equal(Severity.Error, setup.Severities["SA1300"]);  // on by default: the category applies
        Assert.False(setup.IsOn("SA1305"));
        Assert.Contains("dotnet_diagnostic.BRO1310.severity = none", Migration.Generate(setup, root).Lines);

        // A rule's own entry still turns it on.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_analyzer_diagnostic.category-StyleCop.CSharp.NamingRules.severity = error\ndotnet_diagnostic.SA1305.severity = warning\n");
        Assert.Equal(Severity.Warning, StyleCopSetup.Read(root).Severities["SA1305"]);
    }

    [Fact]
    public void CategorySeverity_InAGlobalConfig_DoesNotTurnOnRulesThatAreOffByDefault()
    {
        Write("eng/Style.globalconfig", "is_global = true\ndotnet_analyzer_diagnostic.category-StyleCop.CSharp.MaintainabilityRules.severity = warning\n");

        var setup = StyleCopSetup.Read(root);

        Assert.False(setup.IsOn("SA1412"));  // off by default: no 'charset = utf-8-bom'
        Assert.DoesNotContain(Migration.Generate(setup, root).Lines, l => l.StartsWith("charset", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("i/lf    w/lf    attr/text=auto          \ta.cs\0", 1)]
    [InlineData("i/lf    w/crlf  attr/text=auto          \ta.cs\0", 1)]
    [InlineData("i/lf    w/crlf  attr/                   \ta.cs\0", 1)]
    [InlineData("i/lf    w/lf    attr/text eol=lf        \ta.cs\0", 0)]
    [InlineData("i/lf    w/lf    attr/                   \ta.cs\0", 0)]
    [InlineData("i/crlf  w/crlf  attr/text=auto          \ta.cs\0", 0)]
    [InlineData("i/lf    w/lf    attr/-text              \ta.cs\0", 0)]
    public void PickedPerMachine_CountsFilesGitGivesEachMachinesEnding(string output, int count)
    {
        // 'text' without 'eol' gives LF on Linux and CRLF on Windows, also where this machine's checkout has LF.
        Assert.Equal(count, Conventions.PickedPerMachine(output));
    }

    [Fact]
    public void EndOfLine_ThatGitsCheckoutContradicts_IsUnset_AndIde0055IsASuggestion()
    {
        // kubernetes-client: '[*] end_of_line = lf' and '*.cs text=auto'. On Windows, IDE0055 at error (from SA1500) failed
        // the build 266 times, and 'format' rewrote 205 files to LF.
        Write(".editorconfig", "root = true\n[*]\nend_of_line = lf\n");
        Write(".gitattributes", "*.cs text=auto\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        Git("init", "-q");
        Git("add", ".");
        Write(".editorconfig", "root = true\n[*]\nend_of_line = lf\n[*.cs]\ndotnet_diagnostic.SA1500.severity = error\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("end_of_line = unset", result.Lines);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = suggestion", result.Lines);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = error", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("Line endings: your .editorconfig says end_of_line = lf, but git checks out 3 C# files with each machine's ending", StringComparison.Ordinal)
            && n.Contains("'*.cs text eol=lf' in .gitattributes", StringComparison.Ordinal));

        // init writes the same.
        Write(".editorconfig", "root = true\n[*]\nend_of_line = lf\n");
        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("\nend_of_line = unset\n", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.IDE0055.severity = suggestion\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = warning", output, StringComparison.Ordinal);
        Assert.Contains("  note     Line endings: your .editorconfig says end_of_line = lf", output, StringComparison.Ordinal);

        // A fixed ending in .gitattributes settles it.
        Write(".gitattributes", "*.cs text eol=lf\n");
        Assert.DoesNotContain("end_of_line = unset", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void StyleCopVersion_IsRecorded_AndReadBackAfterThePackageSwap()
    {
        // kubernetes-client: a second --write after the swap couldn't find StyleCop 1.1.118 any more, and turned on BRO1123,
        // BRO1124 and BRO1311 (they replace rules 1.1.118 doesn't have). It also said "add StyleBro.Analyzers" again.
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.1.118" /></ItemGroup></Project>""");
        Write("A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("A.cs", "class A\n{\n}\n");
        var first = Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));
        Assert.Contains(Program.NextStep, first, StringComparison.Ordinal);
        Assert.Contains($"{Migration.VersionPrefix}1.1.118 ", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);

        Write("Directory.Build.props", """<Project><PropertyGroup><StyleBroPreset>none</StyleBroPreset></PropertyGroup><ItemGroup><PackageReference Include="StyleBro.Analyzers" Version="1.0.0" /></ItemGroup></Project>""");
        var setup = StyleCopSetup.Read(root);
        Assert.Equal("1.1.118", setup.Version);
        Assert.True(setup.VersionRecorded);
        Assert.False(setup.IsOn("SA1141"));

        var second = Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));
        var config = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.BRO1123.severity = none", config, StringComparison.Ordinal);
        Assert.Contains($"{Migration.VersionPrefix}1.1.118 ", config, StringComparison.Ordinal);
        Assert.Contains("StyleCop.Analyzers version: 1.1.118 (recorded by the last --write; the package is gone)", second, StringComparison.Ordinal);
        Assert.Contains(Program.FormatHint, second, StringComparison.Ordinal);
        Assert.DoesNotContain(Program.NextStep, second, StringComparison.Ordinal);
    }

    [Fact]
    public void NextStep_AfterWrite_FollowsThePackageSwap()
    {
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.1.118" /><PackageReference Include="StyleBro.Analyzers" Version="1.0.0" /></ItemGroup></Project>""");
        Write("A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        Assert.Equal(Program.RemoveStyleCopStep, Program.NextAfterWrite(StyleCopSetup.Read(root), root));

        Write("Directory.Build.props", "<Project />");
        Assert.Equal(Program.AddPackageHint, Program.NextAfterWrite(StyleCopSetup.Read(root), root));
    }

    [Theory]
    [InlineData("9.16.0.82469", false, false)]
    [InlineData("9.32.0.97167", false, true)]
    [InlineData("10.30.0.144632", true, true)]
    [InlineData("10.35.0.4138", true, true)]
    public void SonarDefaults_FollowTheReferencedVersion(string version, bool s8969, bool s2325)
    {
        // tfplan2md has SonarAnalyzer 9.16, which has no S8969 (it came in 10.30): BRO1147 removed '!' in 26 files.
        // S2325 joined "Sonar way" in 9.32.
        Write("src/Directory.Build.props", $"""<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="{version}" /></ItemGroup></Project>""");

        var sonar = SonarSetup.Read(root)!;

        Assert.Equal(version, sonar.Version);
        Assert.Equal(s8969, sonar.IsOn("S8969"));
        Assert.Equal(s2325, sonar.IsOn("S2325"));
        Assert.True(sonar.IsOn("S1116"));
        var lines = Migration.Generate(StyleCopSetup.Read(root), root, sonar: sonar).Lines;
        Assert.Equal(s8969, lines.Contains("dotnet_diagnostic.BRO1147.severity = warning"));   // S8969 only where the version has it
        Assert.Equal(s8969, lines.Contains("stylebro_allow_null_forgiving_on_unannotated = true"));
    }

    [Fact]
    public void SonarRule_SetExplicitly_CountsOnlyWhereTheVersionHasIt()
    {
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="9.16.0.82469" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.S8969.severity = warning\ndotnet_diagnostic.S2325.severity = warning\n");

        var sonar = SonarSetup.Read(root)!;

        Assert.False(sonar.IsOn("S8969"));
        Assert.True(sonar.IsOn("S2325"));
    }

    [Theory]
    [InlineData("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="SonarAnalyzer.CSharp"><Version>9.16.0.82469</Version></PackageVersion></ItemGroup></Project>""")]
    [InlineData("App/packages.config", """<?xml version="1.0" encoding="utf-8"?><packages><package id="SonarAnalyzer.CSharp" version="9.16.0.82469" targetFramework="net48" developmentDependency="true" /></packages>""")]
    [InlineData("App/App.csproj", """<Project ToolsVersion="15.0"><ItemGroup><Analyzer Include="..\packages\SonarAnalyzer.CSharp.9.16.0.82469\analyzers\SonarAnalyzer.CSharp.dll" /></ItemGroup></Project>""")]
    public void SonarVersion_IsReadLikeStyleCops(string file, string content)
    {
        Write(file, content);

        var sonar = SonarSetup.Read(root);

        Assert.Equal("9.16.0.82469", sonar?.Version);
        Assert.False(sonar!.IsOn("S8969"));
    }

    [Theory]
    [InlineData("class C\n{\n    void M()\n    {\n        F(1,\n            \"a\", \"b\");\n    }\n}\n", 0, 1)]
    [InlineData("class C\n{\n    void M()\n    {\n        F(\n            1,\n            \"a\");\n    }\n}\n", 1, 0)]
    [InlineData("class C\n{\n    void M()\n    {\n        F(\n            1, \"a\");\n    }\n}\n", 0, 0)]
    [InlineData("class C\n{\n    void M(int a,\n        int b)\n    {\n    }\n}\n", 1, 0)]
    public void Conventions_CountItemsPerLine(string source, int onePerLine, int grouped)
    {
        // metadata-extractor groups arguments ('GetIndexedDescription(TagX, 1,' then '"a", "b")'); BRO1108 split them in 25
        // files. Lists whose items all start on one line say nothing.
        var counts = Conventions.NewCounts();

        Conventions.Count(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source), counts);

        Assert.Equal(new[] { onePerLine, grouped }, counts["dotnet_diagnostic.BRO1108.severity"]);
    }

    [Fact]
    public void Init_GroupedItems_TurnBro1108Off()
    {
        var calls = string.Concat(Enumerable.Range(0, 4).Select(i => $"        F({i},\n            \"a\", \"b\");\n"));
        Write("A.cs", $"class A\n{{\n    void M()\n    {{\n{calls}    }}\n\n    void F(int i, string a, string b)\n    {{\n    }}\n}}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        Assert.Contains("  off      split lists' items: 4 of 4 grouped on lines -> BRO1108 is off", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.BRO1108.severity = none\n", output, StringComparison.Ordinal);
    }
}
