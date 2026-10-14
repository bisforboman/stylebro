using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_SetsStyleBroRulesById_WhereANestedBulkSeverityHidesThemFromFormat()
    {
        // Trial 19: tests/.editorconfig with 'dotnet_analyzer_diagnostic.severity = none'; format said clean, the build failed.
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("tests/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = none\ndotnet_diagnostic.BRO1002.severity = none\n");
        Write("tests/App.Tests/App.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("tests/App.Tests/C.cs", "class C\n{\n}\n");
        Assert.StartsWith("Not clean: .editorconfig's bulk severity", FormatCommand.HiddenLine(root), StringComparison.Ordinal);

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, "tests", ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains(InitCommand.ByIdComment, text, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1002.severity = warning", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BRO1001", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
        Assert.Null(FormatCommand.HiddenLine(root));

        // A second init changes nothing; without the bulk severity the block goes.
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(text, File.ReadAllText(Path.Combine(root, "tests", ".editorconfig")).Replace("\r\n", "\n"));
        Write("tests/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = warning\n" + text.Substring(text.IndexOf(InitCommand.WhitespaceBegin, StringComparison.Ordinal)));
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("BRO1001", File.ReadAllText(Path.Combine(root, "tests", ".editorconfig")), StringComparison.Ordinal);
    }

    [Fact]
    public void NestedHiddenFromFormat_TakesTheSettingsOnTheWayDown()
    {
        Write(".editorconfig", "root = true\n");
        Write("src/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = silent\n");
        Write("src/Lib/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = warning\n");
        Write("other/.editorconfig", "[*.cs]\nindent_size = 2\n");

        var hidden = InitCommand.NestedHiddenFromFormat(root, new Dictionary<string, string>(), withBlocks: true);

        Assert.Equal(["src"], hidden.Keys);

        // A bulk 'silent' in the root's settings reaches the nested files, an id there wins.
        hidden = InitCommand.NestedHiddenFromFormat(root, new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "silent", ["dotnet_diagnostic.BRO1001.severity"] = "none" }, withBlocks: true);
        Assert.Equal(["other", "src"], hidden.Keys);
        Assert.DoesNotContain(hidden["other"], l => l.Contains("BRO1001", StringComparison.Ordinal));
    }
}
