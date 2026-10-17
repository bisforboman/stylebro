using StyleBro.Migrate;

namespace StyleBro.Tests;

public sealed partial class MigrationTests
{
    [Fact]
    public void UsesStyleCop_ChecksEveryTargetFramework()
    {
        // Trial 23 (fluentassertions): StyleCop.Analyzers under '$(TargetFramework)' == 'net6.0'; the outer evaluation of the
        // multi-targeted project has no TargetFramework, so Src/ looked like it had no StyleCop.
        const string Outer = """{ "Properties": { "TargetFrameworks": "net8.0;netstandard2.0" }, "Items": { "PackageReference": [] } }""";
        const string With = """{ "Items": { "PackageReference": [ { "Identity": "StyleCop.Analyzers" } ] } }""";
        const string Without = """{ "Items": { "PackageReference": [ { "Identity": "Newtonsoft.Json" } ] } }""";

        Assert.True(StyleCopSetup.UsesStyleCop(Outer, framework => framework == "net8.0" ? With : Without));
        Assert.False(StyleCopSetup.UsesStyleCop(Outer, _ => Without));

        // One framework: the outer evaluation is that framework's, nothing more to ask.
        Assert.False(StyleCopSetup.UsesStyleCop("""{ "Properties": { "TargetFrameworks": "net8.0" }, "Items": {} }""", _ => With));
    }

