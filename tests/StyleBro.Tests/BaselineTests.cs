using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Baseline;
using StyleBro.Analyzers.Readability;

namespace StyleBro.Tests;

public class BaselineTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "stylebro-baseline-test");

    private const string Old = """
        class Old
        {
            string a = "";
            string b = "", c = "";
        }
        """;

    [Fact]
    public async Task BaselinedViolations_AreSuppressed_OthersReported()
    {
        var baseline = Entries(("src/Old.cs", "    string a = \"\";", 1));
        var reported = await RunAsync(baseline, ("src/Old.cs", Old), ("src/New.cs", "class New { string a = \"\"; }"));

        Assert.Equal(new[] { "src/New.cs:1", "src/Old.cs:4", "src/Old.cs:4" }, reported);
    }

    [Fact]
    public async Task Count_CoversThatManyViolationsOnTheLine()
    {
        var baseline = Entries(("src/Old.cs", "string b = \"\", c = \"\";", 1));
        var reported = await RunAsync(baseline, ("src/Old.cs", Old));

        Assert.Equal(new[] { "src/Old.cs:3", "src/Old.cs:4" }, reported);
    }

    [Fact]
    public async Task MovedLines_StayBaselined_EditedLinesDont()
    {
        var baseline = Entries(("src/Old.cs", "string a = \"\";", 1), ("src/Old.cs", "string b = \"\", c = \"\";", 2));
        var moved = "// a new comment\n\n" + Old.Replace("string a = \"\";", "string a = \"\";").Replace("string b", "string  b");
        var reported = await RunAsync(baseline, ("src/Old.cs", moved));

        // Line 'string a' moved down two lines and stays hidden; 'string  b' changed, so both of its violations are new.
        Assert.Equal(new[] { "src/Old.cs:6", "src/Old.cs:6" }, reported);
    }

    [Fact]
    public async Task OtherRulesAndFilesOutsideTheFolder_AreNotAffected()
    {
        var baseline = Entries(("src/Old.cs", "string a = \"\";", 1)).Replace("BRO1106", "BRO1001");
        var reported = await RunAsync(baseline, ("src/Old.cs", Old));

        Assert.Equal(3, reported.Length);
    }

    [Fact]
    public void Render_IsSortedAndParsesBack()
    {
        var text = "# comment\nBRO1106\tsrc/B.cs\t0000000000000001\t2\nBRO1001\tsrc/A.cs\t0000000000000002\t1\nBRO1106\tsrc\\B.cs\t0000000000000001\t1\nbroken line\n";
        var baseline = Baseline.Parse(text);

        Assert.Equal(2, baseline.Count);
        Assert.Equal(3, baseline.Allowed(new Baseline.Key("BRO1106", "src/B.cs", "0000000000000001")));
        var rendered = baseline.Render();
        Assert.StartsWith(Baseline.Header, rendered);
        Assert.EndsWith("BRO1001\tsrc/A.cs\t0000000000000002\t1\nBRO1106\tsrc/B.cs\t0000000000000001\t3\n", rendered);
        Assert.Equal(rendered, Baseline.Parse(rendered).Render());
    }

    [Fact]
    public void Fingerprint_IgnoresIndentation_AndIsStable()
    {
        Assert.Equal(Baseline.Fingerprint("  return x;"), Baseline.Fingerprint("\treturn x;  "));
        Assert.NotEqual(Baseline.Fingerprint("return x;"), Baseline.Fingerprint("return y;"));
        Assert.Equal("af63dc4c8601ec8c", Baseline.Fingerprint("a"));
    }

    [Fact]
    public void Generator_ReadsDotnetFormatsReport()
    {
        var dir = Path.Combine(Path.GetTempPath(), "stylebro-baseline-gen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "src"));
        try
        {
            var file = Path.Combine(dir, "src", "A.cs");
            File.WriteAllText(file, "class A\r\n{\r\n    string a = \"\", b = \"\";\r\n}\r\n");
            string Change(int line, int column, string id) => $$"""{ "LineNumber": {{line}}, "CharNumber": {{column}}, "DiagnosticId": "{{id}}", "FormatDescription": "x" }""";
            var document = $$"""{ "FileName": "A.cs", "FilePath": {{System.Text.Json.JsonSerializer.Serialize(file)}}, "FileChanges": [{{Change(3, 16, "BRO1106")}}, {{Change(3, 26, "BRO1106")}}, {{Change(1, 1, "IDE0040")}}, {{Change(2, 1, "WHITESPACE")}}, {{Change(1, 1, "CA1050")}}] }""";
            // The same document twice: two target frameworks.
            var result = StyleBro.Migrate.BaselineCommand.Build(dir, "[" + document + "," + document + "]");

            Assert.Equal(2, result.PerRule["BRO1106"]);
            Assert.Equal(1, result.PerRule["IDE0040"]);
            Assert.Equal(1, result.Whitespace);
            Assert.Equal(1, result.NotCovered["CA1050"]);
            Assert.Equal(2, result.Baseline.Allowed(new Baseline.Key("BRO1106", "src/A.cs", Baseline.Fingerprint("string a = \"\", b = \"\";"))));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string Entries(params (string Path, string Line, int Count)[] entries) =>
        Baseline.Header + string.Concat(entries.Select(e => $"BRO1106\t{e.Path}\t{Baseline.Fingerprint(e.Line)}\t{e.Count}\n"));

    /// <summary>Runs BRO1106 with the suppressor; returns the reported (not suppressed) diagnostics as "path:line".</summary>
    private static async Task<string[]> RunAsync(string baseline, params (string Path, string Code)[] files)
    {
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(f.Code, path: Path.Combine(Root, f.Path.Replace('/', Path.DirectorySeparatorChar)))).ToList();
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is "System.Runtime.dll" or "System.Private.CoreLib.dll")
            .Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("Test", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var options = new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(new Text(Path.Combine(Root, Baseline.FileName), baseline)));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EmptyStringAnalyzer(), new BaselineSuppressor());
        var diagnostics = await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "BRO1106" && !d.IsSuppressed)
            .Select(d => Path.GetRelativePath(Root, d.Location.SourceTree!.FilePath).Replace('\\', '/') + ":" + (d.Location.GetLineSpan().StartLinePosition.Line + 1))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class Text : AdditionalText
    {
        private readonly string content;

        public Text(string path, string content)
        {
            Path = path;
            this.content = content;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }
}
