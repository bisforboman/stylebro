using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the forty-fourth trial (charls-dotnet, NJsonSchema, dailydevops/arguments, MessagePack-CSharp) found in the tool.</summary>
public sealed partial class MigrationTests
{
    private const string TemplateConfig = "root = true\n\n[*.cs]\nindent_size = 4\n";

    [Fact]
    public void PackageCopy_FindsThePackageFileTheRootEditorConfigCopies_WithOrWithoutTheBlock()
    {
        // dailydevops/arguments: NetEvolve.Defaults' build copies its template.editorconfig over the root file before every
        // build. init's block was gone after the first build, which reported 610 findings format and check had called clean.
        WritePackageCopyRepository("package");

        Assert.Equal(new Migration.PackageConfig("Copier.Defaults 1.2.3", "configurations/template.editorconfig"), Migration.PackageCopy(root));

        // With the block written (and other blank lines), it is still the package's text around it.
        Write(".editorconfig", TemplateConfig.Replace("\n\n", "\n\n\n") + "\n" + Migration.BeginMarker + "\n[*.cs]\nx = 1\n" + Migration.EndMarker + "\n");
        Assert.NotNull(Migration.PackageCopy(root));

        // The repository's own text: not a copy.
        Write(".editorconfig", TemplateConfig + "dotnet_diagnostic.SA1101.severity = none\n");
        Assert.Null(Migration.PackageCopy(root));
    }

    [Fact]
    public void PackageCopy_OnlyCountsPackages()
    {
        // A project reference listed in the restore output isn't a package the build copies from.
        WritePackageCopyRepository("project");
        Assert.Null(Migration.PackageCopy(root));
    }

    [Fact]
    public void Init_SaysWhenTheBuildPutsAPackagesEditorConfigBack()
    {
        WritePackageCopyRepository("package");

        var output = Capture(() => Assert.Equal(0, Program.Main(["init", root, "--write"])));

        Assert.Contains("Note: the root .editorconfig is a copy of Copier.Defaults 1.2.3's configurations/template.editorconfig, and that package's build copies it back", output);
        Assert.Contains(Migration.BeginMarker, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void PackageCopyGap_MakesTheVerdictNotVerified()
    {
        var gap = Migration.PackageCopyGap(new Migration.PackageConfig("Copier.Defaults 1.2.3", "template.editorconfig"));

        Assert.True(FormatCommand.IsGapLine(gap));
        Assert.Equal([gap], FormatCommand.Unverified([gap]));
    }

    private void WritePackageCopyRepository(string type)
    {
        var packages = Path.Combine(root, "packages");
        Write("packages/copier.defaults/1.2.3/configurations/template.editorconfig", TemplateConfig);
        Write(".editorconfig", TemplateConfig);
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/obj/project.assets.json", $$"""
            {
              "version": 3,
              "libraries": {
                "Copier.Defaults/1.2.3": {
                  "type": "{{type}}",
                  "path": "copier.defaults/1.2.3",
                  "files": ["build/Copier.Defaults.targets", "configurations/template.editorconfig"]
                }
              },
              "packageFolders": { "{{packages.Replace("\\", "\\\\")}}/": {} }
            }
            """);
    }
}
