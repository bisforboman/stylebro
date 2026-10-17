using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the eleventh trial for the tool (Autofac, Mapster, ical.net, Fitnet).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void SdkStyleProjects_WithAStrayPackagesConfig_UsePackageReference()
    {
        // Mapster.EFCore: SDK-style with a leftover packages.config. --diff installed it the packages.config way, and the
        // Directory.Build.props condition kept the PackageReference away: the patched project didn't build.
        Write("App.sln", string.Empty);
        Write("Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("Lib/packages.config", "<packages />");
        Write("Sdk/Sdk.csproj", "<Project>\n  <Sdk Name=\"Microsoft.NET.Sdk\" />\n</Project>\n");
        Write("Sdk/packages.config", "<packages />");

        Assert.False(FormatCommand.UsesPackagesConfig(Path.Combine(root, "Lib/Lib.csproj")));
        Assert.False(FormatCommand.UsesPackagesConfig(Path.Combine(root, "Sdk/Sdk.csproj")));
        Assert.Empty(PackagesConfig.Projects(StyleCopSetup.EnumerateFiles(root)));

        var added = PreviewCommand.AddPackage(root);
        Assert.DoesNotContain("packages.config", added, StringComparison.Ordinal);
        Assert.Equal("<packages />", File.ReadAllText(Path.Combine(root, "Lib/packages.config")));
        Assert.DoesNotContain("EnsureNuGetPackageBuildImports", File.ReadAllText(Path.Combine(root, "Lib/Lib.csproj")), StringComparison.Ordinal);

        // The condition for a props file packages.config projects import lets SDK-style projects through.
        Assert.StartsWith("'$(UsingMicrosoftNETSdk)' == 'true' or ", PackagesConfig.NotPackagesConfig, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondWrite_AfterThePackageSwap_WritesTheSameBlock()
    {
        // Autofac: StyleCop ran in some projects only. After the swap every project counted: the AOT test projects (no
        // ruleset, so StyleCop's defaults) turned SA1309 on, and BRO1303 switched to 'camelCase'.
        Write("A/A.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><CodeAnalysisRuleSet>..\A.ruleset</CodeAnalysisRuleSet></PropertyGroup><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("A.ruleset", """<RuleSet Name="A" ToolsVersion="16.0"><Rules AnalyzerId="StyleCop.Analyzers" RuleNamespace="StyleCop.Analyzers"><Rule Id="SA1309" Action="None" /></Rules></RuleSet>""");
        Write("A/A.cs", "class A\n{\n    private int _a;\n    private int _b;\n}\n");
        Write("B/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("B/B.cs", "class B\n{\n}\n");
        Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("stylebro_private_field_naming = _camelCase", first, StringComparison.Ordinal);
        Assert.Contains(Migration.UnformattedPrefix + "B", first, StringComparison.Ordinal);

        Write("A/A.csproj", File.ReadAllText(Path.Combine(root, "A/A.csproj")).Replace("StyleCop.Analyzers", "StyleBro.Analyzers", StringComparison.Ordinal));
        Capture(() => Assert.Equal(0, Program.Main(new[] { root, "--write" })));

        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Format_AProjectWithoutTheRunsFramework_AsksItsReferencesForAllTheirs()
    {
        // Autofac: ScannedAssembly (netstandard2.1) references Autofac, which the net10.0 run forces onto net10.0; the tests
        // that reference ScannedAssembly didn't load ("Required references did not load").
        var projects = new Dictionary<string, (string[] Frameworks, string[] References)>
        {
            ["Autofac"] = (new[] { "net10.0", "net8.0", "netstandard2.1" }, Array.Empty<string>()),
            ["Scanned"] = (new[] { "netstandard2.1" }, new[] { "Autofac" }),
            ["Tests"] = (new[] { "net10.0", "net8.0" }, new[] { "Autofac", "Scanned" }),
            ["Other"] = (new[] { "netstandard2.0" }, Array.Empty<string>()),
        };
        var keep = FormatCommand.Keep(projects, "net10.0");

        Assert.Equal(new[] { "Scanned" }, FormatCommand.Other(projects, keep, "net10.0"));
        Assert.Empty(FormatCommand.Other(projects, FormatCommand.Keep(projects, "netstandard2.0"), "netstandard2.0"));
        Assert.Contains("StyleBroFormatLoose=true", FormatCommand.SelectFrameworkTargets, StringComparison.Ordinal);
        Assert.Contains("'$(StyleBroFormatLoose)' == 'true'", FormatCommand.SelectFrameworkTargets, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_AProjectNoRunLoaded_IsNotClean()
    {
        var lines = new List<string>();
        int Run(string[] args, Action<string>? output, IEnumerable<string> said)
        {
            foreach (var line in said)
            {
                output?.Invoke(line);
            }

            return 0;
        }

        var whole = FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["Tests"] = new() { "net10.0", "net8.0" } }, new Dictionary<string, List<string>> { ["Tests"] = new() { "net10.0", "net8.0" } });
        Assert.Contains(whole, l => l.Contains("per-framework runs are the cause", StringComparison.Ordinal));
        Assert.Equal(1, FormatCommand.Verify(new[] { "--verify-no-changes" }, root, lines.Add, (a, o) => Run(a, o, whole)));
        Assert.Equal(FormatCommand.NotVerified, lines[^1]);

        // Skipped for one framework only (no other loaded in its place): not verified either (trial 28). Because of an F#
        // reference: a warning.
        var partly = FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["Tests"] = new() { "net10.0" } }, new Dictionary<string, List<string>> { ["Tests"] = new() { "net10.0", "net8.0" } });
        Assert.Equal(1, FormatCommand.Verify(new[] { "--verify-no-changes" }, root, lines.Add, (a, o) => Run(a, o, partly)));
        var fSharp = FormatCommand.Incomplete(new Dictionary<string, SortedSet<string>> { ["Tests"] = new() { string.Empty } }, new Dictionary<string, List<string>>(), new[] { "Lib.fsproj" });
        Assert.Equal(0, FormatCommand.Verify(new[] { "--verify-no-changes" }, root, lines.Add, (a, o) => Run(a, o, fSharp)));
        Assert.DoesNotContain(fSharp, l => l.Contains("per-framework runs are the cause", StringComparison.Ordinal));
    }

    [Fact]
    public void Format_AMalformedSolutionFilter_IsAClearError()
    {
        Write("App.sln", string.Empty);
        Write("Bad.slnf", "{ \"solution\": { \"path\": ");
        Write("Good.slnf", "{ \"solution\": { \"path\": \"App.sln\", \"projects\": [] } }");
        Write("NoPath.slnf", "{ \"solution\": { } }");

        Assert.StartsWith("Bad.slnf isn't a valid solution filter (", FormatCommand.SolutionFilterError(Path.Combine(root, "Bad.slnf")), StringComparison.Ordinal);
        Assert.StartsWith("NoPath.slnf isn't a valid solution filter (", FormatCommand.SolutionFilterError(Path.Combine(root, "NoPath.slnf")), StringComparison.Ordinal);
        Assert.Null(FormatCommand.SolutionFilterError(Path.Combine(root, "Good.slnf")));

        var lines = new List<string>();
        Assert.Equal(1, FormatCommand.Run(new[] { Path.Combine(root, "Bad.slnf"), "--no-restore", FormatCommand.OnceOption }, lines.Add));
        Assert.Contains(lines, l => l.StartsWith("Bad.slnf isn't a valid solution filter", StringComparison.Ordinal));
    }

    [Fact]
    public void Preview_AFailedGitCommand_Throws_InsteadOfAnEmptyDiff()
    {
        // Fitnet: 'git add' failed on long paths, the commits were empty and the preview said "nothing to change".
        var error = Assert.Throws<GitFailedException>(() => PreviewCommand.GitChecked(root, "rev-parse", "--verify", "no-such-ref-stylebro"));
        Assert.Contains("'git rev-parse --verify no-such-ref-stylebro' failed (exit code", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_SettingsSummary_CountsOnlySuppressions_NotThePackageReference()
    {
        // Autofac: the 9 project files StyleBro.Analyzers went into counted as "27 other lines" of carried-over suppressions.
        Write("A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n</Project>\n");
        Write("A/A.cs", "class A\n{\n}\n");
        PreviewCommand.MakeRepository(root);
        var from = PreviewCommand.GitChecked(root, "rev-parse", "HEAD").Trim();
        Write("A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"StyleBro.Analyzers\" Version=\"1.0.0\" />\n  </ItemGroup>\n  <PropertyGroup><NoWarn>BRO1001</NoWarn></PropertyGroup>\n</Project>\n");
        Write("A/A.cs", "#pragma warning disable BRO1001\nclass A\n{\n}\n");
        PreviewCommand.GitChecked(root, "add", "-A");
        PreviewCommand.GitChecked(root, "-c", "user.name=t", "-c", "user.email=t@t", "commit", "-q", "-m", "settings");

        var summary = PreviewCommand.SettingsSummary(root, from, "HEAD");

        Assert.Contains("suppressions carried over: 1 #pragma, 1 <NoWarn>", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("other lines", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_OneHunkForSeveralRules_IsPrintedOnce()
    {
        var hunk = new List<string> { "-        private int _count;", "+        private int count;" };
        var lines = PreviewCommand.SampleLines(
            new[] { ("BRO1303", "New/Gadget.cs", 3, new[] { "BRO1505", "BRO1509" }), ("BRO1505", "New/Gadget.cs", 3, new[] { "BRO1303", "BRO1509" }), ("IDE0055", "B.cs", 1, Array.Empty<string>()) },
            (file, _) => file == "B.cs" ? new List<string> { "-x", "+y" } : hunk);

        Assert.Equal(
            new[] { "Sample (BRO1303, BRO1505, New/Gadget.cs; BRO1509 changed this file too):", "  -        private int _count;", "  +        private int count;", "Sample (IDE0055, B.cs):", "  -x", "  +y" },
            lines);
    }
}
