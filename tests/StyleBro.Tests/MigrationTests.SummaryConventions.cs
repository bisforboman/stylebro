using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Issue #132: init counts how property summaries begin (BRO1604); constructors' don't count (issue #179).</summary>
public sealed partial class MigrationTests
{
    private const string PropertySummaryKey = "dotnet_diagnostic.BRO1604.severity";
    private const string ConstructorSummaryKey = "dotnet_diagnostic.BRO1606.severity";

    [Fact]
    public void Conventions_CountPropertyAndConstructorSummaries_TheStyleCopWayOrTheirOwn()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(
            CSharpSyntaxTree.ParseText(
                """
            class A
            {
                /// <summary>Initializes a new instance of the <see cref="A"/> class.</summary>
                public A() { }

                /// <summary>Opens a file.</summary>
                public A(string path) { }

                /// <summary>
                /// Initializes static members of the <see cref="A"/> class.
                /// </summary>
                static A() { }

                /// <summary><see cref="A"/> from a number.</summary>
                public A(int n) { }

                /// <summary>Gets or sets the name.</summary>
                public string Name { get; set; }

                /// <summary>Gets the size.</summary>
                public int Size { get; }

                /// <summary>The samples, float32 interleaved.</summary>
                public float[] Samples { get; }

                /// <summary>Whether the decoder gives floats.</summary>
                public bool IsFloat { get; }

                /// <summary>Getsome text.</summary>
                public int Other { get; }

                /// <summary>Runs.</summary>
                public void Run() { }
            }
            """,
                new CSharpParseOptions(documentationMode: Microsoft.CodeAnalysis.DocumentationMode.Diagnose)),
            counts);

        // Properties: Name, Size the StyleCop way; Samples, IsFloat, Other in their own words; Run isn't a property.
        // Constructors aren't counted: BRO1606 accepts a summary in its own words (issue #179).
        Assert.Equal(new[] { 2, 3 }, counts[PropertySummaryKey]);
        Assert.False(counts.TryGetValue(ConstructorSummaryKey, out var constructors) && constructors.Sum() > 0);
    }

    [Theory]
    [InlineData(PropertySummaryKey, 0, 113, "dotnet_diagnostic.BRO1604.severity = none")]
    [InlineData(PropertySummaryKey, 5, 5, "dotnet_diagnostic.BRO1604.severity = none")]
    [InlineData(PropertySummaryKey, 40, 2, null)]
    public void Conventions_SummaryWording_IsFollowed(string key, int first, int second, string? expected)
    {
        var lines = Conventions.Decide(new Dictionary<string, int[]> { [key] = new[] { first, second } }, new Dictionary<string, string>()).Lines
            .Where(l => !l.StartsWith('#') && !l.StartsWith("dotnet_sort", StringComparison.Ordinal))
            .ToList();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, lines);
    }
}
