using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>'bro format' on Newtonsoft.Json (2026-10-23): a '#region' line some builds leave out, a comment in an else body '#if' nests.</summary>
public sealed partial class MigrationTests
{
    // DynamicTests.cs: the whole class is '#if !(NET35 || NET20)'. Where it is compiled the formatter indents '#endregion' with
    // the code; in net20 and net35 it is inside the left-out branch, and below '#endif' the formatter put it in column 0. The
    // runs undid each other and 'bro format' ended "still changing after 3 runs".
    private const string RegionInLeftOutCode = "class C\n{\n#if !NET20\n    void M()\n    {\n        #region Add\n        x();\n#if NET6_0_OR_GREATER\n        y();\n#endif\n        #endregion\n    }\n#endif\n}\n";

    // TraceJsonWriter.cs: an if/else chain split by '#if', its last else body a block whose lines each build nests its own
    // way; the '//' line above a statement there went 8 columns deeper and back on every run (Windows, the newest SDK).
    private const string CommentInIfBody = "class C\n{\n    void M(object value)\n    {\n#if A\n        if (value is int)\n        {\n            X();\n        }\n        else\n#endif\n#if B\n        if (value is long)\n        {\n            X();\n        }\n        else\n#endif\n        {\n            // Y would throw here\n            Z();\n        }\n    }\n}\n";

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

    [Fact]
    public void IfBodies_AlsoAreTheCommentLinesAboveOne()
    {
        var lines = IfBodies.Lines(CommentInIfBody, [["A", "B"], ["B"], []]);
        Assert.Contains(20, lines);
        Assert.Contains(19, lines);

        // The comment gets its indentation back with its statement.
        var moved = CommentInIfBody.Replace("            // Y", "                    // Y", StringComparison.Ordinal);
        Assert.Equal(CommentInIfBody, IfBodies.Keep(CommentInIfBody, lines, moved));
    }
}
