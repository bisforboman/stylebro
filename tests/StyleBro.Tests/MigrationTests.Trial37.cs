using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the thirty-seventh trial (MSBuildSdks, Xunit.Combinatorial, OneOf, shouldly) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void WithoutRestore_TurnsTheRestoreOffForMSBuildQueriesOnly()
    {
        Assert.Equal(["msbuild", "-restore:false", "a.csproj", "-getProperty:X"], FormatCommand.WithoutRestore(["msbuild", "a.csproj", "-getProperty:X"]));
        Assert.Equal(["msbuild", "a.csproj", "-restore"], FormatCommand.WithoutRestore(["msbuild", "a.csproj", "-restore"]));
        Assert.Equal(["restore", "a.csproj"], FormatCommand.WithoutRestore(["restore", "a.csproj"]));
    }

    [Fact]
    public void Capture_AnMSBuildQuery_DoesNotRestoreForADirectoryBuildRsp()
    {
        // MSBuildSdks: Directory.Build.rsp says /Restore, so every query restored; one with -property:TargetFramework=net9.0
        // wrote a net9.0 restore output for a netstandard2.0 project (NU1109), which 'dotnet format' then couldn't load.
        // A query that runs a target (CompileSettings) restores then; a plain -getProperty doesn't.
        Write("Directory.Build.rsp", "/Restore\n");
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n</Project>\n");

        Assert.Contains("net8.0", FormatCommand.Capture(root, "msbuild", Path.Combine(root, "App", "App.csproj"), "-target:AddImplicitDefineConstants", "-getProperty:TargetFramework", "-nologo"));
        Assert.False(File.Exists(Path.Combine(root, "App", "obj", "project.assets.json")));
    }

    [Fact]
    public void PackageTargets_ReadTheSnippetNamesOfTheRepositorysMarkdown()
    {
        // shouldly: mdsnippets.json in src/, the docs in documentation/ at the repository root.
        Write(".git/HEAD", "ref: refs/heads/main\n");
        Write("src/mdsnippets.json", "{}\n");
        Write("docs/a.md", "# It's \"quoted\"; with semicolons; $(Foo) @(Bar) %41\nsnippet: ShouldBeObjects\n<!-- snippet: InPlace -->\n```cs\nx; y\n```\n");
        Write("bin/b.md", "snippet: InBin\n");
        var targets = Path.Combine(Presets.Root, "src", "StyleBro.Package", "build", "StyleBro.Analyzers.targets");
        Write("src/App/App.csproj", $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>\n  <Import Project=\"{targets}\" />\n</Project>\n");
        string Snippets() => FormatCommand.Capture(root, "msbuild", Path.Combine(root, "src", "App", "App.csproj"), "-target:_StyleBroFindSnippets", "-getProperty:StyleBroSnippets", "-nologo").Trim();

        Assert.Equal(["InPlace", "ShouldBeObjects"], Snippets().Split('|', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal));

        // Without an mdsnippets.json nothing is read.
        File.Delete(Path.Combine(root, "src", "mdsnippets.json"));
        Assert.Equal(string.Empty, Snippets());
    }
}
