using Microsoft.CodeAnalysis.CSharp;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What the tenth trial (CleanArchitecture, openmcdf, actions/runner) found in init and the migration.</summary>
public sealed partial class MigrationTests
{
    [Fact]
    public void EditorConfigRoots_AreTheFoldersWhoseEditorConfigSaysRootTrue()
    {
        Write(".editorconfig", "root = true\n[*.cs]\nindent_size = 4\n");
        Write("sample/.editorconfig", "# Core options\nroot = true\n[*]\nindent_style = space\n");
        Write("sample/deeper/.editorconfig", "root=TRUE\n");
        Write("src/.editorconfig", "[*.cs]\nroot = true\n");
        Write("tests/.editorconfig", "[*.cs]\nindent_size = 2\n");
        Write("sample/A.cs", "class A\n{\n}\n");
        Write("sample/deeper/B.cs", "class B\n{\n}\n");
        Write("C.cs", "class C\n{\n}\n");

        Assert.Equal(new[] { "sample", "sample/deeper" }, StyleCopSetup.EditorConfigRoots(root));
        Assert.Equal(new[] { "C.cs" }, Migration.OwnCSharpFiles(root).Select(f => Path.GetRelativePath(root, f)));
        Assert.Equal(new[] { "A.cs" }, Migration.OwnCSharpFiles(Path.Combine(root, "sample")).Select(f => Path.GetRelativePath(Path.Combine(root, "sample"), f)));
    }

