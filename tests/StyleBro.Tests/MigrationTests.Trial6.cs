using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What a trial on vs-validation, SmartEnum, Scrutor and TodoApi found in 'stylebro-migrate init'.</summary>
public sealed partial class MigrationTests
{
    private const string SortKey = "dotnet_sort_system_directives_first";

    [Theory]
    [InlineData("a\nb\n", 0)]
    [InlineData("a\r\nb\r\n", 1)]
    [InlineData("a\r\nb\n", -1)]
    [InlineData("a", -1)]
    public void Conventions_LineEnding(string text, int expected) => Assert.Equal(expected, Conventions.LineEnding(text));

    [Fact]
    public void Init_WritesTheFilesLineEnding_LikeSmartEnum()
    {
        // SmartEnum: LF files, no end_of_line; 'format' on Windows wrote 'Metadata =\r\n' into 26 of them.
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        Assert.Contains("  kept     C# files' line endings: 3 of 3 LF -> lf\n", output);
        Assert.Contains("# init: C# files' line endings: LF in 3 of 3 files in your code\nend_of_line = lf\n", output);

        // Mixed: nothing written, the verdict says so.
        Write("B0.cs", "class B0\r\n{\r\n}\r\n");
        output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("  mixed    C# files' line endings: 3 LF, 1 CRLF -> default stays (unset)\n", output);
        Assert.DoesNotContain("end_of_line = ", output);

        // The repository's own key stays.
        File.Delete(Path.Combine(root, "B0.cs"));
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n");
        output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("  set      C# files' line endings: 3 of 3 LF -> set in .editorconfig (crlf)\n", output);
        Assert.DoesNotContain("end_of_line = lf", output);
    }

    [Fact]
    public void Conventions_Converted_AreFilesGitConvertsOnCheckout()
    {
        var output = "i/lf    w/crlf  attr/text=auto          \ta.cs\0"
            + "i/lf    w/lf    attr/                   \tb.cs\0"
            + "i/lf    w/crlf  attr/text eol=crlf      \tc.cs\0"
            + "i/crlf  w/crlf  attr/                   \td e.cs\0"
            + "i/-text w/-text attr/                   \tf.bin\0";

        Assert.Equal(new[] { "a.cs" }, Conventions.Converted(output));
    }

    [Fact]
    public void Init_LeavesTheLineEndingUnset_WhenGitConvertsIt()
    {
        // core.autocrlf=true: the repository stores LF, a Windows checkout has CRLF and a Linux one LF. Writing crlf would
        // make 'format' rewrite every file on Linux; unset, 'format' writes the OS's ending, which the checkout has.
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        Assert.Equal(0, PreviewCommand.Git(root, "init", "-q").Code);
        Assert.Equal(0, PreviewCommand.Git(root, "add", ".").Code);
        Assert.Equal(0, PreviewCommand.Git(root, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "x").Code);
        foreach (var file in Directory.GetFiles(root, "*.cs"))
        {
            File.Delete(file);
        }

        Assert.Equal(0, PreviewCommand.Git(root, "-c", "core.autocrlf=true", "checkout", "--", ".").Code);
        Assert.Equal(1, Conventions.LineEnding(File.ReadAllText(Path.Combine(root, "A0.cs"))));

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        Assert.Contains("  note     line endings: 3 files that git converts on checkout (core.autocrlf) don't count\n", output);
        Assert.DoesNotContain("end_of_line = ", output);
    }

