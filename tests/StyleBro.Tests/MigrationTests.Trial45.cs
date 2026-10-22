using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the forty-fifth trial (dailydevops/guard, parlot, lambda-test-server, FASTER) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void PackageCopy_FindsAnOlderCopy_WhenThePackagesBuildFilesNameIt()
    {
        // dailydevops/guard: the committed root file was an older version of NetEvolve.Defaults' template, which the build
        // copies over it; comparing the text missed it, and the first build reported 214 findings format had called clean.
        WritePackageCopyRepository("package");
        Write(".editorconfig", TemplateConfig + "max_line_length = 120\n");
        Assert.Null(Migration.PackageCopy(root));

        Write("packages/copier.defaults/1.2.3/build/Copier.Defaults.targets", "<Project>\n  <Target Name=\"Copy\">\n    <Exec Command=\"cp -f '$(MSBuildThisFileDirectory)..\\configurations\\template.editorconfig' .editorconfig\" />\n  </Target>\n</Project>\n");
        Assert.Equal(new Migration.PackageConfig("Copier.Defaults 1.2.3", "configurations/template.editorconfig"), Migration.PackageCopy(root));

        // A package that only adds the file as a global config doesn't copy it (Meziantou.Analyzer, Mapperly's real-world run).
        Write("packages/copier.defaults/1.2.3/build/Copier.Defaults.targets", "<Project>\n  <ItemGroup>\n    <GlobalAnalyzerConfigFiles Include=\"$(MSBuildThisFileDirectory)..\\configurations\\template.editorconfig\" />\n  </ItemGroup>\n</Project>\n");
        Assert.Null(Migration.PackageCopy(root));

        // Only the package's build .props/.targets count, not other files that mention it.
        var assets = Path.Combine(root, "src", "Lib", "obj", "project.assets.json");
        File.WriteAllText(assets, File.ReadAllText(assets).Replace("\"configurations/template.editorconfig\"]", "\"configurations/template.editorconfig\", \"build/notes.txt\", \"tools/Copy.targets\"]"));
        Write("packages/copier.defaults/1.2.3/build/Copier.Defaults.targets", "<Project />\n");
        Write("packages/copier.defaults/1.2.3/build/notes.txt", "cp -f template.editorconfig .editorconfig\n");
        Write("packages/copier.defaults/1.2.3/tools/Copy.targets", "<Project><Target Name=\"C\"><Copy SourceFiles=\"template.editorconfig\" DestinationFolder=\".\" /></Target></Project>\n");
        Assert.Null(Migration.PackageCopy(root));
    }

    [Fact]
    public void SetPreset_PutsTheClosingTagOfAOneLineProjectOnItsOwnLine()
    {
        // dailydevops/guard's Directory.Build.props was '<Project></Project>': init wrote '</PropertyGroup></Project>', which
        // CSharpier's check rejected (format said clean, check didn't).
        Assert.Equal(
            "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n  </PropertyGroup>\n</Project>\n",
            Migration.SetPreset("<Project></Project>\n", "1.0", "c"));
        Assert.Equal(
            "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n  </PropertyGroup>\n</Project>",
            Migration.SetPreset("<Project> </Project>", "1.0", "c"));
    }
}
