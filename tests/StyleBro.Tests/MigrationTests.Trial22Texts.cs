using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void ContradictedFinalNewline_SaysWhichFilesFormatChanges()
    {
        // ctk: "insert_final_newline = true is written ... and 'format' leaves the files alone", then format added the line
        // break to the 32 files without one.
        var counts = new Dictionary<string, int[]> { [Conventions.FinalNewlineKey] = [32, 349] };
        var own = new Dictionary<string, string> { [Conventions.FinalNewlineKey] = "false" };
        var (line, note) = Conventions.ContradictedFileSetting(counts, own, Conventions.FinalNewlineKey)!.Value;
        Assert.Equal("insert_final_newline = true", line);
        Assert.Contains("'format' adds a last line break to the 32 files that lack one", note, StringComparison.Ordinal);

        counts[Conventions.FinalNewlineKey] = [0, 20];
        Assert.Contains("'format' leaves the files alone", Conventions.ContradictedFileSetting(counts, own, Conventions.FinalNewlineKey)!.Value.Note, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_TurnsTheBuiltInRulesOff_WhereAFolderTurnsTheStyleCategoryOff()
    {
        // Spot 9e: src/Legacy/.editorconfig says category-Style.severity = none, and the root block's IDE0055 = warning (a rule's
        // own entry wins over a category) reformatted it without a word.
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Legacy/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.category-Style.severity = none\n");
        Write("src/Legacy/W.cs", "namespace Legacy;\n\npublic class W\n{\n}\n");
        Write("src/Other/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.category-Style.severity = warning\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var legacy = File.ReadAllText(Path.Combine(root, "src", "Legacy", ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none", legacy, StringComparison.Ordinal);
        Assert.Contains(InitCommand.StyleOffComment.Split('\n')[0], legacy, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0009.severity", legacy, StringComparison.Ordinal);
        Assert.Contains("src/Legacy/.editorconfig turns the Style category off", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity = none", File.ReadAllText(Path.Combine(root, "src", "Other", ".editorconfig")), StringComparison.Ordinal);
    }

    [Fact]
    public void Migrate_CarriesSuppressionsOnlyWhereStyleCopRan()
    {
        // kafkaflow: the samples' '<NoWarn>...;SA1600</NoWarn>' got BRO1601 although StyleCop (and now StyleBro) runs only in src/.
        Write("src/Lib/Lib.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><NoWarn>SA1600</NoWarn></PropertyGroup><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("src/Lib/C.cs", "public class C\n{\n}\n");
        Write("samples/App/App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><NoWarn>SA1600</NoWarn></PropertyGroup></Project>""");
        Write("samples/App/P.cs", "public class P\n{\n}\n");

        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));

        Assert.Contains("SA1600;BRO1601", File.ReadAllText(Path.Combine(root, "src", "Lib", "Lib.csproj")), StringComparison.Ordinal);
        Assert.DoesNotContain("BRO1601", File.ReadAllText(Path.Combine(root, "samples", "App", "App.csproj")), StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NamesStyleSharp_WhenOnlyStyleSharpRuns()
    {
        Assert.Contains("# Generated from this repository's StyleSharp settings", Migration.Render([("*.cs", ["a = b"])], "StyleSharp"), StringComparison.Ordinal);
        Assert.Contains("# Generated from this repository's StyleCop settings", Migration.Render([("*.cs", ["a = b"])]), StringComparison.Ordinal);
    }
}
