using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void Init_PinsThePresetVersion_AndLeavesASetOneAlone()
    {
        // Owner's decision 2026-10-13: a new setup takes every rule the tool knows, later package upgrades add none.
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/App/C.cs", "namespace App;\n\ninternal sealed class C\n{\n}\n");
        var props = Path.Combine(root, "Directory.Build.props");
        var pin = $"<StyleBroPreset>{PresetVersions.Latest}</StyleBroPreset>";

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains($"--write also sets the preset version in Directory.Build.props ({pin})", output, StringComparison.Ordinal);
        Assert.False(File.Exists(props));

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains(pin, File.ReadAllText(props), StringComparison.Ordinal);

        File.WriteAllText(props, "<Project>\n  <PropertyGroup>\n    <StyleBroPreset>latest</StyleBroPreset>\n  </PropertyGroup>\n</Project>\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("preset version", output, StringComparison.Ordinal);
        Assert.DoesNotContain(pin, File.ReadAllText(props), StringComparison.Ordinal);
    }

    [Fact]
    public void SetPreset_AddsThePropertyOnce()
    {
        Assert.Contains("<StyleBroPreset>1.0</StyleBroPreset>", Migration.SetPreset(null, "1.0", "c"), StringComparison.Ordinal);
        var props = "<Project>\n  <PropertyGroup>\n    <LangVersion>latest</LangVersion>\n  </PropertyGroup>\n</Project>\n";
        var pinned = Migration.SetPreset(props, "1.0", "c")!;
        Assert.Equal("<Project>\n  <PropertyGroup>\n    <StyleBroPreset>1.0</StyleBroPreset>\n    <LangVersion>latest</LangVersion>\n  </PropertyGroup>\n</Project>\n", pinned);
        Assert.Null(Migration.SetPreset(pinned, "none", "c"));
    }
}
