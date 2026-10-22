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
    public async Task ParallelWork_GivesTheResultsInTheItemsOrder()
    {
        // The documents of a Fix All are fixed in parallel; the merge must still see their changes in document order.
        var items = Enumerable.Range(0, 40).ToList();
        var results = await LinkedFileFixAllProvider.ForEachAsync(
            items,
            async i =>
            {
                await Task.Delay((40 - i) % 7);
                return i * 2;
            },
            CancellationToken.None);
        Assert.Equal(items.Select(i => i * 2), results);
    }

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
    public async Task CopiesWithDifferentText_AllGetTheFirstCopysFixedText()
    {
        // 'dotnet format' runs its whitespace and code style fixes before the analyzers' and can leave the copies of a
        // multi-targeted file different. Edits for one copy's text used to be applied to the other's ("Changes must be
        // within bounds of SourceText" in Serilog); fixing each copy on its own gave different edits at the same places,
        // which 'dotnet format' wrote as conflict markers (Newtonsoft.Json), and so did fixing only one copy. Every copy
        // must end with the same text: the first copy's, fixed.
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
        Assert.Equal(shortText.Replace("\"\"", "string.Empty"), (await fixedSolution.GetDocument(ids[1])!.GetTextAsync()).ToString());
    }

    [Fact]
    public async Task RemovingRegions_SortsInTheCopiesWhereTheCodeIsActive()
    {
        // Newtonsoft.Json's samples: a '#region' around the file header, the rest inside '#if'. The copy where the code
        // is inactive only removes the header's region; the active copy also sorts. That sort used to be one whole-text
        // edit, which lost the merge to the header edit, so BRO1001 sorted on the next run.
        var path = Path.Combine(Path.GetTempPath(), "Regions.cs");
        var text = "#region License\n// header\n#endregion\n\n#if ACTIVE\nnamespace N\n{\n    #region Classes\n    public class B\n    {\n    }\n    #endregion\n\n    #region Interfaces\n    public interface IA\n    {\n    }\n    #endregion\n}\n#endif\n";
        var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var ids = new List<Microsoft.CodeAnalysis.DocumentId>();
        foreach (var symbols in new[] { Array.Empty<string>(), new[] { "ACTIVE" } })
        {
            var project = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
            var document = Microsoft.CodeAnalysis.DocumentId.CreateNewId(project);
            solution = solution
                .AddProject(project, "p" + symbols.Length, "p" + symbols.Length, Microsoft.CodeAnalysis.LanguageNames.CSharp)
                .WithProjectParseOptions(project, new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(preprocessorSymbols: symbols))
                .AddDocument(document, "Regions.cs", SourceText.From(text), filePath: path);
            ids.Add(document);
        }

        var analyzer = new StyleBro.Analyzers.Readability.RegionsAnalyzer();
        var fixer = new StyleBro.CodeFixes.Readability.RegionsCodeFixProvider();
        var first = solution.GetDocument(ids[0])!;
        var diagnostics = await GetDiagnosticsAsync(first, analyzer);
        Microsoft.CodeAnalysis.CodeActions.CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(first, diagnostics[0], (a, _) => action ??= a, CancellationToken.None));
        var context = new Microsoft.CodeAnalysis.CodeFixes.FixAllContext(
            first, fixer, Microsoft.CodeAnalysis.CodeFixes.FixAllScope.Solution, action!.EquivalenceKey, new[] { "BRO1112" }, new Provider(analyzer), CancellationToken.None);
        var operations = await (await fixer.GetFixAllProvider()!.GetFixAsync(context))!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().Single().ChangedSolution;

        var expected = "// header\n\n#if ACTIVE\nnamespace N\n{\n    public interface IA\n    {\n    }\n\n    public class B\n    {\n    }\n}\n#endif\n";
        Assert.Equal(expected, (await fixedSolution.GetDocument(ids[1])!.GetTextAsync()).ToString());
    }

    [Fact]
    public async Task RenameThatIsUnsafeInOneCopy_ChangesNoCopy()
    {
        // Polly: an abstract member of a multi-targeted library was renamed in the copies where it was safe, while the
        // test project (referencing one target framework) kept its override: CS0115. A rename is all or nothing.
        var path = Path.Combine(Path.GetTempPath(), "Base.cs");
        var text = "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"tests\")]\n\ninternal abstract class Base\n{\n    public abstract void run();\n}\n";
        var corlib = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var solution = workspace.CurrentSolution;
        var ids = new List<Microsoft.CodeAnalysis.DocumentId>();
        var projects = new List<Microsoft.CodeAnalysis.ProjectId>();
        foreach (var name in new[] { "net8", "net10" })
        {
            var project = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
            var document = Microsoft.CodeAnalysis.DocumentId.CreateNewId(project);
            solution = solution
                .AddProject(project, name, name, Microsoft.CodeAnalysis.LanguageNames.CSharp)
                .WithProjectCompilationOptions(project, new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReference(project, corlib)
                .AddDocument(document, "Base.cs", SourceText.From(text), filePath: path);
            ids.Add(document);
            projects.Add(project);
        }

        // The tests reference the first copy only, and their derived type already has the new name.
        var tests = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
        solution = solution
            .AddProject(tests, "tests", "tests", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithProjectCompilationOptions(tests, new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(tests, corlib)
            .AddProjectReference(tests, new Microsoft.CodeAnalysis.ProjectReference(projects[0]))
            .AddDocument(Microsoft.CodeAnalysis.DocumentId.CreateNewId(tests), "Derived.cs", SourceText.From(
                "internal class Derived : Base\n{\n    public override void run()\n    {\n    }\n\n    public void Run()\n    {\n    }\n}\n"));

        var analyzer = new StyleBro.Analyzers.Naming.PascalCaseNamingAnalyzer();
        var fixer = new StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider();
        var first = solution.GetDocument(ids[1])!;
        var diagnostics = await GetDiagnosticsAsync(first, analyzer);
        Microsoft.CodeAnalysis.CodeActions.CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(first, diagnostics.Single(), (a, _) => action ??= a, CancellationToken.None));

        // Kept on purpose: no action. 'dotnet format' then runs Fix All without an equivalence key, as here.
        Assert.Null(action);
        var context = new Microsoft.CodeAnalysis.CodeFixes.FixAllContext(
            first, fixer, Microsoft.CodeAnalysis.CodeFixes.FixAllScope.Solution, action?.EquivalenceKey!, new[] { "BRO1309" }, new Provider(analyzer), CancellationToken.None);
        var operations = await (await fixer.GetFixAllProvider()!.GetFixAsync(context))!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().Single().ChangedSolution;

        Assert.Equal(text, (await fixedSolution.GetDocument(ids[0])!.GetTextAsync()).ToString());
        Assert.Equal(text, (await fixedSolution.GetDocument(ids[1])!.GetTextAsync()).ToString());
    }

    // The fix-loop measurement (2026-10): Serilog's Log.cs got 'ref Serilog.Log.logger' in some 'dotnet format' runs and
    // 'ref Log.logger' in others. A static field's qualifier was spelled with the field's type from the copy whose
    // diagnostic started the rename, against another copy's semantic model, where that type isn't the one 'Log' binds to.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task StaticFieldQualifier_IsTheSameWhicheverCopyIsFixed(int copy)
    {
        var path = Path.Combine(Path.GetTempPath(), "Log.cs");
        var text = "namespace Serilog;\n\npublic static class Log\n{\n    static object _logger = new object();\n\n    public static object Logger => _logger;\n\n    public static void CloseAndFlush()\n    {\n        var logger = System.Threading.Interlocked.Exchange(ref _logger, new object());\n    }\n}\n";
        var corlib = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var solution = new Microsoft.CodeAnalysis.AdhocWorkspace().CurrentSolution;
        var ids = new List<Microsoft.CodeAnalysis.DocumentId>();
        foreach (var name in new[] { "net8", "net10" })
        {
            var project = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
            var document = Microsoft.CodeAnalysis.DocumentId.CreateNewId(project);
            solution = solution
                .AddProject(project, name, name, Microsoft.CodeAnalysis.LanguageNames.CSharp)
                .WithProjectCompilationOptions(project, new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReference(project, corlib)
                .AddDocument(document, "Log.cs", SourceText.From(text), filePath: path);
            ids.Add(document);
        }

        var analyzer = new StyleBro.Analyzers.Naming.FieldNamingAnalyzer();
        var fixer = new StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider();
        var first = solution.GetDocument(ids[copy])!;
        var diagnostic = (await GetDiagnosticsAsync(first, analyzer)).Single(d => d.Id == "BRO1303");
        Microsoft.CodeAnalysis.CodeActions.CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(first, diagnostic, (a, _) => action ??= a, CancellationToken.None));
        var operations = await action!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().Single().ChangedSolution;

        var expected = text.Replace("_logger", "logger", StringComparison.Ordinal).Replace("ref logger", "ref Log.logger", StringComparison.Ordinal);
        Assert.Equal(expected, (await fixedSolution.GetDocument(ids[0])!.GetTextAsync()).ToString());
        Assert.Equal(expected, (await fixedSolution.GetDocument(ids[1])!.GetTextAsync()).ToString());
    }

    // The performance audit (2026-10): BRO1112's and BRO1409's fixes sort with BRO1001 on the document they have just
    // changed, a new solution each, and the names in code (cached per solution) were collected again for every fixed file:
    // Newtonsoft.Json's BRO1112 Fix All took 387 s instead of 15.5 s. One Fix All scans the solution once.
    [Theory]
    [InlineData("BRO1112")]
    [InlineData("BRO1409")]
    public async Task FixAllThatSorts_ScansTheSolutionOnce(string id)
    {
        var corlib = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var project = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
        var options = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
            .WithSpecificDiagnosticOptions(new Dictionary<string, Microsoft.CodeAnalysis.ReportDiagnostic> { [id] = Microsoft.CodeAnalysis.ReportDiagnostic.Warn });
        var solution = new Microsoft.CodeAnalysis.AdhocWorkspace().CurrentSolution
            .AddProject(project, "p", "p", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, options)
            .AddMetadataReference(project, corlib);
        var ids = new List<Microsoft.CodeAnalysis.DocumentId>();
        for (var i = 0; i < 3; i++)
        {
            var text = id == "BRO1112"
                ? $"public class C{i}\n{{\n    #region Methods\n    public void M()\n    {{\n    }}\n    #endregion\n\n    #region Fields\n    private int x;\n    #endregion\n}}\n"
                : $"internal class C{i}\n{{\n    public void B()\n    {{\n    }}\n\n    public int P => 0;\n}}\n";
            var document = Microsoft.CodeAnalysis.DocumentId.CreateNewId(project);
            solution = solution.AddDocument(document, $"C{i}.cs", SourceText.From(text), filePath: Path.Combine(Path.GetTempPath(), $"C{i}.cs"));
            ids.Add(document);
        }

        Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer analyzer = id == "BRO1112"
            ? new StyleBro.Analyzers.Readability.RegionsAnalyzer()
            : new StyleBro.Analyzers.Maintainability.InternalTypeMethodAnalyzer();
        Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider fixer = id == "BRO1112"
            ? new StyleBro.CodeFixes.Readability.RegionsCodeFixProvider()
            : new StyleBro.CodeFixes.Maintainability.InternalTypeMethodCodeFixProvider();
        var first = solution.GetDocument(ids[0])!;
        var diagnostics = await GetDiagnosticsAsync(first, analyzer);

        // From before the first fix asks: BRO1409's single fix already reads the names (of the same solution).
        var scans = new System.Runtime.CompilerServices.StrongBox<int>();
        StyleBro.CodeFixes.Naming.CamelCaseRenamer.NameScans.Value = scans;
        Microsoft.CodeAnalysis.CodeActions.CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(first, diagnostics.First(d => d.Id == id), (a, _) => action ??= a, CancellationToken.None));

        var context = new Microsoft.CodeAnalysis.CodeFixes.FixAllContext(
            first, fixer, Microsoft.CodeAnalysis.CodeFixes.FixAllScope.Solution, action!.EquivalenceKey, new[] { id }, new Provider(analyzer), CancellationToken.None);
        var operations = await (await fixer.GetFixAllProvider()!.GetFixAsync(context))!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation>().Single().ChangedSolution;
        StyleBro.CodeFixes.Naming.CamelCaseRenamer.NameScans.Value = null;

        // Every file was fixed and sorted (the sort is what asks for the names).
        foreach (var document in ids)
        {
            var text = (await fixedSolution.GetDocument(document)!.GetTextAsync()).ToString();
            var sorted = id == "BRO1112"
                ? text.IndexOf("private int x", StringComparison.Ordinal) < text.IndexOf("void M", StringComparison.Ordinal) && !text.Contains("#region", StringComparison.Ordinal)
                : text.IndexOf("public int P", StringComparison.Ordinal) < text.IndexOf("internal void B", StringComparison.Ordinal);
            Assert.True(sorted, text);
        }

        Assert.Equal(1, scans.Value);
    }

    private static async Task<System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>> GetDiagnosticsAsync(
        Microsoft.CodeAnalysis.Document document, Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer analyzer)
    {
        var compilation = await document.Project.GetCompilationAsync();
        var diagnostics = await compilation!.WithAnalyzers(System.Collections.Immutable.ImmutableArray.Create(analyzer)).GetAnalyzerDiagnosticsAsync();
        var tree = await document.GetSyntaxTreeAsync();
        return diagnostics.Where(d => d.Location.SourceTree == tree).ToImmutableArray();
    }

    private static string Apply(params TextChange[] changes)
    {
        return SourceText.From(Original).WithChanges(LinkedFileFixAllProvider.Merge(changes.ToList())).ToString();
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
}
