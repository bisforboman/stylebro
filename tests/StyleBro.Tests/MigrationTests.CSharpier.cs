using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>The StyleCop migration and init in a repository CSharpier formats.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Migration_WithCSharpier_TurnsItsLayoutRulesOff_AndReportsTheStyleCopRulesItDecides()
    {
        Write(".csharpierrc.yaml", "printWidth: 110\n");
        Write("A.cs", "class A\n{\n}\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        // Every layout rule off once, IDE0055 included (StyleCop's spacing rules would have turned it on).
        Assert.All(Conventions.CSharpierRules, id => Assert.Single(result.Lines, l => l.StartsWith($"dotnet_diagnostic.{id}.severity = ", StringComparison.Ordinal)));
        Assert.All(Conventions.CSharpierRules, id => Assert.Contains($"dotnet_diagnostic.{id}.severity = none", result.Lines));
        Assert.Contains(result.Lines, l => l.StartsWith("# CSharpier formats this repository (.csharpierrc.yaml):", StringComparison.Ordinal));
        Assert.Contains("stylebro_layout_by = csharpier", result.Lines);
        Assert.Contains("stylebro_layout_print_width = 110", result.Lines);

        // SA1116 (BRO1107) and SA1000 (IDE0055) are CSharpier's now; SA1201 (BRO1001), SA1514 (BRO1513) and SA1005 (BRO1002
        // as well as IDE0055) stay StyleBro's.
        Assert.Equal(Migration.CSharpierReason, result.Reasons["SA1116"]);
        Assert.Equal(Migration.CSharpierReason, result.Reasons["SA1000"]);
        Assert.DoesNotContain("SA1116", result.Covered);
        Assert.Contains("SA1201", result.Covered);
        Assert.Contains("SA1514", result.Covered);
        Assert.Contains("SA1005", result.Covered);
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("off      CSharpier (.csharpierrc.yaml) -> IDE0055 and", StringComparison.Ordinal));

        var output = Capture(() => Program.Migrate([root])).Replace("\r\n", "\n");
        Assert.Contains("  CSharpier decides it (the repository formats with CSharpier):\n", output, StringComparison.Ordinal);
        Assert.Contains("    SA1116: ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_WithoutCSharpier_EnforcesTheLayoutRules()
    {
        Write("A.cs", "class A\n{\n}\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.BRO1107.severity = warning", result.Lines);
        Assert.Contains("SA1116", result.Covered);
        Assert.DoesNotContain(Migration.CSharpierReason, result.Reasons.Values);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("stylebro_layout_by", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_WithCSharpier_WritesTheLayoutKeys_AndCSharpiersUsingOrder()
    {
        // Mediator: dotnet_sort_system_directives_first = false, and 'format' reordered every file CSharpier had sorted.
        Write(".csharpierrc.json", "{ \"printWidth\": 120 }\n");
        Write(".editorconfig", "root = true\n\n[*.{cs,vb}]\ndotnet_sort_system_directives_first = false\n");
        Write("A.cs", "using System;\nusing Alpha;\n\nclass A\n{\n}\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var config = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("stylebro_layout_by = csharpier\nstylebro_layout_print_width = 120\ndotnet_sort_system_directives_first = true\n", config, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_separate_import_directive_groups", config, StringComparison.Ordinal);
        Assert.Equal(config, File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n"));
    }

    [Theory]
    [InlineData(".csharpierrc.yaml", "printWidth: 110\n", null, 110)]
    [InlineData(".csharpierrc", "{}\n", "90", 100)]
    [InlineData(".config/dotnet-tools.json", "{ \"tools\": { \"csharpier\": {} } }\n", "90", 90)]
    [InlineData(".config/dotnet-tools.json", "{ \"tools\": { \"csharpier\": {} } }\n", null, 100)]
    public void CSharpierPrintWidth_ComesFromItsConfig_ThenMaxLineLength(string file, string content, string? maxLineLength, int width)
    {
        Write(file, content);
        var settings = maxLineLength is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["max_line_length"] = maxLineLength };

        Assert.Equal(width, Conventions.CSharpierPrintWidth(root, settings));
    }
}
