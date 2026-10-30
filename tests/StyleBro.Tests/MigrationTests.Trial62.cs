using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What trial 62 found in bro.</summary>
public sealed partial class MigrationTests
{
    private const string SnakeFieldKey = "dotnet_diagnostic.BRO1308.severity";

    private const string CommentFrameKey = "dotnet_diagnostic.BRO1120.severity";

    // jbevain/mono.linq.expressions commits its Directory.Build.props as UTF-16 with a BOM and CRLF: init read it as UTF-8 and
    // wrote the preset version back as UTF-8 with LF, so git showed the whole file as changed (a binary diff).
    [Theory]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-32")]
    public void EditFile_KeepsAUtf16Or32FilesEncodingAndLineEndings(string name)
    {
        var encoding = Encoding.GetEncoding(name);
        var path = Path.Combine(root, "Directory.Build.props");
        File.WriteAllText(path, "<Project>\r\n  <PropertyGroup>\r\n    <A>1</A>\r\n  </PropertyGroup>\r\n</Project>\r\n", encoding);
        string? seen = null;

        PreviewCommand.EditFile(path, text => Migration.SetPreset(seen = text, "1.0", "comment")!);

        Assert.StartsWith("<Project>\r\n", seen, StringComparison.Ordinal);
        Assert.Equal(encoding.GetPreamble(), File.ReadAllBytes(path).Take(encoding.GetPreamble().Length));
        Assert.Equal(
            "<Project>\r\n  <PropertyGroup>\r\n    <StyleBroPreset>1.0</StyleBroPreset>\r\n    <A>1</A>\r\n  </PropertyGroup>\r\n</Project>\r\n",
            File.ReadAllText(path));
    }

    // jbevain/mono.linq.expressions names 11 of its 13 multi-word private fields snake_case ('break_target'), and BRO1308
    // renamed them (owner's decision 2026-10-29). Only multi-word names of private fields count: not
    // one-word names, 'm_' prefixes, constants, static readonly or non-private fields.
    [Fact]
    public void Conventions_CountMultiWordPrivateFieldsCamelAndSnakeCase()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class C
            {
                readonly object break_target, continue_target;
                int unique_seed;
                static int _shared_count;
                int uniqueNames;
                int _lastValue;
                int body;
                int _test;
                int m_count;
                int s_x;
                const int max_size = 1;
                static readonly int empty_list = 0;
                internal int open_count;
                public int close_count;
            }
            """),
            counts);

        // camelCase: uniqueNames, _lastValue. snake_case: break_target, continue_target, unique_seed, _shared_count.
        Assert.Equal(new[] { 2, 4 }, counts[SnakeFieldKey]);
    }

    [Theory]
    [InlineData(10, 0, null)]
    [InlineData(2, 11, "dotnet_diagnostic.BRO1308.severity = none")]
    [InlineData(0, 3, "dotnet_diagnostic.BRO1308.severity = none")]
    [InlineData(5, 4, "dotnet_diagnostic.BRO1308.severity = none")]
    [InlineData(30, 2, null)]
    [InlineData(2, 0, null)]
    public void Conventions_SnakeCaseFields_TurnBRO1308OffUnlessCamelCaseWins(int camel, int snake, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [SnakeFieldKey] = new[] { camel, snake } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }

    // Ninject's untouched code fails its TWAE build on xUnit1013 in both target frameworks; MSBuild's parallel output gave the
    // two lines in either order, so two check --build runs' logs differed.
    [Fact]
    public void StoppedLines_AreSortedAndDistinct()
    {
        FormatCommand.BuildMessage Csc(string framework) => new("k" + framework, "xUnit1013", true, $"  CSC: xUnit1013: Public method 'Dispose' ({framework})", InSource: false);

        var lines = FormatCommand.StoppedLines([Csc("net6.0"), Csc("net462"), Csc("net6.0"), new("k", "CS0001", true, "  in source", InSource: true)]);

        Assert.Equal(["  CSC: xUnit1013: Public method 'Dispose' (net462)", "  CSC: xUnit1013: Public method 'Dispose' (net6.0)"], lines);
    }

    // mono.linq.expressions indents with tabs and has no .editorconfig: BRO1107 indented its new lines with spaces (owner's
    // decision 2026-10-29). One count per file with indented lines: most start with a tab, or with two spaces or more.
    [Fact]
    public void Conventions_CountFilesIndentedWithTabsOrSpaces()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText("class A\n{\n\tint x;\n\tint y;\n    int z;\n}\n"), counts);
        Conventions.Count(CSharpSyntaxTree.ParseText("class B\n{\n    int x;\n}\n"), counts);
        Conventions.Count(CSharpSyntaxTree.ParseText("class C { }\n"), counts);

        Assert.Equal(new[] { 1, 1 }, counts["indent_style"]);
    }

    [Theory]
    [InlineData(0, 30, "indent_style = tab")]
    [InlineData(30, 0, null)]
    [InlineData(10, 10, null)]
    public void Conventions_TabIndentedCode_KeepsTabs(int spaces, int tabs, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { ["indent_style"] = new[] { spaces, tabs } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }

    // mono.linq.expressions frames 4 of its 11 comments in code with empty '//' lines, and BRO1120 removed them (owner's
    // decision 2026-10-29). Counted: the groups BRO1120 checks with text in them; not the file header, not a '//' a pair of
    // braces holds, not a group of empty comments only.
    [Fact]
    public void Conventions_CountCommentGroupsFramedByEmptyComments()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            //
            // Header
            //

            class C
            {
                void M()
                {
                    //
                    // Framed.
                    //
                    M();

                    // Plain.
                    // Two lines.
                    M();

                    // Text, then empty.
                    //
                    M();

                    //
                    M();

                    try
                    {
                    }
                    catch
                    {
                        //
                    }

                    M(); // after code
                }
            }
            """),
            counts);

        // Framed: 'Framed.', 'Text, then empty.'. Plain: 'Plain.', 'after code'.
        Assert.Equal(new[] { 2, 2 }, counts[CommentFrameKey]);
    }

    [Theory]
    [InlineData(7, 4, "dotnet_diagnostic.BRO1120.severity = none")]
    [InlineData(40, 1, null)]
    [InlineData(0, 5, "dotnet_diagnostic.BRO1120.severity = none")]
    public void Conventions_FramedComments_TurnBRO1120OffUnlessPlainWins(int plain, int framed, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [CommentFrameKey] = new[] { plain, framed } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }

    // The UTF-16 Directory.Build.props init edits is binary to git: without '--binary' the preview's patch said "Binary files
    // differ" for it and didn't apply (the fixture utf16-props).
    [Fact]
    public void PreviewPatch_CarriesBinaryFiles()
    {
        Assert.Equal(
            ["diff", "--cached", "--binary", "--ignore-submodules", "--output=p.patch", "base", "--", ".", ":(exclude)vendor/"],
            PreviewCommand.PatchArguments("p.patch", "base", ["vendor"]));
    }
}
