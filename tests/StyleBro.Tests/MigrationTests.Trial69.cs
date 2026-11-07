using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the sixty-ninth trial (azure-cost-cli, dotliquid, NModbus, Arch) found in the tool.</summary>
public sealed partial class MigrationTests
{
    // Arch: 'net8.0; net6.0; netstandard2.1'. Trial 68's trim returned the list with its ';' escaped (MSBuild escapes a
    // property function's result), so the SDK's <_TargetFramework Include="$(TargetFrameworks)" /> made ONE item and the inner
    // builds got 'TargetFramework=net8.0;net6.0;netstandard2.1' (Unsupported,Version=v0.0): Arch.Tests, Arch.Samples and
    // Arch.Benchmarks, which reference it, didn't load on the host ("cannot be referenced by a project that targets
    // '.NETCoreApp,Version=v8.0'").
    [Fact]
    public void RunTargets_KeepTheInnerBuildsOfATrimmedTargetFrameworks()
    {
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFrameworks>net8.0; netstandard2.0</TargetFrameworks>\n  </PropertyGroup>\n</Project>\n");
        Write("select.targets", FormatCommand.SelectFrameworkTargets);
        var project = Path.Combine(root, "Lib", "Lib.csproj");

        var json = FormatCommand.Capture(root, "msbuild", project, $"-property:BeforeMicrosoftNETSdkTargets={Path.Combine(root, "select.targets")}", "-target:GetTargetFrameworks", "-getTargetResult:GetTargetFrameworks", "-nologo");
        Assert.Contains("\"TargetFrameworkMonikers\": \".NETCoreApp,Version=v8.0;.NETStandard,Version=v2.0\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Unsupported", json, StringComparison.Ordinal);
    }
}
