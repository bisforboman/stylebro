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

    [Fact]
    public void PreviewOfMigrate_ReadsThePackageGlobalConfigFromTheRepositorysRestore()
    {
        // Trial 48, Pidgin: the settings step ran in a copy without restore output, so even in a restored repository the
        // preview couldn't see the package's global config (SA1101 off there) and showed 71 files with 'this.' added.
        var package = Directory.CreateTempSubdirectory("bro-package-").FullName;
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        try
        {
            var config = Path.Combine(package, "Build.globalconfig");
            File.WriteAllText(config, "is_global = true\ndotnet_diagnostic.SA1101.severity = none\n");
            Write("Directory.Packages.props", "<Project>\n  <ItemGroup>\n    <GlobalPackageReference Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" />\n    <GlobalPackageReference Include=\"BuildScripts\" Version=\"2.4.0\" />\n  </ItemGroup>\n</Project>\n");
            Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n  </PropertyGroup>\n</Project>\n");
            Write("App/C.cs", "class C\n{\n}\n");
            Write(".gitignore", "obj/\n");
            Write("App/obj/project.assets.json", "{}");
            Write("App/obj/App.csproj.nuget.g.props", $"<Project>\n  <ItemGroup>\n    <GlobalAnalyzerConfigFiles Include=\"{config}\" />\n  </ItemGroup>\n</Project>\n");
            PreviewCommand.MakeRepository(root);
            int Format(string[] args, Action<string> log)
            {
                var report = Array.IndexOf(args, "--report");
                if (report >= 0)
                {
                    Directory.CreateDirectory(args[report + 1]);
                    File.WriteAllText(Path.Combine(args[report + 1], "format-report.json"), "[]");
                }

                return 0;
            }

            var output = Capture(() => Assert.Equal(0, PreviewCommand.Run("migrate", new[] { root, "--project", "App/App.csproj", "--diff=" + patch }, Format)));
            Assert.DoesNotContain(StyleCopSetup.NotRestoredWarning, output, StringComparison.Ordinal);
            Assert.Contains("+dotnet_diagnostic.IDE0009.severity = none", File.ReadAllText(patch), StringComparison.Ordinal);
            Assert.DoesNotContain("nuget.g.props", File.ReadAllText(patch), StringComparison.Ordinal);

            // Without the restore output the preview says what migrate says.
            DeleteFolder(Path.Combine(root, "App", "obj"));
            output = Capture(() => Assert.Equal(0, PreviewCommand.Run("migrate", new[] { root, "--project", "App/App.csproj", "--diff=" + patch }, Format)));
            Assert.Contains($"{StyleCopSetup.NotRestoredWarning} App/App.csproj.", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(package, recursive: true);
            Directory.Delete(Path.GetDirectoryName(patch)!, recursive: true);
        }
    }

    [Fact]
    public void Copy_LeavesUntrackedFilesOfExtractedPackagesBehind()
    {
        // Trial 48, Cronos: its packages folder is inside the repository and '*.nupkg' and 'build/' are ignored, so the copy got
        // each package's '.nupkg.sha512' alone; NuGet took them as installed and net40's reference assemblies were missing.
        Write(".gitignore", "*.nupkg\nbuild/\n");
        Write("App/C.cs", "class C\n{\n}\n");
        Write("packages/net40/1.0.3/net40.1.0.3.nupkg", string.Empty);
        Write("packages/net40/1.0.3/net40.1.0.3.nupkg.sha512", "x");
        Write("packages/net40/1.0.3/build/net40.targets", "<Project />");
        Write("lib/old/1.0.0/old.1.0.0.nupkg", string.Empty);
        Write("lib/old/1.0.0/lib/Old.dll", "x");
        PreviewCommand.Git(root, "init", "-q");
        PreviewCommand.Git(root, "add", "App", ".gitignore", "lib/old/1.0.0/lib/Old.dll");
        var copy = Directory.CreateTempSubdirectory("bro-copy-").FullName;
        try
        {
            PreviewCommand.Copy(root, copy);

            Assert.True(File.Exists(Path.Combine(copy, "App", "C.cs")));
            Assert.False(File.Exists(Path.Combine(copy, "packages", "net40", "1.0.3", "net40.1.0.3.nupkg.sha512")));
            Assert.True(File.Exists(Path.Combine(copy, "lib", "old", "1.0.0", "lib", "Old.dll")));
        }
        finally
        {
            DeleteFolder(copy);
        }
    }
}