    [Theory]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = none", false)]
    [InlineData("dotnet_analyzer_diagnostic.severity = none", true)]
    public void Init_RootStyleOff_LeavesTheBuiltInRulesOut(string setting, bool bulk)
    {
        // Trial 23 (b5a): the root .editorconfig turns the Style category (or every analyzer) off; init's block wrote IDE0055,
        // IDE0036 and IDE0049 = warning by id, which win over that, and format re-indented the code.
        Write(".editorconfig", $"root = true\n\n[*.cs]\nindent_size = 4\n{setting}\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "namespace Lib;\n\npublic class C\n{\n    public int  X() => 1;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("the block leaves out the built-in rules it would turn on by id (IDE0036, IDE0049, IDE0055)", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0055.severity", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0036.severity", text, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0011.severity = none", text, StringComparison.Ordinal);

        // StyleBro's own rules stay on (the 2026-10-13 decision): by id where the bulk severity would hide them from format.
        Assert.Equal(bulk, text.Contains("dotnet_diagnostic.BRO1001.severity = warning", StringComparison.Ordinal));
        Assert.True(FormatCommand.WhitespaceOff(root));
    }

    [Fact]
    public void StyleOffIds_KeepsRulesTheFileSetsAndACategoryThatOverridesTheBulk()
    {
        var lines = new[] { "dotnet_diagnostic.IDE0055.severity = warning", "dotnet_diagnostic.IDE0011.severity = none", "dotnet_diagnostic.IDE0036.severity = warning" };
        var off = new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.category-Style.severity"] = "silent", ["dotnet_diagnostic.IDE0036.severity"] = "warning" };
        Assert.Equal(["IDE0055"], InitCommand.StyleOffIds(off, lines));
        Assert.Empty(InitCommand.StyleOffIds(new Dictionary<string, string> { ["dotnet_analyzer_diagnostic.severity"] = "none", ["dotnet_analyzer_diagnostic.category-Style.severity"] = "warning" }, lines));
        Assert.Empty(InitCommand.StyleOffIds(new Dictionary<string, string>(), lines));
    }

    [Fact]
    public void Init_RepeatsTheFilesOwnPathSections()
    {
        // Trial 23 (b-own-path-sections): '[src/**/*.cs]' with IDE0055 = none came before the block, whose '[*.cs]' IDE0055 =
        // warning won, and format re-indented src/.
        const string Own = "root = true\n\n[*]\nindent_style = space\n\n[src/**/*.cs]\nindent_size = 4\ndotnet_diagnostic.IDE0055.severity = none\ndotnet_diagnostic.IDE0049.severity = warning\n";
        Write(".editorconfig", Own);
        Write("src/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/C.cs", "namespace Lib;\n\npublic class C\n{\n    public int  X() => 1;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains(".editorconfig's own [src/**/*.cs] section sets keys the block sets too", output, StringComparison.Ordinal);
        var section = text.LastIndexOf("[src/**/*.cs]", StringComparison.Ordinal);
        Assert.True(section > text.IndexOf("# BEGIN stylebro-migrate", StringComparison.Ordinal));
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = none\n# END stylebro-migrate", text.Substring(section), StringComparison.Ordinal);

        // IDE0049 = warning is what the block says too: nothing to repeat. A second init writes the same block.
        Assert.DoesNotContain("IDE0049", text.Substring(section), StringComparison.Ordinal);
        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Equal(text, File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n"));
    }

    [Fact]
    public void Init_EmptyBlocksKept_KeepsThemFromIde0055()
    {
        // Trial 23 (b8ab): 19 of 19 '{ }' on one line were "kept", and IDE0055 (csharp_new_line_before_open_brace = all) split
        // 'else { }' and 'lock (x) { }'; csharp_preserve_single_line_statements = true keeps them (probed).
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "namespace Lib;\n\npublic class C\n{\n    public void M(bool x)\n    {\n        if (x)\n        {\n            M(false);\n        }\n        else { }\n\n        lock (this) { }\n        while (x) { }\n    }\n}\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        Assert.Contains("stylebro_allow_empty_single_line_blocks = true", text, StringComparison.Ordinal);
        Assert.Contains("csharp_preserve_single_line_statements = true", text, StringComparison.Ordinal);

        // The team's own 'false' stays: then init says IDE0055 splits them.
        Write(".editorconfig", "root = true\n\n[*.cs]\ncsharp_preserve_single_line_statements = false\n");
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("lets IDE0055 split those after a statement", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_LeadingLambdaCalls_DontCountForTheListRules()
    {
        // Trial 23 (b7a): 13 calls with a leading lambda counted for stylebro_allow_leading_multiline_lambda and against BRO1107
        // and BRO1108's conventions (first item after '(', items grouped), so both rules went off too.
        var calls = string.Concat(Enumerable.Range(0, 3).Select(i => $"        Run(() =>\n        {{\n            Work({i});\n        }}, {i});\n"));
        Write("C.cs", $"public class C\n{{\n    public void M()\n    {{\n{calls}    }}\n\n    private void Run(System.Action a, int i)\n    {{\n    }}\n\n    private void Work(int i)\n    {{\n    }}\n}}\n");

        var counts = Conventions.Count(root, new Dictionary<string, string>());
        Assert.Equal([0, 3], counts[StyleBro.Analyzers.Readability.ParameterLayout.LeadingLambdaKey]);
        Assert.Equal([0, 0], counts["stylebro_split_list_first_item"]);
        Assert.DoesNotContain(counts.Keys, k => k.EndsWith(Conventions.LeadingLambdaBucket, StringComparison.Ordinal));

        // Where the option stays off ('false' in the settings), BRO1107/BRO1108 judge them as usual.
        counts = Conventions.Count(root, new Dictionary<string, string> { [StyleBro.Analyzers.Readability.ParameterLayout.LeadingLambdaKey] = "false" });
        Assert.Equal([0, 3], counts["stylebro_split_list_first_item"]);
    }

    [Fact]
    public void Init_OptOutFolder_GetsNoSortKey()
    {
        // Trial 23 (c11b): samples/.editorconfig turns every analyzer off (not root = true); it inherited the block's
        // dotnet_sort_system_directives_first, and 'dotnet format' sorts usings wherever a sort key is set.
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("samples/.editorconfig", "[*.cs]\ndotnet_analyzer_diagnostic.severity = none\n");
        Write("samples/Demo/Demo.cs", "using System.Collections.Generic;\nusing System;\n\nnamespace Samples;\n\npublic class Demo\n{\n}\n");
        Write("src/Lib/Store.cs", "using System;\nusing System.Text;\n\nnamespace Lib;\n\npublic class Store\n{\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        var text = File.ReadAllText(Path.Combine(root, ".editorconfig")).Replace("\r\n", "\n");
        Assert.Contains("samples/: 'dotnet format' sorts usings wherever a sort key is set", output, StringComparison.Ordinal);
        var section = text.IndexOf("[src/**.cs]\n# The using order, not for samples/", StringComparison.Ordinal);
        Assert.True(section > 0, text);
        Assert.DoesNotContain("dotnet_sort_system_directives_first", text.Substring(0, section), StringComparison.Ordinal);
        Assert.Contains("dotnet_sort_system_directives_first = true", text.Substring(section), StringComparison.Ordinal);

        // C# files directly in the root: no section can leave samples/ out, so the key stays and init says so.
        Write("Root.cs", "public class Root\n{\n}\n");
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));
        Assert.Contains("'format' sorts the usings there too", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_CountDocPeriodsCommentsLangwords()
    {
        // Trial 23: stryker's one-line param texts without a period (BRO1603 added them) and '//' comments right below code
        // (BRO1504 added 265 blank lines); jint's '<c>null</c>' about JavaScript (BRO1618 made them C# keyword links).
        var members = string.Concat(Enumerable.Range(0, 4).Select(i =>
            $"    /// <summary>\n    /// Adds {i}.\n    /// </summary>\n    /// <param name=\"a\">the value</param>\n    /// <returns>Returns <c>null</c> when empty</returns>\n"
            + $"    public object M{i}(int a)\n    {{\n        var b = a;\n        // then\n        return b;\n    }}\n\n"));
        Write("C.cs", $"public class C\n{{\n{members}    public void N()\n    {{\n\n        // first\n        var c = 1;\n\n        // spaced\n        N();\n    }}\n}}\n");

        // The comment after '{' needs no blank line, the one below 'var c = 1;' has the one BRO1504 wants.
        var counts = Conventions.Count(root, new Dictionary<string, string>());
        Assert.Equal([4, 8], counts["dotnet_diagnostic.BRO1603.severity"]);
        Assert.Equal([1, 4], counts["dotnet_diagnostic.BRO1504.severity"]);
        Assert.Equal([0, 4], counts["dotnet_diagnostic.BRO1618.severity"]);

        var (lines, _) = Conventions.Decide(counts, new Dictionary<string, string>());
        Assert.Contains("dotnet_diagnostic.BRO1504.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1618.severity = none", lines);
        Assert.Contains("dotnet_diagnostic.BRO1603.severity = none", lines);
    }

    [Fact]
    public void Conventions_CountsNonPrivateFieldsAgainstTheirNamingRule()
    {
        // Trial 23 (jint): a 'suggestion' naming rule asks for '_camelCase' on every instance field; the code's public fields are
        // PascalCase, and BRO1306 renamed them. The code contradicting the rule turns BRO1306 off.
        const string Rule = "dotnet_naming_rule.instance_fields.severity = suggestion\ndotnet_naming_rule.instance_fields.symbols = fields\ndotnet_naming_rule.instance_fields.style = underscored\n"
            + "dotnet_naming_symbols.fields.applicable_kinds = field\ndotnet_naming_style.underscored.capitalization = camel_case\ndotnet_naming_style.underscored.required_prefix = _\n";
        Write(".editorconfig", "root = true\n\n[*.cs]\n" + Rule);
        Write("C.cs", "public class C\n{\n    public int Loads;\n    public int Saves;\n    public int Reads;\n    internal int _writes;\n    private int _count;\n}\n");

        var settings = Migration.OwnSettings(root);
        var counts = Conventions.Count(root, settings);
        Assert.Equal([1, 3], counts["dotnet_diagnostic.BRO1306.severity"]);
        Assert.Contains("dotnet_diagnostic.BRO1306.severity = none", Conventions.Decide(counts, new Dictionary<string, string>()).Lines);

        // Without the rule (or with it off), the fields count against StyleCop's PascalCase (trial 29): BRO1306 renames '_writes'.
        Write(".editorconfig", "root = true\n\n[*.cs]\n" + Rule.Replace("= suggestion", "= none", StringComparison.Ordinal));
        Assert.Equal([3, 1], Conventions.Count(root, Migration.OwnSettings(root))["dotnet_diagnostic.BRO1306.severity"]);
    }

    [Fact]
    public void Suppressions_SA1300OnAMember_GetsNoNamespaceRule()
    {
        // Trial 23: '[SuppressMessage("StyleCop...", "SA1300")]' on a method got BRO1312 (namespaces) too, which can't fire there.
        var replacements = new Dictionary<string, SortedSet<string>> { ["SA1300"] = new(StringComparer.Ordinal) { "BRO1309", "BRO1312" } };
        var (text, added) = Suppressions.Rewrite("class C\n{\n    [System.Diagnostics.CodeAnalysis.SuppressMessage(\"StyleCop.CSharp.NamingRules\", \"SA1300\")]\n    void m() { }\n}\n", replacements);
        Assert.Equal(1, added);
        Assert.Contains("\"BRO1309\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BRO1312", text, StringComparison.Ordinal);

        // At the assembly level (a namespace target) both stay; so do pragmas.
        (text, added) = Suppressions.Rewrite("[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(\"StyleCop.CSharp.NamingRules\", \"SA1300\", Target = \"~N:lib\")]\n#pragma warning disable SA1300\n", replacements);
        Assert.Equal(4, added);
    }

    [Fact]
    public void Migrate_NoStyleCopAtAll_DoesntCallAnythingLeftovers()
    {
        // Trial 23 (tools/Gen): "there are no StyleCop settings" followed by "StyleCop's severities there don't describe the code".
        Write("Gen.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("G.cs", "class G { }\n");

        var output = Capture(() => Assert.Equal(1, Program.Migrate([root])));

        Assert.Contains("there are no StyleCop settings", output, StringComparison.Ordinal);
        Assert.DoesNotContain("severities there don't describe", output, StringComparison.Ordinal);
        Assert.Contains("Nothing to migrate. Run 'stylebro-migrate init' instead", output, StringComparison.Ordinal);
    }

    [Fact]
    public void InitDiff_OnAStyleCopRepository_RefusesBeforeThePreview()
    {
        // Trial 23: 'init --diff' on a StyleCop repository ran the preview, whose settings step refused, and ended with "The
        // preview failed".
        Write("Lib.csproj", """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" /></ItemGroup></Project>""");
        Write("C.cs", "public class C\n{\n}\n");

        var output = Capture(() => Assert.Equal(1, Program.Main(["init", root, "--diff"])));

        Assert.Contains("Run 'stylebro-migrate --write' instead", output, StringComparison.Ordinal);
        Assert.DoesNotContain("preview", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Init_LeavesFixtureFoldersOutsideTheSolutions()
    {
        // Trial 23 (stryker): no solution in the root, src/Stryker.slnx; integrationtest/TargetProjects/NetCore (its own
        // solution, a props file that doesn't import the root's) got a preset version.
        Write("Directory.Build.props", "<Project />\n");
        Write("src/App.slnx", "<Solution>\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>\n");
        Write("src/Lib/Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("src/Lib/C.cs", "namespace Lib;\n\npublic class C\n{\n}\n");
        Write("integrationtest/NetCore/Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <RootDirectory>x</RootDirectory>\n  </PropertyGroup>\n</Project>\n");
        Write("integrationtest/NetCore/Fixture.sln", "Project(\"{}\") = \"App\", \"App\\App.csproj\", \"{}\"\n");
        Write("integrationtest/NetCore/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("integrationtest/NetCore/App/A.cs", "public class A\n{\n}\n");
        Write("integrationtest/Fixtures/All.slnx", "<Solution>\n  <Project Path=\"../NetCore/App/App.csproj\" />\n</Solution>\n");

        Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" })));

        Assert.Contains("<StyleBroPreset>", File.ReadAllText(Path.Combine(root, "Directory.Build.props")), StringComparison.Ordinal);
        Assert.DoesNotContain("<StyleBroPreset>", File.ReadAllText(Path.Combine(root, "integrationtest", "NetCore", "Directory.Build.props")), StringComparison.Ordinal);
    }

    [Fact]
    public void Init_ThisRequiredInTheGlobalConfig_SaysFormatWontFixIt()
    {
        // Trial 23 (b8c): IDE0009 enforced in .globalconfig only; 'dotnet format' reads no severity from it, so verify stayed
        // blind to code without 'this.' that the build reports.
        Write(".globalconfig", "is_global = true\n\ndotnet_style_qualification_for_field = true\ndotnet_diagnostic.IDE0009.severity = warning\n");
        Write("Lib.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />\n");
        Write("C.cs", "namespace Lib;\n\npublic class C\n{\n    private int x;\n\n    public int X => x;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root })));

        Assert.Contains("'format' doesn't fix it", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Conventions_UsingsInsideIf_AreSortedOnTheirOwn()
    {
        // Trial 23 (fluentassertions): a using inside '#if' after the sorted list was "not in order", and the note promised a
        // reorder 'dotnet format' never does (it doesn't sort across a directive).
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("using System;\nusing System.Collections.Generic;\nusing Alias = System.Text.StringBuilder;\n#if !NETSTANDARD2_0\nusing System.Buffers;\n#endif\n\nclass C { }\n");
        Assert.True(Conventions.UsingsSorted(tree.GetRoot(), systemFirst: true));
        Assert.Empty(Conventions.ResortedUsingLists(tree.GetRoot(), systemFirst: true));
    }
}
