using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What trial 62 found in bro.</summary>
public sealed partial class MigrationTests
{
    private const string SnakeFieldKey = "dotnet_diagnostic.BRO1303.severity";

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

    // jbevain/mono.linq.expressions names all 11 of its multi-word private fields snake_case ('break_target'), and BRO1303
    // renamed them (owner's decision 2026-10-29). Only multi-word names of the private fields BRO1303 checks count: not
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
    [InlineData(0, 11, "dotnet_diagnostic.BRO1303.severity = none")]
    [InlineData(0, 3, "dotnet_diagnostic.BRO1303.severity = none")]
    [InlineData(5, 4, "dotnet_diagnostic.BRO1303.severity = none")]
    [InlineData(30, 2, null)]
    [InlineData(2, 0, null)]
    public void Conventions_SnakeCaseFields_TurnBRO1303OffUnlessCamelCaseWins(int camel, int snake, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [SnakeFieldKey] = new[] { camel, snake } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }

    // '_field' against 'field' mixed and snake_case: one off line for BRO1303, not two.
    [Fact]
    public void Conventions_TwoConventionsTurningBRO1303Off_WriteItsLineOnce()
    {
        var lines = Conventions.Decide(
            new Dictionary<string, int[]> { ["stylebro_private_field_naming"] = new[] { 10, 10 }, [SnakeFieldKey] = new[] { 0, 11 } },
            new Dictionary<string, string>()).Lines;

        Assert.Single(lines, l => l == "dotnet_diagnostic.BRO1303.severity = none");
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
}
