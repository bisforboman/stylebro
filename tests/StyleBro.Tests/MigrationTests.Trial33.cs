using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 33's findings in 'bro format'.</summary>
public sealed partial class MigrationTests
{
    [Theory]

    // taglib-sharp: one stray CR at the end of an LF file.
    [InlineData("class A\n{\n    int x;\n}\r}\n", null, "class A\n{\n    int x;\n}\n}\n")]

    // Scriban: CRLF lines with a lone CR among them.
    [InlineData("class A\r\n{\r\n    int x;\r    int y;\r\n}\r\n", null, "class A\r\n{\r\n    int x;\r\n    int y;\r\n}\r\n")]

    // Every line a CR alone: end_of_line decides, else LF.
    [InlineData("class A\r{\r}\r", "crlf", "class A\r\n{\r\n}\r\n")]
    [InlineData("class A\r{\r}\r", null, "class A\n{\n}\n")]

    // A CR inside a string is the string's: it stays.
    [InlineData("class A\n{\n    const string S = @\"a\rb\";\r}\n", null, "class A\n{\n    const string S = @\"a\rb\";\n}\n")]
    public void Format_ConvertsOldMacLineEndings(string text, string? configured, string expected)
    {
        Assert.Equal(expected, FormatCommand.WithoutLoneCrs(text, configured));
        Assert.False(FormatCommand.HasLoneCr(expected));
    }

    [Fact]
    public void Format_ConvertsOldMacLineEndings_KeepingTheByteOrderMark()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\nend_of_line = crlf\n");
        File.WriteAllBytes(Path.Combine(root, "Mac.cs"), [0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes("class A\r{\r}\r")]);
        Write("Plain.cs", "class B\n{\n}\r");

        FormatCommand.ConvertLoneCrs(root, FormatCommand.OldMacLineEndings(root));

        Assert.Equal([0xEF, 0xBB, 0xBF, .. System.Text.Encoding.UTF8.GetBytes("class A\r\n{\r\n}\r\n")], File.ReadAllBytes(Path.Combine(root, "Mac.cs")));
        Assert.Equal("class B\n{\n}\n", File.ReadAllText(Path.Combine(root, "Plain.cs")));
        Assert.Empty(FormatCommand.OldMacLineEndings(root));
    }

    [Fact]
    public void Format_LeavesTheXunitFixesOut_FromXunitAnalyzers124()
    {
        // Tmds.DBus: xunit.v3 3.2.2 brings xunit.analyzers 1.27.0, whose fixes have no Fix All ('dotnet format' can't apply them).
        Write("test/T/T.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"xunit.v3\" Version=\"3.2.2\" />\n  </ItemGroup>\n</Project>\n");
        Assert.Contains("xUnit2013", FormatCommand.Diagnostics(root));
        Assert.Null(FormatCommand.XunitWithoutFixAll(root));

        Write("test/T/obj/project.assets.json", "{\n  \"libraries\": {\n    \"xunit.analyzers/1.23.0\": {},\n    \"xunit.v3/3.2.2\": {}\n  }\n}\n");
        Assert.Null(FormatCommand.XunitWithoutFixAll(root));
        Assert.Contains("xUnit2013", FormatCommand.Diagnostics(root));

        Write("test/T/obj/project.assets.json", "{\n  \"libraries\": {\n    \"xunit.analyzers/1.27.0\": {},\n    \"xunit.v3/3.2.2\": {}\n  }\n}\n");
        Assert.Equal("1.27.0", FormatCommand.XunitWithoutFixAll(root));
        Assert.DoesNotContain("xUnit2013", FormatCommand.Diagnostics(root));
        Assert.StartsWith("xUnit: xunit.analyzers 1.27.0 is restored here, and from 1.24 on", FormatCommand.XunitWithoutFixAllLine("1.27.0"), StringComparison.Ordinal);

        // The first version without Fix All counts; a prerelease of it too.
        Assert.Equal([new Version(1, 24, 0)], FormatCommand.XunitAnalyzersVersions("\"xunit.analyzers/1.24.0-pre.3\": {}"));
    }
}
