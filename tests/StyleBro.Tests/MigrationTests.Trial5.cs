using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What a trial of 0.5.0-alpha.1 on NCronJob, NSubstitute and linkdotnet/Blog found in 'stylebro-migrate init'.</summary>
public sealed partial class MigrationTests
{
    private const string OperatorKey = "dotnet_style_operator_placement_when_wrapping";

    [Fact]
    public void Init_TurnsTheRuleOff_WhenTheCodeContradictsTheRepositorysKey_LikeNCronJob()
    {
        // NCronJob: '.editorconfig' says end_of_line, 85 of 87 operators start the line, and BRO1520 moved 82 lines.
        Write(".editorconfig", $"root = true\n[*.cs]\n{OperatorKey} = end_of_line\n");
        Write("A.cs", "class A\n{\n    bool M(bool a) => a\n        || a\n        || a\n        || a;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains($"{OperatorKey} = end_of_line\n", editorConfig);
        Assert.Contains($"# init: operators where a line wraps: your .editorconfig says {OperatorKey} = end_of_line, but 3 of 3 places in your code are at the start of the line; nothing enforced it, so BRO1520 is off. To enforce it: change the code, then remove the next line.\ndotnet_diagnostic.BRO1520.severity = none", editorConfig);
        Assert.Contains("  off      operators where a line wraps: 3 of 3 at the start of the line -> .editorconfig says end_of_line, unenforced: BRO1520 is off", output);
        Assert.Contains("  Turned 1 rule off because your code doesn't do what it enforces: BRO1520 (operators where a line wraps: your\n    .editorconfig says end_of_line).", output.Replace("\r\n", "\n"));
    }

    [Theory]
    [InlineData(OperatorKey, "end_of_line", 85, 2, "BRO1520")]                                          // NCronJob
    [InlineData(OperatorKey, "beginning_of_line", 85, 2, null)]                                         // the code agrees
    [InlineData(OperatorKey, "end_of_line", 50, 37, null)]                                              // no clear majority: the key decides
    [InlineData(OperatorKey, "end_of_line:warning", 2, 0, null)]                                        // too few to tell
    [InlineData(OperatorKey, "beginning_of_line:warning", 0, 30, "BRO1520")]
    [InlineData(OperatorKey, "sideways", 85, 2, null)]                                                  // not a value of the key
    [InlineData("csharp_prefer_braces", "true", 5, 100, "BRO1514")]
    [InlineData("csharp_prefer_braces", "when_multiline", 100, 5, null)]                                // when_multiline removes no braces
    [InlineData("csharp_using_directive_placement", "inside_namespace", 40, 0, "BRO1008")]
    [InlineData("dotnet_style_parentheses_in_arithmetic_binary_operators", "always_for_clarity", 0, 30, "BRO1406")]
    [InlineData("dotnet_style_parentheses_in_arithmetic_binary_operators", "never_if_unnecessary", 30, 0, null)] // BRO1406 is off then anyway
    [InlineData("csharp_new_line_before_open_brace", "none", 30, 0, null)]                              // IDE0055 enforces it
    public void Conventions_AKeyTheCodeContradicts_TurnsItsRuleOff(string key, string value, int first, int second, string? rule)
    {
        var decided = new Dictionary<string, string> { [key] = $"set in .editorconfig ({value})" };
        var repository = new Dictionary<string, string> { [key] = value };

        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [key] = new[] { first, second } }, decided, repository.ContainsKey, repository);

        Assert.Equal(rule is null ? Array.Empty<string>() : new[] { $"dotnet_diagnostic.{rule}.severity = none" }, lines.Where(l => !l.StartsWith('#')));
        Assert.Equal(rule is null, report.Any(r => r.StartsWith("  set ", StringComparison.Ordinal)));

