using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void StandaloneComments_KeepSA1512sRemoval_WhereSA1512IsOn()
    {
        // Trial 21: BRO1506 keeps the blank line below a standalone comment by default; SA1512 removes it.
        Assert.Contains("stylebro_blank_line_below_standalone_comments = remove", Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1512.severity = none\n");
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("stylebro_blank_line_below_standalone_comments", StringComparison.Ordinal));
    }
}