    [Fact]
    public void Init_FolderWithItsOwnRootEditorConfig_GetsABlockJudgedFromItsOwnCode()
    {
        // CleanArchitecture: sample/ has its own 'root = true' .editorconfig, so the root's block never reached it; formatting
        // it rewrote its '== null' checks (32 of 33) and added trailing commas it never uses.
        Write(".editorconfig", "root = true\n");
        Write("sample/.editorconfig", "root = true\n[*.cs]\nindent_size = 2\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"src/A{i}.cs", $"class A{i}\n{{\n    bool M(object o) => o is null;\n}}\n");
            Write($"sample/B{i}.cs", $"class B{i}\n{{\n    bool M(object o) => o == null;\n}}\n");
        }

        Write("docs/.editorconfig", "root = true\n");
        Write("docs/readme.md", "No code here.\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root, "--write" }))).Replace("\r\n", "\n");

        Assert.Contains("sample/.editorconfig says root = true, so the root's settings don't reach sample/: it gets a block of its own, from its own code.", output, StringComparison.Ordinal);
        Assert.Contains("Conventions in the code under sample/", output, StringComparison.Ordinal);
        Assert.Contains("Wrote the built-in rule severities to sample/.editorconfig.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("docs/", output, StringComparison.Ordinal);
        var sample = File.ReadAllText(Path.Combine(root, "sample", ".editorconfig"));
        Assert.StartsWith("root = true\n[*.cs]\nindent_size = 2\n", sample, StringComparison.Ordinal);
        Assert.Contains("stylebro_null_check_style = equality_operator", sample, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = warning", sample, StringComparison.Ordinal);
        Assert.DoesNotContain("stylebro_null_check_style", File.ReadAllText(Path.Combine(root, ".editorconfig")), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "docs", ".editorconfig")) && File.ReadAllText(Path.Combine(root, "docs", ".editorconfig")).Contains("BEGIN", StringComparison.Ordinal));

        // Run again: both blocks are there.
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.Contains(InitCommand.PresentLine, output, StringComparison.Ordinal);
        Assert.Contains("sample/.editorconfig: " + InitCommand.PresentLine, output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_FolderWithTooLittleCode_KeepsTheDefaults_AndSaysSo()
    {
        Write(".editorconfig", "root = true\n");
        Write("sample/.editorconfig", "root = true\n");
        Write("sample/A.cs", "class A\n{\n    bool M(object o) => o == null;\n}\n");

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        Assert.Contains("  note     too little code here to tell its conventions (fewer than 3 places each), so StyleBro's defaults apply", output, StringComparison.Ordinal);
        Assert.Contains("== sample/.editorconfig (run with --write to add it)", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_EndOfLineTheCommittedFilesContradict_FollowsTheFiles()
    {
        // CleanArchitecture: 'end_of_line = crlf', 349 of 350 C# files LF, no .gitattributes, core.autocrlf false. 'format' turned
        // 86 files to CRLF, 44 of them with no other change. 'unset' would rewrite every formatted file to the OS's ending.
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        Assert.Contains("  note     Line endings: your .editorconfig says end_of_line = crlf, but 3 of 3 C# files are LF as committed and git doesn't convert them", output, StringComparison.Ordinal);
        Assert.Contains("  kept     C# files' line endings: 3 of 3 LF -> lf", output, StringComparison.Ordinal);
        Assert.Contains("\nend_of_line = lf\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("end_of_line = unset", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.IDE0055.severity = warning\n", output, StringComparison.Ordinal);

        // Files that agree with the key: nothing to say.
        Write(".editorconfig", "root = true\n[*]\nend_of_line = lf\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.DoesNotContain("Line endings: your .editorconfig says", output, StringComparison.Ordinal);
        Assert.Contains("  set      C# files' line endings: 3 of 3 LF -> set in .editorconfig (lf)", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new[] { 3, 0 }, "crlf", "lf")]
    [InlineData(new[] { 0, 12 }, "lf", "crlf")]
    [InlineData(new[] { 11, 1 }, "crlf", "lf")]
    [InlineData(new[] { 3, 0 }, "lf", null)]
    [InlineData(new[] { 6, 4 }, "crlf", null)]
    [InlineData(new[] { 2, 0 }, "crlf", null)]
    [InlineData(new[] { 3, 0 }, "crlf:silent", "lf")]
    public void ContradictedEnding_NeedsTheUsualMajorityAgainstTheKey(int[] endings, string setting, string? expected)
    {
        var counts = Conventions.NewCounts();
        counts[Conventions.EndOfLineKey] = endings;

        var result = Conventions.ContradictedEnding(counts, new Dictionary<string, string> { ["end_of_line"] = setting });

        Assert.Equal(expected, result?.Ending);
    }

    [Fact]
    public void Migrate_EndOfLineTheCommittedFilesContradict_FollowsTheFiles()
    {
        Write(".editorconfig", "root = true\n[*]\nend_of_line = crlf\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("end_of_line = lf", result.Lines);
        Assert.DoesNotContain("end_of_line = unset", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("Line endings: your .editorconfig says end_of_line = crlf, but 3 of 3 C# files are LF", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("SA1503", "true")]
    [InlineData("SA1519", "true")]
    [InlineData("SA1520", "true")]
    [InlineData(null, "false")]
    public void Migrate_Braces_AreStyleCopsMeaning(string? on, string expected)
    {
        // openmcdf (SA1519/SA1520 on, SA1503 off): 'when_multiline' is IDE0011's "multi-line", which counts a split 'if' header,
        // and BRO1515 braced a body SA1519 accepts. 'true' with BRO1514 off reports exactly StyleCop's statements.
        var rules = new[] { "SA1503", "SA1519", "SA1520" }.Select(id => $"dotnet_diagnostic.{id}.severity = {(id == on ? "warning" : "none")}\n");
        Write(".editorconfig", "root = true\n[*.cs]\n" + string.Concat(rules));

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains($"csharp_prefer_braces = {expected}", result.Lines);
    }

    [Fact]
    public void Migrate_UnenforcedPreferBraces_IsNotKept()
    {
        // openmcdf: 'csharp_prefer_braces = when_multiline:none' was kept as the team's own setting.
        Write(".editorconfig", "root = true\n[*.cs]\ncsharp_prefer_braces = when_multiline:none\ncsharp_using_directive_placement = outside_namespace:none\ndotnet_diagnostic.SA1503.severity = none\n");

        var result = Migration.Generate(StyleCopSetup.Read(root), root);

        Assert.Contains("csharp_prefer_braces = true", result.Lines);
        Assert.Contains(result.Notes, n => n.StartsWith("Not kept, because nothing enforces them (severity silent or none): csharp_prefer_braces = when_multiline:none;", StringComparison.Ordinal));

        // Other unenforced keys stay the team's: StyleCop's 'inside_namespace' had BRO1008 move openmcdf's usings.
        Assert.DoesNotContain(result.Lines, l => l.StartsWith("csharp_using_directive_placement", StringComparison.Ordinal));

        // Enforced, it stays.
        Write(".editorconfig", "root = true\n[*.cs]\ncsharp_prefer_braces = when_multiline:warning\ndotnet_diagnostic.SA1503.severity = none\n");
        Assert.DoesNotContain(Migration.Generate(StyleCopSetup.Read(root), root).Lines, l => l.StartsWith("csharp_prefer_braces", StringComparison.Ordinal));
    }

    [Fact]
    public void Migrate_FolderWithItsOwnRootEditorConfig_GetsTheWholeBlock()
    {
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.SA1500.severity = warning\n");
        Write("sample/.editorconfig", "root = true\n[*.cs]\nindent_size = 2\nend_of_line = crlf\n");
        for (var i = 0; i < 3; i++)
        {
            Write($"sample/A{i}.cs", $"class A{i}\n{{\n}}\n");
        }

        var setup = StyleCopSetup.Read(root);
        var result = Migration.Generate(setup, root);
        var plan = Migration.Plan(setup, root, result);

        var file = Path.Combine("sample", ".editorconfig");
        var (section, lines) = plan[file][0];
        Assert.Equal(Migration.MainSection, section);
        Assert.Contains("dotnet_diagnostic.IDE0055.severity = warning", lines);
        Assert.Contains("end_of_line = lf", lines);
        Assert.Contains(result.Notes, n => n.StartsWith("sample/.editorconfig says root = true", StringComparison.Ordinal));
        Assert.Contains(result.Notes, n => n.StartsWith("  sample/: Line endings: your .editorconfig says end_of_line = crlf", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("class C { static int s_count; static int t_cache; }", 0, 2)]
    [InlineData("class C { private static readonly object s_lock = new(); static int count; }", 1, 1)]
    [InlineData("class C { public static int s_count; const int s_max = 1; int m_count; private int _x; }", 0, 0)]
    [InlineData("class C { static int Count, s_other; }", 1, 1)]
    public void Conventions_CountStaticPrefixes(string source, int without, int with)
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText(source), counts);

        Assert.Equal(new[] { without, with }, counts[Conventions.StaticPrefixKey]);
    }

    [Theory]
    [InlineData("class C { string M(int i) => i.ToString(); }", 1, 0)]
    [InlineData("class C { String M(int i) => i.ToString(); }", 0, 1)]
    [InlineData("class C { string M() => String.Empty; }", 0, 1)]
    [InlineData("class C { object M() => System.Int32.MaxValue; }", 0, 1)]
    [InlineData("using String = System.Text.StringBuilder;\nclass C { void M(Kind k) { _ = k.String; _ = Other.Int32; } }", 0, 0)]
    [InlineData("class C { void M() { } }", 0, 0)]
    [InlineData("class C { void M(Other.String s) { } }", 0, 0)]
    public void Conventions_CountTypeAliasFiles(string source, int keywords, int names)
    {
        var counts = Conventions.NewCounts();

        Conventions.Count(CSharpSyntaxTree.ParseText(source), counts);

        Assert.Equal(new[] { keywords, names }, counts[Conventions.TypeAliasKey]);
    }

    [Fact]
    public void Init_StaticPrefixesAndFrameworkNames_KeepSCamelCaseAndTurnIde0049Off()
    {
        // actions/runner: ~541 's_' statics renamed by BRO1307, and 'String' -> 'string' 6,424 times in 501 files.
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    private static int s_count{i};\n\n    String M() => String.Empty;\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        // Since trial 26 the 's_' is kept with s_camelCase instead of turning BRO1307 off.
        Assert.Contains("  kept     private static fields: 3 of 3 with an 's_' prefix -> s_camelCase", output, StringComparison.Ordinal);
        Assert.Contains("\nstylebro_private_mutable_static_field_naming = s_camelCase\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1307.severity = none", output, StringComparison.Ordinal);
        Assert.Contains("  off      built-in type names: 3 of 3 framework names ('Int32') -> IDE0049 is off", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.IDE0049.severity = none\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.IDE0049.severity = warning", output, StringComparison.Ordinal);

        // A naming rule that requires 's_' already keeps them; the team's IDE0049 setting decides.
        Write(".editorconfig", "root = true\n[*.cs]\ndotnet_naming_rule.statics.symbols = statics\ndotnet_naming_rule.statics.style = s_style\ndotnet_naming_rule.statics.severity = warning\n"
            + "dotnet_naming_symbols.statics.applicable_kinds = field\ndotnet_naming_symbols.statics.required_modifiers = static\ndotnet_naming_style.s_style.required_prefix = s_\n"
            + "dotnet_naming_style.s_style.capitalization = camel_case\ndotnet_style_predefined_type_for_member_access = false:warning\n");
        output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");
        Assert.Contains("  set      private static fields: 3 of 3 with an 's_'/'t_' prefix -> a naming rule requires 's_', which BRO1307 keeps", output, StringComparison.Ordinal);
        Assert.Contains("  set      built-in type names: 3 of 3 framework names ('Int32') -> set in .editorconfig (false:warning)", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet_diagnostic.BRO1307.severity = none", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_StaticsWithoutPrefix_KeepBro1307On()
    {
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    private static int count{i};\n\n    string M() => string.Empty;\n}}\n");
        }

        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        Assert.Contains("  default  private static fields: 3 of 3 without a prefix -> warning", output, StringComparison.Ordinal);
        Assert.Contains("  default  built-in type names: 3 of 3 only keywords ('int') -> warning", output, StringComparison.Ordinal);
        Assert.DoesNotContain("BRO1307.severity = none", output, StringComparison.Ordinal);
        Assert.Contains("\ndotnet_diagnostic.IDE0049.severity = warning\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Init_StaticPrefixStyleSet_KeepsBro1307On()
    {
        // The team asks for 's_' statics itself (stylebro_private_mutable_static_field_naming = s_camelCase): BRO1307 keeps 's_'.
        for (var i = 0; i < 3; i++)
        {
            Write($"A{i}.cs", $"class A{i}\n{{\n    private static int s_count{i};\n}}\n");
        }

        Write(".editorconfig", "root = true\n[*.cs]\nstylebro_private_mutable_static_field_naming = s_camelCase\n");
        var output = Capture(() => Assert.Equal(0, InitCommand.Run(new[] { root }))).Replace("\r\n", "\n");

        Assert.Contains("  set      private static fields: 3 of 3 with an 's_'/'t_' prefix -> set in .editorconfig (stylebro_private_mutable_static_field_naming = s_camelCase)", output, StringComparison.Ordinal);
        Assert.DoesNotContain("BRO1307.severity = none", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[src/Sdk/**]\ngenerated_code = true\n", "src/Sdk")]
    [InlineData("[/src/Sdk/**.cs]\ngenerated_code = true\n", "src/Sdk")]
    [InlineData("[src/Sdk/**/*.cs]\ngenerated_code = true\n", "src/Sdk")]
    [InlineData("[src/Sdk/**]\ngenerated_code = false\n", null)]
    [InlineData("[**/Sdk/**]\ngenerated_code = true\n", null)]
    [InlineData("# BEGIN stylebro-migrate\n[src/Sdk/**]\ngenerated_code = true\n# END stylebro-migrate\n", null)]
    public void MarkedGeneratedFolders_AreThePathSectionsTheRepositoryMarks(string editorConfig, string? folder)
    {
        Write(".editorconfig", "root = true\n" + editorConfig);

        Assert.Equal(folder is null ? Array.Empty<string>() : new[] { folder }, Migration.MarkedGeneratedFolders(root));
    }

    [Fact]
    public void Conventions_SkipFoldersTheRepositoryMarksGenerated()
    {
        // actions/runner's src/Sdk is copied-in code under no vendor-like name; marked generated_code = true, it doesn't vote.
        Write(".editorconfig", "root = true\n[src/Sdk/**]\ngenerated_code = true\n");
        Write("src/Sdk/A.cs", "class A\n{\n    bool M(object o) => o == null;\n}\n");
        Write("src/B.cs", "class B\n{\n    bool M(object o) => o is null;\n}\n");

        Assert.Equal(new[] { 1, 0 }, Conventions.Count(root)["stylebro_null_check_style"]);
    }
}
