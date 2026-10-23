using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>'bro format' on Newtonsoft.Json (2026-10-23): a '#region' line some builds leave out.</summary>
public sealed partial class MigrationTests
{
    // DynamicTests.cs: the whole class is '#if !(NET35 || NET20)'. Where it is compiled the formatter indents '#endregion' with
    // the code; in net20 and net35 it is inside the left-out branch, and below '#endif' the formatter put it in column 0. The
    // runs undid each other and 'bro format' ended "still changing after 3 runs".
    private const string RegionInLeftOutCode = "class C\n{\n#if !NET20\n    void M()\n    {\n        #region Add\n        x();\n#if NET6_0_OR_GREATER\n        y();\n#endif\n        #endregion\n    }\n#endif\n}\n";

    [Fact]
    public void IfBodies_AlsoAreTheRegionLinesOnlySomeBuildsCompile()
    {
        // '#endregion' below '#endif'; '#region Add' is below code, which the formatter leaves alone in the left-out branch.
        Assert.True(IfBodies.IsCandidate(RegionInLeftOutCode));
        Assert.Equal([10], IfBodies.Lines(RegionInLeftOutCode, [["NET20"], ["NET6_0_OR_GREATER"], []]));

        // Compiled by every build (or left out by every one): each indents them the same way.
        Assert.Empty(IfBodies.Lines(RegionInLeftOutCode, [["NET6_0_OR_GREATER"], []]));

        // A run that moved '#endregion' gets its indentation back.
        var moved = RegionInLeftOutCode.Replace("        #endregion", "#endregion", StringComparison.Ordinal);
        Assert.Equal(RegionInLeftOutCode, IfBodies.Keep(RegionInLeftOutCode, [10], moved));
    }
}
