using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the forty-sixth trial (FluentResults, coravel, sqlkata/querybuilder, dailydevops/codebuilder) found in the tool.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void PackageCopy_IsGone_OnceTheBuildsShowedTheCopyingIsOff_UntilABuildFileChanges()
    {
        // dailydevops/codebuilder: with NetEvolve.Defaults' copying turned off (DisableSupportAdditionalFiles) the builds kept
        // init's block and were clean, and init, format and check still named the package (owner's decision 2026-10-23).
        WritePackageCopyRepository("package");
        Write("Directory.Build.props", $"<Project>\n  <!-- {Guid.NewGuid():N} -->\n  <PropertyGroup><DisableCopy>true</DisableCopy></PropertyGroup>\n</Project>\n");
        var copy = Migration.PackageCopy(root);
        Assert.NotNull(copy);

        Migration.RememberCopyOff(root, copy);
        Assert.Null(Migration.PackageCopy(root));
        Assert.Equal(copy, Migration.FindPackageCopy(root));

        // The copying turned on again (a project or build file changed): the builds have to show it again.
        Write("Directory.Build.props", File.ReadAllText(Path.Combine(root, "Directory.Build.props")).Replace("true", "false"));
        Assert.Equal(copy, Migration.PackageCopy(root));
    }

    [Fact]
    public void CanProveCopyOff_NeedsInitsBlock()
    {
        // Without the block the root file can be the package's own text, which a copy leaves as it is.
        Assert.False(Migration.CanProveCopyOff(null));
        Assert.False(Migration.CanProveCopyOff(TemplateConfig));
        Assert.True(Migration.CanProveCopyOff(TemplateConfig + Migration.BeginMarker + "\n" + Migration.EndMarker + "\n"));
    }

    [Fact]
    public void CopyOffVerdict_DropsThePackagesGap_WhenTheBuildsKeptTheRootEditorConfig()
    {
        WritePackageCopyRepository("package");
        Write("Directory.Build.props", $"<Project>\n  <!-- {Guid.NewGuid():N} -->\n</Project>\n");
        var before = TemplateConfig + Migration.BeginMarker + "\n[*.cs]\nx = 1\n" + Migration.EndMarker + "\n";
        Write(".editorconfig", before);
        var copy = Migration.PackageCopy(root)!;
        var gap = Migration.PackageCopyGap(copy);
        var held = new List<string> { gap, FormatCommand.HeldBackGap(["IDE0049"]), FormatCommand.NotVerifiedLine(1) };

        // A build that wrote the package's file back: the held lines as they were.
        Write(".editorconfig", TemplateConfig);
        var lines = new List<string>();
        Assert.Equal(FormatCommand.NotVerifiedExitCode, FormatCommand.CopyOffVerdict(root, copy, gap, before, null, true, held, FormatCommand.NotVerifiedExitCode, lines.Add));
        Assert.Equal(held, lines);
        Assert.NotNull(Migration.PackageCopy(root));

        // Builds that reported something, or none: the same.
        Write(".editorconfig", before);
        lines.Clear();
        Assert.Equal(FormatCommand.NotVerifiedExitCode, FormatCommand.CopyOffVerdict(root, copy, gap, before, "Not clean: the builds report 3 findings.", true, held, FormatCommand.NotVerifiedExitCode, lines.Add));
        Assert.Equal(FormatCommand.NotVerifiedExitCode, FormatCommand.CopyOffVerdict(root, copy, gap, before, null, false, held, FormatCommand.NotVerifiedExitCode, lines.Add));
        Assert.NotNull(Migration.PackageCopy(root));

        // Clean builds that left the file alone: the copying is off, remembered, and only the note is left.
        lines.Clear();
        Assert.Equal(0, FormatCommand.CopyOffVerdict(root, copy, gap, before, null, true, held, FormatCommand.NotVerifiedExitCode, lines.Add));
        Assert.DoesNotContain(gap, lines);
        Assert.Contains(FormatCommand.HeldBackGap(["IDE0049"]), lines);
        Assert.Equal("Clean.", lines[^1]);
        Assert.Null(Migration.PackageCopy(root));
    }
}
