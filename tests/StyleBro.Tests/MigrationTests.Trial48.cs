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
}
