using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the forty-ninth trial (ConsoleAppFramework, superpower, Cocona, Sawmill) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void BuildCheck_AnEmitErrorTheUntouchedCodeHasToo_IsClean()
    {
        // superpower: its benchmarks (strong-named) reference the unsigned Sprache, CS8002 with warnings as errors, untouched
        // too. The error has no place in the code, so --build said "Not verified: ... compiled nothing past their errors",
        // but the compiler reports it when it emits, after the analyzers ran (a planted finding there was reported).
        var solution = Path.Combine(root, "Lib.slnx");
        File.WriteAllText(solution, "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n  <Project Path=\"Bench/Bench.csproj\" />\n</Solution>\n");
        var bench = Path.Combine(root, "Bench", "Bench.csproj");
        (int, IReadOnlyList<string>) Build(string folder, string workspace, string configuration) =>
            (1, [$"CSC : error CS8002: Referenced assembly 'Sprache, Version=2.3.1.0, Culture=neutral, PublicKeyToken=null' does not have a strong name. [{bench}::TargetFramework=net8.0]", "  Lib -> /tmp/out/Lib.dll"]);

        var lines = new List<string>();
        Assert.Null(FormatCommand.BuildCheckLine(root, solution, lines.Add, Build, () => string.Empty, () => ["Lib", "Bench"]));
        Assert.Contains("Debug: no findings, no new errors (1 error the untouched code has too, not counted).", lines);

        // An error without a place that stops the build before the compiler (a missing reference) still isn't verified.
        (int, IReadOnlyList<string>) Missing(string folder, string workspace, string configuration) =>
            (1, [$"CSC : error CS0006: Metadata file '/tmp/out/Gone.dll' could not be found [{bench}::TargetFramework=net8.0]", "  Lib -> /tmp/out/Lib.dll"]);
        Assert.StartsWith(FormatCommand.NotVerifiedMarker, FormatCommand.BuildCheckLine(root, solution, _ => { }, Missing, () => string.Empty, () => ["Lib", "Bench"]), StringComparison.Ordinal);
    }
}
