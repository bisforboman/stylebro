using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 30's findings in stylebro-migrate.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void MissingWorkloadPacks_ReadsTheSdksItems()
    {
        // AvaloniaVisualBasic6: the Android, iOS and Browser heads need workloads this machine doesn't have; 'dotnet restore'
        // failed with NETSDK1147 and stopped the whole format run. The evaluation lists the missing packs.
        const string Android = """{ "Properties": { "TargetFramework": "net9.0-android" }, "Items": { "MissingWorkloadPack": [ { "Identity": "Microsoft.Android.Sdk.net9", "Version": "35.0.105" }, { "Identity": "Microsoft.Android.Sdk.net9" } ] } }""";
        Assert.Equal(["Microsoft.Android.Sdk.net9"], FormatCommand.MissingWorkloadPacks(Android));
        Assert.Empty(FormatCommand.MissingWorkloadPacks("""{ "Properties": { "TargetFrameworks": "net9.0" }, "Items": { "MissingWorkloadPack": [] } }"""));
        Assert.Empty(FormatCommand.MissingWorkloadPacks("""{ "Properties": { "TargetFramework": "net9.0" } }"""));
    }

    [Fact]
    public void MissingWorkloadsLine_NamesTheProjectsAndTheCommand()
    {
        var line = FormatCommand.MissingWorkloadsLine(["A.Android/A.Android.csproj", "A.iOS/A.iOS.csproj"], ["Microsoft.iOS.Sdk", "Microsoft.Android.Sdk", "Microsoft.iOS.Sdk"]);
        Assert.StartsWith("Leaving alone: A.Android/A.Android.csproj, A.iOS/A.iOS.csproj: they need workloads", line);
        Assert.Contains("(Microsoft.Android.Sdk, Microsoft.iOS.Sdk)", line);
        Assert.Contains("'dotnet workload restore'", line);
        Assert.Contains("'format' leaves it alone", FormatCommand.MissingWorkloadsLine(["A.Android/A.Android.csproj"], ["Microsoft.Android.Sdk"]));
    }
}
