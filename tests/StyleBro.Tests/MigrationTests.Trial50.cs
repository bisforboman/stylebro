using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fiftieth trial (commandline, ObservableCollections, Dahomey.Json, ncalc) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Preview_Format_TakesTheEngineOption()
    {
        // Dahomey.Json: 'bro preview format --engine host' passed --engine on, and 'dotnet format' failed on it ("Unrecognized
        // command or argument '--engine'"). The preview takes it and formats the copy with that engine.
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        string[]? seen = null;
        string? engine = null;
        int Format(string[] args, Action<string> log)
        {
            seen = args;
            engine = FormatCommand.CurrentEngine;
            throw new InvalidOperationException("format ran");
        }

        var project = Path.Combine(root, "App", "App.csproj");
        Capture(() => CaptureError(() => Assert.Throws<InvalidOperationException>(() => PreviewCommand.Run("format", new[] { project, "--diff", "--engine", "host" }, Format))));
        Assert.Equal(FormatCommand.HostEngine, engine);
        Assert.DoesNotContain(FormatCommand.EngineOption, seen!);
        Assert.DoesNotContain(FormatCommand.HostEngine, seen!);
        Assert.Equal(FormatCommand.DotnetFormatEngine, FormatCommand.CurrentEngine);

        // A value that is neither engine stops before the copy, as 'bro format' does.
        var error = CaptureError(() => Capture(() => Assert.Equal(1, PreviewCommand.Run("format", new[] { project, "--diff", "--engine", "fast" }, (_, _) => throw new InvalidOperationException("format ran")))));
        Assert.Contains("--engine takes 'host'", error, StringComparison.Ordinal);
    }
}
