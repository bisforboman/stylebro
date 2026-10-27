using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-eighth trial (LightInject, CommandLineUtils, NPoco, ExpressionTreeToString) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void BuildCheck_FormatsRulesInAFolderFormatLeavesAlone_AreComparedWithTheUntouchedCode()
    {
        // LightInject: StyleCop ran only in the library, so migrate named the tests as without StyleCop and format left them
        // alone. Their xUnit2013 (an xunit.analyzers id format applies; here BRO1002, any of format's ids) fails the untouched
        // build with warnings as errors too, but check --build counted it as a finding of format's rules: "Not clean".
        Write(".editorconfig", $"root = true\n\n{Migration.BeginMarker}\n[src/Lib/**.cs]\n{Migration.UnformattedPrefix}src/Tests; 'bro format' leaves them alone (remove this line to format them)\n{Migration.EndMarker}\n");
        var solution = Path.Combine(root, "A.slnx");
        File.WriteAllText(solution, "<Solution>\n  <Project Path=\"src/Lib/Lib.csproj\" />\n  <Project Path=\"src/Tests/Tests.csproj\" />\n</Solution>\n");
        var copy = Path.Combine(root, "copy", "repo");
        Directory.CreateDirectory(copy);
        string Xunit(string folder, int line) => $"{Path.Combine(folder, "src", "Tests", "T.cs")}({line},13): error BRO1002: Add a space after '//' [{Path.Combine(folder, "src", "Tests", "Tests.csproj")}]";
        (int, IReadOnlyList<string>) Build(string folder, string workspace, string configuration) => (1, [Xunit(folder, folder == root ? 605 : 603), "  Lib -> /tmp/out/Lib.dll"]);

        var lines = new List<string>();
        var line = FormatCommand.BuildCheckLine(root, solution, lines.Add, Build, () => copy, () => ["Lib", "Tests"]);

        Assert.Contains("Debug: no findings, no new errors (1 error the untouched code has too, not counted).", lines);
        Assert.Null(line);

        // Where format ran, the same id is a finding; and a new one in the folder left alone is a new error.
        (int, IReadOnlyList<string>) InLib(string folder, string workspace, string configuration) =>
            folder == root ? (1, [.. Build(folder, workspace, configuration).Item2, $"{Path.Combine(folder, "src", "Lib", "C.cs")}(3,9): error BRO1002: Add a space after '//' [{Path.Combine(folder, "src", "Lib", "Lib.csproj")}]"]) : Build(folder, workspace, configuration);
        Assert.StartsWith("Not clean: the builds report 2 findings", FormatCommand.BuildCheckLine(root, solution, _ => { }, InLib, () => copy, () => ["Lib", "Tests"]), StringComparison.Ordinal);
        (int, IReadOnlyList<string>) NewInTests(string folder, string workspace, string configuration) =>
            folder == root ? (1, [.. Build(folder, workspace, configuration).Item2, Xunit(folder, 700).Replace("a space", "two spaces", StringComparison.Ordinal)]) : Build(folder, workspace, configuration);
        Assert.Equal("Not clean: the Debug, Release builds failed where the untouched code's didn't (above).", FormatCommand.BuildCheckLine(root, solution, _ => { }, NewInTests, () => copy, () => ["Lib", "Tests"]));
    }

    [Fact]
    public void UntouchedHook_KeepsFormatsRulesAsTheyAre_InTheFoldersFormatLeavesAlone()
    {
        // The HEAD copy's build lowers format's rules to warnings (its code isn't formatted yet), but not where format leaves the
        // code alone: there both builds must report the same errors.
        var tests = Path.Combine(root, "src", "Tests");
        var hook = File.ReadAllText(FormatCommand.UntouchedHook(root, ["xUnit2013"], [tests + "/"]));
        Assert.Contains($"<PropertyGroup Condition=\"!$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)').StartsWith('{tests}{Path.DirectorySeparatorChar}'))\">", hook, StringComparison.Ordinal);
        Assert.Contains("<WarningsNotAsErrors>$(WarningsNotAsErrors);xUnit2013</WarningsNotAsErrors>", hook, StringComparison.Ordinal);
        Assert.Contains("  <PropertyGroup>\n", File.ReadAllText(FormatCommand.UntouchedHook(root, ["xUnit2013"])), StringComparison.Ordinal);
    }
}
