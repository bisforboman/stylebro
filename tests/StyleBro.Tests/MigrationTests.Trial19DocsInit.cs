using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the 19th trial found in init and the migration: enforced 'this.', Sonar option rows in NoWarn, BRO1506.</summary>
public sealed partial class MigrationTests
{
    private const string CommentBlankLineKey = "dotnet_diagnostic.BRO1506.severity";

    // RecyclableMemoryStream requires 'this.' (dotnet_style_qualification_for_* = true:warning); init's IDE0009 = none turned it off.
    [Theory]
    [InlineData("dotnet_style_qualification_for_field = true:warning\n", true)]
    [InlineData("dotnet_style_qualification_for_method = true:error\n", true)]
    [InlineData("dotnet_style_qualification_for_field = true:suggestion\n", true)]
    [InlineData("dotnet_style_qualification_for_field = true\n", false)]
    [InlineData("dotnet_style_qualification_for_field = true:silent\n", false)]
    [InlineData("dotnet_style_qualification_for_field = false:warning\n", false)]
    [InlineData("dotnet_diagnostic.IDE0009.severity = warning\n", true)]
    [InlineData("dotnet_style_qualification_for_field = true:warning\ndotnet_diagnostic.IDE0009.severity = none\n", false)]
    public void Init_KeepsAnEnforcedThisQualification(string settings, bool kept)
    {
        Write(".editorconfig", "root = true\n[*.cs]\n" + settings);
        Write("A.cs", "class A\n{\n}\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        // The template's IDE0009 = none comes with this comment.
        Assert.Equal(!kept, File.ReadAllText(Path.Combine(root, ".editorconfig")).Contains("'this.' qualification isn't required", StringComparison.Ordinal));
    }

    // bcrypt.net: S4136 in src/Directory.Build.props' NoWarn became a suppression of all of BRO1001 there. It's an option row
    // (same_access): suppressed, the option goes back to StyleBro's own value there and BRO1001 stays on.
    [Fact]
    public void Init_SonarOptionRuleInNoWarn_ResetsTheOptionThere_AndKeepsTheRule()
    {
        Write("Directory.Build.props", "<Project><ItemGroup><PackageReference Include=\"SonarAnalyzer.CSharp\" Version=\"10.35.0.4138\" /></ItemGroup></Project>");
        Write("src/Directory.Build.props", "<Project><PropertyGroup><NoWarn>NU5105;S4136;S2971</NoWarn></PropertyGroup></Project>");
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\n");
        Write("src/A.cs", "class A\n{\n}\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("<NoWarn>NU5105;S4136;S2971;BRO1150</NoWarn>", File.ReadAllText(Path.Combine(root, "src", "Directory.Build.props")), StringComparison.Ordinal);
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("stylebro_keep_overloads_together = same_access", editorConfig, StringComparison.Ordinal);
        Assert.Contains("[src/**.cs]\n# S4136 in src/Directory.Build.props's <NoWarn>: StyleBro's own value there, the rule stays on\nstylebro_keep_overloads_together = false\n", editorConfig, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_SonarOptionRuleInNoWarn_ResetsTheOptionThere()
    {
        Write("Directory.Build.props", SonarPackage);
        Write("tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><NoWarn>$(NoWarn);S4136</NoWarn></PropertyGroup></Project>");
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\n");

        var output = Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));

        Assert.DoesNotContain("BRO1001", File.ReadAllText(Path.Combine(root, "tests", "Tests.csproj")), StringComparison.Ordinal);
        Assert.Contains("[tests/**.cs]\n# S4136 in tests/Tests.csproj's <NoWarn>", File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Empty(Suppressions.OptionDefaults.Keys.Except(SonarSetup.Mapping.Where(m => m.OptionOnly).SelectMany(m => m.Setting!.Split("; ")).Select(s => s.Split('=')[0].Trim())));
        Assert.Empty(SonarSetup.Mapping.Where(m => m.OptionOnly).SelectMany(m => m.Setting!.Split("; ")).Select(s => s.Split('=')[0].Trim()).Except(Suppressions.OptionDefaults.Keys));
        Assert.NotNull(output);
    }

    // RecyclableMemoryStream's tests keep a blank line below commented-out tests: BRO1506 removed it. Only the last line of a
    // comment group counts, not the file header, a trailing comment or one below code (BRO1506 leaves those alone).
    [Fact]
    public void Conventions_CountCommentsWithAndWithoutABlankLineBelow()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            // Header

            class A
            {
                // [Test]
                // public void Old() { }

                public void M()
                {
                    // right above
                    M();
                    M(); // trailing

                    // blank below

                    M();
                    M();
                    // below code, with a blank line

                    M();
                }
            }
            """),
            counts);

        // "blank below" stands on its own (a blank line above it too): BRO1506 leaves it by default, so it isn't counted.
        Assert.Equal(new[] { 1, 1 }, counts[CommentBlankLineKey]);
    }

    [Theory]
    [InlineData(3, 0, null)]
    [InlineData(0, 3, "dotnet_diagnostic.BRO1506.severity = none")]
    [InlineData(8, 4, "dotnet_diagnostic.BRO1506.severity = none")]
    [InlineData(10, 2, null)]
    public void Conventions_CommentBlankLines_TurnBRO1506OffUnlessTheCodeHasNone(int without, int with, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [CommentBlankLineKey] = new[] { without, with } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }
}
