using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void AddIgnoredSettings_LeavesOutTheEditorConfigsOfExtractedPackages()
    {
        // Trial 48, Cronos: NuGet's packages folder inside the repository ('%USERPROFILE%/...', not expanded off Windows) is
        // excluded in the preview's copy, and its analyzer packages' .editorconfig files came back in as settings.
        PreviewCommand.Git(root, "init", "-q");
        Write(".gitignore", "packages/\n");
        Write("src/.editorconfig", "[*.cs]\n");
        Write("packages/analyzers/3.3.3/analyzers.3.3.3.nupkg", string.Empty);
        Write("packages/analyzers/3.3.3/editorconfig/AllRulesDefault/.editorconfig", "[*.cs]\n");
        Write("packages/other/1.0.0/.nupkg.metadata", "{}");
        Write("packages/other/1.0.0/content/.editorconfig", "[*.cs]\n");
        Write("packages/.editorconfig", "[*.cs]\n");
        Write(".git/info/exclude", "src/.editorconfig\n");

        PreviewCommand.AddIgnoredSettings(root);

        var staged = PreviewCommand.Git(root, "diff", "--cached", "--name-only").Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(["packages/.editorconfig", "src/.editorconfig"], staged.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Read_CountsTheGlobalConfigAPackageAdds()
    {
        // Trial 48, Pidgin: Benjamin.Pizza.BuildScripts' globalconfig (GlobalAnalyzerConfigFiles) turns SA1200 and SA1101 off;
        // read as StyleCop's defaults, the first format moved 100 files' usings and qualified 71 with 'this.'.
        var package = Directory.CreateTempSubdirectory("bro-package-").FullName;
        try
        {
            var config = Path.Combine(package, "Build.globalconfig");
            File.WriteAllText(config, "is_global = true\ndotnet_diagnostic.SA1200.severity = none\ndotnet_diagnostic.SA1101.severity = none\n");
            Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" />\n    <GlobalPackageReference Include=\"BuildScripts\" Version=\"2.4.0\" />\n  </ItemGroup>\n</Project>\n");
            Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
            Write("Repo.globalconfig", "is_global = true\ndotnet_diagnostic.SA1124.severity = none\n");

            var read = new List<string>();
            var setup = StyleCopSetup.Read(root, packageConfigs: project => { read.Add(Path.GetFileName(project)); return [config, Path.Combine(root, "Repo.globalconfig")]; });

            Assert.Equal(["App.csproj"], read);
            Assert.False(setup.IsOn("SA1200"));
            Assert.False(setup.IsOn("SA1101"));
            Assert.False(setup.IsOn("SA1124"));
            Assert.True(setup.IsOn("SA1201"));
            Assert.Contains($"{config} (a package's)", setup.Sources);
            Assert.Single(setup.Sources, s => s.Contains("Repo.globalconfig", StringComparison.Ordinal));

            // Without restore output the package's config can't be seen: said, and StyleCop's defaults stay.
            var unrestored = StyleCopSetup.Read(root, packageConfigs: _ => null);
            Assert.True(unrestored.IsOn("SA1200"));
            Assert.Contains(unrestored.Warnings, w => w.StartsWith("Not restored: App/App.csproj.", StringComparison.Ordinal));

            // Only StyleCop's own package referenced: nothing a restore could add, no warning.
            Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" />\n  </ItemGroup>\n</Project>\n");
            Assert.DoesNotContain(StyleCopSetup.Read(root, packageConfigs: _ => null).Warnings, w => w.StartsWith("Not restored", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(package, recursive: true);
        }
    }

    [Fact]
    public void Restored_FindsTheRestoreOutputNextToTheProject_OrInAnArtifactsFolder()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        var project = Path.Combine(root, "src", "App", "App.csproj");
        Assert.False(StyleCopSetup.Restored(project, root));

        Write("artifacts/obj/App/App.csproj.nuget.g.props", "<Project />\n");
        Assert.True(StyleCopSetup.Restored(project, root));

        DeleteFolder(Path.Combine(root, "artifacts"));
        Write("src/App/obj/App.csproj.nuget.g.props", "<Project />\n");
        Assert.True(StyleCopSetup.Restored(project, root));
    }
}
