using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("stylebro-migrate-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    [Fact]
    public void Defaults_FollowStyleCop()
    {
        var setup = StyleCopSetup.Read(root);

        Assert.True(setup.IsOn("SA1101"));   // on by default in StyleCop
        Assert.False(setup.IsOn("SA1305"));  // off by default
        Assert.Empty(setup.Sources);
    }

    [Fact]
    public void Severities_ComeFromEveryKindOfConfig()
    {
        Write("rules.ruleset", """
            <RuleSet Name="r" ToolsVersion="16.0">
              <Rules AnalyzerId="StyleCop.Analyzers" RuleNamespace="StyleCop.Analyzers">
                <Rule Id="SA1101" Action="None" />
                <Rule Id="SA1124" Action="Warning" />
              </Rules>
            </RuleSet>
            """);
        Write("eng/Library.globalconfig", "is_global = true\ndotnet_diagnostic.SA1124.severity = none\ndotnet_analyzer_diagnostic.category-StyleCop.CSharp.DocumentationRules.severity = none\n");
        Write(".editorconfig", "root = true\n[*.md]\ndotnet_diagnostic.SA1005.severity = none\n[*.cs]\ndotnet_diagnostic.SA1309.severity = none # we use _fields\n");

        var setup = StyleCopSetup.Read(root);

        Assert.False(setup.IsOn("SA1101"));  // ruleset
        Assert.False(setup.IsOn("SA1124"));  // global config beats the ruleset
        Assert.False(setup.IsOn("SA1600"));  // category
        Assert.False(setup.IsOn("SA1309"));  // .editorconfig, with a comment after the value
        Assert.True(setup.IsOn("SA1005"));   // only turned off for Markdown
    }

    [Fact]
    public void SeveralConfigsOfOneKind_TheStrictestWins()
    {
        Write("eng/Library.globalconfig", "is_global = true\ndotnet_diagnostic.SA1124.severity = error\n");
        Write("eng/Test.globalconfig", "is_global = true\ndotnet_diagnostic.SA1124.severity = none\n");

        Assert.Equal(Severity.Error, StyleCopSetup.Read(root).Severities["SA1124"]);
    }

    [Fact]
    public void StyleBroRules_TakeTheWeakestSeverityOfTheRulesTheyReplace()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1201.severity = error\ndotnet_diagnostic.SA1202.severity = error\ndotnet_diagnostic.SA1124.severity = none\n");

        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning", lines); // SA1201-SA1204, SA1214: the weakest
        Assert.Contains("dotnet_diagnostic.BRO1112.severity = none", lines);    // SA1124 off
        Assert.Contains("dotnet_diagnostic.BRO1113.severity = warning", lines); // SA1123 on by default
    }

    [Fact]
    public void StyleBroRule_StaysOff_WhenOneOfItsRulesIsOff()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1201.severity = none\ndotnet_diagnostic.SA1202.severity = none\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.BRO1001.severity = none", result.Lines);
        Assert.Equal("BRO1001 also enforces SA1201, SA1202, which are off", result.Reasons["SA1203"]);
        Assert.DoesNotContain("SA1203", result.Covered);
    }

    [Fact]
    public void RulesMadeMootByStyleCopJson_DontKeepAStyleBroRuleOff()
    {
        Write("stylecop.json", """{ "settings": { "orderingRules": { "elementOrder": ["kind", "accessibility"] } } }""");
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1203.severity = none\ndotnet_diagnostic.SA1204.severity = none\ndotnet_diagnostic.SA1214.severity = none\n");

        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void XmlHeader_TurnsOnBro1615_WithStyleCopsHeaderSettings()
    {
        Write("stylecop.json", """
            { "settings": { "documentationRules": {
                "companyName": "Contoso",
                "copyrightText": "Copyright (c) {companyName}.\nLicensed under the {licenseName} license.",
                "variables": { "licenseName": "MIT" },
                "headerDecoration": "-----" } } }
            """);

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.BRO1615.severity = warning", result.Lines);
        Assert.Contains("stylebro_file_header_company = Contoso", result.Lines);
        Assert.Contains("stylebro_file_header_copyright = Copyright (c) {companyName}.\\nLicensed under the MIT license.", result.Lines);
        Assert.Contains("stylebro_file_header_decoration = -----", result.Lines);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("file_header_template", StringComparison.Ordinal));
        Assert.Contains("SA1641", result.Covered);
    }

    [Fact]
    public void PlainHeader_IsIde0073_AndBro1615StaysOff()
    {
        Write("stylecop.json", """
            { "settings": { "documentationRules": {
                "xmlHeader": false,
                "companyName": "Contoso",
                "copyrightText": "Copyright (c) {companyName}.\nSPDX-License-Identifier: {license}",
                "variables": { "license": "Apache-2.0" } } } }
            """);

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.BRO1615.severity = none", result.Lines);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("stylebro_file_header", StringComparison.Ordinal));
        Assert.Contains("file_header_template = Copyright (c) Contoso.\\nSPDX-License-Identifier: Apache-2.0", result.Lines);
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = warning", result.Lines);
    }

    [Fact]
    public void XmlHeader_WithOneHeaderRuleOff_Bro1615StaysOff()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1636.severity = none\n");

        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.Contains("dotnet_diagnostic.BRO1615.severity = none", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("stylebro_file_header", StringComparison.Ordinal));
    }

    [Fact]
    public void SubDirectoryConfigs_GetTheSettingsThatDiffer()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 4\n");
        Write("test/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1204.severity = none\ndotnet_diagnostic.SA1600.severity = none\n");

        var setup = StyleCopSetup.Read(root);
        var plan = Migration.Plan(setup, root, Migration.Generate(setup, root));

        Assert.True(setup.IsOn("SA1204"));
        var (section, lines) = Assert.Single(plan[Path.Combine("test", ".editorconfig")]);
        Assert.Equal("*.cs", section);
        Assert.Equal(["dotnet_diagnostic.BRO1001.severity = none", "dotnet_diagnostic.BRO1601.severity = none"], lines);
    }

    [Fact]
    public void PathSectionsOfTheRootConfig_GetTheirOwnSection()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1124.severity = warning\n[tests/**.cs]\ndotnet_diagnostic.SA1124.severity = none\n");

        var setup = StyleCopSetup.Read(root);
        var plan = Migration.Plan(setup, root, Migration.Generate(setup, root));

        var sections = plan[".editorconfig"];
        Assert.Equal(["*.cs", "tests/**.cs"], sections.Select(s => s.Section));
        Assert.Equal(["dotnet_diagnostic.BRO1112.severity = none"], sections[1].Lines);
        Assert.StartsWith("# BEGIN stylebro-migrate\n", Migration.Render(sections));
    }

    [Fact]
    public void Suppressions_AreCarriedOver()
    {
        var replacements = Migration.Generate(StyleCopSetup.Read(root), root).Replacements;
        var code = """
            #pragma warning disable SA1202, SA1203 // ordering
            using System.Diagnostics.CodeAnalysis;

            [assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1642:Constructor summary", Justification = "Generated")]

            class C
            {
                [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis")]
                void M() { }
            }
            """;

        var (text, added) = Suppressions.Rewrite(code, replacements);

        Assert.Equal(3, added);
        Assert.Equal("""
            #pragma warning disable SA1202, SA1203, BRO1001 // ordering
            using System.Diagnostics.CodeAnalysis;

            [assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1642:Constructor summary", Justification = "Generated")]
            [assembly: SuppressMessage("StyleBro", "BRO1606", Justification = "Generated")]

            class C
            {
                [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis")]
                [SuppressMessage("Style", "IDE0009")]
                void M() { }
            }
            """.Replace("\r\n", "\n"), text.Replace("\r\n", "\n"));
        Assert.Equal(0, Suppressions.Rewrite(text, replacements).Added);
    }

    [Fact]
    public void EveryStyleBroRule_NamesTheStyleCopRulesItReplaces()
    {
        var rules = Migration.StyleBroRules();

        Assert.NotEmpty(rules);
        Assert.All(rules, r => Assert.NotEmpty(r.StyleCop));
        Assert.Equal(["SA1201", "SA1202", "SA1203", "SA1204", "SA1214"], rules.Single(r => r.Id == "BRO1001").StyleCop);
    }

    [Fact]
    public void StyleCopJson_SettingsAreCarriedOver()
    {
        Write("stylecop.json", """
            {
              // comments and trailing commas are fine in stylecop.json
              "settings": {
                "orderingRules": { "usingDirectivesPlacement": "outsideNamespace", "elementOrder": ["kind", "accessibility", "static"], },
                "indentation": { "indentationSize": 2, "useTabs": false },
              }
            }
            """);

        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.Contains("csharp_using_directive_placement = outside_namespace", lines);
        Assert.Contains("stylebro_member_constants_first = false", lines);
        Assert.Contains("stylebro_member_static_first = true", lines);
        Assert.Contains("indent_size = 2", lines);
        Assert.Contains("indent_style = space", lines);
    }

    [Fact]
    public void OffRules_TurnTheirSdkSettingsOff()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\ndotnet_diagnostic.SA1200.severity = none\ndotnet_diagnostic.SA1507.severity = none\n");

        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.Contains("dotnet_diagnostic.IDE0009.severity = none", lines);
        Assert.Contains("dotnet_style_qualification_for_field = false", lines);
        Assert.Contains("dotnet_diagnostic.IDE0065.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.IDE2000.severity = none", lines);
        Assert.Contains("dotnet_style_allow_multiple_blank_lines_experimental = true", lines);
    }

    [Fact]
    public void TheRepositorysOwnSdkSettings_AreKept()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ncsharp_preserve_single_line_statements = true\ndotnet_diagnostic.IDE0055.severity = warning\n[*.md]\nindent_size = 2\n");
        Write("src/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1107.severity = none\n");

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);

        Assert.DoesNotContain(result.Lines, l => l.StartsWith("csharp_preserve_single_line_statements", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("dotnet_diagnostic.IDE0055.", StringComparison.Ordinal));
        Assert.Contains(result.Notes, n => n.Contains("csharp_preserve_single_line_statements"));
        Assert.False(Migration.Plan(setup, root, result).ContainsKey(Path.Combine("src", ".editorconfig")));
    }

    [Fact]
    public void DocumentationScope_ComesFromStyleCopJson()
    {
        Write("stylecop.json", """{ "settings": { "documentationRules": { "documentInternalElements": false } } }""");

        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.Contains("stylebro_document_exposed_elements = true", lines);
        Assert.Contains("stylebro_document_internal_elements = false", lines);
        Assert.Contains("stylebro_document_private_elements = false", lines);
    }

    [Fact]
    public void NoWarn_IsCarriedOver()
    {
        var replacements = Migration.Generate(StyleCopSetup.Read(root), root).Replacements;
        var project = "<PropertyGroup>\n  <NoWarn>$(NoWarn);CA1062</NoWarn>\n  <NoWarn>$(NoWarn);SA1123;SA1204;SA1600</NoWarn>\n</PropertyGroup>\n";

        var (text, added) = Suppressions.RewriteNoWarn(project, replacements);

        Assert.Equal(3, added);
        Assert.Contains("<NoWarn>$(NoWarn);SA1123;SA1204;SA1600;BRO1113;BRO1001;BRO1601</NoWarn>", text);
        Assert.Contains("<NoWarn>$(NoWarn);CA1062</NoWarn>", text);
        Assert.Equal(0, Suppressions.RewriteNoWarn(text, replacements).Added);
    }

    [Fact]
    public void RulesAddedInStyleCop12_AreOff_WhenTheRepositoryUses11()
    {
        Write("Directory.Packages.props", "<Project><ItemGroup><PackageVersion Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");

        var setup = StyleCopSetup.Read(root);

        Assert.Equal("1.1.118", setup.Version);
        Assert.False(setup.IsOn("SA1141")); // use tuple syntax, new in 1.2
        Assert.True(setup.IsOn("SA1642"));
    }

    [Fact]
    public void XmlDocumentationRules_AreOff_WhenNoProjectGeneratesDocumentation()
    {
        Write("src/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.BRO1606.severity = none", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1601.severity = warning", result.Lines); // text-based, StyleCop checks it too
        Assert.Contains("GenerateDocumentationFile", result.Reasons["SA1642"]);

        Write("Directory.Build.props", "<Project><PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");

        Assert.Contains("dotnet_diagnostic.BRO1606.severity = warning", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void UsingSorting_FollowsSA1208AndSA1210()
    {
        Assert.Contains("dotnet_sort_system_directives_first = true", Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1208.severity = none;\n");
        Assert.Contains("dotnet_sort_system_directives_first = false", Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        // Any value of the key makes 'dotnet format' sort usings, so it's left out when StyleCop didn't sort them.
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1208.severity = none\ndotnet_diagnostic.SA1210.severity = none\n");
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("dotnet_sort_system_directives_first", StringComparison.Ordinal));
        Assert.DoesNotContain("SA1211", result.Covered);
    }

    [Fact]
    public void TheGeneratedSettings_CoverEveryPresetKey()
    {
        var preset = Path.Combine(RepositoryRoot(), "src", "StyleBro.Package", "build", "stylebro.recommended.globalconfig");
        var presetKeys = File.ReadAllLines(preset)
            .Where(l => l.Contains('=') && !l.TrimStart().StartsWith('#'))
            .Select(l => l.Substring(0, l.IndexOf('=')).Trim())
            .Where(k => k is not "is_global" and not "global_level");
        var generated = Migration.Generate(StyleCopSetup.Read(root), root).Lines
            .Where(l => l.Contains('=') && !l.StartsWith('#'))
            .Select(l => l.Substring(0, l.IndexOf('=')).Trim())
            .ToHashSet();

        Assert.All(presetKeys, key => Assert.Contains(key, generated));
    }

    [Fact]
    public void EveryPresetDifferenceFromStyleCop_IsDocumented()
    {
        // What StyleCop's defaults translate to (a repository without StyleCop settings that generates docs) ...
        Write("Directory.Build.props", "<Project><PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");
        var styleCop = KeyValues(Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        // ... against the preset: every key the preset sets differently must be named in docs/differences-from-stylecop.md.
        var preset = KeyValues(File.ReadAllLines(Path.Combine(RepositoryRoot(), "src", "StyleBro.Package", "build", "stylebro.recommended.globalconfig")));
        var page = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "differences-from-stylecop.md"));
        var different = preset.Where(p => p.Key is not "is_global" and not "global_level"
            && styleCop.TryGetValue(p.Key, out var value) && value != p.Value).Select(p => p.Key).ToList();

        Assert.NotEmpty(different);
        Assert.All(different, key => Assert.True(
            page.Contains(key, StringComparison.Ordinal) || page.Contains(key[..key.LastIndexOf('_')] + "_*", StringComparison.Ordinal),
            $"The preset sets {key} = {preset[key]} (StyleCop's defaults: {styleCop[key]}); explain it in docs/differences-from-stylecop.md."));
    }

    [Fact]
    public void EveryRule_IsInTheDifferencesPage()
    {
        var page = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "differences-from-stylecop.md"));

        Assert.All(Migration.StyleBroRules(), rule => Assert.True(
            page.Contains($"[{rule.Id}](rules/{rule.Id}.md)", StringComparison.Ordinal),
            $"{rule.Id} is missing from docs/differences-from-stylecop.md (list it under 'Same as StyleCop' if nothing differs)."));
    }

    private static Dictionary<string, string> KeyValues(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Select(l => l.Trim()).Where(l => l.Contains('=') && !l.StartsWith('#') && !l.StartsWith('[')))
        {
            var equals = line.IndexOf('=');
            result[line[..equals].Trim()] = line[(equals + 1)..].Split('#')[0].Trim();
        }

        return result;
    }
    [Fact]
    public void DisablePreset_AddsThePropertyOnce()
    {
        Assert.Contains("<StyleBroPreset>none</StyleBroPreset>", Migration.DisablePreset(null));

        var props = "<Project>\n  <PropertyGroup Condition=\"x\">\n  </PropertyGroup>\n  <PropertyGroup>\n    <LangVersion>latest</LangVersion>\n  </PropertyGroup>\n</Project>\n";
        var disabled = Migration.DisablePreset(props)!;

        Assert.Equal("<Project>\n  <PropertyGroup Condition=\"x\">\n  </PropertyGroup>\n  <PropertyGroup>\n    <StyleBroPreset>none</StyleBroPreset>\n    <LangVersion>latest</LangVersion>\n  </PropertyGroup>\n</Project>\n", disabled);
        Assert.Null(Migration.DisablePreset(disabled));
    }

    [Fact]
    public void StyleCopsAlternativeRules_AreMapped()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\ndotnet_diagnostic.SX1101.severity = warning\ndotnet_diagnostic.SA1309.severity = none\ndotnet_diagnostic.SX1309.severity = warning\ndotnet_diagnostic.SA1412.severity = warning\n");
        Write("C.cs", "class C { private int count; private int other; }"); // the code would say camelCase; SX1309 wins

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.IDE0003.severity = warning", result.Lines);
        Assert.Contains("dotnet_diagnostic.IDE0009.severity = none", result.Lines);
        Assert.Contains("stylebro_private_field_naming = _camelCase", result.Lines);
        Assert.Contains("charset = utf-8-bom", result.Lines);
        Assert.Contains("SX1101", result.Covered);
        Assert.Contains("SX1309", result.Covered);
        Assert.Contains("SA1412", result.Covered);
    }

    [Fact]
    public void CharsetIsLeftAlone_WhenSA1412IsOff()
    {
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("charset", StringComparison.Ordinal));
    }
    [Fact]
    public void PrivateFieldStyle_IsInferredFromTheCode()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1309.severity = none\n");
        Write("C.cs", "class C { private int _a; int _b; private int c; private const int D = 1; private static readonly int E = 1; public int F; }");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("stylebro_private_field_naming = _camelCase", result.Lines);
        Assert.Contains(result.Notes, n => n.Contains("2 named '_field', 1 named 'field'"));
    }

    [Fact]
    public void PrivateFieldStyle_FollowsSA1309WhenItsOn()
    {
        Write("C.cs", "class C { private int _a; private int _b; }");

        Assert.Contains("stylebro_private_field_naming = camelCase", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void Apply_ReplacesTheBlockOnASecondRun()
    {
        var first = Migration.Apply("root = true\n", "# BEGIN stylebro-migrate\nold\n# END stylebro-migrate\n");
        var second = Migration.Apply(first, "# BEGIN stylebro-migrate\nnew\n# END stylebro-migrate\n");

        Assert.Equal("root = true\n\n# BEGIN stylebro-migrate\nnew\n# END stylebro-migrate\n", second);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "StyleBro.slnx")))
        {
            directory = directory.Parent!;
        }

        return directory.FullName;
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
