using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the thirty-fifth trial (ExcelDataReader, sqlite-net, Castle Core, MemoryPack) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void PlatformFrameworks_AreTheOnesNamingAPlatform()
    {
        Assert.Equal(["net8.0-ios", "net10.0-windows"], FormatCommand.PlatformFrameworks(["netstandard2.0", "net8.0", "net8.0-ios", "net10.0-windows"]));
    }

    [Fact]
    public void MissingWorkloadPacksOf_AMultiTargetedProject_AsksItsPlatformFrameworks()
    {
        // sqlite-net: SQLite-net-std targets netstandard2.0;net8.0;net8.0-ios. Its outer evaluation lists no MissingWorkloadPack
        // items (only the net8.0-ios one does), so trial 30's check passed it and 'dotnet restore' stopped the whole run.
        Write("Ios/Ios.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0-ios</TargetFramework></PropertyGroup>\n</Project>\n");
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFrameworks>net8.0;net8.0-ios</TargetFrameworks></PropertyGroup>\n</Project>\n");
        var lib = Path.Combine(root, "Lib", "Lib.csproj");
        Assert.Empty(FormatCommand.MissingWorkloadPacks(FormatCommand.EvaluateProject(lib)));

        // What the single-target head needs, or nothing on a machine with the iOS workload.
        var expected = FormatCommand.MissingWorkloadPacks(FormatCommand.EvaluateProject(Path.Combine(root, "Ios", "Ios.csproj")));
        Assert.Equal(expected, FormatCommand.MissingWorkloadPacksOf(lib, ["net8.0", "net8.0-ios"]));
        Assert.Empty(FormatCommand.MissingWorkloadPacksOf(lib, ["net8.0"]));
    }
}
