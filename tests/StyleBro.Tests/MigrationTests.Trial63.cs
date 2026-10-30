using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What trial 63 found in bro.</summary>
public sealed partial class MigrationTests
{
    private const string AdjacentMembersKey = "stylebro_allow_adjacent_single_line_members";

    private const string SeparationKey = "dotnet_diagnostic.BRO1505.severity";

    // mono/t4 groups one-line members ('public void Clear () => session.Clear ();') about 120 times, and BRO1505 added 130
    // blank lines between them (owner's decision 2026-10-30). The pairs the key would allow count for it, not for BRO1505.
    [Fact]
    public void Conventions_CountNeighbouringOneLineMembersApartFromBRO1505()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class C
            {
                public void A() => B();
                public void B() => A();

                public void D() => A();
                public void E()
                {
                }
                public void F()
                {
                }

                public void G()
                {
                }
            }
            """),
            counts);

        // One-line pairs: A/B together, B/D apart. BRO1505: D/E and E/F together, F/G apart.
        Assert.Equal(new[] { 1, 1 }, counts[AdjacentMembersKey]);
        Assert.Equal(new[] { 1, 2 }, counts[SeparationKey]);
    }

    [Theory]
    [InlineData(48, 121, "stylebro_allow_adjacent_single_line_members = true")]
    [InlineData(100, 3, "stylebro_allow_adjacent_single_line_members = true")]
    [InlineData(100, 2, null)]
    [InlineData(0, 0, null)]
    public void Conventions_OneLineMembersTogether_AllowThemFromThree(int apart, int together, string? line)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [AdjacentMembersKey] = new[] { apart, together } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#'))
            .ToList();

        Assert.Equal(line is null ? [] : [line], lines);
    }
}
