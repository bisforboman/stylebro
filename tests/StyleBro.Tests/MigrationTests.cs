using System.Text.Json;
using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed class MigrationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("stylebro-migrate-").FullName;

    /// <inheritdoc/>
    public void Dispose() => DeleteFolder(root);

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
            Assert.DoesNotMatch("(?i)findings|private app|user decision", reason);
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
    public void PlainHeader_WithTheTextRuleOff_LeavesIde0073Off()
    {
        // With a plain header SA1633 only wants a header; SA1636 compares the text. IDE0073 always compares it.
        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false } } }""");
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1636.severity = none\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.DoesNotContain(result.Lines, l => l.StartsWith("file_header_template", StringComparison.Ordinal));
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = none", result.Lines);
        Assert.Contains("SA1633", result.Reasons.Keys);
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
            .Concat(SonarSetup.Mapping.Select(m => $"dotnet_diagnostic.{m.Rule}.severity"))
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
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\ndotnet_diagnostic.SX1101.severity = warning\ndotnet_diagnostic.SA1309.severity = none\ndotnet_diagnostic.SX1309.severity = warning\ndotnet_diagnostic.SX1309S.severity = warning\ndotnet_diagnostic.SA1412.severity = warning\n");
        Write("C.cs", "class C { private int count; private int other; }"); // the code would say camelCase; SX1309 wins

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("dotnet_diagnostic.IDE0003.severity = warning", result.Lines);
        Assert.Contains("dotnet_diagnostic.IDE0009.severity = none", result.Lines);
        Assert.Contains("stylebro_private_field_naming = _camelCase", result.Lines);
        Assert.Contains("charset = utf-8-bom", result.Lines);
        Assert.Contains("SX1101", result.Covered);
        Assert.Contains("SX1309", result.Covered);
        Assert.Contains("SX1309S", result.Covered);
        Assert.Contains("SA1412", result.Covered);
    }

    [Fact]
    public void StaticUnderscoreAlone_IsNotExpressible_WhenInstanceFieldsAreCamelCase()
    {
        // SX1309S wants '_' on private static fields only; BRO1303 has one style for those and instance fields.
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1309.severity = none\ndotnet_diagnostic.SX1309S.severity = warning\n");
        Write("C.cs", "class C { private int count; private int other; private static int _instances; }");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("stylebro_private_field_naming = camelCase", result.Lines);
        Assert.DoesNotContain("SX1309S", result.Covered);
        Assert.Contains("SX1309S", result.Reasons.Keys);
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

    [Fact]
    public void StaticFieldCasing_IsPinnedToStyleCopsPascalCase()
    {
        // A camel-case naming rule for static fields that StyleCop never enforced (IDE1006 off) must not rename them.
        Write(".editorconfig", "[*.cs]\ndotnet_naming_rule.static_fields.symbols = static_fields\ndotnet_naming_symbols.static_fields.required_modifiers = static\n");

        Assert.Contains($"{StyleBro.Analyzers.Naming.FieldNames.StaticStyleKey} = PascalCase", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }

    [Fact]
    public void Submodules_AreSkipped()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = none\n");
        Write("ext/Lib/.git", "gitdir: ../../.git/modules/ext/Lib\n");
        Write("ext/Lib/.editorconfig", "[*.cs]\ndotnet_diagnostic.SA1101.severity = warning\n");
        Write("ext/Lib/src/Code.cs", "#pragma warning disable SA1202\nclass C { }\n");
        Write("ext/Lib/src/Lib.csproj", "<Project><PropertyGroup><TargetFrameworks>net8.0;net48</TargetFrameworks></PropertyGroup></Project>");
        Write("ext/Clone/.git/HEAD", "ref: refs/heads/main\n");
        Write("ext/Clone/Code.cs", "class D { }\n");

        var files = StyleCopSetup.EnumerateFiles(root).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/')).ToList();

        Assert.Equal(new[] { ".editorconfig" }, files);
        Assert.Empty(StyleCopSetup.Read(root).Scopes);
        Assert.Empty(InitCommand.MultiTargetedProjects(root));
        Assert.Equal(0, Program.Main(new[] { root, "--write" }));
        Assert.Equal("#pragma warning disable SA1202\nclass C { }\n", File.ReadAllText(Path.Combine(root, "ext/Lib/src/Code.cs")));
        Assert.DoesNotContain(Migration.BeginMarker, File.ReadAllText(Path.Combine(root, "ext/Lib/.editorconfig")));
    }

    [Fact]
    public void AllRulesOff_InAFolder_TurnsTheReplacingRulesOffThere()
    {
        // A folder of vendored code: 'dotnet_analyzer_diagnostic.severity = none' silences StyleCop there, but not the
        // StyleBro and SDK rules the root block turns on by id (an id beats the bulk setting). So they're written off.
        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false } } }""");
        Write("ext/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = none\n");

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);
        var scope = Assert.Single(setup.Scopes);
        var lines = Migration.GenerateScope(setup, scope, root, result);

        Assert.Contains("dotnet_diagnostic.BRO1306.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.IDE0065.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1514.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = none", lines);
    }

    [Fact]
    public void AllRulesBulkSeverity_DoesNotTurnOnRulesThatAreOffByDefault()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = error\n");

        var setup = StyleCopSetup.Read(root);

        Assert.Equal(Severity.Error, setup.Severities["SA1101"]);
        Assert.False(setup.IsOn("SA1305"));
    }

    [Fact]
    public void NextStep_NamesStyleBroFormat()
    {
        // Single- or multi-targeted: plain 'dotnet format' also applies other analyzers' fixes (owner's decision 2026-10-09).
        Assert.Contains("run 'stylebro-migrate format'", Program.NextStep);
        Assert.Contains(Program.FormatHint, Capture(() => InitCommand.Run(new[] { root, "--write" })));
    }

    [Fact]
    public void FormatReports_AreMergedPerFile_WithoutDuplicates()
    {
        const string Net8 = """
            [{ "DocumentId": { "Id": "1" }, "FileName": "A.cs", "FilePath": "C:\\r\\A.cs",
               "FileChanges": [{ "LineNumber": 1, "CharNumber": 1, "DiagnosticId": "BRO1001", "FormatDescription": "x" }] }]
            """;
        const string Net48 = """
            [{ "DocumentId": { "Id": "2" }, "FileName": "A.cs", "FilePath": "C:\\r\\A.cs",
               "FileChanges": [{ "LineNumber": 1, "CharNumber": 1, "DiagnosticId": "BRO1001", "FormatDescription": "x" },
                               { "LineNumber": 9, "CharNumber": 1, "DiagnosticId": "IDE0055", "FormatDescription": "y" }] },
             { "DocumentId": { "Id": "3" }, "FileName": "B.cs", "FilePath": "C:\\r\\B.cs", "FileChanges": [] }]
            """;

        using var merged = JsonDocument.Parse(FormatCommand.MergeReports(new[] { Net8, Net48 }));

        var files = merged.RootElement.EnumerateArray().ToList();
        Assert.Equal(new[] { "A.cs", "B.cs" }, files.Select(f => f.GetProperty("FileName").GetString()));
        Assert.Equal(new[] { "BRO1001", "IDE0055" }, files[0].GetProperty("FileChanges").EnumerateArray().Select(c => c.GetProperty("DiagnosticId").GetString()));
    }

    [Fact]
    public void FormatOutput_ShowsEachLineOnce()
    {
        var shown = new HashSet<string>();
        var lines = new[] { "Warnings were encountered while loading the workspace.", "A.cs(1,1): warning BRO1001", string.Empty, "Warnings were encountered while loading the workspace.", "A.cs(1,1): warning BRO1001", string.Empty };

        Assert.Equal(new[] { lines[0], lines[1], string.Empty, string.Empty }, lines.Where(l => FormatCommand.IsNewLine(shown, l)));
    }

    [Fact]
    public void SettingsInASubmodule_AreRead_WhenTheRepositoryPointsThere()
    {
        // SixLabors.Fonts: the StyleCop setup lives in a shared-infrastructure submodule that the repository imports.
        Write("shared/.git", "gitdir: ../.git/modules/shared\n");
        Write("shared/base.ruleset", """
            <RuleSet Name="base" ToolsVersion="17.0">
              <Rules AnalyzerId="StyleCop.Analyzers" RuleNamespace="StyleCop.Analyzers">
                <Rule Id="SA1413" Action="None" />
                <Rule Id="SA1101" Action="Warning" />
              </Rules>
            </RuleSet>
            """);
        Write("shared/stylecop.json", """{ "settings": { "orderingRules": { "elementOrder": ["kind"] }, "documentationRules": { "xmlHeader": false } } }""");
        Write("shared/props/Global.props", """
            <Project>
              <PropertyGroup><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
                <AdditionalFiles Include="$(MSBuildThisFileDirectory)..\stylecop.json" />
              </ItemGroup>
            </Project>
            """);
        Write("Directory.Build.props", """<Project><Import Project="$(MSBuildThisFileDirectory)shared\props\Global.props" /></Project>""");
        Write("src/App.ruleset", """
            <RuleSet Name="app" ToolsVersion="17.0">
              <Include Path="..\shared\base.ruleset" Action="Default" />
              <Rules AnalyzerId="StyleCop.Analyzers" RuleNamespace="StyleCop.Analyzers">
                <Rule Id="SA1101" Action="None" />
              </Rules>
            </RuleSet>
            """);

        // A bulk severity doesn't override a ruleset's own entry for a rule (the compiler doesn't either).
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_analyzer_diagnostic.severity = warning\n");

        var setup = StyleCopSetup.Read(root);
        var lines = Migration.Generate(setup, root).Lines;

        Assert.False(setup.IsOn("SA1413"));  // from the included ruleset
        Assert.False(setup.IsOn("SA1101"));  // the including ruleset's own entry wins
        Assert.True(setup.DocumentationParsed);
        Assert.Equal("1.2.0-beta.556", setup.Version);
        Assert.Contains(Path.Combine("shared", "stylecop.json"), setup.Sources);
        Assert.Contains("dotnet_diagnostic.BRO1401.severity = none", lines);
        Assert.Contains("stylebro_member_constants_first = false", lines);
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = none", lines);  // elementOrder without accessibility
        Assert.Contains("dotnet_diagnostic.BRO1615.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = warning", lines);
        Assert.Contains(StyleCopSetup.Evidence(root), e => e.Contains("stylecop.json", StringComparison.Ordinal));
    }

    [Fact]
    public void ARulesetAFoldersPropsSelects_IsAScope()
    {
        // Tests with ordering off: production code keeps BRO1001, the tests folder gets it off.
        Write("tests/tests.ruleset", """
            <RuleSet Name="t" ToolsVersion="17.0">
              <Rules AnalyzerId="StyleCop.Analyzers" RuleNamespace="StyleCop.Analyzers">
                <Rule Id="SA1201" Action="None" />
              </Rules>
            </RuleSet>
            """);
        Write("tests/Directory.Build.props", """<Project><PropertyGroup><CodeAnalysisRuleSet Condition="'$(CodeAnalysisRuleSet)' == ''">$(MSBuildThisFileDirectory)tests.ruleset</CodeAnalysisRuleSet></PropertyGroup></Project>""");
        Write("tests/App.Tests/App.Tests.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var setup = StyleCopSetup.Read(root);
        var plan = Migration.Plan(setup, root, Migration.Generate(setup, root));

        Assert.True(setup.IsOn("SA1201"));
        Assert.Contains("dotnet_diagnostic.BRO1001.severity = warning", plan[".editorconfig"][0].Lines);
        var (section, lines) = Assert.Single(plan[Path.Combine("tests", ".editorconfig")]);
        Assert.Equal("*.cs", section);
        Assert.Equal(["dotnet_diagnostic.BRO1001.severity = none"], lines);
    }

    [Fact]
    public void XmlHeader_WithoutACompanyName_LeavesBro1615Off_AndIde0073IsNeverOnWithIt()
    {
        // StyleCop's default company is 'PlaceholderCompany': a header nobody wants, so BRO1615 isn't turned on for it.
        var noCompany = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("dotnet_diagnostic.BRO1615.severity = none", noCompany.Lines);
        Assert.DoesNotContain(noCompany.Lines, l => l.Contains("PlaceholderCompany", StringComparison.Ordinal));
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = none", noCompany.Lines);
        Assert.Contains(noCompany.Reasons.Values, r => r.Contains("companyName", StringComparison.Ordinal));

        Write("stylecop.json", """{ "settings": { "documentationRules": { "companyName": "Contoso" } } }""");
        var withCompany = Migration.Generate(StyleCopSetup.Read(root), root).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1615.severity = warning", withCompany);
        Assert.Contains("dotnet_diagnostic.IDE0073.severity = none", withCompany);
    }

    [Fact]
    public void Init_WithTheRepositorysOwnPlainHeader_TurnsBro1615Off()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nfile_header_template = Copyright (c) Contoso.\n");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));

        Assert.Contains("dotnet_diagnostic.BRO1615.severity = none", File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.DoesNotContain("BRO1615", InitCommand.Block());
    }

    [Fact]
    public void Format_ExcludesSubmodules_AndFixesOnlyStyleBroAndTheBuiltInRulesByDefault()
    {
        Write("ext/Lib/.git", "gitdir: ../../.git/modules/ext/Lib\n");
        Write("ext/Lib/Code.cs", "class C { }\n");
        Write("tools/Vendored/.git/HEAD", "ref: refs/heads/main\n");
        Write("src/App/App.cs", "class D { }\n");

        Assert.Equal(new[] { "ext/Lib/", "tools/Vendored/" }, StyleCopSetup.NestedRepositories(root));

        var ids = FormatCommand.Diagnostics(root);
        Assert.Contains("BRO1001", ids);
        Assert.Contains("BRO1520", ids);
        Assert.Contains("IDE0055", ids);    // whitespace formatting's rule, from init's template
        Assert.Contains("IDE0036", ids);
        Assert.DoesNotContain("IDE0090", ids);  // a modernization rule: only with init --modernize's block
        Assert.DoesNotContain(ids, id => id.StartsWith("CS", StringComparison.Ordinal) || id.StartsWith("S1", StringComparison.Ordinal) || id == "IDE1006");

        // An SDK rule only the migration's block turns on.
        Write(".editorconfig", "root = true\n" + Migration.Render(new[] { ("*.cs", new List<string> { "dotnet_diagnostic.IDE0009.severity = warning", "dotnet_diagnostic.IDE0003.severity = none" }) }));
        Assert.Contains("IDE0003", FormatCommand.Diagnostics(root));

        File.AppendAllText(Path.Combine(root, ".editorconfig"), $"\n{InitCommand.ModernizeBegin}\n[*.cs]\ndotnet_diagnostic.IDE0090.severity = warning\n{InitCommand.ModernizeEnd}\n");
        Assert.Contains("IDE0090", FormatCommand.Diagnostics(root));
    }

    [Fact]
    public void Init_KeepsALeadingUnderscore_WhenMostPrivateFieldsHaveIt()
    {
        Write("src/A.cs", "class A { private int _a; private int _b; private readonly string _c; private int d; }");
        Write("src/Migrations/20240101_Init.cs", "class M { private int x; private int y; private int z; private int w; }");
        Write("src/Form.Designer.cs", "class F { private int button1; private int button2; }");

        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        Assert.Contains("stylebro_private_field_naming = _camelCase", File.ReadAllText(Path.Combine(root, ".editorconfig")));

        // Below three quarters: StyleBro's default (camelCase), nothing written.
        Write("src/B.cs", "class B { private int e; }");
        File.Delete(Path.Combine(root, ".editorconfig"));
        Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }));
        Assert.DoesNotContain("stylebro_private_field_naming", File.ReadAllText(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Init_StopsInAStyleCopRepository_AndPointsToTheMigration()
    {
        // Spectre.Console: no stylecop.json, but StyleCop rules turned off in .editorconfig.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1309.severity = none\n# SA1101 is fine\n");
        var before = File.ReadAllText(Path.Combine(root, ".editorconfig"));

        var output = Capture(() => Assert.Equal(1, InitCommand.Run(new[] { root, "--write" })));

        Assert.Equal(before, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains(".editorconfig (StyleCop rule severities)", output, StringComparison.Ordinal);
        Assert.Contains("stylebro-migrate --write", output, StringComparison.Ordinal);

        File.Delete(Path.Combine(root, ".editorconfig"));
        Write("Directory.Packages.props", "<Project><ItemGroup><PackageVersion Include=\"StyleCop.Analyzers\" Version=\"1.1.118\" /></ItemGroup></Project>");
        Assert.Equal(new[] { "Directory.Packages.props (StyleCop.Analyzers package)" }, StyleCopSetup.Evidence(root));

        File.Delete(Path.Combine(root, "Directory.Packages.props"));
        Write("Code.cs", "// SA1101\n");
        Assert.Empty(StyleCopSetup.Evidence(root));
    }

    [Fact]
    public void EveryRuleTheMigrationTurnsOff_IsWrittenOffByItsId()
    {
        // 'dotnet_analyzer_diagnostic.severity = warning' (SixLabors) turns on every rule without its own entry, also
        // StyleBro's off-by-default ones: the block names each rule, so a bulk severity can't reach them.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_analyzer_diagnostic.severity = warning\n");

        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;

        Assert.All(Migration.StyleBroRules(), r => Assert.Single(lines, l => l.StartsWith($"dotnet_diagnostic.{r.Id}.severity = ", StringComparison.Ordinal)));
        Assert.Contains("dotnet_diagnostic.BRO1310.severity = none", lines);  // off by default (SA1305 too)
        Assert.Contains("dotnet_diagnostic.BRO1520.severity = none", lines);  // beyond StyleCop
        Assert.Contains("dotnet_diagnostic.BRO1135.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1525.severity = none", lines);
    }

    [Fact]
    public void ABuildThatCopiesAnEditorConfig_GetsANote()
    {
        Write("Directory.Build.targets", """<Project><ItemGroup><ConfigFilesToCopy Include="$(MSBuildThisFileDirectory)shared\.editorconfig" /></ItemGroup></Project>""");

        Assert.Contains(Migration.Generate(StyleCopSetup.Read(root), root).Notes, n => n.StartsWith("Directory.Build.targets copies an .editorconfig", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("help")]
    public void Help_PrintsTheUsageOfEveryCommand_AndChangesNothing(string option)
    {
        Write("stylecop.json", "{}");

        var output = Capture(() => Assert.Equal(0, Program.Main(new[] { option })));
        Assert.Equal(0, Program.Main(new[] { "init", root, "--help" }));

        Assert.All(new[] { "stylebro-migrate [path] [--write]", "stylebro-migrate init", "stylebro-migrate format", "stylebro-migrate baseline" }, u => Assert.Contains(u, output, StringComparison.Ordinal));
        Assert.Equal(new[] { "stylecop.json" }, Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName));
    }

    [Fact]
    public void UnknownOptions_FailWithTheUsage()
    {
        Assert.Equal(1, Program.Main(new[] { root, "--wirte" }));
        Assert.Equal(1, Program.Main(new[] { "init", root, "--modernise" }));
        Assert.Equal(1, Program.Main(new[] { "baseline", root, "--projct", "x.sln" }));
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void ThePackageReadmes_HaveOnlyAbsoluteLinks()
    {
        // nuget.org renders a relative link as href="": the packed READMEs link to GitHub.
        foreach (var readme in new[] { "README.md", Path.Combine("src", "StyleBro.Migrate", "README.md") })
        {
            var text = File.ReadAllText(Path.Combine(RepositoryRoot(), readme));
            Assert.DoesNotMatch(@"\]\((?!https?://)[^)]*\)|^\s*\[[^\]]+\]:\s*(?!https?://)|(?:href|src)=""(?!https?://)", text);
        }
    }

    [Fact]
    public void PreviewCopy_TakesTrackedAndUntrackedFiles_NotIgnoredOnes()
    {
        Write(".gitignore", "ignored.txt\nbin/\n");
        Write("src/Tracked.cs", "class A { }\n");
        Write("src/bin/Debug/App.dll", "binary");
        PreviewCommand.MakeRepository(root);
        Write("src/Untracked.cs", "class B { }\n");
        Write("ignored.txt", "secret");
        Write("ext/Lib/Code.cs", "class C { }\n");
        PreviewCommand.MakeRepository(Path.Combine(root, "ext", "Lib")); // a repository inside: its working tree comes along
        var before = Hashes(root);
        var target = Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName;
        try
        {
            PreviewCommand.Copy(root, target);

            var copied = Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(target, f).Replace('\\', '/'))
                .Where(f => !f.Contains(".git/", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal);
            Assert.Equal(new[] { ".gitignore", "ext/Lib/Code.cs", "src/Tracked.cs", "src/Untracked.cs" }, copied);
            Assert.True(Directory.Exists(Path.Combine(target, "ext", "Lib", ".git"))); // still a repository of its own: format skips it
            Assert.Equal(before, Hashes(root));
        }
        finally
        {
            DeleteFolder(target);
        }
    }

    [Fact]
    public void Preview_AppliesInitAndFormatToACopy_WritesThePatch_AndLeavesTheRepositoryAlone()
    {
        Write("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "class C { int b; int a; }\n");
        Write("D.cs", "class D {  }\n");
        PreviewCommand.MakeRepository(root);
        var before = Hashes(root);
        var patch = Path.Combine(Directory.CreateTempSubdirectory("stylebro-preview-test-").FullName, "out.patch");
        var runs = 0;
        int Format(string[] args, Action<string> log)
        {
            var folder = args[0];
            Assert.True(File.Exists(Path.Combine(folder, ".editorconfig"))); // init ran first, in the copy
            var report = Array.IndexOf(args, "--report");
            if (report >= 0)
            {
                Directory.CreateDirectory(args[report + 1]);
                var c = Path.Combine(folder, "C.cs").Replace("\\", "\\\\");
                var d = Path.Combine(folder, "D.cs").Replace("\\", "\\\\");
                File.WriteAllText(Path.Combine(args[report + 1], "format-report.json"), $$"""
                    [ { "FilePath": "{{c}}", "FileChanges": [ { "LineNumber": 1, "CharNumber": 11, "DiagnosticId": "BRO1001" } ] },
                      { "FilePath": "{{d}}", "FileChanges": [ { "LineNumber": 1, "CharNumber": 10, "DiagnosticId": "WHITESPACE" } ] } ]
                    """);
                return 2;
            }

            runs++;
            File.WriteAllText(Path.Combine(folder, "C.cs"), "class C { int a; int b; }\n");
            File.WriteAllText(Path.Combine(folder, "D.cs"), "class D { }\n");
            return 0;
        }

        var output = Capture(() => Assert.Equal(0, PreviewCommand.Run("init", new[] { root, "--diff=" + patch, "--keep" }, Format)));

        Assert.Equal(2, runs); // the second run changed nothing
        Assert.Equal(before, Hashes(root));
        Assert.Contains("Format: 2 files, 2 reported changes, clean after 1 run", output);
        Assert.Matches("BRO1001 .* 1 file", output);
        Assert.Matches("IDE0055 .* 1 file", output);
        Assert.Contains("+class C { int a; int b; }", output); // the sample
        var text = File.ReadAllText(patch);
        Assert.Contains("+++ b/.editorconfig", text);
        Assert.Contains("+++ b/Directory.Build.props", text);
        Assert.Contains("Include=\"StyleBro.Analyzers\"", text);
        Assert.Contains("+class C { int a; int b; }", text);
        var kept = System.Text.RegularExpressions.Regex.Match(output, @"The copy is kept: (.*) \(log").Groups[1].Value;
        Assert.True(Directory.Exists(kept));
        DeleteFolder(Path.GetDirectoryName(kept)!);
        DeleteFolder(Path.GetDirectoryName(patch)!);
    }

    [Fact]
    public void PreviewAttribution_CountsFilesPerReportedRule_WhitespaceAndOther()
    {
        var report = """
            [ { "FilePath": "C:/Temp/stylebro-preview-x/repo/src/A.cs", "FileChanges": [
                  { "LineNumber": 9, "CharNumber": 1, "DiagnosticId": "BRO1001" }, { "LineNumber": 3, "CharNumber": 1, "DiagnosticId": "BRO1001" },
                  { "LineNumber": 3, "CharNumber": 1, "DiagnosticId": "BRO1001" }, { "LineNumber": 4, "CharNumber": 2, "DiagnosticId": "WHITESPACE" } ] },
              { "FilePath": "/tmp/stylebro-preview-x/repo/src/B.cs", "FileChanges": [ { "LineNumber": 5, "CharNumber": 1, "DiagnosticId": "BRO1514" } ] },
              { "FilePath": "/tmp/stylebro-preview-x/repo/src/Unchanged.cs", "FileChanges": [ { "LineNumber": 1, "CharNumber": 1, "DiagnosticId": "BRO1303" } ] } ]
            """;
        var changed = new[] { "src/A.cs", "src/B.cs", "src/Spaces.cs", "src/Moved.cs" };

        var (rules, changes) = PreviewCommand.Attribute(report, "stylebro-preview-x/repo", changed, f => f == "src/Spaces.cs");

        Assert.Equal(4, changes); // a diagnostic reported twice (two frameworks) counts once; Unchanged.cs isn't counted
        Assert.Equal(new[] { "BRO1001", "BRO1514", "IDE0055", "other" }, rules.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(3, rules["BRO1001"]["src/A.cs"]); // the first reported line, for the sample
        Assert.Equal(new[] { "src/A.cs", "src/Spaces.cs" }, rules["IDE0055"].Keys);
        Assert.Equal(new[] { "src/Moved.cs" }, rules["other"].Keys);
        Assert.Equal(new[] { ("IDE0055", "src/A.cs", 4), ("BRO1001", "src/A.cs", 3), ("BRO1514", "src/B.cs", 5) }, PreviewCommand.Samples(rules, 3));

        var summary = PreviewCommand.FormatSummary(4, changes, rules, new[] { true, false });
        Assert.StartsWith("Format: 4 files, 4 reported changes, clean after 1 run", summary);
        Assert.EndsWith("1 file", summary.Split('\n').Last()); // 'other' comes last
        Assert.Contains("other", summary.Split('\n').Last());
    }

    [Fact]
    public void PreviewHunk_IsTheOneCoveringTheLine()
    {
        var diff = "diff --git a/A.cs b/A.cs\n@@ -2 +2 @@\n-a\n+b\n@@ -10,2 +10,5 @@\n-if (x) return;\n-y\n+if (x)\n+{\n+    return;\n+}\n+y\n";

        Assert.Equal(new[] { "-a", "+b" }, PreviewCommand.Hunk(diff, 2, 12));
        Assert.Equal(new[] { "-if (x) return;", "-y", "+if (x)", "  ..." }, PreviewCommand.Hunk(diff, 11, 3));
        Assert.Equal(new[] { "-a", "+b" }, PreviewCommand.Hunk(diff, 99, 12)); // not found: the first hunk
    }

    [Fact]
    public void PreviewPackage_FollowsCentralPackageManagement()
    {
        Assert.Equal("<Project>\n  <ItemGroup>\n    <X />\n  </ItemGroup>\n</Project>\n", PreviewCommand.AddItem(null, "<X />"));
        Assert.Equal("<Project>\n  <A />\n  <ItemGroup>\n    <X />\n  </ItemGroup>\n</Project>", PreviewCommand.AddItem("<Project>\n  <A />\n</Project>", "<X />"));

        Write("Directory.Packages.props", "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup></Project>");
        Assert.NotNull(PreviewCommand.AddPackage(root));
        Assert.Contains("<PackageVersion Include=\"StyleBro.Analyzers\" Version=", File.ReadAllText(Path.Combine(root, "Directory.Packages.props")));
        Assert.Contains("<PackageReference Include=\"StyleBro.Analyzers\" PrivateAssets=\"all\" />", File.ReadAllText(Path.Combine(root, "Directory.Build.props")));
        Assert.Null(PreviewCommand.AddPackage(root)); // referenced now
    }

    [Fact]
    public void Preview_DiffAndWrite_DontGoTogether()
    {
        Assert.Equal(1, Program.Main(new[] { root, "--write", "--diff" }));
        Assert.Equal(1, Program.Main(new[] { "init", root, "--diffs" }));
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));
    }

    [Fact]
    public void Sonar_WithoutASetup_ChangesNothing()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1101.severity = none\n");

        Assert.Null(SonarSetup.Read(root));
        var result = Migration.Generate(StyleCopSetup.Read(root), root, sonar: SonarSetup.Read(root));
        Assert.Null(result.Sonar);
        Assert.DoesNotContain(result.Lines, l => l.Contains("Sonar", StringComparison.Ordinal));
        Assert.DoesNotContain("Sonar", Capture(() => Program.Main(new[] { root })), StringComparison.Ordinal);
    }

    [Fact]
    public void Sonar_ThePackage_BringsItsDefaultRules()
    {
        // Central package management: the version in Directory.Packages.props, the reference in Directory.Build.props.
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="SonarAnalyzer.CSharp" Version="10.34.0.3385" /></ItemGroup></Project>""");
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" PrivateAssets="all" /></ItemGroup></Project>""");

        var sonar = SonarSetup.Read(root)!;
        Assert.True(sonar.IsOn("S2325"));   // Sonar way
        Assert.False(sonar.IsOn("S1659"));  // off by default
        Assert.Contains(sonar.Sources, s => s.StartsWith("SonarAnalyzer.CSharp 10.34.0.3385", StringComparison.Ordinal));
        Assert.Equal(SonarSetup.Rules.Count(r => r.Value.SonarWay), sonar.Severities.Count(s => s.Value == Severity.Warning));

        var lines = Migration.Generate(StyleCopSetup.Read(root), root, sonar: sonar).Lines;
        Assert.Contains("dotnet_diagnostic.CA1822.severity = warning", lines);
        Assert.Contains("dotnet_code_quality.CA1822.api_surface = private, internal", lines);
        Assert.Contains("dotnet_diagnostic.BRO1313.severity = warning", lines);   // S927; off after the migration otherwise
        Assert.Contains("dotnet_diagnostic.BRO1135.severity = warning", lines);   // S818
        Assert.Contains("stylebro_upper_case_literal_suffixes = l_only", lines);
        Assert.Contains("stylebro_keep_overloads_together = true", lines);       // S4136: BRO1001 is on (StyleCop's defaults)
        Assert.DoesNotContain("dotnet_diagnostic.BRO1142.severity = warning", lines);  // S1659 is off
        Assert.Single(lines, l => l.StartsWith("dotnet_diagnostic.BRO1313.severity", StringComparison.Ordinal));
        Assert.Single(lines, l => l.StartsWith("dotnet_diagnostic.BRO1309.severity", StringComparison.Ordinal));  // on through SA1300 already
    }

    [Fact]
    public void Sonar_SeveritiesComeFromRulesetsConfigsAndTheEditorConfig()
    {
        // No package: the scanner's and SonarLint's rulesets alone are a setup (the scanner runs Sonar in CI only).
        Write(".sonarqube/conf/Sonar-cs.ruleset", SonarRuleset(("S3052", "Warning"), ("S1659", "Warning")));
        Write(".sonarqube/conf/Sonar-cs-none.ruleset", SonarRuleset(("S3052", "None")));
        Write(".sonarlint/myprojectcsharp.ruleset", SonarRuleset(("S100", "Error")));
        Write("eng/Library.globalconfig", "is_global = true\ndotnet_diagnostic.S1659.severity = none\ndotnet_diagnostic.S2325.severity = suggestion\n");
        Write("eng/Test.globalconfig", "is_global = true\ndotnet_diagnostic.S2325.severity = none\n");
        Write(".editorconfig", "root = true\n[*.md]\ndotnet_diagnostic.S1116.severity = warning\n[*.cs]\ndotnet_diagnostic.S3052.severity = none\n");

        var sonar = SonarSetup.Read(root)!;

        Assert.False(sonar.IsOn("S3052"));  // the root .editorconfig wins
        Assert.True(sonar.IsOn("S1659"));   // the ruleset's warning; a global config can't turn it off for everything ...
        Assert.Equal(Severity.Warning, sonar.Severities["S1659"]);
        Assert.Equal(Severity.Suggestion, sonar.Severities["S2325"]);  // ... the strictest of the global configs wins
        Assert.Equal(Severity.Error, sonar.Severities["S100"]);
        Assert.False(sonar.IsOn("S1116"));  // not a C# section
        Assert.False(sonar.IsOn("S927"));   // no package: no defaults
        Assert.DoesNotContain(sonar.Sources, s => s.Contains("none", StringComparison.Ordinal));

        var lines = Migration.Generate(StyleCopSetup.Read(root), root, sonar: sonar).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1309.severity = error", lines);       // S100, stronger than SA1300's warning
        Assert.Single(lines, l => l.StartsWith("dotnet_diagnostic.BRO1309.severity", StringComparison.Ordinal));
        Assert.Contains("dotnet_diagnostic.CA1822.severity = suggestion", lines);
        Assert.Contains("dotnet_diagnostic.BRO1142.severity = warning", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("dotnet_diagnostic.CA1805", StringComparison.Ordinal));
    }

    [Fact]
    public void Sonar_AQualityProfile_ReplacesTheDefaults()
    {
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.34.0.3385" /></ItemGroup></Project>""");
        var profile = Path.Combine(root, "..", Path.GetFileName(root) + "-profile.xml");
        File.WriteAllText(profile, """
            <?xml version="1.0" encoding="UTF-8"?>
            <profile>
              <name>Team way</name>
              <language>cs</language>
              <rules>
                <rule><repositoryKey>csharpsquid</repositoryKey><key>S1066</key><type>CODE_SMELL</type><priority>MAJOR</priority><parameters/></rule>
                <rule><repositoryKey>csharpsquid</repositoryKey><key>S3052</key><type>CODE_SMELL</type><priority>MINOR</priority><parameters/></rule>
                <rule><repositoryKey>vbnet</repositoryKey><key>S2325</key><type>CODE_SMELL</type><priority>MINOR</priority><parameters/></rule>
              </rules>
            </profile>
            """);
        try
        {
            var sonar = SonarSetup.Read(root, profile)!;
            Assert.Equal(new[] { "S1066", "S3052" }, sonar.Severities.Where(s => s.Value >= Severity.Suggestion).Select(s => s.Key).Order());

            var lines = Migration.Generate(StyleCopSetup.Read(root), root, sonar: sonar).Lines;
            Assert.Contains("dotnet_diagnostic.BRO1149.severity = warning", lines);
            Assert.Contains("dotnet_diagnostic.CA1805.severity = warning", lines);
            Assert.DoesNotContain(lines, l => l.StartsWith("dotnet_diagnostic.CA1822", StringComparison.Ordinal));

            // On the command line; a missing file fails before anything is read.
            var output = Capture(() => Assert.Equal(0, Program.Main(new[] { "--sonar-profile", profile, root })));
            Assert.Contains("S1066 -> BRO1149", output, StringComparison.Ordinal);
            Assert.Contains("quality profile", output, StringComparison.Ordinal);
            Assert.Equal(1, Program.Main(new[] { root, "--sonar-profile=" + profile + ".missing" }));
        }
        finally
        {
            File.Delete(profile);
        }
    }

    [Fact]
    public void Sonar_AndStyleCop_ARuleOnThroughEitherIsOn_ConflictsAreListed()
    {
        // StyleCop's ordering off: BRO1001 stays off, so S4136's overloads option can't apply.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1201.severity = none\ndotnet_diagnostic.SA1503.severity = none\ndotnet_diagnostic.SA1519.severity = none\ndotnet_diagnostic.SA1520.severity = none\n"
            + "dotnet_diagnostic.S4136.severity = warning\ndotnet_diagnostic.S121.severity = warning\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root, sonar: SonarSetup.Read(root));

        Assert.Contains("dotnet_diagnostic.BRO1001.severity = none", result.Lines);
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("stylebro_keep_overloads_together", StringComparison.Ordinal));
        Assert.Contains(result.Sonar!.NotApplied, n => n.Sonar == "S4136" && n.Reason.Contains("BRO1001 is off", StringComparison.Ordinal));

        // S121 turns BRO1514 on and wants braces everywhere, where StyleCop's setup didn't: Sonar's setting wins, noted.
        Assert.Contains("dotnet_diagnostic.BRO1514.severity = warning", result.Lines);
        Assert.Single(result.Lines, l => l.StartsWith("csharp_prefer_braces", StringComparison.Ordinal));
        Assert.Contains("csharp_prefer_braces = true", result.Lines);
        Assert.Contains(result.Sonar.Notes, n => n.StartsWith("csharp_prefer_braces: true for S121 instead of false", StringComparison.Ordinal));
    }

    [Fact]
    public void Sonar_TheReport_SaysWhatIsFixedAndWhatStaysSonars()
    {
        Write("src/App/App.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.35.0.4138" /></ItemGroup></Project>""");
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1201.severity = none\n");

        var output = Capture(() => Assert.Equal(0, Program.Main(new[] { root })));

        Assert.Contains("Sonar: read SonarAnalyzer.CSharp 10.35.0.4138", output, StringComparison.Ordinal);
        Assert.Contains($"Sonar rules on: {SonarSetup.Rules.Count(r => r.Value.SonarWay)}; fixed by StyleBro or the SDK from now on: ", output, StringComparison.Ordinal);
        Assert.Contains("  S2325 -> CA1822 (dotnet_code_quality.CA1822.api_surface = private, internal; public members stay Sonar's", output, StringComparison.Ordinal);
        Assert.Contains("    S4136: BRO1001 is off here", output, StringComparison.Ordinal);
        Assert.Contains("Sonar keeps reporting its own ids", output, StringComparison.Ordinal);
        Assert.Contains("# S2325: Methods and properties that don't access instance data should be static", output, StringComparison.Ordinal);

        // The preview shows the same part of the report.
        var part = PreviewCommand.SonarPart(output);
        Assert.StartsWith("Sonar: read ", part, StringComparison.Ordinal);
        Assert.EndsWith("Sonar keeps reporting its own ids, the ones above too.\n", part, StringComparison.Ordinal);
    }

    [Fact]
    public void Sonar_Init_TurnsOnTheSameRules_WithANote()
    {
        Write("Directory.Build.props", """<Project><ItemGroup><PackageReference Include="SonarAnalyzer.CSharp" Version="10.34.0.3385" /></ItemGroup></Project>""");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var config = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("Sonar rules on:", output, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.CA1822.severity = warning\n", config, StringComparison.Ordinal);
        Assert.Contains("stylebro_keep_overloads_together = true\n", config, StringComparison.Ordinal);  // BRO1001 is on in the preset
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = warning\n", config, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(config, "# BEGIN stylebro-migrate"));

        // The format command fixes the CA rules the block turns on.
        Assert.Contains("CA1822", FormatCommand.Diagnostics(root));
    }

    [Fact]
    public void Sonar_TheMapping_NamesKnownRules()
    {
        // BRO1149-BRO1151 (S1066, S2971, S3878) are being added in parallel; until they're on main they are only written.
        var pending = new HashSet<string> { "BRO1149", "BRO1150", "BRO1151" };
        var rules = Migration.StyleBroRules().Select(r => r.Id).ToHashSet();

        Assert.NotEmpty(SonarSetup.Mapping);
        Assert.All(SonarSetup.Mapping, m =>
        {
            Assert.True(SonarSetup.Rules.ContainsKey(m.Sonar), m.Sonar);
            Assert.True(rules.Contains(m.Rule) || pending.Contains(m.Rule) || System.Text.RegularExpressions.Regex.IsMatch(m.Rule, @"^(IDE|CA)\d{4}$"), m.Rule);
            Assert.True(!m.OptionOnly || m.Setting is not null, m.Sonar);
        });
        Assert.Contains(SonarSetup.Mapping, m => m.Sonar == "S4136" && m.Rule == "BRO1001" && m.OptionOnly);
    }

    private static string SonarRuleset(params (string Id, string Action)[] rules) =>
        "<RuleSet Name=\"Sonar\" ToolsVersion=\"14.0\">\n  <Rules AnalyzerId=\"SonarAnalyzer.CSharp\" RuleNamespace=\"SonarAnalyzer.CSharp\">\n"
        + string.Concat(rules.Select(r => $"    <Rule Id=\"{r.Id}\" Action=\"{r.Action}\" />\n"))
        + "  </Rules>\n  <Rules AnalyzerId=\"StyleCop.Analyzers\" RuleNamespace=\"StyleCop.Analyzers\">\n    <Rule Id=\"S2325\" Action=\"Warning\" />\n  </Rules>\n</RuleSet>\n";

    private static Dictionary<string, string> Hashes(string folder)
    {
        return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .ToDictionary(f => f, f => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f))));
    }

    private static void DeleteFolder(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal); // git's object files are read-only
        }

        Directory.Delete(folder, recursive: true);
    }

    private static string Capture(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
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