    [Fact]
    public void Sonar_S3260_IsNotCovered_CA1852SealsClassesWithProtectedMembers()
    {
        // SmartEnum: 'private sealed class TestEnumBool : SmartEnum<...> { protected TestEnumBool(...) }', 144 CS0628 warnings.
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /></ItemGroup></Project>""");

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        Assert.DoesNotContain("CA1852", output.Substring(output.IndexOf("# BEGIN", StringComparison.Ordinal)));
        Assert.DoesNotContain("S3260 ->", output);
        Assert.Contains("  Not covered (no safe fix):\n    S3260: CA1852's fix also seals private nested classes with protected members", output);
        Assert.DoesNotContain(SonarSetup.Mapping, m => m.Sonar == "S3260");
    }

    [Fact]
    public void Conventions_CountTheUsingOrder()
    {
        var counts = Conventions.NewCounts();
        foreach (var text in new[]
        {
            "using System;\nusing System.Text;\nusing Alpha;\nusing static Beta.C;\nusing X = Gamma;\n",   // System first
            "using Alpha;\nusing System;\nusing Zeta;\n",                                               // sorted without System first
            "using Alpha;\nusing Beta.Gamma;\n",                                                        // sorted both ways
            "using Zeta;\nusing Alpha;\n",                                                              // unsorted
            "using X = Gamma;\nusing Alpha;\n",                                                         // aliases go last
            "using System;\nnamespace N\n{\n    using Zeta;\n    using Alpha;\n}\n",                     // a namespace's list unsorted
            "global using Zeta;\nusing Alpha;\n",                                                       // fewer than 2 (global aside)
        })
        {
            Conventions.Count(CSharpSyntaxTree.ParseText(text), counts);
        }

        Assert.Equal(new[] { 1, 1, 1, 3 }, counts["using order"]);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, "true", null)]                     // nothing to count: StyleBro's default (sorted, System first)
    [InlineData(1, 0, 1, 0, "true", "too few")]
    [InlineData(20, 2, 10, 2, "true", "default")]              // sorted, System first
    [InlineData(2, 20, 10, 2, "false", "kept")]                // sorted, System not first
    [InlineData(1, 1, 10, 0, "true", "default")]               // too few files tell the two apart: the default
    [InlineData(50, 28, 37, 15, null, "both")]                 // SmartEnum: sorted, but both ways
    [InlineData(10, 0, 0, 40, null, "kept")]                   // mostly unsorted
    [InlineData(10, 0, 10, 10, null, "both")]                  // mixed
    public void Conventions_DecideTheUsingOrder(int systemFirst, int plain, int both, int unsorted, string? value, string? tag)
    {
        var counts = Conventions.NewCounts();
        counts["using order"] = new[] { systemFirst, plain, both, unsorted };

        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());

        Assert.Equal(value is null ? Array.Empty<string>() : new[] { $"{SortKey} = {value}" }, lines.Where(l => !l.StartsWith('#')));
        Assert.Single(lines, l => l.StartsWith("# init: using directives: ", StringComparison.Ordinal));
        if (value is null)
        {
            Assert.Contains(lines, l => l.EndsWith($"so they aren't sorted ('dotnet format' sorts them whenever {SortKey} is set). To sort them: add {SortKey} = true", StringComparison.Ordinal));
        }

        Assert.Equal(tag is null ? 0 : 1, report.Count(r => r.StartsWith($"  {tag,-8} using directives: ", StringComparison.Ordinal)));

        // A repository that sets either key keeps its own.
        foreach (var key in new[] { SortKey, "dotnet_separate_import_directive_groups" })
        {
            Assert.Empty(Conventions.Decide(counts, new Dictionary<string, string>(), k => k == key).Lines);
        }
    }

    [Fact]
    public void ThePreset_DoesNotSortUsings()
    {
        // 'dotnet format' sorts usings whenever a sort key is set (any value), and an .editorconfig can't unset the preset's.
        var preset = File.ReadAllLines(Path.Combine(RepositoryRoot(), "src", "StyleBro.Package", "build", "stylebro.recommended.globalconfig"));

        Assert.DoesNotContain(preset, l => l.StartsWith(SortKey, StringComparison.Ordinal) || l.StartsWith("dotnet_separate_import_directive_groups", StringComparison.Ordinal));
    }

    [Fact]
    public void Conventions_CountOverloadsBro1001CouldSplit()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class Scrutor
            {
                public bool CanDecorate(int a) => true;
                private bool CanDecorate(string a) => true;
                public void Other() { }
            }

            class Apart
            {
                public void M() { }
                public void Other() { }
                private void M(int a) { }
            }

            class SameKeys
            {
                public void N() { }
                public void Other() { }
                public void N(int a) { }
            }
            """),
            counts);

        Assert.Equal(new[] { 1, 1 }, counts["stylebro_keep_overloads_together"]);
    }

    [Theory]
    [InlineData(0, 3, true)]
    [InlineData(2, 10, true)]
    [InlineData(5, 10, false)]   // mixed: the default stays
    [InlineData(0, 2, false)]    // too few
    public void Conventions_KeepOverloadsTogether_WhenTheCodeDoes(int apart, int together, bool written)
    {
        const string Key = "stylebro_keep_overloads_together";
        var (lines, _) = Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { apart, together } }, new Dictionary<string, string>());

        Assert.Equal(written ? new[] { Key + " = true" } : Array.Empty<string>(), lines.Where(l => !l.StartsWith('#')));
    }

    [Fact]
    public void Conventions_BracesWithoutAMajority_AreKept_NotAnEmptyRule_LikeSmartEnum()
    {
        // SmartEnum: 266 of 314 bodies without braces read "-> is off" and "so  is off".
        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { ["csharp_prefer_braces"] = new[] { 48, 266 } }, new Dictionary<string, string>());

        Assert.Equal(new[] { "# init: one-line if/else/loop/using/lock bodies: without braces in 266 of 314 places in your code", "csharp_prefer_braces = when_multiline" }, lines);
        Assert.Contains("  kept     one-line if/else/loop/using/lock bodies: 266 of 314 without braces -> when_multiline", report);
    }

    [Fact]
    public void Conventions_EveryVerdict_NamesItsRule()
    {
        foreach (var convention in Conventions.All)
        {
            foreach (var count in new[] { new[] { 30, 0 }, new[] { 0, 30 }, new[] { 15, 15 }, new[] { 1, 1 } })
            {
                var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [convention.Key] = count }, new Dictionary<string, string>());
                foreach (var text in lines.Concat(report))
                {
                    Assert.DoesNotContain("  is off", text);
                    Assert.DoesNotContain("->  ", text);
                    Assert.DoesNotContain(":  (", text);
                    Assert.DoesNotContain("so  ", text);
                }
            }
        }
    }
}
