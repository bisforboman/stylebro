using StyleBro.Analyzers;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>Trial 24's findings in 'stylebro-migrate format', verify, the preview and baseline, and CSharpier after format.</summary>
public sealed partial class MigrationTests
{
    [Theory]

    // The line break after '$"""' belongs to the start token: the first line wasn't protected (CS8999 after the run).
    [InlineData("class C\n{\n#if LEGACY_TFM\n    string F(string n) => $$\"\"\"\n        #if LEGACY\n        class {{n}} { }\n        #endif\n        \"\"\";\n#endif\n}\n", "#if LEGACY\n|#endif\n        \"")]
    [InlineData("class C\n{\n#if LEGACY_TFM\n    string F(string n) => $\"\"\"\n        #pragma warning disable\n        class {n} { }\n        \"\"\";\n#endif\n}\n", "#pragma")]
    [InlineData("class C\n{\n#if LEGACY_TFM\n    const string S = \"\"\"\n        #nullable enable\n        \"\"\";\n#endif\n}\n", "#nullable")]
    [InlineData("class C\n{\n#if LEGACY_TFM\n    static readonly byte[] U = \"\"\"\n        #nullable enable\n        \"\"\"u8.ToArray();\n#endif\n}\n", "#nullable")]
    [InlineData("class C\n{\n#if LEGACY_TFM\n    const string S = @\"\n#pragma warning disable\n#region R\";\n#endif\n}\n", "#pragma|#region")]
    [InlineData("class C\n{\n#if LEGACY_TFM\n    string F(string n) => $@\"\n#pragma warning disable {n}\";\n#endif\n}\n", "#pragma")]
    [InlineData("class C\n{\n#if LEGACY_TFM\n    string F(string n) => @$\"\n#nullable disable {n}\n#endif\";\n#endif\n}\n", "#nullable|#endif\"")]
    public void Format_ProtectsTheFirstLineOfEveryStringShape(string text, string lines)
    {
        Assert.Equal(lines.Split('|').Select(l => text.IndexOf(l, StringComparison.Ordinal)).Order(), FormatCommand.DirectiveStringOffsets(text));
    }

    [Fact]
    public void Format_NotAHashThatDoesntStartALineOfTheString()
    {
        // '#' after other text on its line, or on the line the string starts on: no directive there in any parse.
        Assert.Empty(FormatCommand.DirectiveStringOffsets("class C\n{\n#if X\n    const string S = @\"#pragma x\n    a #if b\";\n#endif\n}\n"));
    }

    [Fact]
    public void Format_LeavesAStringsLoneCarriageReturn_WhereEndOfLineIsSet()
    {
        // A verbatim regex with a CR: the whitespace and style passes wrote end_of_line's LF there (the program's output changed).
        Write(".editorconfig", "root = true\n\n[*.cs]\nend_of_line = lf\n");
        Write("src/Regex.cs", "class R\n{\n    const string Mac = @\"a\rb\";\n}\n");
        Write("src/Plain.cs", "class P\n{\n}\n");
        Write("src/Mac.cs", "class M\r{\r}\r");

        // Old Mac line endings in code too: left out of every pass already (OldMacLineEndings).
        Write("src/Both.cs", "class B\r{\r    const string S = @\"a\rb\";\r}\r");
        Assert.Equal(["src/Regex.cs"], FormatCommand.LoneCrStringFiles(root));
        Assert.Equal((false, true), FormatCommand.LoneCrs("class R { const string S = @\"a\rb\"; }"));
        Assert.Equal((true, false), FormatCommand.LoneCrs("class M\r{\r}\r"));
        Assert.StartsWith("No whitespace pass: src/Regex.cs: a string there holds a CR without an LF", FormatCommand.LoneCrStringLine(["src/Regex.cs"]), StringComparison.Ordinal);

        // Without end_of_line the passes leave the CR alone.
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Assert.Empty(FormatCommand.LoneCrStringFiles(root));

        // The style pass gets them in --exclude too.
        Assert.Equal(["--diagnostics", "BRO1001", "--exclude", "src/Regex.cs"], FormatCommand.Excluding(["--diagnostics", "BRO1001"], ["src/Regex.cs"]));
        Assert.Equal(["--exclude", "src/Regex.cs", "obj/"], FormatCommand.Excluding(["--exclude", "obj/"], ["src/Regex.cs"]));
    }

