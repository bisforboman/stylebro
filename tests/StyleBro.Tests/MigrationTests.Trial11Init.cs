using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the eleventh trial (Ical.Net, Fitnet, Autofac) found in init's conventions and the Sonar mapping.</summary>
public sealed partial class MigrationTests
{
    private const string StaticCasingSource = """
        class A
        {
            private const int Max = 1;
            const string name = "a";
            private static readonly int _count = 2;
            static readonly int Other = 3;
            public const int Ignored = 4;
            private static int notReadonly;
            private const int s_prefixed = 5;
            private const int Two_Words = 6;
        }

        interface I
        {
            const int lower = 1;
        }
        """;

    [Fact]
    public void Conventions_CountPrivateConstantsAndStaticReadonlyFieldsByCasing()
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText(StaticCasingSource), counts);

        Assert.Equal(new[] { 2, 1, 1 }, counts[Conventions.StaticCasingKey]);   // Max, Other; name; _count
    }

    [Theory]
    [InlineData(new[] { 26, 8, 50 }, "dotnet_diagnostic.BRO1306.severity = none")]   // Ical.Net: mixed
    [InlineData(new[] { 2, 30, 0 }, "stylebro_private_static_field_naming = camelCase")]
    [InlineData(new[] { 0, 0, 4 }, "stylebro_private_static_field_naming = _camelCase")]
    [InlineData(new[] { 0, 2, 2 }, "dotnet_diagnostic.BRO1306.severity = none")]    // camelCase and _camelCase disagree
    [InlineData(new[] { 40, 1, 2 }, null)]                                              // PascalCase: BRO1306's default
    [InlineData(new[] { 0, 2, 0 }, null)]                                               // too few
    public void Conventions_PrivateStaticFieldCasing_IsWrittenOrTurnsBRO1306Off(int[] count, string? expected)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.StaticCasingKey] = count;

        var lines = Conventions.Decide(counts, new Dictionary<string, string>()).Lines.Where(l => !l.StartsWith('#') && !l.StartsWith("dotnet_sort", StringComparison.Ordinal)).ToList();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, lines);
    }

    [Fact]
    public void Conventions_PrivateStaticFieldCasing_ARepositorySettingWins()
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.StaticCasingKey] = new[] { 26, 8, 50 };

        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string> { [Conventions.StaticCasingKey] = "set in .editorconfig (PascalCase)" });
        Assert.DoesNotContain(lines, l => l.Contains("BRO1306", StringComparison.Ordinal) || l.StartsWith(Conventions.StaticCasingKey, StringComparison.Ordinal));
        Assert.Contains(report, l => l.Contains("set in .editorconfig (PascalCase)", StringComparison.Ordinal));

        Assert.DoesNotContain(Conventions.Decide(counts, new Dictionary<string, string>(), key => key == "dotnet_diagnostic.BRO1306.severity").Lines, l => l.Contains("BRO1306", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_MixedPrivateStaticFieldCasing_TurnsBRO1306Off_UnlessANamingRuleDecides()
    {
        Write(".editorconfig", "root = true\n");
        Write("A.cs", StaticCasingSource.Replace("class A", "class A0", StringComparison.Ordinal));
        Write("B.cs", "class B\n{\n    private const int first = 1;\n    private const int second = 2;\n}\n");

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("  off      private constants/static readonly fields: 2 PascalCase, 3 camelCase, 1 _camelCase -> BRO1306 is off", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.BRO1306.severity = none\n", output, StringComparison.Ordinal);

        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_naming_rule.consts.symbols = consts\ndotnet_naming_rule.consts.style = camel\ndotnet_naming_rule.consts.severity = warning\n"
            + "dotnet_naming_symbols.consts.applicable_kinds = field\ndotnet_naming_symbols.consts.applicable_accessibilities = private\ndotnet_naming_symbols.consts.required_modifiers = const\n"
            + "dotnet_naming_style.camel.capitalization = camel_case\n");
        output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("  set      private constants/static readonly fields: 6 fields -> a dotnet_naming_rule decides", output, StringComparison.Ordinal);
        Assert.DoesNotContain("BRO1306.severity = none", output, StringComparison.Ordinal);

        // A rule nothing enforces (IDE1006 off) doesn't decide: the code does again.
        Write(".editorconfig", File.ReadAllText(Path.Combine(root, ".editorconfig")) + "dotnet_diagnostic.IDE1006.severity = none\n");
        output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("\ndotnet_diagnostic.BRO1306.severity = none\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_CountRelativeUsingsInsideNamespaces()
    {
        // Fitnet: 'using Common.BusinessRulesEngine;' inside 'namespace EvolutionaryArchitecture.Fitnet.Contracts.Data'.
        var trees = new[]
        {
            "namespace App.Common.Rules { class R { } }",
            "namespace App.Contracts.Data;\n\nusing Common.Rules;\nusing App.Common.Rules;\nusing System;\nusing global::System.IO;\nusing static Common.Rules.R;\nusing X = Common.Rules.R;\n\nclass C { }",
            "using Common.Rules;\n\nnamespace Other { class D { } }",
            "namespace App { namespace Contracts { using Common.Rules; } }",
        }.Select(t => CSharpSyntaxTree.ParseText(t)).ToList();
        var counts = Conventions.NewCounts();

        Conventions.CountRelativeUsings(trees, counts);

        // Relative: 'Common.Rules', 'static Common.Rules.R', 'X = Common.Rules.R', the nested one; qualified: the other three.
        Assert.Equal(new[] { 3, 4 }, counts[Conventions.RelativeUsingsKey]);
    }

    [Theory]
    [InlineData(new[] { 91, 92 }, "dotnet_diagnostic.BRO1126.severity = none")]   // Fitnet: mixed
    [InlineData(new[] { 1, 30 }, "dotnet_diagnostic.BRO1126.severity = none")]    // relative
    [InlineData(new[] { 30, 1 }, null)]                                           // qualified: BRO1126's form
    public void Conventions_RelativeUsings_TurnBRO1126Off(int[] count, string? expected)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.RelativeUsingsKey] = count;

        var lines = Conventions.Decide(counts, new Dictionary<string, string>()).Lines.Where(l => !l.StartsWith('#') && !l.StartsWith("dotnet_sort", StringComparison.Ordinal)).ToList();

        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, lines);
    }

    [Theory]
    [InlineData(new[] { 115, 108 }, "utf-8", "charset = unset")]        // Ical.Net: mixed, each file keeps its BOM
    [InlineData(new[] { 112, 15 }, "utf-8-bom", "charset = utf-8")]     // Fitnet: 'format' added 112 BOMs
    [InlineData(new[] { 0, 5 }, "utf-8", "charset = utf-8-bom")]
    [InlineData(new[] { 100, 3 }, "utf-8", null)]                       // the key's form has the share: it stays
    [InlineData(new[] { 3, 0 }, "utf-8-bom:warning", "charset = utf-8")]
    [InlineData(new[] { 1, 1 }, "utf-8", null)]                         // too few
    [InlineData(new[] { 0, 5 }, "latin1", null)]
    public void ContradictedCharset_FilesDecideAKeyTheyDontFollow(int[] files, string setting, string? expected)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.CharsetKey] = files;

        var result = Conventions.ContradictedFileSetting(counts, new Dictionary<string, string> { ["charset"] = setting }, Conventions.CharsetKey);

        Assert.Equal(expected, result?.Line);
        Assert.Null(Conventions.ContradictedFileSetting(counts, new Dictionary<string, string>(), Conventions.CharsetKey));
    }

    [Fact]
    public void Init_CharsetTheFilesDontFollow_IsWrittenForCSharpFiles()
    {
        Write(".editorconfig", "root = true\n[*]\ncharset = utf-8\n");
        for (var i = 0; i < 4; i++)
        {
            Write($"A{i}.cs", (i < 2 ? "﻿" : string.Empty) + $"class A{i}\n{{\n}}\n");
        }

        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        Assert.Contains("  note     Charset: your .editorconfig says charset = utf-8, but 2 of 4 C# files start with a BOM", output, StringComparison.Ordinal);
        Assert.Contains("\ncharset = unset\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_CountFileHeadersWithEmptyCommentLines()
    {
        var counts = Conventions.NewCounts();
        foreach (var text in new[]
        {
            "//\n// Copyright A.\n//\n\nclass A { }",
            "// Copyright B.\n//\nusing System;\nclass B { }",
            "// Copyright C.\nclass C { }",
            "/// <summary>Doc.</summary>\nclass D { }",
            "class E { }",
        })
        {
            Conventions.Count(CSharpSyntaxTree.ParseText(text), counts);
        }

        Assert.Equal(new[] { 1, 2 }, counts[Conventions.HeaderKey]);
    }

    [Theory]
    [InlineData(@"\nCopyright ical.net project maintainers and contributors.\nLicensed under the MIT license.\n", new[] { 1, 0 }, true)]   // Ical.Net
    [InlineData("Copyright A.", new[] { 10, 0 }, false)]
    [InlineData(null, new[] { 1, 9 }, true)]         // the headers have them
    [InlineData(null, new[] { 5, 5 }, false)]
    [InlineData(null, new[] { 0, 2 }, false)]        // too few
    public void EmptyHeaderLines_TurnBRO1120Off(string? template, int[] headers, bool off)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.HeaderKey] = headers;
        var own = template is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["file_header_template"] = template };

        var lines = Conventions.EmptyHeaderLines(counts, own);

        Assert.Equal(off, lines?.Contains("dotnet_diagnostic.BRO1120.severity = none") == true);
        own["dotnet_diagnostic.BRO1120.severity"] = "warning";
        Assert.Null(Conventions.EmptyHeaderLines(counts, own));   // the repository's own severity stays
    }

    [Fact]
    public void Sonar_MappingsStricterThanSonar_AreNotCovered()
    {
        // S1905: IDE0004 removed '(Service)' and '(int)' casts Sonar accepted (Autofac). S100/S101: BRO1309 also checks events,
        // enum members, enums, delegates, names with '_' inside and test classes.
        foreach (var id in new[] { "S100", "S101", "S1905" })
        {
            Assert.DoesNotContain(SonarSetup.Mapping, m => m.Sonar == id);
        }

        // S927: BRO1313 with Sonar's exceptions (Fitnet: 'Handle(FakeEvent @event, ...)' of MediatR's 'Handle(TNotification notification, ...)').
        Assert.Contains(SonarSetup.Mapping, m => m.Sonar == "S927" && m.Rule == "BRO1313");
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /></ItemGroup></Project>""");
        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");
        Assert.Contains("\nstylebro_match_base_parameter_names = non_generic_methods\n", output, StringComparison.Ordinal);
        Assert.Contains("    S1905: IDE0004 removes more casts than S1905", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0004.severity", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Sonar_StyleBroRules_ComeWithTheOptionsThatMatchSonar()
    {
        // The owner's decision (2026-10-12): each StyleBro rule a Sonar rule maps to reports no more than the Sonar rule.
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.S121.severity = warning\n");
        var output = Capture(() => InitCommand.Run(new[] { root })).Replace("\r\n", "\n");

        foreach (var line in new[]
        {
            "stylebro_upper_case_literal_suffixes = lone_l",                 // S818
            "stylebro_allow_semicolon_after_closing_brace = true",           // S1116
            "csharp_prefer_braces = true",                                   // S121
            "stylebro_braces_statements = if_and_loops",                     // S121
            "stylebro_params_arrays = except_object_and_jagged",             // S3878
            "stylebro_allow_null_forgiving_on_unannotated = true",           // S8969
            "stylebro_allow_nested_dynamic_ifs = true",                      // S1066
        })
        {
            Assert.Contains("\n" + line + "\n", output, StringComparison.Ordinal);
        }

        Assert.Contains("  S121 -> BRO1514 (csharp_prefer_braces = true; stylebro_braces_statements = if_and_loops)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Sonar_ARuleTheStyleCopSetupTurnsOn_KeepsStyleCopsScope()
    {
        // SA1503 and S121 both on: BRO1514 keeps the braces on 'using', 'lock' and 'fixed' that SA1503 wants.
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /><PackageReference Include="StyleCop.Analyzers" Version="1.1.118" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.S121.severity = warning\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root, sonar: SonarSetup.Read(root));

        Assert.Contains("dotnet_diagnostic.BRO1514.severity = warning", result.Lines);
        Assert.Contains("csharp_prefer_braces = true", result.Lines);
        Assert.DoesNotContain("stylebro_braces_statements = if_and_loops", result.Lines);
        Assert.Contains(result.Sonar!.Notes, n => n.StartsWith("stylebro_braces_statements not written for S121: BRO1514 is already on", StringComparison.Ordinal));
    }
}
