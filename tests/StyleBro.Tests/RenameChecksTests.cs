using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Naming;
using StyleBro.CodeFixes.Naming;

namespace StyleBro.Tests;

/// <summary>
/// The fix-loop measurement (2026-10): 'dotnet format' registers the fix of one diagnostic only to read the action's
/// equivalence key, and the renames' registration ran the whole rename-safety check, which Fix All then ran again on the
/// same solution (Shouldly: BRO1311 8.3 s in the registration, 4.1 s in Fix All). Each check is made once per solution.
/// </summary>
public class RenameChecksTests
{
    [Theory]
    [InlineData("BRO1303", "public class C\n{\n    private int _first;\n    private int _second;\n\n    public int Sum() => _first + _second;\n}\n", "first + second")]
    [InlineData("BRO1311", "public class C\n{\n    public int Sum()\n    {\n        (int first, int second) pair = (1, 2);\n        return pair.first + pair.second;\n    }\n}\n", "pair.First + pair.Second")]
    public async Task RegistrationThenFixAll_ChecksEachRenameOnce(string id, string source, string renamed)
    {
        var project = ProjectId.CreateNewId();
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { [id] = ReportDiagnostic.Warn });
        var document = DocumentId.CreateNewId(project);
        var solution = new AdhocWorkspace().CurrentSolution
            .AddProject(project, "p", "p", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, options)
            .AddMetadataReference(project, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(project, MetadataReference.CreateFromFile(typeof((int, int)).Assembly.Location))
            .AddDocument(document, "C.cs", SourceText.From(source), filePath: Path.Combine(Path.GetTempPath(), "RenameChecks", "C.cs"));
        DiagnosticAnalyzer analyzer = id == "BRO1303" ? new FieldNamingAnalyzer() : new TupleElementNamingAnalyzer();
        CodeFixProvider fixer = id == "BRO1303" ? new CamelCaseNamingCodeFixProvider() : new TupleElementNamingCodeFixProvider();
        var first = solution.GetDocument(document)!;
        var diagnostics = await GetDiagnosticsAsync(first, analyzer);
        Assert.Equal(2, diagnostics.Count(d => d.Id == id));

        var made = new System.Runtime.CompilerServices.StrongBox<int>();
        RenameChecks.Made.Value = made;
        CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new CodeFixContext(first, diagnostics.First(d => d.Id == id), (a, _) => action ??= a, CancellationToken.None));
        Assert.NotNull(action);
        var afterRegistration = made.Value;

        var context = new FixAllContext(first, fixer, FixAllScope.Solution, action!.EquivalenceKey, new[] { id }, new Provider(analyzer), CancellationToken.None);
        var operations = await (await fixer.GetFixAllProvider()!.GetFixAsync(context))!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        RenameChecks.Made.Value = null;

        Assert.Contains(renamed, (await fixedSolution.GetDocument(document)!.GetTextAsync()).ToString(), StringComparison.Ordinal);

        // One check per rename: the registration's ones are reused by Fix All, which adds only the other names'.
        Assert.True(afterRegistration >= 1);
        Assert.Equal(2, made.Value);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, DiagnosticAnalyzer analyzer)
    {
        var compilation = await document.Project.GetCompilationAsync();
        var diagnostics = await compilation!.WithAnalyzers(ImmutableArray.Create(analyzer)).GetAnalyzerDiagnosticsAsync();
        var tree = await document.GetSyntaxTreeAsync();
        return diagnostics.Where(d => d.Location.SourceTree == tree).ToImmutableArray();
    }

    private sealed class Provider : FixAllContext.DiagnosticProvider
    {
        private readonly DiagnosticAnalyzer analyzer;

        public Provider(DiagnosticAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) =>
            await GetDiagnosticsAsync(document, analyzer);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());
    }
}