        // The repository's own severity for the rule wins.
        if (rule is not null)
        {
            var own = new Dictionary<string, string>(repository) { [$"dotnet_diagnostic.{rule}.severity"] = "warning" };
            Assert.Empty(Conventions.Decide(new Dictionary<string, int[]> { [key] = new[] { first, second } }, decided, own.ContainsKey, own).Lines);
        }
    }

    [Fact]
    public void Conventions_CountStatementsOnTheirOwnersLine_LikeNSubstitute()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class C
            {
                int M(int a)
                {
                    if (a == 1) return 1;
                    else return 2;
                    foreach (var x in new[] { 1 }) a++;
                    switch (a)
                    {
                        case 1: a++; break;
                        case 2:
                            a--;
                            break;
                    }

                    if (a == 3)
                        return 3;
                    if (a == 4) { return 4; }
                    if (a == 5) a++; else if (a == 6) a--;
                    return a;
                }
            }
            """),
            counts);

        // Shared: 'return 1', 'return 2', 'a++' (foreach), 'a++; break;', 'a++' and 'a--' of the last chain. Own line: 'a--;
        // break;' and 'return 3'. Blocks and 'else if' don't count.
        Assert.Equal(new[] { 3, 7 }, counts["csharp_preserve_single_line_statements"]);
    }

    [Theory]
    [InlineData(0, 3, true)]     // 3 are enough
    [InlineData(500, 3, true)]   // also without a majority (owner's decision 2026-10-10)
    [InlineData(500, 2, false)]
    [InlineData(0, 2, false)]
    public void Conventions_KeepStatementsOnTheirOwnersLine_FromThreePlaces(int own, int shared, bool kept)
    {
        const string Key = "csharp_preserve_single_line_statements";
        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { own, shared } }, new Dictionary<string, string>());

        Assert.Equal(kept, lines.Contains(Key + " = true"));
        Assert.Equal(kept, lines.Contains($"# init: statements on their 'if'/loop/'case' line: on their owner's line in {shared} of {own + shared} places in your code (3 are enough to keep them: {Key} = false would move each to a line of its own)"));
        Assert.DoesNotContain(lines, l => l.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal));
        Assert.Equal(kept, report.Any(r => r.StartsWith("  kept ", StringComparison.Ordinal)));

        // A repository that sets the key keeps it.
        Assert.Empty(Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { own, shared } }, new Dictionary<string, string> { [Key] = "set in .editorconfig (false)" }).Lines);
    }

    [Fact]
    public void Conventions_CountTheMembersBro1601Checks()
    {
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("""
                public interface IJob { void Run(); int Count { get; } }
                public interface IOther { void Go(); }
                public abstract class Base { public abstract void Stop(); }
                """),
            CSharpSyntaxTree.ParseText("""
                using System;

                public class Job : Base, IJob, IOther, IDisposable
                {
                    public void Run() { }

                    /// <summary>Gets the count.</summary>
                    public int Count => 0;

                    public override void Stop() { }

                    public override string ToString() => string.Empty;

                    public void Dispose() { }

                    void IOther.Go() { }

                    public void Other() { }

                    private sealed class Hidden : IDisposable { public void Dispose() { } }
                }
                """),
        };
        var counts = Conventions.NewCounts();

        Conventions.CountInheritDoc(trees, counts);

        // With: Count. Without: Run, Stop, ToString, Dispose (a library interface). Not checked: the explicit implementation,
        // Other, and the private class's member (stylebro_document_private_elements is false).
        Assert.Equal(new[] { 1, 4 }, counts["dotnet_diagnostic.BRO1601.severity"]);

        // Only the trees of projects that generate documentation count.
        var none = Conventions.NewCounts();
        Conventions.CountInheritDoc(trees, none, only: new HashSet<Microsoft.CodeAnalysis.SyntaxTree> { trees[0] });
        Assert.Equal(new[] { 0, 0 }, none["dotnet_diagnostic.BRO1601.severity"]);

        // The repository's own settings: private members too.
        var all = Conventions.NewCounts();
        Conventions.CountInheritDoc(trees, all, new Dictionary<string, string> { ["stylebro_document_private_elements"] = "true" });
        Assert.Equal(new[] { 1, 5 }, all["dotnet_diagnostic.BRO1601.severity"]);
    }

    [Theory]
    [InlineData(60, 8, null)]                       // documented: BRO1601 stays on
    [InlineData(10, 41, "doesn't do what it enforces")] // mostly undocumented
    [InlineData(41, 15, "mixes both forms")]
    public void Conventions_TurnBro1601Off_WhenTheCodeDoesNotDocumentOverrides(int with, int without, string? why)
    {
        const string Key = "dotnet_diagnostic.BRO1601.severity";
        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { with, without } }, new Dictionary<string, string>());

        Assert.Equal(why is not null, lines.Contains(Key + " = none"));
        Assert.DoesNotContain(lines, l => l == Key + " = warning");
        Assert.Equal(why is null, report.Any(r => r == $"  default  overrides/interface implementations: {with} of {with + without} with a doc comment -> warning"));
        if (why is not null)
        {
            Assert.Contains(report, r => r.StartsWith("  off      overrides/interface implementations: ", StringComparison.Ordinal) && r.EndsWith("-> BRO1601 is off", StringComparison.Ordinal));
            Assert.Contains(report, r => r.Contains(why, StringComparison.Ordinal) && r.Contains("BRO1601 (overrides/interface implementations", StringComparison.Ordinal));
            Assert.DoesNotContain(report, r => r.StartsWith("  Kept your style", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Init_CountsBro1601OnlyWhereDocumentationIsGenerated()
    {
        const string Code = "public class Job : System.IDisposable\n{\n    public void Dispose() { }\n}\n";
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");
        Write("tests/Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        foreach (var name in new[] { "A", "B", "C" })
        {
            Write($"src/Lib/{name}.cs", Code.Replace("Job", "Job" + name));
            Write($"tests/Tests/{name}.cs", Code.Replace("Job", "Test" + name));
        }

        var counts = Conventions.Count(root);

        Assert.Equal(new[] { 0, 3 }, counts["dotnet_diagnostic.BRO1601.severity"]);
        Assert.True(Conventions.GeneratesDocumentation(root, Path.Combine(root, "src", "Lib")));
        Assert.False(Conventions.GeneratesDocumentation(root, Path.Combine(root, "tests", "Tests")));

        // A project in a folder above doesn't count (the nearest .csproj does), a Directory.Build.props above the project does.
        Write("Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");
        Assert.False(Conventions.GeneratesDocumentation(root, Path.Combine(root, "tests", "Tests")));
        Write("tests/Directory.Build.props", "<Project><PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");
        Assert.True(Conventions.GeneratesDocumentation(root, Path.Combine(root, "tests", "Tests")));
    }

    [Fact]
    public void Conventions_CountCodeInactiveWithoutSymbols_LikeNSubstitutesPolyfill()
    {
        // NSubstitute's nullability attributes sit in '#if NETSTANDARD2_0': 'format' reformatted 22 one-line summaries init
        // hadn't seen. The active code (the first summary) counts once.
        var counts = Conventions.NewCounts();
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>Active.</summary>
            class A { }
            #if NETSTANDARD2_0
            /// <summary>One.</summary>
            class B { }
            /// <summary>Two.</summary>
            class C { }
            #elif NET8_0
            /// <summary>Never seen.</summary>
            class D { }
            #endif
            """);

        Conventions.Count(tree, counts);
        Assert.Equal(new[] { 0, 1 }, counts["stylebro_summary_layout"]);

        Conventions.CountConditional(tree, counts);
        Assert.Equal(new[] { 0, 3 }, counts["stylebro_summary_layout"]);
    }

    [Fact]
    public void Init_HeaderSaysWhatEachVerdictMeans()
    {
        // NSubstitute: 'mixed <inheritdoc/> tags ... default stays' kept the rule on, but the header said "mixed: the rule ...
        // is turned off".
        Write("A.cs", "interface I { void M(); }\nclass A : I\n{\n    /// <inheritdoc/>\n    public void M() { }\n    /// <inheritdoc />\n    void N() { }\n    /// <inheritdoc />\n    void O() { }\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));

        Assert.Contains("  mixed    <inheritdoc/> tags: 1 '<inheritdoc/>', 2 '<inheritdoc />' -> default stays (compact)", output);
        Assert.Contains("  mixed: the default stays, nothing is turned off", output);
        Assert.DoesNotContain("mixed: the rule", output);
    }

    [Fact]
    public void Init_AddsNoSuppressionsForASonarRuleThatIsOff_LikeBlog()
    {
        // linkdotnet/Blog: S8969 is off in .editorconfig and in NoWarn; init added BRO1147 to NoWarn although it stays off.
        Write("Directory.Build.props", "<Project><PropertyGroup><NoWarn>$(NoWarn);S8969;S1481</NoWarn></PropertyGroup><ItemGroup><PackageReference Include=\"SonarAnalyzer.CSharp\" Version=\"10.35.0.4138\" /></ItemGroup></Project>");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.S8969.severity = none\n");
        Write("A.cs", "class A { }\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("<NoWarn>$(NoWarn);S8969;S1481;IDE0059</NoWarn>", File.ReadAllText(Path.Combine(root, "Directory.Build.props")));
    }
}
