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

    [Fact]
    public void OldCSharpFiles_AreSharedFilesAnOlderBuildCompiles()
    {
        // sqlite-net: src/SQLite.cs is compiled by SQLite.Tests (net8.0, C# 12) and SQLite-net-static (netstandard2.0, C# 7.3).
        FormatCommand.CompileView View(string langVersion, params string[] files) => new([], langVersion, "enable", files);
        var files = FormatCommand.OldCSharpFiles(
        [
            View("12.0", "/r/src/SQLite.cs", "/r/tests/Test.cs"),
            View("7.3", "/r/src/SQLite.cs", "/r/src/Only.cs"),
            View("9.0", "/r/src/SQLite.cs"),
            View("12.0", "/r/src/Modern.cs"),
            View("14.0", "/r/src/Modern.cs"),
            View("preview", "/r/src/Preview.cs"),
            View("10.0", "/r/src/Preview.cs"),
        ]);

        Assert.Equal([("/r/src/Preview.cs", "10.0"), ("/r/src/SQLite.cs", "7.3")], files);
    }

    [Fact]
    public void FormatsHook_AddsTheOldCSharpList()
    {
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n</Project>\n");
        Write("select.targets", FormatCommand.SelectFrameworkTargets);
        var list = Path.Combine(root, "x.oldcsharp");
        string Items(params string[] properties) => FormatCommand.Capture(root, ["msbuild", Path.Combine(root, "App", "App.csproj"), $"-property:BeforeMicrosoftNETSdkTargets={Path.Combine(root, "select.targets")}", .. properties, "-getItem:AdditionalFiles", "-nologo"]);

        Assert.Contains("x.oldcsharp", Items($"-property:StyleBroFormatOldCSharp={list}"));
        Assert.DoesNotContain("oldcsharp", Items());
    }
}
