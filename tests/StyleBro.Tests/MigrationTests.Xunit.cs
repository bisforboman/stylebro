using StyleBro.Analyzers.Baseline;
using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Format_FixesXunitAssertions_WhereTheRepositoryReferencesXunit()
    {
        // Backlog 073: xunit.analyzers ships with every xunit reference; format applies its safe fixes there.
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.DoesNotContain("xUnit2004", FormatCommand.Diagnostics(root));

        // The runners and the assertion library alone bring no analyzers.
        Write("tests/Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"xunit.runner.visualstudio\" />\n    <PackageReference Include=\"xunit.assert\" />\n    <PackageReference Include=\"xunit.v3.assert\" />\n  </ItemGroup>\n</Project>\n");
        Assert.False(FormatCommand.ReferencesXunit(root));

        Write("tests/Tests/Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"xunit\" Version=\"2.9.3\" />\n  </ItemGroup>\n</Project>\n");
        var ids = FormatCommand.Diagnostics(root);
        Assert.Contains("xUnit2004", ids);
        Assert.Contains("xUnit2013", ids);
        Assert.Contains("BRO1001", ids);

        // Fixes that changed a test's meaning in the survey stay out: xUnit1013 added [Theory] to an ILogger.Log
        // implementation (CacheCow), xUnit1030 failed a test relying on the lost synchronization context (stateless).
        Assert.DoesNotContain("xUnit1013", ids);
        Assert.DoesNotContain("xUnit1030", ids);
        Assert.DoesNotContain("xUnit2002", ids);

        // Central package management, xunit.v3's metapackages and packages.config count too.
        File.Delete(Path.Combine(root, "tests", "Tests", "Tests.csproj"));
        Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"xunit.v3.mtp-v2\" Version=\"3.2.2\" />\n  </ItemGroup>\n</Project>\n");
        Assert.True(FormatCommand.ReferencesXunit(root));
        File.Delete(Path.Combine(root, "Directory.Packages.props"));
        Write("tests/Old/packages.config", "<packages>\n  <package id=\"xunit\" version=\"2.4.1\" />\n</packages>\n");
        Assert.True(FormatCommand.ReferencesXunit(root));
    }

    [Fact]
    public void Format_SaysItFixesXunitRules_OnlyWhereItDoes()
    {
        Assert.DoesNotContain("xunit", FormatCommand.RulesLine(verify: false, 140), StringComparison.Ordinal);
        Assert.Contains("xunit.analyzers' assertion rules", FormatCommand.RulesLine(verify: false, 160, xunit: true), StringComparison.Ordinal);
        Assert.StartsWith("Checking StyleBro's rules, xunit.analyzers'", FormatCommand.RulesLine(verify: true, 160, xunit: true), StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_CoversTheXunitRulesFormatFixes()
    {
        // Owner's decision 2026-10-18: 'bro baseline' records existing findings of the xUnit rules format fixes.
        var suppressed = new BaselineSuppressor().SupportedSuppressions.Select(s => s.SuppressedDiagnosticId).ToList();
        Assert.All(BaselineSuppressor.XunitIds, id => Assert.Contains(id, suppressed));
        Assert.DoesNotContain("xUnit1013", suppressed);

        var report = $$"""[{"FilePath":{{System.Text.Json.JsonSerializer.Serialize(Path.Combine(root, "T.cs"))}},"FileChanges":[{"LineNumber":1,"CharNumber":1,"DiagnosticId":"xUnit2004","FormatDescription":"x"}]}]""";
        Write("T.cs", "Assert.Equal(true, x);\n");
        var result = BaselineCommand.Build(root, report);
        Assert.Empty(result.NotCovered);
        Assert.Equal(1, result.PerRule["xUnit2004"]);
    }
}
