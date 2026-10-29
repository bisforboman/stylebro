using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What trial 61 found in bro.</summary>
public sealed partial class MigrationTests
{
    private const string BraceBlankLineKey = "dotnet_diagnostic.BRO1503.severity";

    // jbevain/cecil puts a blank line below 543 of 720 namespace and type '{', and BRO1503 removed them in ~200 files (owner's
    // decision 2026-10-29). Only namespaces' and types' braces count: not blocks in members, empty bodies, one-line bodies or
    // a brace with a comment after it.
    [Fact]
    public void Conventions_CountNamespaceAndTypeBracesWithAndWithoutABlankLineBelow()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            namespace N {

                class A {

                    void M()
                    {

                        M();
                    }
                }

                class B
                {
                    int x;
                }

                class Empty
                {
                }

                class OneLine { int y; }

                class Commented { // note

                    int z;
                }

                enum E {

                    One,
                }

                record R;
            }
            """),
            counts);

        // With: N, A, E. Without: B.
        Assert.Equal(new[] { 1, 3 }, counts[BraceBlankLineKey]);
    }

    [Theory]
    [InlineData(10, 0, null)]
    [InlineData(0, 3, "dotnet_diagnostic.BRO1503.severity = none")]
    [InlineData(177, 543, "dotnet_diagnostic.BRO1503.severity = none")]
    [InlineData(5, 3, "dotnet_diagnostic.BRO1503.severity = none")]
    [InlineData(20, 2, null)]
    public void Conventions_BraceBlankLines_TurnBRO1503OffUnlessTheCodeHasNone(int without, int with, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [BraceBlankLineKey] = new[] { without, with } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }
}
