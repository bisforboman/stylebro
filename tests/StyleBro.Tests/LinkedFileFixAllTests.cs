using Microsoft.CodeAnalysis.Text;
using StyleBro.CodeFixes;

namespace StyleBro.Tests;

/// <summary>
/// The merge step of <see cref="LinkedFileFixAllProvider"/>: combining the edits that each target framework's copy of
/// a file wants. Getting this wrong wrote conflict markers or duplicated members into multi-targeted repos.
/// </summary>
public class LinkedFileFixAllTests
{
    private const string Original = "var a = new[]\n{\n    1,\n    2\n};\n";

    // Right after the last item "2", where a trailing comma goes.
    private static readonly int AfterLastItem = Original.IndexOf("2\n", StringComparison.Ordinal) + 1;

    [Fact]
    public void IdenticalEditsFromSeveralCopies_AreAppliedOnce()
    {
        var comma = new TextChange(new TextSpan(AfterLastItem, 0), ",");
        Assert.Equal("var a = new[]\n{\n    1,\n    2,\n};\n", Apply(comma, comma, comma));
    }

    [Fact]
    public void DifferentIndependentEdits_AreCombined()
    {
        var net10Only = new TextChange(new TextSpan(AfterLastItem, 0), ",");
        var everywhere = new TextChange(new TextSpan(0, 3), "int[]");
        Assert.Equal("int[] a = new[]\n{\n    1,\n    2,\n};\n", Apply(net10Only, everywhere, everywhere));
    }

    [Fact]
    public void ConflictingInsertionsAtTheSamePosition_KeepTheFirst()
    {
        Assert.Equal("var a = new[]\n{\n    1,\n    2,\n};\n", Apply(
            new TextChange(new TextSpan(AfterLastItem, 0), ","),
            new TextChange(new TextSpan(AfterLastItem, 0), ", ")));
    }

    [Fact]
    public void InsertionInsideAReplacedRange_IsDropped()
    {
        Assert.Equal("var a = new[] { 1, 2 };\n", Apply(
            new TextChange(TextSpan.FromBounds(Original.IndexOf("new", StringComparison.Ordinal), Original.IndexOf('}') + 1), "new[] { 1, 2 }"),
            new TextChange(new TextSpan(AfterLastItem, 0), ",")));
    }

    [Fact]
    public void DifferentWholeFileRewrites_KeepTheFirstInsteadOfMixingThem()
    {
        // Fixes that rewrite the syntax tree (like member ordering) count as one whole-text change per copy. Mixing
        // two of them used to duplicate members; now the first result wins and the next run handles the rest.
        var first = new TextChange(new TextSpan(0, Original.Length), "first");
        var second = new TextChange(new TextSpan(0, Original.Length), "second");
        Assert.Equal("first", Apply(first, second));
        Assert.Equal("first", Apply(first, first));
    }

    private static string Apply(params TextChange[] changes)
    {
        return SourceText.From(Original).WithChanges(LinkedFileFixAllProvider.Merge(changes.ToList())).ToString();
    }
}
