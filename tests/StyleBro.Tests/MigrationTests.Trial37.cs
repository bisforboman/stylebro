using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the thirty-seventh trial (MSBuildSdks, Xunit.Combinatorial, OneOf, shouldly) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void WithoutRestore_TurnsTheRestoreOffForMSBuildQueriesOnly()
    {
        Assert.Equal(["msbuild", "-restore:false", "a.csproj", "-getProperty:X"], FormatCommand.WithoutRestore(["msbuild", "a.csproj", "-getProperty:X"]));
        Assert.Equal(["msbuild", "a.csproj", "-restore"], FormatCommand.WithoutRestore(["msbuild", "a.csproj", "-restore"]));
        Assert.Equal(["restore", "a.csproj"], FormatCommand.WithoutRestore(["restore", "a.csproj"]));
    }

    [Fact]
    public void Capture_AnMSBuildQuery_DoesNotRestoreForADirectoryBuildRsp()
    {
        // MSBuildSdks: Directory.Build.rsp says /Restore, so every query restored; one with -property:TargetFramework=net9.0
        // wrote a net9.0 restore output for a netstandard2.0 project (NU1109), which 'dotnet format' then couldn't load.
        // A query that runs a target (CompileSettings) restores then; a plain -getProperty doesn't.
        Write("Directory.Build.rsp", "/Restore\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n</Project>\n");

        Assert.Contains("net8.0", FormatCommand.Capture(root, ["msbuild", Path.Combine(root, "App", "App.csproj"), "-target:AddImplicitDefineConstants", "-getProperty:TargetFramework", "-nologo"]));
        Assert.False(File.Exists(Path.Combine(root, "App", "obj", "project.assets.json")));
    }
}
