using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fifty-seventh trial (Newtonsoft.Json.Bson, OpenAPI.NET, ErrorProne.NET, DapperAOT) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void FirstBuildError_IsTheMessageOfTheFirstErrorLine()
    {
        // DapperAOT's design-time build in a shallow clone: Nerdbank.GitVersioning's task fails, then says why.
        const string output = "  Restore complete.\n"
            + "/x/Lib.csproj : warning NU1603: Something resolved instead. [/x/Lib.csproj]\n"
            + "/nuget/nerdbank.gitversioning/build/Nerdbank.GitVersioning.targets(80,5): error : build ('-2') must be a non-negative value. (Parameter 'build') [/x/Lib.csproj]\n"
            + "/x/Lib.csproj(9,5): error MSB4018: The second one. [/x/Lib.csproj]\n";
        Assert.Equal("build ('-2') must be a non-negative value. (Parameter 'build')", FormatCommand.FirstBuildError(output));
        Assert.Equal("The second one.", FormatCommand.FirstBuildError("/x/Lib.csproj(9,5): error MSB4018: The second one. [/x/Lib.csproj]"));
        Assert.Null(FormatCommand.FirstBuildError("/x/Lib.csproj : warning NU1603: Something. [/x/Lib.csproj]\n"));
    }

    [Fact]
    public void LoadFailureGap_NamesTheRuns_AndCounts()
    {
        var gap = FormatCommand.LoadFailureGap("Dapper.AOT.Analyzers", ["netstandard2.0", "netstandard2.0, Release"], "build ('-2') must be a non-negative value.");
        Assert.Equal("Not checked: Dapper.AOT.Analyzers (netstandard2.0; netstandard2.0, Release): MSBuild failed loading it (build ('-2') must be a non-negative value), so 'dotnet format' had none of its build settings and StyleBro's rules didn't run there", gap);
        Assert.StartsWith("Not checked: Lib: ", FormatCommand.LoadFailureGap("Lib", [], "x"), StringComparison.Ordinal);
        Assert.Single(FormatCommand.Unverified([gap]));
    }
}
