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

    // mono/t4: the first format of a fresh clone ran net7.0 on its own, later ones merged it with net6.0. Loading the projects
    // evaluated Mono.TextTemplating.Tests' net6.0 before the restore (no Microsoft.NET.Test.Sdk Program.cs yet), and the run
    // plan reused that evaluation. After the restore the per-framework evaluations go; the projects' own stay.
    [Fact]
    public void ForgetFrameworkEvaluations_DropsThePerFrameworkOnes()
    {
        var evaluations = new System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in new[] { "/r/Tests.csproj", "/r/Tests.csproj|net6.0|", "/r/Tests.csproj|net7.0|Release", "/r/App.csproj|net8.0|workloads" })
        {
            evaluations[key] = new Lazy<string>(() => key);
        }

        FormatCommand.ForgetFrameworkEvaluations(evaluations);

        Assert.Equal(["/r/Tests.csproj"], evaluations.Keys);
    }
}
