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
}
