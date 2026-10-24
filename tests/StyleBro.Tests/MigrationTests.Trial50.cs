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

    [Fact]
    public void Preview_Init_TakesTheEngineOption()
    {
        // commandline: 'bro preview init --engine host' stopped at "Unknown option: --engine". Program takes it for every
        // preview (and its value isn't read as the path).
        var error = CaptureError(() => Capture(() => Program.Main(["preview", "init", root, "--engine", "host"])));
        Assert.Contains($"No solution or project in {root};", error, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown option", error, StringComparison.Ordinal);

        error = CaptureError(() => Capture(() => Assert.Equal(1, Program.Main(["preview", "init", root, "--engine", "fast"]))));
        Assert.Contains("--engine takes 'host'", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_OfASubfolder_SeesTheReferenceAbove()
    {
        // Dahomey.Json's re-check: 'bro preview format Lib' with the reference in the root Directory.Build.props said "wasn't
        // referenced" and added Lib/Directory.Build.props, which would hide the root one.
        Write("Directory.Build.props", "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n");
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        var lib = Path.Combine(root, "Lib");

        Assert.Equal([Path.Combine(root, "Directory.Build.props")], PreviewCommand.ImportedFromAbove(lib, root));
        Assert.Null(PreviewCommand.AddPackage(lib, root));
        Assert.False(File.Exists(Path.Combine(lib, "Directory.Build.props")));

        // The preview itself (its copy's root is the top).
        PreviewCommand.MakeRepository(root);
        var output = Capture(() => CaptureError(() => Assert.Throws<InvalidOperationException>(() => PreviewCommand.Run("format", new[] { Path.Combine(lib, "Lib.csproj"), "--diff=" + Path.Combine(root, "out.patch") }, (_, _) => throw new InvalidOperationException("format ran")))));
        Assert.DoesNotContain("wasn't referenced", output, StringComparison.Ordinal);

        // Nothing above the top counts, and without a reference the subfolder still gets one.
        Assert.Empty(PreviewCommand.ImportedFromAbove(lib, lib));
        File.WriteAllText(Path.Combine(root, "Directory.Build.props"), "<Project />\n");
        Assert.NotNull(PreviewCommand.AddPackage(lib, root));
        Assert.Contains("StyleBro.Analyzers", File.ReadAllText(Path.Combine(lib, "Directory.Build.props")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 2, 2)]
    [InlineData(2, 0, 2)]
    [InlineData(2, 1, 1)]
    [InlineData(1, 2, 1)]
    [InlineData(1, 3, 1)]
    public void Runs_AFailureAfterFindings_IsTheExitCode(int exit, int code, int expected)
    {
        // ncalc: the first runs found kept findings (2), the NCalc.Benchmarks run crashed (1), and 'bro check' said "Not
        // verified: ... the rest is clean" (3) for code that run never checked.
        Assert.Equal(expected, FormatCommand.Worse(exit, code));
    }

    [Fact]
    public void Format_AFileOneProjectHasAsAnAdditionalFileAndAnotherCompiles_RunsApart()
    {
        // ncalc: Parlot's generator makes NCalc.Parser's '*.parlot.cs' additional files, NCalc.Benchmarks compiles one, and
        // MSBuildWorkspace crashed loading any solution or solution filter with both ("The solution does not contain the
        // specified document"); a project file loads. Benchmarks references Parser (through Core): a run of its own, which
        // opens its project file; Other doesn't load Parser and runs with the rest.
        var compile = new Dictionary<string, string[]>
        {
            ["Parser"] = ["parser.cs"],
            ["Core"] = ["core.cs"],
            ["Benchmarks"] = ["Grammar.parlot.cs", "bench.cs"],
            ["Tests"] = ["tests.cs"],
            ["Other"] = ["other.cs"],
        };
        var references = new Dictionary<string, string[]> { ["Core"] = ["Parser"], ["Benchmarks"] = ["Core"], ["Tests"] = ["Core"] };
        IEnumerable<string> Refs(string p) => references.TryGetValue(p, out var r) ? r : [];
        IEnumerable<string> Additional(string p) => p == "Parser" ? ["Grammar.parlot.cs"] : [];

        Assert.True(FormatCommand.LoadsAdditionalAsCompile(FormatCommand.LoadedWith("Benchmarks", Refs), p => compile[p], Additional));
        Assert.False(FormatCommand.LoadsAdditionalAsCompile(FormatCommand.LoadedWith("Tests", Refs), p => compile[p], Additional));
        Assert.False(FormatCommand.LoadsAdditionalAsCompile(["Benchmarks"], p => compile[p], Additional));
        Assert.Equal(
            new List<List<string>> { new() { "Parser", "Core", "Tests", "Other" }, new() { "Benchmarks" } },
            FormatCommand.Separate(["Parser", "Core", "Benchmarks", "Tests", "Other"], Refs, p => compile[p], additional: Additional));

        // Without the reference, Benchmarks alone loads no additional file, but not in one run with Parser.
        references.Remove("Benchmarks");
        Assert.Equal(
            new List<List<string>> { new() { "Parser", "Core", "Tests", "Other" }, new() { "Benchmarks" } },
            FormatCommand.Separate(["Parser", "Core", "Benchmarks", "Tests", "Other"], Refs, p => compile[p], additional: Additional));
        Assert.Equal(
            new List<List<string>> { new() { "Benchmarks", "Other" }, new() { "Parser", "Core", "Tests" } },
            FormatCommand.Separate(["Benchmarks", "Other", "Parser", "Core", "Tests"], Refs, p => compile[p], additional: Additional));

        // Without additional files: one run, as before.
        Assert.Single(FormatCommand.Separate(["Parser", "Core", "Benchmarks", "Tests", "Other"], Refs, p => compile[p]));

        // The line says why (not roslyn#86001's '#if' reason).
        var line = FormatCommand.SharedFilesLine([[["Parser", "Core"], ["Benchmarks"]]], ["src/Grammar.parlot.cs"]);
        Assert.StartsWith("Shared files: each group gets a run of its own: Parser, Core | Benchmarks. Some projects have a file as an additional file (AdditionalFiles) that another compiles (src/Grammar.parlot.cs)", line, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_ShowsTheEngineLine()
    {
        // ncalc: the host's "Engine: ... 'dotnet format' runs the checks and fixes" line was held back with the runs' output
        // in a check that found something, so it didn't say which engine checked. Engine lines are headers, shown as they come.
        Assert.True(FormatCommand.IsHeaderLine(FormatHost.LoadFailedLine("The solution does not contain the specified document.")));
        Assert.True(FormatCommand.IsHeaderLine(FormatHost.FallbackLine("no SDK folder")));
        Assert.False(FormatCommand.IsHeaderLine("Engine is not a header without the colon"));
    }

    [Fact]
    public void AFrameworksView_HasItsAdditionalFiles()
    {
        // ncalc: NCalc.Parser is multi-targeted, and Parlot's targets add '*.parlot.cs' as additional files in each framework's
        // evaluation only: the outer one had none, so the runs weren't split.
        var view = FormatCommand.ReadView("""
            {
              "Properties": { "DefineConstants": "NET8_0", "LangVersion": "", "Nullable": "enable" },
              "Items": {
                "Compile": [ { "FullPath": "/r/Parser.cs" } ],
                "AdditionalFiles": [ { "FullPath": "/r/Grammar.parlot.cs" } ]
              }
            }
            """);
        Assert.Equal(["/r/Grammar.parlot.cs"], view!.Additional);
        Assert.Equal(["/r/Parser.cs"], view.Compile);
    }
}
