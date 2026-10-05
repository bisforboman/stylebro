using System.Text.Json;
using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("stylebro-migrate-").FullName;

    /// <inheritdoc/>
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
    public void ModifierOrder_IsIde0036_OffWhenBothStyleCopRulesAreOff()
    {
        var on = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("dotnet_diagnostic.IDE0036.severity = warning", on.Lines);
        Assert.Contains(on.Lines, l => l.StartsWith("csharp_preferred_modifier_order = public,private,protected,internal,", StringComparison.Ordinal));
        Assert.Contains("SA1207", on.Covered);

        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1206.severity = none\ndotnet_diagnostic.SA1207.severity = none\n");
        Assert.Contains("dotnet_diagnostic.IDE0036.severity = none", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void AllowConsecutiveUsings_ComesFromStyleCopJson()
    {
        Assert.Contains("stylebro_allow_consecutive_usings = true", Migration.Generate(StyleCopSetup.Read(root), root).Lines);

        Write("stylecop.json", """{ "settings": { "layoutRules": { "allowConsecutiveUsings": false } } }""");
        Assert.Contains("stylebro_allow_consecutive_usings = false", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void TheReport_ShowsNoSurveyNotes()
    {
        // The mapping's reasons carry survey evidence for the project's docs; the report shows users only the reason.
        foreach (var (id, proposal) in Program.LoadMapping().Where(m => m.Value.StartsWith("Drop", StringComparison.Ordinal) || m.Value.StartsWith("Not applicable", StringComparison.Ordinal)))
        {
            var reason = Program.UserFacingReason(proposal);
            Assert.False(string.IsNullOrWhiteSpace(reason), id);
            Assert.DoesNotMatch(@"(?i)findings|private app|user decision", reason);
        }

        Assert.Equal("Missing documentation on enum members; the only fix is placeholder text.", Program.UserFacingReason("Drop: Missing documentation on enum members; the only fix is placeholder text (no stubs, user decision). 530 findings in the surveyed app."));
    }

    [Fact]
    public void TupleElementCasing_ComesFromStyleCopJson()
    {
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("stylebro_tuple_element_name_casing", StringComparison.Ordinal));

        Write("stylecop.json", """{ "settings": { "namingRules": { "tupleElementNameCasing": "camelCase", "includeInferredTupleElementNames": true } } }""");
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("stylebro_tuple_element_name_casing = camelCase", result.Lines);
        Assert.Contains(result.Notes, n => n.Contains("includeInferredTupleElementNames", StringComparison.Ordinal));
    }

    [Fact]
    public void HungarianPrefixes_ComeFromStyleCopJson()
    {
        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1310.severity = none", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("stylebro_allow", StringComparison.Ordinal) && l.Contains("hungarian"));

        Write("stylecop.json", """{ "settings": { "namingRules": { "allowedHungarianPrefixes": ["db", "ui"], "allowCommonHungarianPrefixes": false } } }""");
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1305.severity = warning\n");
        lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1310.severity = warning", lines);
        Assert.Contains("stylebro_allowed_hungarian_prefixes = db, ui", lines);
        Assert.Contains("stylebro_allow_common_hungarian_prefixes = false", lines);
    }

    [Fact]
    public void AllowedNamespaceComponents_ComeFromStyleCopJson()
    {
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("stylebro_allowed_namespace", StringComparison.Ordinal));

        Write("stylecop.json", """{ "settings": { "namingRules": { "allowedNamespaceComponents": ["eBay", "iOS"] } } }""");
        Assert.Contains("stylebro_allowed_namespace_components = eBay, iOS", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void AccessModifiers_AreBro1404AndBro1007_EachWithItsStyleCopRule()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1400.severity = none\n");
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("dotnet_diagnostic.IDE0040.severity = none", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1404.severity = none", result.Lines);
        Assert.Contains("dotnet_diagnostic.BRO1007.severity = warning", result.Lines);
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
        Assert.Equal(
            """
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
            """.Replace("\r\n", "\n"),
            text.Replace("\r\n", "\n"));
        Assert.Equal(0, Suppressions.Rewrite(text, replacements).Added);
    }

    [Fact]
    public void EveryStyleBroRule_NamesTheStyleCopRulesItReplaces()
    {
        var rules = Migration.StyleBroRules();

        Assert.NotEmpty(rules);
        Assert.Equal(["SA1201", "SA1202", "SA1203", "SA1204", "SA1214"], rules.Single(r => r.Id == "BRO1001").StyleCop);

        // A rule without a StyleCop rule is one beyond StyleCop: listed as such on the differences page.
        var page = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "differences-from-stylecop.md"));
        var beyond = page.Substring(page.IndexOf("## Rules beyond StyleCop", StringComparison.Ordinal));
        beyond = beyond.Substring(0, beyond.IndexOf("\n## ", 1, StringComparison.Ordinal) is var end and > 0 ? end : beyond.Length);
        Assert.All(rules.Where(r => r.StyleCop.Count == 0), r => Assert.True(
            beyond.Contains($"[{r.Id}](rules/{r.Id}.md)", StringComparison.Ordinal),
            $"{r.Id} names no StyleCop rule it replaces; add 'Replaces StyleCop SAxxxx' to its description or list it under 'Rules beyond StyleCop'."));
    }

    [Fact]
    public void RulesBeyondStyleCop_AreTurnedOff()
    {
        // A StyleCop-clean repository shouldn't change because of rules StyleCop doesn't have.
        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.All(Migration.StyleBroRules().Where(r => r.StyleCop.Count == 0), r => Assert.Contains($"dotnet_diagnostic.{r.Id}.severity = none", lines));
        Assert.Contains("dotnet_diagnostic.BRO1520.severity = none", lines);
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
        Assert.Contains("dotnet_diagnostic.BRO1517.severity = none", lines);
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
    public void NonEnglishDocumentationCulture_TurnsOffTheEnglishSentenceRules()
    {
        Write("Directory.Build.props", "<Project><PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup></Project>");
        Write("stylecop.json", """{ "settings": { "documentationRules": { "documentationCulture": "de-DE" } } }""");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        foreach (var id in new[] { "BRO1604", "BRO1605", "BRO1606", "BRO1607" })
        {
            Assert.Contains($"dotnet_diagnostic.{id}.severity = none", result.Lines);
        }

        Assert.Contains("dotnet_diagnostic.BRO1603.severity = warning", result.Lines);
        Assert.Contains("de-DE", result.Reasons["SA1642"]);
        Assert.Contains(result.Notes, n => n.Contains("documentationCulture", StringComparison.Ordinal));

        Write("stylecop.json", """{ "settings": { "documentationRules": { "documentationCulture": "en-GB" } } }""");

        Assert.Contains("dotnet_diagnostic.BRO1606.severity = warning", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void ExcludeFromPunctuationCheck_IsWrittenWhenItDiffersFromTheDefault()
    {
        static List<string> Excluded(List<string> lines) =>
            lines.Where(l => l.StartsWith("stylebro_exclude_from_punctuation_check", StringComparison.Ordinal)).ToList();

        Assert.Empty(Excluded(Migration.Generate(StyleCopSetup.Read(root), root).Lines));

        Write("stylecop.json", """{ "settings": { "documentationRules": { "excludeFromPunctuationCheck": [ "seealso" ] } } }""");
        Assert.Empty(Excluded(Migration.Generate(StyleCopSetup.Read(root), root).Lines));

        Write("stylecop.json", """{ "settings": { "documentationRules": { "excludeFromPunctuationCheck": [ "seealso", "example", "summary" ] } } }""");
        Assert.Equal(["stylebro_exclude_from_punctuation_check = seealso, example, summary"], Excluded(Migration.Generate(StyleCopSetup.Read(root), root).Lines));
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
    public void TheBaseline_CoversEverySdkRuleInitOrTheToolTurnsOn()
    {
        var preset = Path.Combine(RepositoryRoot(), "src", "StyleBro.Package", "build", "stylebro.recommended.globalconfig");

        // A repository with a plain file header and SX1101 turns on IDE0073 and IDE0003 too.
        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false } } }""");
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SX1101.severity = warning\n");
        var ids = File.ReadAllLines(preset).Concat(InitCommand.Template().Split('\n')).Concat(InitCommand.ModernizeTemplate().Split('\n'))
            .Concat(Migration.Generate(StyleCopSetup.Read(root), root).Lines)
            .Select(l => System.Text.RegularExpressions.Regex.Match(l, @"^dotnet_diagnostic\.((?:IDE|CA)\d{4})\.severity"))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        Assert.Contains("IDE0073", ids);
        Assert.All(ids, id => Assert.Contains(id, StyleBro.Analyzers.Baseline.BaselineSuppressor.SdkIds));
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

    [Fact]
    public void Init_InAMultiTargetedRepository_KeepsFormattingOn()
    {
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>net8.0;net10.0</TargetFrameworks></PropertyGroup></Project>");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        // 'stylebro-migrate format' makes IDE0055 safe there; init points to it instead of turning the rule down.
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0036.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0048.severity = none", InitCommand.Block());
        Assert.Contains("dotnet_diagnostic.IDE0047.severity = none", InitCommand.Block());
        Assert.Contains("dotnet_diagnostic.IDE0011.severity = none", InitCommand.Block());
        Assert.Contains("dotnet_diagnostic.IDE0040.severity = none", InitCommand.Block());
    }

    [Fact]
    public void Modernize_InASingleTargetRepository_TurnsEveryTierOn()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--modernize" }));
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        Assert.Contains("dotnet_diagnostic.IDE0036.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0041.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0090.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.CA1510.severity = warning", editorConfig);
        Assert.Contains("csharp_style_namespace_declarations = file_scoped", editorConfig);
        Assert.DoesNotContain("suggestion", editorConfig);

        // Debated or conflicting rules stay opt-in by hand (docs/modernizing.md).
        Assert.All(new[] { "IDE0251", "IDE0290", "IDE0305", "IDE0066", "IDE0063", "CA1866", "IDE0038", "CA1860" }, id => Assert.DoesNotContain(id, editorConfig));
    }

    [Fact]
    public void Modernize_InAMultiTargetedRepository_MakesLanguageRulesSuggestions_ApiRulesStayWarnings()
    {
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks></PropertyGroup></Project>");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--modernize" }));
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        Assert.Contains("dotnet_diagnostic.IDE0041.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0090.severity = suggestion", editorConfig);

        // SDK 10's IDE0031 writes C# 14's 'node?.Count = 2;' (CS8370 in a net48 copy), IDE0350 '(out r)'.
        Assert.Contains("dotnet_diagnostic.IDE0031.severity = suggestion", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0350.severity = suggestion", editorConfig);

        // The multi-target guard (MultiTargetSuppressor) hides the API rules where a framework lacks the API.
        Assert.Contains("dotnet_diagnostic.CA1510.severity = warning", editorConfig);
        Assert.Contains("dotnet_diagnostic.IDE0330.severity = warning", editorConfig);
    }

    [Fact]
    public void Modernize_WithLangVersionInDirectoryBuildProps_TurnsLanguageRulesOn()
    {
        Write("Directory.Build.props", "<Project><PropertyGroup><LangVersion>latest</LangVersion></PropertyGroup></Project>");
        Write("src/Directory.Build.props", "<Project><Import Project=\"$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" /></Project>");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>net48;net8.0</TargetFrameworks></PropertyGroup></Project>");

        Assert.True(InitCommand.SetsLangVersion(root, Path.Combine("src", "Lib", "Lib.csproj")));
        var block = InitCommand.Modernize(root, InitCommand.MultiTargetedProjects(root).ToList()).Block;
        Assert.Contains("dotnet_diagnostic.IDE0090.severity = warning", block);
        Assert.Contains("dotnet_diagnostic.CA1510.severity = warning", block);

        // A Directory.Build.props that doesn't import its parent's hides the LangVersion above it.
        Write("src/Directory.Build.props", "<Project />");
        Assert.False(InitCommand.SetsLangVersion(root, Path.Combine("src", "Lib", "Lib.csproj")));
    }

    [Fact]
    public void Modernize_IsIdempotent_KeepsOwnKeys_AndInitWithoutItLeavesTheBlock()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\ncsharp_style_namespace_declarations = block_scoped\n");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--modernize" }));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains(InitCommand.ModernizeBegin, first);
        Assert.DoesNotContain("file_scoped", first);
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--modernize" }));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));

        // Next to settings from 'stylebro-migrate --write' the modernize block is still added.
        Write(".editorconfig", Migration.Apply("root = true\n", Migration.Render(new[] { ("[*.cs]", new List<string> { "dotnet_diagnostic.IDE0040.severity = none" }) })));
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write", "--modernize" }));
        var migrated = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.IDE0040.severity = none", migrated);
        Assert.Contains("dotnet_diagnostic.IDE0090.severity = warning", migrated);
        Assert.DoesNotContain("stylebro-migrate init:", migrated);
    }

    [Fact]
    public void TheMultiTargetGuard_CoversExactlyTierC()
    {
        var tierC = InitCommand.ModernizeTemplate().Replace("\r\n", "\n").Split("# Tier C")[1].Split('\n')
            .Where(l => l.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal))
            .Select(l => l.Split('.')[1]);

        Assert.Equal(tierC.Order(), StyleBro.Analyzers.Modernize.MultiTargetSuppressor.Minimums.Keys.Order());
    }

    [Fact]
    public void Init_WithoutModernize_WritesNoModernizationRules()
    {
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));

        Assert.DoesNotContain("modernize", File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void ThePreset_SetsNoBuiltInRuleSeverities()
    {
        // 'dotnet format' ignores rule severities in a package's global config; they belong in 'stylebro-migrate init'.
        var preset = File.ReadAllLines(Path.Combine(RepositoryRoot(), "src", "StyleBro.Package", "build", "stylebro.recommended.globalconfig"));

        Assert.DoesNotContain(preset, l => l.StartsWith("dotnet_diagnostic.IDE", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_WritesTheBuiltInRulesOnce_AndLeavesMigratedSettingsAlone()
    {
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.StartsWith("root = true\n", first);
        Assert.Contains("dotnet_diagnostic.IDE0036.severity = warning", first);
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));

        var migrated = Migration.Apply("root = true\n", Migration.Render(new[] { ("[*.cs]", new List<string> { "dotnet_diagnostic.IDE0040.severity = none" }) }));
        Write(".editorconfig", migrated);
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        Assert.Equal(migrated, File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Format_PlansOneRunPerTargetFramework_WithTheProjectsThatTargetIt()
    {
        var frameworks = new Dictionary<string, string[]>
        {
            ["src\\Lib\\Lib.csproj"] = new[] { "net8.0", "netstandard2.0" },
            ["test\\Tests\\Tests.csproj"] = new[] { "net8.0" },
        };

        var plan = FormatCommand.Plan(frameworks);

        Assert.Equal(new[] { "net8.0", "netstandard2.0" }, plan.Select(p => p.Framework));
        Assert.Equal(new[] { "src\\Lib\\Lib.csproj", "test\\Tests\\Tests.csproj" }, plan[0].Projects);
        Assert.Equal(new[] { "src\\Lib\\Lib.csproj" }, plan[1].Projects);
        Assert.Empty(FormatCommand.Plan(new Dictionary<string, string[]> { ["a.csproj"] = new[] { "net8.0" }, ["b.csproj"] = new[] { "net10.0" } }));
    }

    [Fact]
    public void Format_KeepsTheProjectsThatTargetTheFramework_AndThoseThatReferenceThem()
    {
        // Serilog: TestDummies (no net8.0) references Serilog (net8.0), so it's loaded for net8.0 too; Newtonsoft.Json:
        // the library (no net46) is referenced by the net46 tests, so it loads as usual.
        var projects = new Dictionary<string, (string[] Frameworks, string[] References)>
        {
            ["Serilog"] = (new[] { "net8.0", "netstandard2.0" }, Array.Empty<string>()),
            ["TestDummies"] = (new[] { "netstandard2.0", "net462" }, new[] { "Serilog" }),
            ["Serilog.Tests"] = (new[] { "net8.0", "net48" }, new[] { "Serilog", "TestDummies" }),
            ["Lib"] = (new[] { "net45" }, Array.Empty<string>()),
            ["Lib.Tests"] = (new[] { "net46" }, new[] { "Lib" }),
        };

        Assert.Equal(new[] { "Serilog", "Serilog.Tests", "TestDummies" }, FormatCommand.Keep(projects, "net8.0").OrderBy(p => p));
        Assert.Equal(new[] { "Lib.Tests" }, FormatCommand.Keep(projects, "net46"));
    }

    [Fact]
    public void Format_ReadsTheFrameworks_AndWritesASolutionFilter()
    {
        Assert.Equal(new[] { "net8.0", "net10.0" }, FormatCommand.ParseProject("""{ "Properties": { "TargetFrameworks": "net8.0; net10.0;", "TargetFramework": "" } }""").Frameworks);
        var single = FormatCommand.ParseProject("""{ "Properties": { "TargetFrameworks": "", "TargetFramework": "net8.0" }, "Items": { "ProjectReference": [ { "Identity": "../Lib/Lib.csproj", "FullPath": "C:/r/Lib/Lib.csproj" } ] } }""");
        Assert.Equal(new[] { "net8.0" }, single.Frameworks);
        Assert.Equal(new[] { "C:/r/Lib/Lib.csproj" }, single.References);

        Assert.Equal(new[] { "src/Lib/" }, FormatCommand.Include(root, root, new[] { Path.Combine("src", "Lib", "Lib.csproj"), Path.Combine("src", "Lib", "Other.csproj") }));
        Assert.Equal(new[] { "**/*.cs" }, FormatCommand.Include(root, root, new[] { "App.csproj" }));

        using var filter = JsonDocument.Parse(FormatCommand.SolutionFilter("App.slnx", new[] { "src\\Lib\\Lib.csproj" }));
        Assert.Equal("App.slnx", filter.RootElement.GetProperty("solution").GetProperty("path").GetString());
        Assert.Equal("src\\Lib\\Lib.csproj", filter.RootElement.GetProperty("solution").GetProperty("projects")[0].GetString());
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
