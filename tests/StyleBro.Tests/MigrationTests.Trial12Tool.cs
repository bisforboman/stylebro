using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Findings of the twelfth trial for the tool (Steeltoe, sourcegit).</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void UnformattedFolders_AreReadFromTheBlockAboveASolutionInASubfolder()
    {
        // Steeltoe: the block is in the repository root, the solution in src/. Format ran from src/ and read only
        // src/.editorconfig, so it reformatted the vendored src/Tools/src.
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Write(".editorconfig", $"root = true\n\n{Migration.BeginMarker}\n[*.cs]\n{Migration.UnformattedLine(["src/Tools/src", "other"])}\n{Migration.VersionPrefix}1.1.118 (x)\n{Migration.EndMarker}\n");
        Write("src/.editorconfig", "[*.cs]\nindent_size = 4\n");

        Assert.Equal(root, Migration.BlockFolder(Path.Combine(root, "src")));
        Assert.Equal(["Tools/src/"], Migration.UnformattedFolders(Path.Combine(root, "src"))); // 'other' isn't below src/
        Assert.Equal(["src/Tools/src/", "other/"], Migration.UnformattedFolders(root));
        Assert.Equal("1.1.118", Migration.RecordedVersion(Path.Combine(root, "src")));

        // The search stops at the repository's top.
        Directory.Delete(Path.Combine(root, ".git"));
        Directory.CreateDirectory(Path.Combine(root, "src", ".git"));
        Assert.Equal(Path.Combine(root, "src"), Migration.BlockFolder(Path.Combine(root, "src")));
        Assert.Empty(Migration.UnformattedFolders(Path.Combine(root, "src")));
    }

    [Fact]
    public void PackageVersions_FromProperties_AreResolved()
    {
        // Steeltoe: Version="$(StyleCopVersion)" with the value in versions.props; the report and the block said
        // '$(StyleCopVersion)'.
        Write("versions.props", "<Project><PropertyGroup>\n  <SonarAnalyzerVersion>10.33.0.1635</SonarAnalyzerVersion>\n  <!-- <StyleCopVersion>1.0.0</StyleCopVersion> -->\n  <StyleCopVersion>$(OldStyleCop)</StyleCopVersion>\n  <OldStyleCop></OldStyleCop>\n  <OldStyleCop>1.1.118</OldStyleCop>\n</PropertyGroup></Project>");
        Write("shared.props", "<Project><ItemGroup>\n  <PackageReference Include=\"StyleCop.Analyzers\" Version=\"$(StyleCopVersion)\" />\n  <PackageReference Include=\"SonarAnalyzer.CSharp\" Version=\"$(SonarAnalyzerVersion)\" />\n</ItemGroup></Project>");
        Write("A/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><Import Project=\"../shared.props\" /></Project>");

        var setup = StyleCopSetup.Read(root);
        Assert.Equal("1.1.118", setup.Version);
        Assert.Null(setup.UnresolvedVersion);
        Assert.Contains(SonarSetup.Read(root)!.Sources, s => s.StartsWith("SonarAnalyzer.CSharp 10.33.0.1635 ", StringComparison.Ordinal));

        // Not defined anywhere: never the property's text, and the report says so.
        Write("versions.props", "<Project />");
        setup = StyleCopSetup.Read(root);
        Assert.Null(setup.Version);
        Assert.Equal("$(StyleCopVersion)", setup.UnresolvedVersion);
        Assert.DoesNotContain("$(", string.Concat(Migration.Plan(setup, root, Migration.Generate(setup, root)).SelectMany(p => p.Value).Select(s => Migration.Render([s]))), StringComparison.Ordinal);
        Assert.Contains(SonarSetup.Read(root)!.Sources, s => s.StartsWith("SonarAnalyzer.CSharp (version unknown) ", StringComparison.Ordinal));
        Assert.Contains("unknown (the reference says $(StyleCopVersion)", Capture(() => Program.Migrate([root])), StringComparison.Ordinal);
    }

    [Fact]
    public void ASolutionWithSeveralFiltersOfIt_IsTheWorkspace()
    {
        // Steeltoe: src/Steeltoe.All.slnx and seven .slnf parts of it. Every command asked for --project.
        Write("src/All.slnx", "<Solution />");
        Write("src/A.slnf", "{ \"solution\": { \"path\": \"All.slnx\", \"projects\": [] } }");
        Write("src/B.slnf", "{ \"solution\": { \"path\": \"All.sln\", \"projects\": [] } }");

        Assert.Equal(Path.Combine("src", "All.slnx"), BaselineCommand.FindWorkspace(root));
        Assert.Equal("Using src/All.slnx: no solution or project at the root, and it's the only solution one folder down; its solution filters (src/A.slnf, src/B.slnf) are parts of it: name one to run on only its projects, e.g. 'stylebro-migrate format src/A.slnf'.", BaselineCommand.PickedNote(root, BaselineCommand.FindWorkspace(root)!));
        Assert.Equal("All.slnx", BaselineCommand.FindWorkspace(Path.Combine(root, "src")));
        Assert.StartsWith("Using All.slnx; its solution filters (A.slnf, B.slnf)", BaselineCommand.PickedNote(Path.Combine(root, "src"), "All.slnx"), StringComparison.Ordinal);

        // A filter of another solution (or a broken one) keeps the choice open.
        Write("src/C.slnf", "{ \"solution\": { \"path\": \"Other.slnx\", \"projects\": [] } }");
        Assert.Null(BaselineCommand.FindWorkspace(Path.Combine(root, "src")));
        File.Delete(Path.Combine(root, "src/C.slnf"));
        Write("src/C.slnf", "{ \"solution\": ");
        Assert.Null(BaselineCommand.FindWorkspace(Path.Combine(root, "src")));
    }

    [Fact]
    public void Baseline_TakesASolutionOrProjectFirst_LikeFormat()
    {
        // sourcegit: 'baseline src/SourceGit.csproj' said "Not a directory"; with --project it said the project was picked.
        Write("src/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Assert.Equal((root, Path.Combine("src", "App.csproj")), BaselineCommand.Arguments(["src/App.csproj"], root));
        Assert.Equal((Path.Combine(root, "src"), "App.csproj"), BaselineCommand.Arguments([Path.Combine(root, "src/App.csproj")], Path.Combine(root, "other")));
        Assert.Equal((root, "src/App.csproj"), BaselineCommand.Arguments([".", "--project", "src/App.csproj"], root));
        Assert.Equal((Path.Combine(root, "src"), (string?)null), BaselineCommand.Arguments(["src"], root));
    }

    [Fact]
    public void CiSteps_WithPlainVerifyNoChanges_GetANote()
    {
        // sourcegit's format-check.yml failed after adoption on the findings StyleBro keeps.
        Write(".github/workflows/format-check.yml", "jobs:\n  check:\n    steps:\n      # run: dotnet format --verify-no-changes\n      - run: dotnet format --verify-no-changes src/SourceGit.csproj\n      - run: stylebro-migrate format --verify-no-changes\n");
        Write("azure-pipelines.yml", "steps:\n- script: dotnet build\n");

        var notes = Program.VerifyNoChangesSteps(root);
        Assert.StartsWith(".github/workflows/format-check.yml:5 runs 'dotnet format --verify-no-changes'", Assert.Single(notes), StringComparison.Ordinal);
        Assert.Empty(Program.VerifyNoChangesSteps(Path.Combine(root, ".github")));
    }

    [Fact]
    public void RestoreHint_ForAMissingStyleBroPackage_NamesTheSourceAndItsMapping()
    {
        var hint = FormatCommand.RestoreHintFor([@"C:\r\App.csproj : error NU1102: Unable to find package StyleBro.Analyzers with version (>= 0.6.0-dev.1) [C:\r\App.slnx]"]);
        Assert.Contains("<packageSources>", hint, StringComparison.Ordinal);
        Assert.Contains("<package pattern=\"StyleBro.*\" />", hint, StringComparison.Ordinal);
        Assert.DoesNotContain("<packageSources>", FormatCommand.RestoreHintFor([@"C:\r\App.csproj : error NU1102: Unable to find package Serilog with version (>= 9.0.0)"]), StringComparison.Ordinal);
    }
}
