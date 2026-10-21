using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the fortieth trial (YetAnotherHttpHandler, ImageSharp.Web, Blazored.Toast, workflow-core) found in the tool.</summary>
public sealed partial class MigrationTests
{
    private const string SharedConfig = "root = true\n\n[*.cs]\nindent_size = 4\n";

    [Fact]
    public void Migrate_WritesItsBlockIntoTheProjectFolders_WhenTheBuildCopiesTheRootEditorConfigFromASubmodule()
    {
        // ImageSharp.Web: SixLaborsCopyConfigFiles copies shared-infrastructure/.editorconfig over the root one before every
        // build. migrate's block in the root file was gone after the first build, and the TWAE build reported 897 findings
        // format and check had called clean.
        WriteSyncedRepository();
        Write("Directory.Build.props", "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" />\n  </ItemGroup>\n</Project>\n");

        var output = Capture(() => Assert.Equal(0, Program.Main([root, "--write"])));

        Assert.Contains("The root .editorconfig is a copy of shared/.editorconfig (a git submodule's), which the build puts back: the settings go into src/.editorconfig, tests/.editorconfig instead", output);
        Assert.Equal(SharedConfig, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        foreach (var folder in new[] { "src", "tests" })
        {
            var text = File.ReadAllText(Path.Combine(root, folder, ".editorconfig"));
            Assert.StartsWith(Migration.BeginMarker, text);
            Assert.DoesNotContain("root = true", text);
        }

        // A second run finds its block there and writes the same.
        var src = File.ReadAllText(Path.Combine(root, "src", ".editorconfig"));
        Assert.Equal(Path.Combine(root, "src", ".editorconfig"), Migration.BlockFile(root));
        Assert.True(InitCommand.IsMigrated(root));
        Capture(() => Assert.Equal(0, Program.Main([root, "--write"])));
        Assert.Equal(src, File.ReadAllText(Path.Combine(root, "src", ".editorconfig")));
    }

    [Fact]
    public void Init_WritesItsBlockIntoTheProjectFolders_WhenTheBuildCopiesTheRootEditorConfigFromASubmodule()
    {
        WriteSyncedRepository();

        var output = Capture(() => Assert.Equal(0, Program.Main(["init", root, "--write"])));

        Assert.Contains("which the build puts back: the settings go into src/.editorconfig, tests/.editorconfig instead", output);
        Assert.Equal(SharedConfig, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        var text = File.ReadAllText(Path.Combine(root, "src", ".editorconfig"));
        Assert.StartsWith(Migration.BeginMarker + " (stylebro-migrate init", text);
        Assert.DoesNotContain("root = true", text);
        Assert.Equal(text, File.ReadAllText(Path.Combine(root, "tests", ".editorconfig")));
    }

    [Fact]
    public void Synced_NeedsTheSubmodulesText_AndEveryProjectInAFolder()
    {
        WriteSyncedRepository();
        var synced = Migration.Synced(root);
        Assert.Equal("shared/.editorconfig", synced?.Source);
        Assert.Equal(["src", "tests"], synced?.Folders);

        // A project at the root sees only the root's file.
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Assert.Null(Migration.Synced(root));
        File.Delete(Path.Combine(root, "App.csproj"));

        // The repository's own text (not the submodule's): written as before.
        Write(".editorconfig", SharedConfig + "dotnet_diagnostic.SA1101.severity = none\n");
        Assert.Null(Migration.Synced(root));
        Assert.Equal(Path.Combine(root, ".editorconfig"), Migration.BlockFile(root));
    }

    [Fact]
    public void Rebase_KeepsTheFoldersOwnSections()
    {
        const string Block = "# BEGIN stylebro-migrate\n[*.cs]\na = 1\n\n[src/**.cs]\nb = 2\n\n[tests/Unit/*.cs]\nc = 3\n\n[{src,samples}/Gen/**]\nd = 4\n\n[**/Migrations/**]\ne = 5\n# END stylebro-migrate\n";

        Assert.Equal("# BEGIN stylebro-migrate\n[*.cs]\na = 1\n\n[**.cs]\nb = 2\n\n[Gen/**]\nd = 4\n\n[**/Migrations/**]\ne = 5\n# END stylebro-migrate\n", Migration.Rebase(Block, "src"));
        Assert.Equal("# BEGIN stylebro-migrate\n[*.cs]\na = 1\n\n[Unit/*.cs]\nc = 3\n\n[**/Migrations/**]\ne = 5\n# END stylebro-migrate\n", Migration.Rebase(Block, "tests"));
    }

    private void WriteSyncedRepository()
    {
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write(".gitmodules", "[submodule \"shared\"]\n\tpath = shared\n\turl = ../shared\n");
        Write("shared/.git", "gitdir: ../.git/modules/shared\n");
        Write("shared/.editorconfig", SharedConfig);
        Write(".editorconfig", SharedConfig);
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/Code.cs", "namespace Lib;\n\npublic class Code\n{\n}\n");
        Write("tests/Lib.Tests/Lib.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
    }
}