    [Fact]
    public void Format_FormatsFilesLinkedForSomeFrameworksOnly()
    {
        // refit: src/Polyfills/*.cs is compiled for net4* only, which the outer evaluation doesn't list.
        Write("Shared/Poly.cs", "class Poly\n{\n}\n");
        var project = Path.Combine(root, "Lib", "Lib.csproj");
        var own = Path.Combine(root, "Lib", "A.cs");
        var polyfill = Path.Combine(root, "Shared", "Poly.cs");
        var compile = FormatCommand.FrameworkCompile([own], [own, polyfill]);
        Assert.Equal([polyfill], FormatCommand.Linked(project, [], compile));
        Assert.Empty(FormatCommand.Linked(project, [], FormatCommand.FrameworkCompile([own], null)));
        Assert.Equal(["Lib/", "Shared/"], FormatCommand.Include(root, root, [project], FormatCommand.Linked(project, [], compile)));
    }

    [Fact]
    public void Format_FixesUnusedUsings_WhereOnlyAFolderEnforcesThem()
    {
        // spectre-console: src/.editorconfig enforces IDE0005, and format ran from the repository's root.
        Write(".editorconfig", "root = true\n\n[*.cs]\nindent_size = 4\n");
        Write("src/.editorconfig", "[*.cs]\ndotnet_diagnostic.IDE0005.severity = warning\n");
        Write("src/Lib/C.cs", "class C\n{\n}\n");
        Assert.True(FormatCommand.EnforcesUnusedUsings(root));
        Write("src/.editorconfig", "[*.cs]\ndotnet_diagnostic.IDE0005.severity = suggestion\n");
        Assert.False(FormatCommand.EnforcesUnusedUsings(root));
    }

