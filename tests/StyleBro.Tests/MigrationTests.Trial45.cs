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

        // Only the package's build files count, not any file that mentions it.
        Write("packages/copier.defaults/1.2.3/build/Copier.Defaults.targets", "<Project />\n");
        Write("packages/copier.defaults/1.2.3/README.md", "copies template.editorconfig\n");
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
