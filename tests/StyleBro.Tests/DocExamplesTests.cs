using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Tests;

/// <summary>
/// Every rule page in docs/rules has an "Example" with "Before" and "After" code. This runs each example the way
/// 'dotnet format' does: the Before code must compile and get the rule's diagnostic, one Fix All pass must produce
/// exactly the After code, and the After code must compile without the diagnostic. So the docs can't go stale.
/// An ```ini block between "## Example" and the first "### Before" is the examples' .editorconfig.
/// </summary>
public partial class DocExamplesTests
{
    private static readonly string RulesFolder = Path.Combine(FindRepoRoot(), "docs", "rules");

    public static TheoryData<string> Rules()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(RulesFolder, "BRO*.md").OrderBy(f => f, StringComparer.Ordinal))
        {
            data.Add(Path.GetFileNameWithoutExtension(file));
        }

        return data;
    }

    [Fact]
    public void TheRuleIndex_ListsEveryRulePage()
    {
        var index = File.ReadAllText(Path.Combine(RulesFolder, "README.md"));
        var listed = Regex.Matches(index, @"^\| \[(BRO\d{4})\]\(\1\.md\) \|", RegexOptions.Multiline).Select(m => m.Groups[1].Value);
        var pages = Directory.GetFiles(RulesFolder, "BRO*.md").Select(Path.GetFileNameWithoutExtension);
        Assert.Equal(pages.Order(StringComparer.Ordinal), listed.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Rules))]
    public async Task ExampleIsFixedAsDocumented(string id)
    {
        var markdown = File.ReadAllText(Path.Combine(RulesFolder, id + ".md")).Replace("\r\n", "\n");
        var examples = ExamplePattern().Matches(markdown);
        Assert.True(examples.Count > 0, $"{id}.md needs an '## Example' with '### Before' and '### After' csharp blocks.");

        var analyzer = FindAnalyzer(id);
        var fixer = FindCodeFix(id);
        var exampleStart = markdown.IndexOf("## Example", StringComparison.Ordinal);
        var settings = ConfigPattern().Match(markdown, exampleStart, markdown.IndexOf("### Before", exampleStart, StringComparison.Ordinal) - exampleStart);
        foreach (Match example in examples)
        {
            var before = example.Groups["before"].Value;
            var after = example.Groups["after"].Value;

            var document = CreateDocument(before, settings.Success ? settings.Groups["config"].Value : null);
            await AssertCompilesAsync(document, id, "Before");
            var diagnostics = await GetDiagnosticsAsync(document, analyzer, id);
            Assert.True(diagnostics.Length > 0, $"{id}: the Before example doesn't get a {id} diagnostic.");

            var fixedDocument = await FixAllAsync(document, fixer, analyzer, id, diagnostics);
            var fixedText = (await fixedDocument.GetTextAsync()).ToString();
            Assert.Equal(after, fixedText);

            await AssertCompilesAsync(fixedDocument, id, "After");
            Assert.Empty(await GetDiagnosticsAsync(fixedDocument, analyzer, id));
        }
    }

    internal static Document CreateDocument(string code, string? editorConfig)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal)
                || Path.GetFileName(path) is "mscorlib.dll" or "netstandard.dll")
            .Select(path => MetadataReference.CreateFromFile(path));
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "Example",
            "Example",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: references));
        if (editorConfig is not null)
        {
            project = project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(editorConfig), filePath: "/.editorconfig").Project;
        }

        return project.AddDocument("Example.cs", SourceText.From(code), filePath: "/Example.cs");
    }

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, ImmutableArray<DiagnosticAnalyzer> analyzer, string id)
    {
        var compilation = await document.Project.GetCompilationAsync();
        var all = await compilation!.WithAnalyzers(analyzer, document.Project.AnalyzerOptions)
            .GetAnalyzerDiagnosticsAsync();
        return all.Where(d => d.Id == id).ToImmutableArray();
    }

    internal static async Task<Document> FixAllAsync(
        Document document, CodeFixProvider fixer, ImmutableArray<DiagnosticAnalyzer> analyzer, string id, ImmutableArray<Diagnostic> diagnostics)
    {
        // Find the fix's equivalence key from one registered code action, then run its Fix All on the document.
        CodeAction? first = null;
        await fixer.RegisterCodeFixesAsync(new CodeFixContext(
            document, diagnostics[0], (action, _) => first ??= action, CancellationToken.None));
        Assert.NotNull(first);

        var context = new FixAllContext(
            document,
            fixer,
            FixAllScope.Document,
            first!.EquivalenceKey,
            new[] { id },
            new DocumentDiagnosticProvider(analyzer, id),
            CancellationToken.None);
        var fixAll = await fixer.GetFixAllProvider()!.GetFixAsync(context);
        var operations = await fixAll!.GetOperationsAsync(CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        return solution.GetDocument(document.Id)!;
    }

    // Several analyzers can report one id (BRO1310: variables and parameters, fields).
    internal static ImmutableArray<DiagnosticAnalyzer> FindAnalyzer(string id)
    {
        return typeof(StyleBro.Analyzers.DiagnosticIds).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .Where(a => a.SupportedDiagnostics.Any(d => d.Id == id))
            .ToImmutableArray();
    }

    internal static CodeFixProvider FindCodeFix(string id)
    {
        return typeof(StyleBro.CodeFixes.LinkedFileFixAllProvider).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(t) && t.GetCustomAttribute<ExportCodeFixProviderAttribute>() is not null)
            .Select(t => (CodeFixProvider)Activator.CreateInstance(t)!)
            .Single(f => f.FixableDiagnosticIds.Contains(id));
    }

    [GeneratedRegex(@"### Before\s*\n```csharp\n(?<before>.*?)```\s*\n### After\s*\n```csharp\n(?<after>.*?)```", RegexOptions.Singleline)]
    private static partial Regex ExamplePattern();

    [GeneratedRegex(@"```ini
(?<config>.*?)```", RegexOptions.Singleline)]
    private static partial Regex ConfigPattern();

    private static async Task AssertCompilesAsync(Document document, string id, string which)
    {
        var compilation = await document.Project.GetCompilationAsync();
        var errors = compilation!.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, $"{id}: the {which} example doesn't compile:\n" + string.Join("\n", errors));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "StyleBro.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("StyleBro.slnx not found above " + AppContext.BaseDirectory);
    }

    private sealed class DocumentDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> analyzer;
        private readonly string id;

        public DocumentDiagnosticProvider(ImmutableArray<DiagnosticAnalyzer> analyzer, string id)
        {
            this.analyzer = analyzer;
            this.id = id;
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) =>
            GetDiagnosticsAsync(document, analyzer, id).ContinueWith(t => (IEnumerable<Diagnostic>)t.Result, cancellationToken);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());
    }
}
