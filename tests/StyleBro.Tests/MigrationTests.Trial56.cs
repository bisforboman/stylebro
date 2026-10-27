using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-sixth trial (FluentFTP, FusionCache, NetEscapades.EnumGenerators, DynamicData) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void WithEmptyTarget_RetriesAPropertyFunctionErrorOfAQueryWithoutATarget()
    {
        // DynamicData: src/Directory.Build.targets sets DependentUpon with Regex.Replace(%(Filename), ...), and once the test
        // project had been built, its outer evaluation without a target got '.NETCoreApp,Version=v9.0.AssemblyAttributes',
        // whose comma split the arguments (MSB4186); 'bro check' stopped at its first query after format's builds. A build's
        // evaluation, and the query with a target, read the project.
        const string error = "src/Directory.Build.targets(9,7): error MSB4186: Invalid static method invocation syntax: \"[System.Text.RegularExpressions.Regex]::Replace(.NETCoreApp,Version=v9.0.AssemblyAttributes, '\\.[^\\.]+$', '.cs')\".";
        Assert.Equal(["msbuild", "a.csproj", "-getProperty:X", "-target:InitializeSourceControlInformation"], FormatCommand.WithEmptyTarget(["msbuild", "a.csproj", "-getProperty:X"], error)!);
        Assert.Null(FormatCommand.WithEmptyTarget(["msbuild", "a.csproj", "-target:Build", "-getProperty:X"], error));
    }
}
