using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 29's findings in stylebro-migrate.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void TextTemplateOutputs_FollowTheTemplatesOutputExtension()
    {
        // Moq: 'ReturnsExtensions.tt' says '<#@ output extension=".Generated.cs" #>'; the 'ReturnsExtensions.cs' beside it is
        // hand-written and was left unformatted (the build reported it). A template without the directive writes '.cs'.
        Write("ReturnsExtensions.tt", "<#@ template language=\"C#\" #>\n<#@ output extension=\".Generated.cs\" #>\n");
        Write("ReturnsExtensions.cs", "class Hand { }");
        Write("ReturnsExtensions.Generated.cs", "class Written { }");
        Write("Format.tt", "<#@ template language=\"C#\" #>\n");
        Write("Format.cs", "class Written { }");
        Write("Page.tt", "<#@ output extension=\"html\" #>\n");
        Write("Page.cs", "class Hand { }");

        Assert.Equal(["Format.cs", "ReturnsExtensions.Generated.cs"], Migration.TextTemplateOutputs(root).Order(StringComparer.Ordinal));

        // A build that runs the templates rewrites only their outputs, not the hand-written file beside one.
        const string Build = "<TransformOnBuild>true</TransformOnBuild>";
        Assert.True(FormatCommand.IsGenerated(Path.Combine(root, "ReturnsExtensions.Generated.cs"), Build));
        Assert.False(FormatCommand.IsGenerated(Path.Combine(root, "ReturnsExtensions.cs"), Build));
        Assert.True(FormatCommand.IsGenerated(Path.Combine(root, "Format.cs"), Build));
    }

    [Fact]
    public void Conventions_CountsNonPrivateFieldCasing_WithoutANamingRule()
    {
        // Trial 29 (Harmony, owner's choice "Count it, BRO1306 off"): public and internal fields named camelCase, no naming
        // rule; BRO1306 renamed the internal ones to PascalCase. Constants and static readonly fields have keys of their own,
        // protected fields are camelCase anyway (SA1306), private ones are BRO1303's.
        Write(".editorconfig", "root = true\n\n[*.cs]\n");
        Write("C.cs", "public class C\n{\n" + string.Concat(Enumerable.Range(0, 11).Select(i => $"    public int count{i};\n"))
            + "    internal int offset;\n    public int Upper;\n    public const int max = 1;\n    internal static readonly int shared = 2;\n"
            + "    protected int other;\n    private int Hidden;\n}\n");

        var counts = Conventions.Count(root, Migration.OwnSettings(root));
        Assert.Equal([1, 12], counts["dotnet_diagnostic.BRO1306.severity"]);
        Assert.Contains("dotnet_diagnostic.BRO1306.severity = none", Conventions.Decide(counts, new Dictionary<string, string>()).Lines);

        // PascalCase code keeps BRO1306 on.
        Write("C.cs", "public class C\n{\n" + string.Concat(Enumerable.Range(0, 11).Select(i => $"    public int Count{i};\n")) + "}\n");
        counts = Conventions.Count(root, Migration.OwnSettings(root));
        Assert.Equal([11, 0], counts["dotnet_diagnostic.BRO1306.severity"]);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1306.severity = none", Conventions.Decide(counts, new Dictionary<string, string>()).Lines);
    }
}
