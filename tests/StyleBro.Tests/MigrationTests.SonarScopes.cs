using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Sonar severities per part of the repository (tfplan2md lowers S3878 for its tests in a path section).</summary>
public sealed partial class MigrationTests
{
    private const string SonarPackage = """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /><PackageReference Include="StyleCop.Analyzers" Version="1.1.118" /></ItemGroup></Project>""";

    [Fact]
    public void Sonar_APathSectionLowersTheFixingRule_OnlyThere()
    {
        // tfplan2md's section: braces with paths ('{src/tests/**/*.cs,tests/**/*.cs}') weren't read at all before.
        Write("Directory.Build.props", SonarPackage);
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\n\n[{src/tests/**/*.cs,tests/**/*.cs}]\ndotnet_diagnostic.S3878.severity = suggestion\n");
        var setup = StyleCopSetup.Read(root);
        var main = Migration.Generate(setup, root, sonar: SonarSetup.Read(root));

        var plan = Migration.Plan(setup, root, main);

        Assert.Contains("dotnet_diagnostic.BRO1151.severity = warning", main.Lines);
        var (_, lines) = Assert.Single(plan[".editorconfig"], s => s.Section == "{src/tests/**/*.cs,tests/**/*.cs}");
        Assert.Equal(["dotnet_diagnostic.BRO1151.severity = suggestion"], lines);
    }

    [Fact]
    public void Sonar_ASubFolderEditorConfigTurnsTheRuleOff_TurnsTheFixingRuleOffThere()
    {
        Write("Directory.Build.props", SonarPackage);
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\n");
        Write("tests/.editorconfig", "[*.cs]\ndotnet_diagnostic.S3878.severity = none\n");
        var setup = StyleCopSetup.Read(root);
        var main = Migration.Generate(setup, root, sonar: SonarSetup.Read(root));

        var plan = Migration.Plan(setup, root, main);

        var (section, lines) = Assert.Single(plan[Path.Combine("tests", ".editorconfig")]);
        Assert.Equal("*.cs", section);
        Assert.Equal(["dotnet_diagnostic.BRO1151.severity = none"], lines);
        Assert.DoesNotContain("tests", string.Join("\n", setup.Sources), StringComparison.Ordinal);   // no StyleCop settings there
    }

    [Fact]
    public void Sonar_AScopeOverEveryStyleCopFolder_KeepsItsSonarSeverities()
    {
        // StyleCop runs in src/A only: src/.editorconfig's StyleCop severities become the base, its Sonar ones stay its own.
        Write("src/A/A.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /><PackageReference Include="StyleCop.Analyzers" Version="1.1.118" /></ItemGroup></Project>""");
        Write("src/B/B.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\n");
        Write("src/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\ndotnet_diagnostic.S3878.severity = suggestion\n");
        var setup = StyleCopSetup.Read(root);
        var main = Migration.Generate(setup, root, sonar: SonarSetup.Read(root));

        var plan = Migration.Plan(setup, root, main);

        Assert.Contains("dotnet_diagnostic.BRO1151.severity = suggestion", Assert.Single(plan[Path.Combine("src", ".editorconfig")]).Lines);
    }

    [Fact]
    public void Init_SonarSeveritiesPerFolder_GetSectionsOfTheirOwn()
    {
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\nindent_style = space\n\n[tests/**.cs]\ndotnet_diagnostic.S3878.severity = suggestion\n");
        Write("other/.editorconfig", "[*.cs]\ndotnet_diagnostic.S3878.severity = none\n");
        var sonar = SonarSetup.Read(root)!;
        var main = new List<string>();
        sonar.Apply(main, _ => true, new HashSet<string>());

        var scopes = InitCommand.SonarScopes(root, sonar, main, new HashSet<string>());

        Assert.Contains("dotnet_diagnostic.BRO1151.severity = warning", main);
        Assert.Equal(
            [
                ("other/**.cs", "dotnet_diagnostic.BRO1151.severity = none"),
                ("tests/**.cs", "dotnet_diagnostic.BRO1151.severity = suggestion"),
            ],
            scopes.Select(s => (s.Section, string.Join("\n", s.Lines.Where(l => !l.StartsWith('#'))))).OrderBy(s => s.Section, StringComparer.Ordinal));
        Assert.Contains("[tests/**.cs]\n# Sonar's severities in .editorconfig [tests/**.cs]\ndotnet_diagnostic.BRO1151.severity = suggestion", InitCommand.Block(sonar: main, sonarScopes: scopes), StringComparison.Ordinal);
    }
}