    [Fact]
    public void Format_TakesStyleBroRulesOutOfWarningsNotAsErrors()
    {
        // 'dotnet format' skipped BRO1001 and BRO1505 listed there; the build reported them as warnings.
        Assert.Equal(["BRO1001", "BRO1505"], FormatCommand.NotAsErrors(["CS0618;BRO1505;bro1001", "BRO1001", string.Empty]));
        Assert.Empty(FormatCommand.NotAsErrors(["CS0618;nullable"]));
        Assert.StartsWith("Warnings not as errors: BRO1001 is in <WarningsNotAsErrors>", FormatCommand.NotAsErrorsLine(["BRO1001"]), StringComparison.Ordinal);
        Assert.Contains("'$(StyleBroFormatWarnings)' == 'true'", FormatCommand.SelectFrameworkTargets, StringComparison.Ordinal);
        Assert.Contains("Regex]::Replace('$(WarningsNotAsErrors)', '(?i)\\bBRO\\d+\\b', '')", FormatCommand.SelectFrameworkTargets, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_BaselinesKeptFindings_InABaselineOutsideTheCode()
    {
        // <StyleBroBaseline> names eng/stylebro.baseline: the kept findings in src/ were silently not written.
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <StyleBroBaseline>$(MSBuildThisFileDirectory)eng\\stylebro.baseline</StyleBroBaseline>\n  </PropertyGroup>\n</Project>\n");
        Write("src/C.cs", "class C\n{\n    private int Count;\n}\n");
        var kept = new[] { new KeptFinding(Path.Combine(root, "src", "C.cs"), 3, 17, "BRO1303", KeptReason.NameInString, "Rename 'Count' to 'count'") };
        Assert.StartsWith("Baseline: eng/stylebro.baseline: added 1 kept finding", FormatCommand.BaselineKept(root, kept), StringComparison.Ordinal);
        var entries = Analyzers.Baseline.Baseline.Parse(File.ReadAllText(Path.Combine(root, "eng", "stylebro.baseline"))).Entries;
        Assert.Equal("../src/C.cs", Assert.Single(entries).Key.Path);
        Assert.Null(FormatCommand.BaselineKept(root, kept));
        Assert.Null(FormatCommand.BaselineOffLine(root, kept.Length));

        // Turned off: the kept findings fail a build with warnings as errors, which 'format' says.
        Write("Directory.Build.props", "<Project>\n  <PropertyGroup>\n    <StyleBroBaseline>none</StyleBroBaseline>\n  </PropertyGroup>\n</Project>\n");
        Assert.StartsWith("Baseline off: <StyleBroBaseline>none</StyleBroBaseline> in Directory.Build.props, so the kept finding isn't baselined", FormatCommand.BaselineOffLine(root, 1), StringComparison.Ordinal);
        Assert.Null(FormatCommand.BaselineOffLine(root, 0));
    }

    [Fact]
    public void Format_AMigrationsDesigner_IsntGeneratedByTheBuild()
    {
        // Dawnshard: 'dotnet ef migrations add' wrote them once; the note said to rebuild 60 of them.
        Write("Db/Schema/20200101000000_Init.Designer.cs", "// <auto-generated />\nusing Microsoft.EntityFrameworkCore.Migrations;\n\n[Migration(\"20200101000000_Init\")]\npartial class Init\n{\n}\n");
        Write("Migrations/20200102000000_Next.Designer.cs", "// <auto-generated />\nnamespace Repro.Migrations;\n\npartial class Next\n{\n}\n");
        Write("Data/Model.Designer.cs", "// <auto-generated />\npartial class Model\n{\n}\n");
        Assert.False(FormatCommand.IsGenerated(Path.Combine(root, "Db", "Schema", "20200101000000_Init.Designer.cs")));
        Assert.False(FormatCommand.IsGenerated(Path.Combine(root, "Migrations", "20200102000000_Next.Designer.cs")));
        Assert.True(FormatCommand.IsGenerated(Path.Combine(root, "Data", "Model.Designer.cs")));
    }

    [Fact]
    public void CSharpier_RunsTheRepositorysTool()
    {
        // Dawnshard: 21 files failed CI's 'dotnet csharpier check .' after format.
        Assert.Null(CSharpierStep.Find(root));
        Write(".config/dotnet-tools.json", "{ \"version\": 1, \"isRoot\": true, \"tools\": { \"csharpier\": { \"version\": \"1.3.0\", \"commands\": [ \"csharpier\" ] } } }");
        var tool = CSharpierStep.Find(root)!;
        Assert.Equal(Path.GetFullPath(root), Path.GetFullPath(tool.Folder));
        Assert.True(tool.Verbs);
        Assert.Equal([string.Empty], tool.Scopes);
        Assert.Equal([["csharpier", "format", "src/A.cs", "B.cs"]], CSharpierStep.Commands(tool, "format", ["src/A.cs", "B.cs"]));
        Assert.Equal([["csharpier", "check", "B.cs"]], CSharpierStep.Commands(tool, "check", ["B.cs"]));

        // Before 1.0 there are no verbs.
        var old = tool with { Verbs = false };
        Assert.Equal([["csharpier", "B.cs"]], CSharpierStep.Commands(old, "format", ["B.cs"]));
        Assert.Equal([["csharpier", "--check", "B.cs"]], CSharpierStep.Commands(old, "check", ["B.cs"]));

        // A long list takes several calls; the package's copy doesn't check the package version.
        var many = Enumerable.Range(0, 400).Select(i => $"src/Folder{i}/File{i}.cs").ToList();
        var calls = CSharpierStep.Commands(tool, "format", many);
        Assert.True(calls.Count > 1);
        Assert.Equal(many, calls.SelectMany(c => c.Skip(2)));
        Assert.Equal(["exec", "C.dll", "format", "--no-msbuild-check", "B.cs"], CSharpierStep.Commands(tool with { Command = ["exec", "C.dll"], Manifest = null }, "format", ["B.cs"]).Single());

        Assert.Equal("0.30.6", CSharpierStep.ToolVersion("{ \"tools\": { \"CSharpier\": { \"version\": \"0.30.6\" } } }"));
        Assert.Null(CSharpierStep.ToolVersion("{ \"tools\": { \"dotnet-ef\": { \"version\": \"9.0.0\" } } }"));
        Assert.Equal("1.2.1", CSharpierStep.PackageVersion("<PackageVersion Include=\"CSharpier.MsBuild\" Version=\"1.2.1\" />"));
        Assert.Equal("1.0.0", CSharpierStep.PackageVersion("<PackageReference Include=\"CSharpier.MsBuild\" Version=\"1.0.0\" PrivateAssets=\"all\" />"));
        Assert.Null(CSharpierStep.PackageVersion("<PackageReference Include=\"CSharpier.Core\" Version=\"1.0.0\" />"));
    }

    [Fact]
    public void CSharpier_OnlyTheCSharpFilesItFormats_AndWhatItPutsBack()
    {
        Write("src/Pretty/A.cs", "class A { }\n");
        Write("src/Other/B.cs", "class B { }\n");
        Write("src/Pretty/C.json", "{}\n");
        var tool = new CSharpierStep.Tool(root, ["csharpier"], true, ["src/Pretty"], null);
        var files = new[] { "src/Pretty/A.cs", "src/Other/B.cs", "src/Pretty/C.json", "src/Pretty/Gone.cs" }.Select(f => Path.Combine(root, f.Replace('/', Path.DirectorySeparatorChar)));
        Assert.Equal([Path.Combine(root, "src", "Pretty", "A.cs")], CSharpierStep.InScope(tool, root, files));

        // IDE0036 wrote 'static file', CSharpier wrote 'file static' back: no run settles it.
        var before = new Dictionary<string, string> { ["a"] = "1", ["b"] = "1", ["c"] = "1" };
        var afterFixes = new Dictionary<string, string> { ["a"] = "2", ["b"] = "2", ["c"] = "1" };
        var after = new Dictionary<string, string> { ["a"] = "1", ["b"] = "3", ["c"] = "1" };
        Assert.Equal(["a"], CSharpierStep.PutBack(before, afterFixes, after));
        Assert.StartsWith("Not clean: CSharpier puts back what the fixes changed in a,", CSharpierStep.PutBackLine(["a"]), StringComparison.Ordinal);
    }

    [Fact]
    public void Format_SaysItRanCSharpier()
    {
        Write(".config/dotnet-tools.json", "{ \"tools\": { \"csharpier\": { \"version\": \"1.3.0\" } } }");
        Assert.Contains("'format' ran it on the 2 C# files it changed", Assert.Single(FormatCommand.AfterNotes(root, [Path.Combine(root, "A.cs")], 2)), StringComparison.Ordinal);
        Assert.Contains("run it now", Assert.Single(FormatCommand.AfterNotes(root, [Path.Combine(root, "A.cs")])), StringComparison.Ordinal);
    }

    [Fact]
    public void Capture_ReadsOnItsOwnThreads()
    {
        // init's nested parallel evaluations waited minutes for thread-pool threads to read 'dotnet' output.
        var lines = new List<string>();
        FormatCommand.ReadOnThread(new StreamReader(new MemoryStream("a\nb\n"u8.ToArray())), line: lines.Add).Join();
        Assert.Equal(["a", "b"], lines);
        var text = string.Empty;
        FormatCommand.ReadOnThread(new StreamReader(new MemoryStream("a\nb\n"u8.ToArray())), t => text = t).Join();
        Assert.Equal("a\nb\n", text);
        Assert.Matches(@"^\d+\.\d+", FormatCommand.Capture(root, "--version"));
    }
}
