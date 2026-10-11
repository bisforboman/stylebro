using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the eighth trial (StabilityMatrix, Flow.Launcher, Orleans, Xbox Live tools) found in 'stylebro-migrate init'.</summary>
public sealed partial class MigrationTests
{
    private const string MemberOrderKey = "dotnet_diagnostic.BRO1001.severity";

    [Theory]
    [InlineData(".csharpierrc.yaml", "printWidth: 110\n", ".csharpierrc.yaml")]
    [InlineData(".csharpierrc", "{}\n", ".csharpierrc")]
    [InlineData(".config/dotnet-tools.json", "{ \"tools\": { \"csharpier\": { \"version\": \"1.0.1\" } } }\n", ".config/dotnet-tools.json")]
    [InlineData("src/App/App.csproj", "<Project><ItemGroup><PackageReference Include=\"CSharpier.MsBuild\" /></ItemGroup></Project>\n", "src/App/App.csproj")]
    [InlineData(".config/dotnet-tools.json", "{ \"tools\": { \"husky\": { \"version\": \"0.7.2\" } } }\n", null)]
    public void CSharpier_IsDetected(string file, string content, string? evidence)
    {
        // StabilityMatrix: .csharpierrc.yaml, the tool in .config/dotnet-tools.json and a Husky pre-commit hook.
        Write(file, content);

        Assert.Equal(evidence, Conventions.CSharpier(root));
    }

    [Fact]
    public void Init_WithCSharpier_TurnsItsLayoutRulesOff_AndKeepsTheRest()
    {
        // StabilityMatrix: after StyleBro's format, CSharpier's check rejected 161 files it had accepted.
        Write(".csharpierrc.yaml", "printWidth: 110\n");
        var mixed = string.Concat(Enumerable.Range(0, 6).Select(i => $"    bool M{i}(bool a, bool b) =>\n        a {(i % 2 == 0 ? "&&\n        b" : "\n        && b")};\n\n"));
        Write("A.cs", $"class A\n{{\n{mixed}    string S() => \"\";\n\n    string T() => \"\";\n\n    string U() => \"\";\n}}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))).Replace("\r\n", "\n");
        var config = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        Assert.Contains("  off      CSharpier (.csharpierrc.yaml) -> IDE0055 and 32 layout rules are off: BRO1105,", output, StringComparison.Ordinal);
        Assert.Contains("# init: CSharpier formats this repository (.csharpierrc.yaml):", config, StringComparison.Ordinal);
        Assert.All(Conventions.CSharpierRules, id => Assert.Contains($"dotnet_diagnostic.{id}.severity = none\n", config, StringComparison.Ordinal));
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = warning", config, StringComparison.Ordinal);
        Assert.DoesNotContain("# Formatting: spacing", config, StringComparison.Ordinal);
        Assert.True(FormatCommand.WhitespaceOff(root));

        // The operator convention is CSharpier's (no second BRO1520 line); what CSharpier doesn't decide is still detected.
        Assert.Contains("  set      operators where a line wraps: 6 places -> CSharpier formats it", output, StringComparison.Ordinal);
        Assert.Single(config.Split('\n'), l => l == "dotnet_diagnostic.BRO1520.severity = none");
        Assert.Contains("  kept     empty strings: 3 of 3 \"\" -> literal", output, StringComparison.Ordinal);
        Assert.DoesNotContain(Conventions.CSharpierRules, id => id is "BRO1001" or "BRO1514" or "BRO1519" or "BRO1525" or "BRO1303" or "BRO1601" or "BRO1616");
    }

    [Fact]
    public void CSharpier_DecidesLayoutConventions_NotTheOthers()
    {
        bool Decides(string key) => Conventions.IsCSharpiers(Conventions.All.Single(c => c.Key == key));

        Assert.True(Decides("dotnet_style_operator_placement_when_wrapping"));
        Assert.True(Decides("csharp_new_line_before_open_brace"));
        Assert.True(Decides("stylebro_split_list_first_item"));
        Assert.False(Decides("csharp_prefer_braces"));
        Assert.False(Decides("stylebro_null_check_style"));
        Assert.False(Decides(MemberOrderKey));
    }

    [Theory]
    [InlineData("class A\n{\n    public int X;\n    private int y;\n    public void M() { }\n    private void N() { }\n}\n", 1, 0)]
    [InlineData("class A\n{\n    private void N() { }\n    public void M() { }\n}\n", 0, 1)]
    [InlineData("class A\n{\n    public void M() { }\n    public void N() { }\n}\n", 0, 0)]
    [InlineData("class A\n{\n    public void M() { }\n    private void M(int x) { }\n    public void N() { }\n}\n", 1, 0)]
    public void Conventions_CountMemberOrder(string source, int sorted, int unsorted)
    {
        // Types whose members sort apart: in BRO1001's order or not. Overloads kept together count as sorted (the overloads
        // convention decides that); members that all sort alike say nothing.
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText(source), counts);

        Assert.Equal(new[] { sorted, unsorted }, counts[MemberOrderKey]);
    }

