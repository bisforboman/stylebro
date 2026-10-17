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
        Write("Page.html", "<html />");

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

    [Fact]
    public void Format_GivesAProjectOutsideTheSolutionToOneRun()
    {
        // Moq: Moq.Tests (net472;net8.0) references Issue1648Harness (net472), which Moq.sln doesn't list; the build reported it
        // and no run formatted it. The net472 run that loads it formats it; a multi-targeted one is left out (copies merged).
        string P(string name) => Path.Combine(root, name, name + ".csproj");
        foreach (var name in new[] { "Tests", "Moq", "Harness", "Multi", "Deep" })
        {
            Write($"{name}/{name}.csproj", "<Project />");
        }

        var known = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase)
        {
            [P("Tests")] = (["net472", "net8.0"], [P("Moq"), P("Harness"), P("Multi")]),
            [P("Moq")] = (["net462", "net8.0"], []),
        };
        var outside = new Dictionary<string, (string[] Frameworks, string[] References)>(StringComparer.OrdinalIgnoreCase)
        {
            [P("Harness")] = (["net472"], [P("Deep")]),
            [P("Multi")] = (["net472", "net8.0"], []),
            [P("Deep")] = (["net8.0"], []),
        };
        List<(string Framework, string Configuration, IReadOnlyList<string> Projects)> runs =
        [
            ("net462", string.Empty, [P("Moq")]),
            ("net472", string.Empty, [P("Tests")]),
            ("net8.0", "Release", [P("Tests")]),
            ("net8.0", string.Empty, [P("Moq"), P("Tests")]),
        ];

        var assigned = FormatCommand.OutsideProjectRuns(runs, known, p => outside[p], root, out var left);

        Assert.Equal([1, 3], assigned.Keys.Order());
        Assert.Equal([P("Harness")], assigned[1]);
        Assert.Equal([P("Deep")], assigned[3]); // through the harness, in the default configuration's net8.0 run
        Assert.Equal([P("Multi")], left);
    }
}
