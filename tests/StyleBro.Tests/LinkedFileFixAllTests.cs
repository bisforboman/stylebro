using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.CodeFixes;

namespace StyleBro.Tests;

/// <summary>
/// The merge step of <see cref="LinkedFileFixAllProvider"/>: combining the edits that each target framework's copy of
/// a file wants. Getting this wrong wrote conflict markers or duplicated members into multi-targeted repos.
/// </summary>
public class LinkedFileFixAllTests
{
    private const string Original = "var a = new[]\n{\n    1,\n    2\n};\n";

    // Right after the last item "2", where a trailing comma goes.
    private static readonly int AfterLastItem = Original.IndexOf("2\n", StringComparison.Ordinal) + 1;

    [Fact]
    public void IdenticalEditsFromSeveralCopies_AreAppliedOnce()
    {
        var comma = new TextChange(new TextSpan(AfterLastItem, 0), ",");
        Assert.Equal("var a = new[]\n{\n    1,\n    2,\n};\n", Apply(comma, comma, comma));
    }

    [Fact]
    public void DifferentIndependentEdits_AreCombined()
    {
        var net10Only = new TextChange(new TextSpan(AfterLastItem, 0), ",");
        var everywhere = new TextChange(new TextSpan(0, 3), "int[]");
        Assert.Equal("int[] a = new[]\n{\n    1,\n    2,\n};\n", Apply(net10Only, everywhere, everywhere));
    }

    [Fact]
    public void ConflictingInsertionsAtTheSamePosition_KeepTheFirst()
    {
        Assert.Equal("var a = new[]\n{\n    1,\n    2,\n};\n", Apply(
            new TextChange(new TextSpan(AfterLastItem, 0), ","),
            new TextChange(new TextSpan(AfterLastItem, 0), ", ")));
    }

    [Fact]
    public void InsertionInsideAReplacedRange_IsDropped()
    {
        Assert.Equal("var a = new[] { 1, 2 };\n", Apply(
            new TextChange(TextSpan.FromBounds(Original.IndexOf("new", StringComparison.Ordinal), Original.IndexOf('}') + 1), "new[] { 1, 2 }"),
            new TextChange(new TextSpan(AfterLastItem, 0), ",")));
    }

    [Fact]
    public void DifferentWholeFileRewrites_KeepTheFirstInsteadOfMixingThem()
    {
        // Fixes that rewrite the syntax tree (like member ordering) count as one whole-text change per copy. Mixing
        // two of them used to duplicate members; now the first result wins and the next run handles the rest.
        var first = new TextChange(new TextSpan(0, Original.Length), "first");
        var second = new TextChange(new TextSpan(0, Original.Length), "second");
        Assert.Equal("first", Apply(first, second));
        Assert.Equal("first", Apply(first, first));
    }

    [Fact]
    public async Task CopiesWithDifferentText_AreFixedOnTheirOwn()
    {
        // 'dotnet format' runs its whitespace and code style fixes before the analyzers' and can leave the copies of a
        // multi-targeted file different. Edits for one copy's text used to be applied to the other's ("Changes must be
        // within bounds of SourceText" in Serilog).
        var path = Path.Combine(Path.GetTempPath(), "Linked.cs");
        var shortText = "class C { string a = \"\"; }\n";
        var longText = "// a comment the other copy doesn't have\nclass C\n{\n    string a = \"\";\n    string b = \"\";\n}\n";
        var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var ids = new List<Microsoft.CodeAnalysis.DocumentId>();
        foreach (var (name, text) in new[] { ("net8", shortText), ("net10", longText) })
        {
            var project = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
            var document = Microsoft.CodeAnalysis.DocumentId.CreateNewId(project);
            solution = solution
                .AddProject(project, name, name, Microsoft.CodeAnalysis.LanguageNames.CSharp)
                .AddDocument(document, "Linked.cs", SourceText.From(text), filePath: path);
            ids.Add(document);
        }

        var analyzer = new StyleBro.Analyzers.Readability.EmptyStringAnalyzer();
        var fixer = new StyleBro.CodeFixes.Readability.EmptyStringCodeFixProvider();
        var first = solution.GetDocument(ids[0])!;
        var diagnostics = await GetDiagnosticsAsync(first, analyzer);
        Microsoft.CodeAnalysis.CodeActions.CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(first, diagnostics[0], (a, _) => action ??= a, CancellationToken.None));
        var context = new Microsoft.CodeAnalysis.CodeFixes.FixAllContext(
            first, fixer, Microsoft.CodeAnalysis.CodeFixes.FixAllScope.Solution, action!.EquivalenceKey, new[] { "BRO1106" }, new Provider(analyzer), CancellationToken.None);
        var operations = await (await fixer.GetFixAllProvider()!.GetFixAsync(context))!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().Single().ChangedSolution;

        Assert.Equal(shortText.Replace("\"\"", "string.Empty"), (await fixedSolution.GetDocument(ids[0])!.GetTextAsync()).ToString());
        Assert.Equal(longText.Replace("\"\"", "string.Empty"), (await fixedSolution.GetDocument(ids[1])!.GetTextAsync()).ToString());
    }

    private static async Task<System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>> GetDiagnosticsAsync(
        Microsoft.CodeAnalysis.Document document, Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer analyzer)
    {
        var compilation = await document.Project.GetCompilationAsync();
        var diagnostics = await compilation!.WithAnalyzers(System.Collections.Immutable.ImmutableArray.Create(analyzer)).GetAnalyzerDiagnosticsAsync();
        var tree = await document.GetSyntaxTreeAsync();
        return diagnostics.Where(d => d.Location.SourceTree == tree).ToImmutableArray();
    }

    private sealed class Provider : Microsoft.CodeAnalysis.CodeFixes.FixAllContext.DiagnosticProvider
    {
        private readonly Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer analyzer;

        public Provider(Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public override async Task<IEnumerable<Microsoft.CodeAnalysis.Diagnostic>> GetDocumentDiagnosticsAsync(Microsoft.CodeAnalysis.Document document, CancellationToken cancellationToken) =>
            await GetDiagnosticsAsync(document, analyzer);

        public override Task<IEnumerable<Microsoft.CodeAnalysis.Diagnostic>> GetProjectDiagnosticsAsync(Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Microsoft.CodeAnalysis.Diagnostic>());

        public override Task<IEnumerable<Microsoft.CodeAnalysis.Diagnostic>> GetAllDiagnosticsAsync(Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Microsoft.CodeAnalysis.Diagnostic>());
    }

    private static string Apply(params TextChange[] changes)
    {
        return SourceText.From(Original).WithChanges(LinkedFileFixAllProvider.Merge(changes.ToList())).ToString();
    }
}
