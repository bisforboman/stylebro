using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 34's findings in 'bro format' and verify.</summary>
public sealed partial class MigrationTests
{
    // netmq: '#if DEBUG' wraps 'try {' around a statement, so Debug indents it one level deeper than Release; no line before
    // a directive ends with ')' or 'else', so the if/else shape alone didn't make the file a candidate.
    private const string IfDebugTry = "class C\n{\n    void M()\n    {\n#if DEBUG\n        try\n        {\n#endif\n            Open();\n#if DEBUG\n        }\n        catch (System.Exception)\n        {\n            throw;\n        }\n#endif\n        Open();\n    }\n\n    void Open() { }\n}\n";

    [Fact]
    public void IfBodies_AlsoAreTheLinesInABlockOnlySomeBuildsOpen()
    {
        Assert.True(IfBodies.IsCandidate(IfDebugTry));
        Assert.Equal([8], IfBodies.Lines(IfDebugTry, [["DEBUG"], []]));

        // Balanced branches ('#if' around a whole block) open no block for one build only.
        Assert.False(IfBodies.IsCandidate("class C\n{\n    void M()\n    {\n#if DEBUG\n        {\n            Open();\n        }\n#endif\n    }\n\n    void Open() { }\n}\n"));
    }

    [Fact]
    public void Format_AProjectTheWorkspaceDoesntList_LoosensItsReferences()
    {
        // protobuf-net: protobuf-net.Test (net472;net8.0) references TestCompatibilityLevel (netstandard2.0, not in the solution
        // filter), which references protobuf-net.Core (multi-targeted). The net8.0 run forced Core onto net8.0, so
        // TestCompatibilityLevel couldn't reference it and protobuf-net.Test didn't load.
        var core = Path.Combine(root, "Core", "Core.csproj");
        var test = Path.Combine(root, "Test", "Test.csproj");
        var level = Path.Combine(root, "Level", "Level.csproj");
        var listed = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase)
        {
            [core] = (["netstandard2.0", "net8.0"], []),
            [test] = (["net472", "net8.0"], [core, level]),
        };
        var outside = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase) { [level] = (["netstandard2.0"], [core]) };

        var all = FormatCommand.WithOutside(listed, outside);
        var keep = FormatCommand.Keep(all, "net8.0");
        Assert.Contains(level, FormatCommand.Other(all, keep, "net8.0"));

        // A listed project's own entry wins.
        Assert.Equal(["net472", "net8.0"], FormatCommand.WithOutside(all, new Dictionary<string, (string[] Frameworks, string[] References)> { [test] = (["net8.0"], []) })[test].Frameworks);
    }
}
