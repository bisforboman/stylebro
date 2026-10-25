using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-fifth trial (ZLinq, Newtonsoft.Json.Schema, FeatureManagement-Dotnet, protobuf-net.Grpc) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Format_OnTheRoot_PutsANewBaselineNextToTheOnlySolutionOneFolderDown_AsThePreviewDoes()
    {
        // Newtonsoft.Json.Schema: its only solution is in Src/. The preview's patch had Src/stylebro.baseline, 'bro format'
        // (on the root) wrote stylebro.baseline, and 'bro format Src/Newtonsoft.Json.Schema.slnx' Src/stylebro.baseline.
        Write("Src/App.slnx", "<Solution>\n  <Project Path=\"App/App.csproj\" />\n</Solution>\n");
        Write("Src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("Src/App/C.cs", "class C\n{\n    private int Count;\n}\n");
        var lines = new List<string>();
        var code = FormatCommand.Run([root], lines.Add, (args, _) =>
        {
            File.AppendAllText(Environment.GetEnvironmentVariable(KeptFinding.Variable)!, new KeptFinding(Path.Combine(root, "Src", "App", "C.cs"), 3, 17, "BRO1303", KeptReason.NameInString, "Rename 'Count' to 'count'") + "\n");
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Contains(lines, l => l.StartsWith("Baseline: Src/stylebro.baseline: added 1 kept finding,", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(root, "stylebro.baseline")));
        Assert.Single(Analyzers.Baseline.Baseline.Parse(File.ReadAllText(Path.Combine(root, "Src", "stylebro.baseline"))).Entries);
    }
}