    [Fact]
    public void Conventions_CountMemberOrder_WithTheRepositorysOrder()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText("class A\n{\n    private void N() { }\n    public void M() { }\n}\n"), counts, settings: new Dictionary<string, string> { ["stylebro_member_access_order"] = "private, public" });

        Assert.Equal(new[] { 1, 0 }, counts[MemberOrderKey]);
    }

    [Theory]
    [InlineData(2, 30, "dotnet_diagnostic.BRO1001.severity = none")]
    [InlineData(12, 10, "dotnet_diagnostic.BRO1001.severity = none")]
    [InlineData(30, 2, null)]
    public void Conventions_DecideMemberOrder(int sorted, int unsorted, string? line)
    {
        // StabilityMatrix (370 sorted, 414 not) and Flow.Launcher (107, 171) never sorted: BRO1001 reordered 407 and 167 files.
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [MemberOrderKey] = new[] { sorted, unsorted } }, new Dictionary<string, string>()).Lines;

        Assert.Equal(line, lines.SingleOrDefault(l => !l.StartsWith('#')));
    }

    [Fact]
    public void Init_RunAgain_KeepsTheMemberOrderDecision()
    {
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    private void N() {{ }}\n\n    public void M() {{ }}\n}}\n");
        }

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = none", first, StringComparison.Ordinal);

        // After a format run that sorted them, the line stays.
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    public void M() {{ }}\n\n    private void N() {{ }}\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("types' members: 3 of 3 in BRO1001's order -> kept from the last init (none)", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("true:silent", true)]
    [InlineData("true:none", true)]
    [InlineData("true:warning", false)]
    [InlineData("true", false)]
    public void Init_ASilentKey_IsNotEnforced(string value, bool unenforced)
    {
        var own = new Dictionary<string, string> { ["csharp_prefer_braces"] = value };

        Assert.Equal(unenforced, InitCommand.Unenforced(own).Contains("csharp_prefer_braces"));
    }

    [Fact]
    public void Init_AKeyWhoseIdeRuleIsSilent_IsNotEnforced()
    {
        Assert.Contains("csharp_prefer_braces", InitCommand.Unenforced(new Dictionary<string, string> { ["csharp_prefer_braces"] = "true", ["dotnet_diagnostic.IDE0011.severity"] = "silent" }));
        Assert.Empty(InitCommand.Unenforced(new Dictionary<string, string> { ["csharp_prefer_braces"] = "true", ["dotnet_diagnostic.IDE0011.severity"] = "warning" }));
    }

    [Fact]
    public void Init_SilentBraces_FollowTheCode()
    {
        // Flow.Launcher: csharp_prefer_braces = true:silent, and BRO1514 added braces in 140 files.
        var bodies = string.Concat(Enumerable.Range(0, 4).Select(i => $"        if (x == {i})\n            return {i};\n"));
        Write("A.cs", $"class A\n{{\n    int M(int x)\n    {{\n{bodies}        return 0;\n    }}\n}}\n");
        Write(".editorconfig", "root = true\n[*.cs]\ncsharp_prefer_braces = true:silent\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        Assert.Contains("  note     csharp_prefer_braces = true:silent in .editorconfig isn't enforced", output, StringComparison.Ordinal);
        Assert.Contains("(your .editorconfig's csharp_prefer_braces = true:silent isn't enforced)\ncsharp_prefer_braces = when_multiline\n", output, StringComparison.Ordinal);

        // The other way round: the code's form is StyleBro's default, but the silent key says otherwise, so it's written.
        Write("A.cs", $"class A\n{{\n    int M(int x)\n    {{\n{bodies.Replace("            return", "        {\n            return").Replace(";\n", ";\n        }\n")}        return 0;\n    }}\n}}\n");
        Write(".editorconfig", "root = true\n[*.cs]\ncsharp_prefer_braces = when_multiline:silent\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.Contains("\ncsharp_prefer_braces = true\n", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("0u", 0, 1)]
    [InlineData("1UL", 1, 0)]
    [InlineData("1Lu", 0, 1)]
    [InlineData("0xFFu", 0, 1)]
    [InlineData("1L", 0, 0)]
    [InlineData("1.5", 0, 0)]
    public void Conventions_CountUnsignedSuffixes(string literal, int upper, int lower)
    {
        // Orleans: 114 '0u' became '0U'. 'L' alone isn't counted: nobody writes '1l'.
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText($"class A {{ object x = {literal}; }}"), counts);

        Assert.Equal(new[] { upper, lower }, counts["dotnet_diagnostic.BRO1135.severity"]);
    }

    [Theory]
    [InlineData(2, 30, true)]
    [InlineData(423, 185, true)]
    [InlineData(30, 2, false)]
    public void Conventions_DecideSuffixes(int upper, int lower, bool off)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { ["dotnet_diagnostic.BRO1135.severity"] = new[] { upper, lower } }, new Dictionary<string, string>()).Lines;

        Assert.Equal(off, lines.Contains("dotnet_diagnostic.BRO1135.severity = none"));
    }

    [Fact]
    public void MultiTargetedProjects_AreEvaluated()
    {
        // Orleans: '<TargetFrameworks>$(DefaultTargetFrameworks)</TargetFrameworks>'; the text search found 9 of ~130.
        Write("Directory.Build.props", "<Project><PropertyGroup><DefaultTargetFrameworks>net8.0;net10.0</DefaultTargetFrameworks></PropertyGroup></Project>\n");
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>$(DefaultTargetFrameworks)</TargetFrameworks></PropertyGroup></Project>\n");

        Assert.Equal(new[] { "App.csproj" }, InitCommand.MultiTargetedProjects(root));
        Assert.Empty(InitCommand.MultiTargetedFiles(root));
    }
}
