using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Imports through MSBuild's GetDirectoryNameOfFileAbove (OpenTelemetry, measured 2026-11-07).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void GetDirectoryNameOfFileAbove_IsFollowed_SoTheRulesetIsRead()
    {
        // OpenTelemetry: src/Directory.Build.props imports build/Common.prod.props this way, which sets the ruleset. Not
        // followed, SA1512 and SA1600 counted as on: BRO1506 and BRO1601 changed 209 files of a StyleCop-clean repository.
        Write("OpenTelemetry.slnx", "<Solution />");
        Write("build/Common.prod.props", "<Project>\n  <PropertyGroup>\n    <CodeAnalysisRuleSet>$(MSBuildThisFileDirectory)/OpenTelemetry.prod.ruleset</CodeAnalysisRuleSet>\n  </PropertyGroup>\n</Project>\n");
        Write("build/OpenTelemetry.prod.ruleset", "<RuleSet Name=\"r\" ToolsVersion=\"15.0\"><Rules AnalyzerId=\"StyleCop.Analyzers\" RuleNamespace=\"StyleCop.Analyzers\"><Rule Id=\"SA1512\" Action=\"None\" /></Rules></RuleSet>\n");
        Write("src/Directory.Build.props", "<Project>\n  <Import Project=\"$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'OpenTelemetry.slnx'))\\build\\Common.prod.props\" />\n</Project>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.3.151-beta\" /></ItemGroup></Project>\n");

        Assert.Equal([Path.Combine(root, "build", "Common.prod.props")], StyleCopSetup.Imports(Path.Combine(root, "src", "Directory.Build.props"), root));
        var config = StyleCopSetup.ProjectConfig(Path.Combine(root, "src", "Lib", "Lib.csproj"), root);
        Assert.Equal(Path.Combine(root, "build", "OpenTelemetry.prod.ruleset"), config.Ruleset);

        // Unquoted, and with the start folder written out.
        Write("src/Directory.Build.props", "<Project><Import Project=\"$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildThisFileDirectory), OpenTelemetry.slnx))/build/Common.prod.props\" /></Project>\n");
        Assert.Equal([Path.Combine(root, "build", "Common.prod.props")], StyleCopSetup.Imports(Path.Combine(root, "src", "Directory.Build.props"), root));

        // A file that isn't above: nothing is imported.
        Write("src/Directory.Build.props", "<Project><Import Project=\"$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Missing.slnx'))\\build\\Common.prod.props\" /></Project>\n");
        Assert.Empty(StyleCopSetup.Imports(Path.Combine(root, "src", "Directory.Build.props"), root));
    }
}
