using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the sixty-sixth trial (jwt, moq4, NetEscapades.AspNetCore.SecurityHeaders, FluentBuilder) found in the tool.</summary>
public sealed partial class MigrationTests
{
    // moq4: a Directory.Build.targets drops net48 off Windows, so a test project's <TargetFrameworks>net48;net10.0</TargetFrameworks>
    // evaluates to "net10.0": one run for the whole solution, no framework in it. The probe built the project without a
    // framework, got the outer build ("The target CoreCompile does not exist") and said "Not checked" for a project that loaded.
    [Fact]
    public void LoadProbeFramework_NamesTheOnlyFramework_OfARunWithoutOne()
    {
        Assert.Equal("net10.0", FormatCommand.LoadProbeFramework(string.Empty, ["net10.0"]));
        Assert.Equal("net8.0", FormatCommand.LoadProbeFramework(string.Empty, ["net8.0", "net48"]));
        Assert.Equal(string.Empty, FormatCommand.LoadProbeFramework(string.Empty, []));
    }

    [Fact]
    public void LoadProbeFramework_TakesTheProjectsSpelling_OfTheRunsFramework()
    {
        Assert.Equal("net462", FormatCommand.LoadProbeFramework(FormatCommand.NormalizeFramework("net462"), ["net462", "net8.0"]));
        Assert.Equal("net6.0", FormatCommand.LoadProbeFramework("net6.0", ["net8.0"]));
    }
}
