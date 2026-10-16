using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the twenty-fifth trial (Autofac, MimeKit, Speckle, Flow.Launcher) found in migrate and init.</summary>
public sealed partial class MigrationTests
{
    private const string StyleCopProject = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleCop.Analyzers\" Version=\"1.2.0-beta.556\" /></ItemGroup></Project>";

    [Fact]
    public void Migrate_TurnsTheRulesOffWhereStyleCopDidntRun()
    {
        // Autofac's AOT tests: no StyleCop, the preset off for the whole repository, and BRO1401 failed the Release build there
        // while 'format' (which leaves those folders alone) said Clean.
        Write("Directory.Packages.props", GlobalStyleCop);
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write("test/Aot/Aot.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.IDE0011.severity = warning\n");

        var setup = StyleCopSetup.Read(root, project => !project.Contains("Aot", StringComparison.Ordinal));
        var plan = Migration.Plan(setup, root, Migration.Generate(setup, root));

        var (section, lines) = Assert.Single(plan[".editorconfig"], s => s.Section == "test/**.cs");
        Assert.Equal(Migration.WithoutStyleCopComment, lines[0]);
        Assert.Contains("dotnet_diagnostic.BRO1401.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none", lines);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0011.severity = none", lines);  // the team's own entry stays
        Assert.Contains(Program.ScopeReport(setup), l => l.StartsWith("No StyleCop in test: StyleBro's rules and the built-in ones the block sets are off there by id", StringComparison.Ordinal));

        // Every project runs StyleCop: no such section.
        setup = StyleCopSetup.Read(root, _ => true);
        Assert.DoesNotContain(Migration.Plan(setup, root, Migration.Generate(setup, root))[".editorconfig"], s => s.Lines.Contains(Migration.WithoutStyleCopComment));
    }

