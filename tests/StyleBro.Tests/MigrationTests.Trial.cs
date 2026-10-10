using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What a trial of 0.3.0-alpha.1 on Ocelot, eShop, AutoMapper and vs-threading found in stylebro-migrate.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void Init_DetectsThePrivateFieldStyle_DespiteNamingRulesForOtherSymbols()
    {
        // Ocelot and eShop: naming rules for interfaces or constants only; 1058 '_x' fields were renamed before.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_naming_rule.interfaces.symbols = interface_symbols\ndotnet_naming_rule.interfaces.style = prefix_i\n"
            + "dotnet_naming_symbols.interface_symbols.applicable_kinds = interface\ndotnet_naming_style.prefix_i.required_prefix = I\ndotnet_naming_style.prefix_i.capitalization = pascal_case\n");
        Write("A.cs", "class A { private int _a, _b, _c, _d, _e, _f, _g, _h, _i, _j; }");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("stylebro_private_field_naming = _camelCase", File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("  kept     private fields: 10 of 10 named '_field' -> _camelCase", output);
    }

    [Fact]
    public void Migration_FieldStyle_SkipsGeneratedAndVendoredCode()
    {
        Write(".editorconfig", "[*.cs]\ndotnet_diagnostic.SA1309.severity = none\n");
        Write("src/A.cs", "class A { private int _a, _b; private int c; }");
        Write("src/Form.Designer.cs", "class F { private int button1, button2, button3; }");
        Write("src/Data/Migrations/20240101_Init.cs", "class M { private int x, y, z; }");
        Write("vendor/Lib.cs", "class L { private int p, q, r; }");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("stylebro_private_field_naming = _camelCase", result.Lines);
        Assert.Contains(result.Notes, n => n.Contains("2 named '_field', 1 named 'field'"));
    }

    [Fact]
    public void Init_LeavesThePrivateFieldStyle_ToANamingRuleForPrivateFields()
    {
        var settings = new Dictionary<string, string>
        {
            ["dotnet_naming_rule.private_fields.symbols"] = "private_fields",
            ["dotnet_naming_rule.private_fields.style"] = "underscore",
            ["dotnet_naming_rule.private_fields.severity"] = "warning",
            ["dotnet_naming_symbols.private_fields.applicable_kinds"] = "field",
            ["dotnet_naming_symbols.private_fields.applicable_accessibilities"] = "private",
            ["dotnet_naming_style.underscore.capitalization"] = "camel_case",
            ["dotnet_naming_style.underscore.required_prefix"] = "_",
        };
        Assert.Equal("private_fields", InitCommand.FieldNamingRule(settings));

        // A rule for constants or static fields doesn't decide it.
        settings["dotnet_naming_symbols.private_fields.required_modifiers"] = "const";
        Assert.Null(InitCommand.FieldNamingRule(settings));
        settings.Remove("dotnet_naming_symbols.private_fields.required_modifiers");
        settings["dotnet_naming_symbols.private_fields.applicable_kinds"] = "interface";
        Assert.Null(InitCommand.FieldNamingRule(settings));
    }

    [Fact]
    public void Conventions_AreCountedFromTheSyntax()
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(
            CSharpSyntaxTree.ParseText("""
            class C
            {
                private int _a;
                int P
                    => 1;
                int Q =>
                    2;
                int M(int a,
                    int b)
                {
                    var s = "";
                    var t = string.Empty;
                    var x = a == null || b is null;
                    var y = a
                        + b;
                    var z = new int[]
                    {
                        1,
                        2,
                    };
                    if (a > 0) return default;
                    if (a > 1)
                    {
                        return default(int);
                    }

                    switch (a)
                    {
                        case 1:
                            return 1;

                        case 2:
                            return 2;
                    }

                    return new D() { E = 1 }.E;
                }

                /// <summary>One line.</summary>
                /// <inheritdoc />
                int N() { return 0; }
            }
            """),
            counts);

        Assert.Equal(new[] { 0, 1 }, counts["stylebro_private_field_naming"]);
        Assert.Equal(new[] { 1, 1 }, counts["stylebro_arrow_placement_when_wrapping"]);
        Assert.Equal(new[] { 1, 1 }, counts["stylebro_empty_string_style"]);
        Assert.Equal(new[] { 1, 1 }, counts["stylebro_null_check_style"]);
        Assert.Equal(new[] { 1, 0 }, counts["dotnet_style_operator_placement_when_wrapping"]);
        Assert.Equal(new[] { 1, 0 }, counts["stylebro_trailing_comma"]);
        Assert.Equal(new[] { 1, 1 }, counts["csharp_prefer_braces"]);
        Assert.Equal(new[] { 1, 1 }, counts["csharp_prefer_simple_default_expression"]);
        Assert.Equal(new[] { 1, 0 }, counts["stylebro_blank_line_between_switch_sections"]);
        Assert.Equal(new[] { 0, 1 }, counts["stylebro_object_creation_parentheses"]);
        Assert.Equal(new[] { 0, 1 }, counts["stylebro_summary_layout"]);
        Assert.Equal(new[] { 0, 1 }, counts["stylebro_inheritdoc_style"]);
        Assert.Equal(new[] { 1, 0 }, counts["stylebro_closing_parenthesis_placement"]);
        Assert.Equal(new[] { 0, 1 }, counts["stylebro_split_list_first_item"]);
    }

    [Theory]
    [InlineData("using System;\nnamespace N\n{\n    class C { }\n}\n", 1, 0)]
    [InlineData("namespace N\n{\n    using System;\n    class C { }\n}\n", 0, 1)]
    [InlineData("namespace N;\nusing System;\nclass C { }\n", 0, 1)]
    [InlineData("using System;\nnamespace N;\nclass C { }\n", 1, 0)]
    [InlineData("namespace A\n{\n    namespace B\n    {\n        using System;\n    }\n}\n", 0, 1)]
    [InlineData("global using System;\nnamespace N\n{\n    using System.IO;\n}\n", 0, 1)]
    [InlineData("using System;\nnamespace N\n{\n    using System.IO;\n}\n", 0, 0)] // both levels
    [InlineData("using System;\nclass C { }\n", 0, 0)] // no namespace
    [InlineData("global using System;\nnamespace N { }\n", 0, 0)] // global usings only
    [InlineData("namespace N { class C { } }\n", 0, 0)] // no usings
    public void Conventions_CountUsingPlacementPerFile(string source, int outside, int inside)
    {
        var counts = Conventions.NewCounts();
        Conventions.Count(CSharpSyntaxTree.ParseText(source), counts);
        Assert.Equal(new[] { outside, inside }, counts["csharp_using_directive_placement"]);
    }

    [Fact]
    public void Conventions_WriteInsideNamespaceOnlyWhenMostFilesDoIt()
    {
        const string Key = "csharp_using_directive_placement";
        var none = new Dictionary<string, string>();
        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { 4, 120 } }, none);
        Assert.Contains(Key + " = inside_namespace", lines);
        Assert.Contains("  kept     files' using directives: 120 of 124 inside the namespace -> inside_namespace", report);
        Assert.Empty(Lines(new[] { 120, 4 })); // outside is the preset's
        Assert.Equal(new[] { "dotnet_diagnostic.BRO1008.severity = none" }, Lines(new[] { 4, 9 }).Where(l => !l.StartsWith('#'))); // mixed: 69 %, BRO1008 off
        Assert.Contains(Key + " = inside_namespace", Lines(new[] { 0, 9 })); // 9 files, all inside
        Assert.Empty(Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { 0, 30 } }, new Dictionary<string, string> { [Key] = ".editorconfig sets it" }).Lines);

        IEnumerable<string> Lines(int[] count) => Conventions.Decide(new Dictionary<string, int[]> { [Key] = count }, none).Lines;
    }

    [Fact]
    public void Conventions_AreWrittenOnlyWithEnoughPlacesAndAClearMajority()
    {
        const string Key = "stylebro_arrow_placement_when_wrapping";
        const string Off = "dotnet_diagnostic.BRO1521.severity = none";
        var none = new Dictionary<string, string>();
        Assert.Equal(new[] { Key + " = beginning_of_line" }, Lines(new[] { 1, 9 }));
        Assert.Equal(new[] { Off }, Lines(new[] { 1, 8 })); // 9 places that disagree: too few
        Assert.Equal(new[] { Key + " = beginning_of_line" }, Lines(new[] { 0, 3 })); // 3 places that agree
        Assert.Empty(Lines(new[] { 3, 0 })); // ... on the default
        Assert.Empty(Lines(new[] { 0, 2 })); // 2 places: too few to tell, the default stays
        Assert.Empty(Lines(new[] { 1, 1 })); // ... also when they disagree
        Assert.Equal(new[] { Key + " = beginning_of_line" }, Lines(new[] { 3, 9 })); // 75 %
        Assert.Equal(new[] { Off }, Lines(new[] { 4, 9 })); // 69 %: no clear majority, BRO1521 off
        Assert.Empty(Lines(new[] { 0, 0 })); // nothing to judge: the default stays
        Assert.Empty(Lines(new[] { 20, 0 }));   // the default is never written

        var (lines, report) = Conventions.Decide(new Dictionary<string, int[]> { [Key] = new[] { 0, 30 } }, new Dictionary<string, string> { [Key] = ".editorconfig sets it" });
        Assert.Empty(lines);
        Assert.Contains(report, r => r.EndsWith(".editorconfig sets it", StringComparison.Ordinal));

        // else/catch/finally go together.
        Assert.Contains("csharp_new_line_before_finally = false", Conventions.Decide(new Dictionary<string, int[]> { ["csharp_new_line_before_else"] = new[] { 0, 12 } }, none).Lines);

        IEnumerable<string> Lines(int[] count) => Conventions.Decide(new Dictionary<string, int[]> { [Key] = count }, none).Lines.Where(l => !l.StartsWith('#'));
    }

    [Fact]
    public void Init_KeepsTheArrowAtTheStartOfTheLine_LikeOcelot()
    {
        Write("A.cs", "class A\n{\n" + string.Concat(Enumerable.Range(0, 12).Select(i => $"    int P{i}\n        => {i};\n")) + "}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("stylebro_arrow_placement_when_wrapping = beginning_of_line", File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains(InitCommand.DetectedHeader, output);

        // The preview shows the same part.
        Assert.Contains("12 at the start of the line", PreviewCommand.DetectedPart(output));

        // The repository's own setting wins.
        Write(".editorconfig", "root = true\n[*.cs]\nstylebro_arrow_placement_when_wrapping = end_of_line\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.DoesNotContain("= beginning_of_line", File.ReadAllText(Path.Combine(root, ".editorconfig")));
        Assert.Contains("12 at the start of the line -> set in .editorconfig (end_of_line)", output);
    }

    [Fact]
    public void Migration_KeepsOneLineStatements_SA1107IsReportedAsNotExpressible()
    {
        // vs-threading: 'delegate { /* comment */ }' was split by the SDK formatter with 'false'.
        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("csharp_preserve_single_line_statements = true", result.Lines);
        Assert.Contains("SA1107", result.Reasons.Keys);
        Assert.DoesNotContain("SA1107", result.Covered);
    }

    [Fact]
    public void EfCoreMigrations_AreMarkedAsGenerated()
    {
        Write("src/Ordering/Migrations/20240101_Init.cs", "using Microsoft.EntityFrameworkCore.Migrations;\n[Migration(\"20240101_Init\")]\npartial class Init : Migration { }\n");
        Write("src/Ordering/Migrations/OrderingContextModelSnapshot.cs", "using Microsoft.EntityFrameworkCore.Infrastructure;\nclass S : ModelSnapshot { }\n");
        Write("src/Other/Migrations/Notes.cs", "class Migration { }\n"); // not EF Core

        Assert.Equal(new[] { "src/Ordering/Migrations" }, Migration.MigrationFolders(root));

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("[src/Ordering/Migrations/**]\ngenerated_code = true", File.ReadAllText(Path.Combine(root, ".editorconfig")));

        var setup = StyleCopSetup.Read(root);
        var plan = Migration.Plan(setup, root, Migration.Generate(setup, root));
        Assert.Contains(plan[".editorconfig"], s => s.Section == "src/Ordering/Migrations/**" && s.Lines.Contains("generated_code = true"));

        // The repository marks generated code itself: nothing written.
        File.WriteAllText(Path.Combine(root, ".editorconfig"), "root = true\n[**/Migrations/*.cs]\ngenerated_code = true\n");
        Assert.Empty(Migration.MigrationFolders(root));
    }

    [Fact]
    public void PreviewHunk_IsTheNearestOne_WithoutTheByteOrderMark()
    {
        var diff = "@@ -1 +1 @@\n-﻿using A;\n+﻿using B;\n@@ -20,0 +21 @@\n+    /// <inheritdoc/>\n@@ -40,2 +41 @@\n-x\n-y\n+z\n";

        Assert.Equal(new[] { "-using A;", "+using B;" }, PreviewCommand.Hunk(diff, 1, 12));
        Assert.Equal(new[] { "+    /// <inheritdoc/>" }, PreviewCommand.Hunk(diff, 21, 12)); // an insertion after line 20
        Assert.Equal(new[] { "-x", "-y", "+z" }, PreviewCommand.Hunk(diff, 38, 12)); // nearest
    }

    [Fact]
    public void Workspace_IsNeverPickedSilentlyFromSeveral()
    {
        Write("eShop.slnx", "<Solution />");
        Write("eShop.Web.slnf", "{}");
        Assert.Null(BaselineCommand.FindWorkspace(root));
        Assert.Equal(new[] { "eShop.slnx", "eShop.Web.slnf" }, BaselineCommand.WorkspaceCandidates(root));
        Assert.Contains("eShop.slnx, eShop.Web.slnf", BaselineCommand.NoWorkspace(root, "--project <file>"));
        Assert.Contains("--project eShop.slnx", BaselineCommand.NoWorkspace(root, "--project <file>"));

        // --diff stops before it runs anything.
        var output = CaptureError(() => Assert.Equal(1, PreviewCommand.Run("init", new[] { root, "--diff" }, (_, _) => throw new InvalidOperationException("format ran"))));
        Assert.Contains("--project", output);
        Assert.False(File.Exists(Path.Combine(root, ".editorconfig")));

        // The same solution in both formats is one.
        File.Delete(Path.Combine(root, "eShop.Web.slnf"));
        Write("eShop.sln", string.Empty);
        Assert.Equal("eShop.slnx", BaselineCommand.FindWorkspace(root));
    }

    [Fact]
    public void Format_CountsTheFilesARunChanged()
    {
        var before = new Dictionary<string, (long, DateTime)> { ["a.cs"] = (1, DateTime.MinValue), ["b.cs"] = (2, DateTime.MinValue), ["gone.cs"] = (1, DateTime.MinValue) };
        var after = new Dictionary<string, (long, DateTime)> { ["a.cs"] = (1, DateTime.MinValue), ["b.cs"] = (3, DateTime.MinValue), ["new.cs"] = (1, DateTime.MinValue) };

        Assert.Equal(new[] { "b.cs", "gone.cs", "new.cs" }, FormatCommand.ChangedFiles(before, after));
        Assert.Empty(FormatCommand.ChangedFiles(after, after));
        Assert.Equal(new[] { "  b.cs", "  gone.cs", "  ... and 1 more" }, FormatCommand.ListFiles(".", FormatCommand.ChangedFiles(before, after), 2));
    }

    [Fact]
    public void TheReport_LeavesOutXmlHeaderRules_ForAPlainHeader()
    {
        Write("stylecop.json", """{ "settings": { "documentationRules": { "xmlHeader": false } } }""");
        var output = Capture(() => Assert.Equal(0, Program.Migrate(new[] { root })));

        Assert.DoesNotContain("SA1634", output);
        Assert.DoesNotContain("SA1641", output);
        Assert.Contains("SA1634", Migration.CannotFire(StyleCopSetup.Read(root)));

        File.Delete(Path.Combine(root, "stylecop.json"));
        Assert.Empty(Migration.CannotFire(StyleCopSetup.Read(root)));
    }

    [Fact]
    public void Baseline_SkipsBuildOutput_AndCountsFormattingPasses()
    {
        var file = Path.Combine(root, "src", "A.cs");
        Write("src/A.cs", "class A { }\n");
        string Document(string path, string id) => $$"""{ "FilePath": {{System.Text.Json.JsonSerializer.Serialize(path)}}, "FileChanges": [ { "LineNumber": 1, "CharNumber": 1, "DiagnosticId": "{{id}}" } ] }""";

        var result = BaselineCommand.Build(root, "[" + Document(Path.Combine(root, "src", "obj", "Debug", "AssemblyInfo.cs"), "ENDOFLINE") + "," + Document(file, "ENDOFLINE") + "," + Document(file, "CHARSET") + "]");

        Assert.Equal(2, result.Whitespace);
        Assert.Empty(result.NotCovered);
    }

    [Fact]
    public void SonarSuppressions_AreCarriedOver()
    {
        var replacements = Suppressions.WithSonar(new Dictionary<string, SortedSet<string>>());
        Assert.Contains("CA1822", replacements["S2325"]);

        var code = """
            using System.Diagnostics.CodeAnalysis;
            class C
            {
            #pragma warning disable S1481 // kept on purpose
                [SuppressMessage("Performance", "S2325:Methods should be static", Justification = "API")]
                int M() { var unused = 1; return 1; }
            #pragma warning restore S1481
            }
            """;
        var (text, added) = Suppressions.Rewrite(code, replacements);

        Assert.Equal(3, added); // the pragma pair's disable, and the attribute
        Assert.Contains("#pragma warning disable S1481, IDE0059 // kept on purpose", text);
        Assert.Contains("#pragma warning restore S1481, IDE0059", text);
        Assert.Contains("[SuppressMessage(\"Style\", \"CA1822\", Justification = \"API\")]", text);
        Assert.Equal(0, Suppressions.Rewrite(text, replacements).Added); // a second run adds nothing

        var (props, noWarn) = Suppressions.RewriteNoWarn("<Project><PropertyGroup><NoWarn>$(NoWarn);S1481</NoWarn></PropertyGroup></Project>", replacements);
        Assert.Equal(1, noWarn);
        Assert.Contains("<NoWarn>$(NoWarn);S1481;IDE0059</NoWarn>", props);

        // The migration carries them over with --write.
        Write("C.cs", code);
        Capture(() => Assert.Equal(0, Program.Migrate(new[] { root, "--write" })));
        Assert.Contains("S1481, IDE0059", File.ReadAllText(Path.Combine(root, "C.cs")));
    }

    private static string CaptureError(Action action)
    {
        var original = Console.Error;
        using var writer = new StringWriter();
        Console.SetError(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetError(original);
        }

        return writer.ToString();
    }
}
