using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Theory]
    [InlineData("1.1.118", true)]
    [InlineData("1.2.0-beta.556", false)]
    public void Migrate_StyleCop11_LeavesSwitchExpressionsWithoutTrailingCommas(string version, bool skipped)
    {
        // kafkaflow (StyleCop 1.1.118): BRO1401 added trailing commas to switch expressions in 6 StyleCop-clean files; 1.1.x's
        // SA1413 never checked them. BRO1401 stays on for the other lists.
        Write("Lib.csproj", $"""<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="{version}" /></ItemGroup></Project>""");
        Write("C.cs", "public static class C\n{\n    public static string Name(int x) => x switch\n    {\n        0 => \"zero\",\n        _ => \"other\"\n    };\n}\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.BRO1401.severity = warning", result.Lines);
        Assert.Equal(skipped, result.Lines.Contains("stylebro_trailing_comma_switch_expressions = false"));
    }

    [Fact]
    public void Migrate_SkipsRootTrueFoldersWithoutCSharp()
    {
        // kafkaflow: src/KafkaFlow.Admin.Dashboard/ClientApp/.editorconfig (root = true, an npm app) got a 188-line [*.cs] block.
        Write("Lib.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("C.cs", "public class C\n{\n}\n");
        Write("ClientApp/.editorconfig", "root = true\n\n[*]\nindent_size = 2\n");
        Write("ClientApp/app.js", "export const a = 1;\n");
        Write("Tools/.editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("Tools/T.cs", "public class T\n{\n}\n");

        var setup = StyleCopSetup.Read(root);
        var plan = Migration.Plan(setup, root, Migration.Generate(setup, root));

        Assert.DoesNotContain(plan.Keys, k => k.Replace('\\', '/').StartsWith("ClientApp/", StringComparison.Ordinal));
        Assert.Contains(plan.Keys, k => k.Replace('\\', '/') == "Tools/.editorconfig");
    }

    [Fact]
    public void Migrate_SortsUsingsTheEnforcedRuleAcceptsSorted()
    {
        // Spot a4: the repository's dotnet_sort_system_directives_first = true and StyleCop's SA1210 (SA1208 off): 'Microsoft.*'
        // above 'System' was held back as "format would write what SA1210 reports", but SA1210 accepts System first.
        Write("A.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_sort_system_directives_first = true\ndotnet_diagnostic.SA1208.severity = none\n");
        Write("Handles.cs", "using Microsoft.Win32.SafeHandles;\nusing System;\n\nclass H { }\n");

        var setup = StyleCopSetup.Read(root);
        var (note, kept) = Migration.UnsortedUnderOwnKey(setup, root, Migration.OwnSettings(root))!.Value;
        Assert.Empty(kept);
        Assert.Contains("'format' will reorder it (SA1210, on here, accepts the sorted order too)", note, StringComparison.Ordinal);

        // With systemUsingDirectivesFirst = false, SA1210 compares across System: the sorted order is reported, the file stays.
        Write("stylecop.json", "{ \"settings\": { \"orderingRules\": { \"systemUsingDirectivesFirst\": false } } }");
        Write("A.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /><AdditionalFiles Include="stylecop.json" /></ItemGroup></Project>""");
        (_, kept) = Migration.UnsortedUnderOwnKey(StyleCopSetup.Read(root), root, Migration.OwnSettings(root))!.Value;
        Assert.Equal(["Handles.cs"], kept);
    }

    [Fact]
    public void Init_ReadsTheRootGlobalConfig()
    {
        // Spot d2: a root .globalconfig with IDE0009 = warning (dotnet_style_qualification_for_field = true) and IDE0055 =
        // suggestion; init's block set IDE0009 = none and IDE0055 = warning in .editorconfig, which wins over it.
        Write(".globalconfig", "is_global = true\n\ndotnet_style_qualification_for_field = true\ndotnet_diagnostic.IDE0009.severity = warning\ndotnet_diagnostic.IDE0055.severity = suggestion\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "namespace Lib;\n\npublic class C\n{\n    private int x;\n\n    public int X => this.x;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var block = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.DoesNotContain("dotnet_diagnostic.IDE0009.severity", block, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity", block, StringComparison.Ordinal);
        Assert.Contains(".globalconfig requires 'this.'", output, StringComparison.Ordinal);
        Assert.Contains(".globalconfig sets IDE0055 (suggestion): kept", output, StringComparison.Ordinal);

        // AnalysisMode All with EnforceCodeStyleInBuild: every style rule is a warning in the build, so 'true' alone is enforced.
        var settings = new Dictionary<string, string> { ["dotnet_style_qualification_for_field"] = "true" };
        Assert.False(InitCommand.RequiresThisQualification(InitCommand.WithAnalysisModeAll(root, settings)));
        Write("Directory.Build.props", "<Project><PropertyGroup><AnalysisMode>All</AnalysisMode><EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild></PropertyGroup></Project>");
        Assert.True(InitCommand.RequiresThisQualification(InitCommand.WithAnalysisModeAll(root, settings)));
        Write("Directory.Build.props", "<Project><PropertyGroup><EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild></PropertyGroup></Project>");
        Assert.False(InitCommand.RequiresThisQualification(InitCommand.WithAnalysisModeAll(root, settings)));

        // A .globalconfig without 'is_global = true' isn't one.
        Write(".globalconfig", "dotnet_diagnostic.IDE0009.severity = warning\n");
        Assert.Empty(Migration.WithGlobalConfig(root, new Dictionary<string, string>()));
    }
}