    [Fact]
    public void Migrate_KeepsStyleCopJsonsSettings_AfterTheFileIsGone()
    {
        // b-own-stylecop-sonar: the swap, stylecop.json and its <AdditionalFiles> item removed as the report advised, and a second
        // --write turned stylebro_document_internal_elements back on and moved every file's usings inside the namespace.
        Write("App/App.csproj", StyleCopProject);
        Write("stylecop.json", """{ "settings": { "documentationRules": { "companyName": "Contoso", "documentInternalElements": false }, "orderingRules": { "usingDirectivesPlacement": "outsideNamespace" } } }""");
        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));
        var first = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("stylebro_document_internal_elements = false", first, StringComparison.Ordinal);
        Assert.Contains(Migration.SettingsPrefix, first, StringComparison.Ordinal);

        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.Delete(Path.Combine(root, "stylecop.json"));
        var output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));

        Assert.Equal(first, File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("stylecop.json's settings recorded by the last --write", output, StringComparison.Ordinal);
        Assert.DoesNotContain("stylecop.json sets no companyName", output, StringComparison.Ordinal);

        // A recorded line that doesn't parse counts as none.
        Write(".editorconfig", $"{Migration.BeginMarker}\n{Migration.SettingsPrefix}{{oops\n{Migration.EndMarker}\n");
        Assert.Null(Migration.RecordedSettings(root));
    }

    [Fact]
    public void Migrate_ABraceSettingWeakerThanSA1503_IsNotKept()
    {
        // Autofac: 'when_multiline:warning' kept while SA1503 is on, and braceless one-line statements went unreported.
        Write(".editorconfig", "root = true\n\n[*.cs]\ncsharp_prefer_braces = when_multiline:warning\n");
        var result = Migration.Generate(StyleCopSetup.Read(root), root);
        Assert.Contains("csharp_prefer_braces = true", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("Not kept: csharp_prefer_braces = when_multiline:warning, because SA1503 is on", StringComparison.Ordinal));

        // SA1503 off: the team's value stays; 'true' stays anyway.
        Write(".editorconfig", "root = true\n\n[*.cs]\ncsharp_prefer_braces = when_multiline:warning\ndotnet_diagnostic.SA1503.severity = none\n");
        Assert.DoesNotContain("csharp_prefer_braces = true", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
        Write(".editorconfig", "root = true\n\n[*.cs]\ncsharp_prefer_braces = true:warning\n");
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Notes, n => n.StartsWith("Not kept: csharp_prefer_braces", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_AnIdeRulesOwnSeverity_BeatsTheSuffix()
    {
        // 'dotnet new editorconfig' writes 'csharp_prefer_braces = true:silent'; the team added IDE0011 = warning (c-own2).
        Assert.Empty(InitCommand.Unenforced(new Dictionary<string, string> { ["csharp_prefer_braces"] = "true:silent", ["dotnet_diagnostic.IDE0011.severity"] = "warning" }));
        Assert.Empty(InitCommand.Unenforced(new Dictionary<string, string> { ["dotnet_style_require_accessibility_modifiers"] = "for_non_interface_members:silent", ["dotnet_diagnostic.IDE0040.severity"] = "warning" }));
        Assert.Equal(["csharp_prefer_braces"], InitCommand.Unenforced(new Dictionary<string, string> { ["csharp_prefer_braces"] = "true:silent" }));
        Assert.Equal(["csharp_prefer_braces"], InitCommand.Unenforced(new Dictionary<string, string> { ["csharp_prefer_braces"] = "true:warning", ["dotnet_diagnostic.IDE0011.severity"] = "none" }));

        // The variant without a suffix: the code contradicts the key, but IDE0011 enforces it, so BRO1514 stays on.
        var counts = Conventions.NewCounts();
        counts["csharp_prefer_braces"] = [0, 12];
        var repository = new Dictionary<string, string> { ["csharp_prefer_braces"] = "true", ["dotnet_diagnostic.IDE0011.severity"] = "warning" };
        var decided = new Dictionary<string, string> { ["csharp_prefer_braces"] = "set in .editorconfig (true)" };
        var (lines, _) = Conventions.Decide(counts, decided, repository: repository);
        Assert.DoesNotContain(lines, l => l.Contains("nothing enforced it", StringComparison.Ordinal));
        repository.Remove("dotnet_diagnostic.IDE0011.severity");
        Assert.Contains(Conventions.Decide(counts, decided, repository: repository).Lines, l => l.Contains("nothing enforced it", StringComparison.Ordinal));
    }

    [Fact]
    public void Init_SaysWhenTheBlockTurnsOffABuiltInRuleTheTeamTurnedOn()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.IDE0040.severity = warning\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "namespace Lib;\n\npublic class C\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));
        Assert.Contains(".editorconfig sets IDE0040 (warning): the block turns it off. Access modifiers (SA1400, SA1205) are StyleBro's BRO1404/BRO1007", output, StringComparison.Ordinal);
        Assert.DoesNotContain("sets IDE0011", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_NamespaceBracesTheKeyWouldMove_TurnIde0055OffAndSaySo()
    {
        // MimeKit: 'namespace X {' in 670 files under 'methods,types', and IDE0055 moved 614 braces.
        var counts = Conventions.NewCounts();
        foreach (var code in new[] { "namespace A {\nclass C\n{\n}\n}\n", "namespace B {\nclass D\n{\n}\n}\n", "namespace E\n{\nclass F\n{\n}\n}\n", "namespace G;\nclass H\n{\n}\n" })
        {
            Conventions.Count(CSharpSyntaxTree.ParseText(code), counts);
        }

        Assert.Equal([1, 2], counts[Conventions.NamespaceBracesKey]);
        var (lines, report) = Conventions.NamespaceBraces(counts, "methods,types")!.Value;
        Assert.Equal("dotnet_diagnostic.IDE0055.severity = none", lines[^1]);
        Assert.Contains("2 of 3 files have it at the end of the line", report, StringComparison.Ordinal);
        Assert.Contains("2 files will change", report, StringComparison.Ordinal);

        // The key agrees with them, or a minority would move: no line.
        Assert.Empty(Conventions.NamespaceBraces(counts, "methods, control_blocks:warning")!.Value.Lines);
        counts[Conventions.NamespaceBracesKey] = [0, 2];
        Assert.Null(Conventions.NamespaceBraces(counts, "methods"));
        counts[Conventions.NamespaceBracesKey] = [3, 1];
        Assert.Empty(Conventions.NamespaceBraces(counts, "all")!.Value.Lines);
        counts[Conventions.NamespaceBracesKey] = [3, 0];
        Assert.Null(Conventions.NamespaceBraces(counts, "all"));
    }

    [Fact]
    public void Init_NamespaceBraces_EndToEnd()
    {
        Write(".editorconfig", "root = true\n\n[*.cs]\ncsharp_new_line_before_open_brace = methods,types\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"C{i}.cs", $"namespace Lib {{\n    public class C{i}\n    {{\n    }}\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        var text = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\ndotnet_diagnostic.IDE0055.severity = warning", text, StringComparison.Ordinal);
        Assert.Contains("3 files will change", output, StringComparison.Ordinal);

        // The team's own IDE0055 severity stays: only said.
        File.WriteAllText(Path.Combine(root, ".editorconfig"), "root = true\n\n[*.cs]\ncsharp_new_line_before_open_brace = methods,types\ndotnet_diagnostic.IDE0055.severity = warning\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("\ndotnet_diagnostic.IDE0055.severity = none", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
        Assert.Contains("your .editorconfig sets IDE0055, so 'format' moves them", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_DocOrderOfStubElementsOnly_DecidesWhenNothingElseDoes()
    {
        // b-init-docorder-returns: '<returns>' above '<param>' in 12 of 12 comments, and BRO1619 reordered all of them.
        const string Returns = "class C\n{\n    /// <summary>S.</summary>\n    /// <returns>R.</returns>\n    /// <param name=\"a\">A.</param>\n    int M(int a) => a;\n}\n";
        var counts = Conventions.NewCounts();
        for (var i = 0; i < 3; i++)
        {
            Conventions.Count(CSharpSyntaxTree.ParseText(Returns, new CSharpParseOptions(documentationMode: Microsoft.CodeAnalysis.DocumentationMode.Diagnose)), counts);
        }

        Conventions.FoldDocOrder(counts);
        Assert.Contains("dotnet_diagnostic.BRO1619.severity = none", Conventions.Decide(counts, new Dictionary<string, string>()).Lines);

        // Enough mixed comments: they decide, the stub-only ones don't outvote them.
        counts = Conventions.NewCounts();
        counts["doc element order (stub elements only)"] = [0, 12];
        counts["dotnet_diagnostic.BRO1619.severity"] = [5, 0];
        Conventions.FoldDocOrder(counts);
        Assert.Equal([5, 0], counts["dotnet_diagnostic.BRO1619.severity"]);
        Assert.False(counts.ContainsKey("doc element order (stub elements only)"));
    }

    [Fact]
    public void Conventions_CountStatementsAfterBlocksAndNestedIfs()
    {
        // Speckle: 81 % of statements after a block's '}' after a blank line, unsaid; MimeKit: ~30 nested ifs BRO1149 merged.
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        M(a, b);
                    }

                    if (b)
                    {
                        M(a, b);
                    }
                    M(a, b);
                    if (a)
                    {
                        if (b)
                        {
                            M(a, b);
                        }
                    }
                    if (a) if (b) M(a, b);
                }
            }
            """),
            counts);

        Assert.Equal([1, 2], counts["dotnet_diagnostic.BRO1519.severity"]);
        Assert.Equal(2, counts["dotnet_diagnostic.BRO1149.severity"][1]);

        counts["dotnet_diagnostic.BRO1149.severity"] = [0, 3];
        counts["dotnet_diagnostic.BRO1519.severity"] = [81, 19];
        var (lines, report) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.Contains("dotnet_diagnostic.BRO1149.severity = none", lines);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1519.severity = none", lines);
        Assert.Contains(report, l => l.Contains("statements after a block's '}': 81 of 100 after a blank line", StringComparison.Ordinal));
    }

    [Fact]
    public void ANewPropsFile_SaysItHidesOneAboveTheRepository()
    {
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
        var props = Path.Combine(root, "repo", "Directory.Build.props");
        Assert.Null(Migration.HiddenPropsNote(props));

        Write("Directory.Build.props", "<Project />");
        Assert.Contains("is above the repository, and MSBuild reads only the nearest Directory.Build.props", Migration.HiddenPropsNote(props), StringComparison.Ordinal);

        // Not when the file exists already: it's the team's, and it hid that one before.
        Write("repo/Directory.Build.props", "<Project />");
        Assert.Null(Migration.HiddenPropsNote(props));
    }

    [Fact]
    public void AgentsTip_DependsOnTheFile()
    {
        Assert.Equal([AgentsFile.Tip], AgentsFile.Update(root, write: false, requested: false));
        Write("AGENTS.md", "# Agents\n");
        Assert.Equal(["Tip: --agents-md adds a StyleBro section to your AGENTS.md for AI coding agents."], AgentsFile.Update(root, write: false, requested: false));
        Write("AGENTS.md", AgentsFile.Apply("# Agents\n"));
        Assert.Empty(AgentsFile.Update(root, write: false, requested: false));
    }

    [Fact]
    public void Migrate_StyleSharpsShortSummaries_AreBro1616OnOneLine()
    {
        Write("App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"StyleSharp.Analyzers\" Version=\"5.1.2\" /></ItemGroup></Project>");
        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.SST1653.severity = warning\n");
        var lines = Migration.Generate(StyleCopSetup.Read(root), root).Lines;
        Assert.Contains("dotnet_diagnostic.BRO1616.severity = warning", lines);
        Assert.Contains("stylebro_summary_layout = single_line_when_fits", lines);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1616.severity = none", lines);

        Write(".editorconfig", "root = true\n\n[*.cs]\ndotnet_diagnostic.SST1653.severity = none\n");
        Assert.DoesNotContain("stylebro_summary_layout = single_line_when_fits", Migration.Generate(StyleCopSetup.Read(root), root).Lines);
    }
}
