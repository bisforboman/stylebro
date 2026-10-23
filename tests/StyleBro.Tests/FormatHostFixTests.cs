using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Host;

namespace StyleBro.Tests;

/// <summary>
/// The host's fix runs (--engine host, docs/proposals/format-speed.md, phase 2): the order of the ids, the fixers it calls, the
/// analyses it reuses between ids and how it keeps a loaded workspace as the files on disk are.
/// </summary>
public sealed class FormatHostFixTests : IDisposable
{
    private readonly string folder = Directory.CreateTempSubdirectory("stylebro-host-fix-").FullName;

    [Fact]
    public void TheIds_AreFixedInOrdinalOrder()
    {
        Assert.Equal(["BRO1001", "BRO1519", "CA1822", "IDE0011", "xUnit2013"], FixOrder.Of(["xUnit2013", "IDE0011", "BRO1519", "CA1822", "BRO1001", "BRO1519"]));
    }

    [Fact]
    public void EachFixerType_IsCalledOncePerId()
    {
        // dotnet format collects one instance per project: 17 calls of the same Fix All on Newtonsoft.Json.
        CodeFixProvider[] fixers = [new Fixer("X1", "X2"), new Fixer("X1"), new OtherFixer("X1"), new Fixer("X3"), new OtherFixer("RemoveUnnecessaryImportsFixable")];

        Assert.Equal([typeof(Fixer), typeof(OtherFixer)], FixOrder.FixersFor("X1", fixers).Select(f => f.GetType()));
        Assert.Same(fixers[0], FixOrder.FixersFor("X1", fixers)[0]);
        Assert.Single(FixOrder.FixersFor("X3", fixers));
        Assert.Empty(FixOrder.FixersFor("X4", fixers));
        Assert.IsType<OtherFixer>(Assert.Single(FixOrder.FixersFor("IDE0005", fixers)));
    }

    [Fact]
    public async Task TheCache_RunsAnAnalyzer_OnlyOnACompilationItHasntSeen()
    {
        using var workspace = new AdhocWorkspace();
        var lib = ProjectId.CreateNewId();
        var app = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(lib, VersionStamp.Default, "Lib", "Lib", LanguageNames.CSharp))
            .AddProject(ProjectInfo.Create(app, VersionStamp.Default, "App", "App", LanguageNames.CSharp, projectReferences: [new ProjectReference(lib)]))
            .AddDocument(DocumentId.CreateNewId(lib), "A.cs", "class A { }", filePath: "/r/Lib/A.cs")
            .AddDocument(DocumentId.CreateNewId(lib), "B.cs", "class B { }", filePath: "/r/Lib/B.cs")
            .AddDocument(DocumentId.CreateNewId(app), "C.cs", "class C { }", filePath: "/r/App/C.cs")
            .AddDocument(DocumentId.CreateNewId(app), "D.cs", "class D { }", filePath: "/r/App/D.cs");
        var cache = new AnalysisCache();
        var classes = new ClassAnalyzer("T1");
        var other = new ClassAnalyzer("T2");

        async Task<ImmutableArray<Diagnostic>> Get(Solution s, ProjectId id, params DiagnosticAnalyzer[] analyzers) =>
            await cache.GetAsync((await s.GetProject(id)!.GetCompilationAsync())!, s.GetProject(id)!.AnalyzerOptions, [.. analyzers], null, CancellationToken.None);

        var first = await Get(solution, app, classes);
        Assert.Equal(["C", "D"], first.Select(d => d.GetMessage()));
        Assert.Equal(1, cache.Runs);

        // The same compilation: nothing runs again; another analyzer runs on its own.
        Assert.Equal(first.Select(d => d.GetMessage()), (await Get(solution, app, classes)).Select(d => d.GetMessage()));
        Assert.Equal(1, cache.Runs);
        Assert.Equal(4, (await Get(solution, app, classes, other)).Length);
        Assert.Equal(2, cache.Runs);

        // A fix in App: App runs again, Lib (which App's change can't reach) keeps its results.
        await Get(solution, lib, classes);
        var fixedApp = solution.WithDocumentText(solution.GetProject(app)!.Documents.First().Id, SourceText.From("class C2 { }"));
        Assert.Equal(["C2", "D"], (await Get(fixedApp, app, classes)).Select(d => d.GetMessage()));
        await Get(fixedApp, lib, classes);
        Assert.Equal(4, cache.Runs);

        // A fix in Lib: App references it, so both run again.
        var fixedLib = fixedApp.WithDocumentText(fixedApp.GetProject(lib)!.Documents.First().Id, SourceText.From("class A2 { }"));
        Assert.Equal(["A2", "B"], (await Get(fixedLib, lib, classes)).Select(d => d.GetMessage()));
        await Get(fixedLib, app, classes);
        Assert.Equal(6, cache.Runs);
    }

    [Fact]
    public void Diagnostics_ComeInAFixedOrder()
    {
        var tree1 = CSharpSyntaxTree.ParseText("class A { } class B { }", path: "/r/b.cs");
        var tree2 = CSharpSyntaxTree.ParseText("class C { }", path: "/r/a.cs");
        var descriptor = new DiagnosticDescriptor("T1", "t", "{0}", "c", DiagnosticSeverity.Warning, true);
        Diagnostic At(SyntaxTree tree, int start, string name) => Diagnostic.Create(descriptor, Location.Create(tree, new TextSpan(start, 1)), name);

        Assert.Equal(["C", "A", "B"], AnalysisCache.Ordered([At(tree1, 12, "B"), At(tree2, 0, "C"), At(tree1, 0, "A")]).Select(d => d.GetMessage()));
    }

    [Fact]
    public async Task TheDisk_IsReadBack_IntoEveryCopyOfAChangedFile()
    {
        var path = this.Write("A.cs", "class A { }");
        var other = this.Write("B.cs", "class B { }");
        using var workspace = new AdhocWorkspace();
        var solution = Linked(workspace.CurrentSolution, path, other);
        var disk = new DiskState();
        disk.Remember(solution);
        foreach (var document in solution.Projects.SelectMany(p => p.Documents))
        {
            await document.GetTextAsync(); // read, as the first check does
        }

        // Nothing changed: the same solution.
        var (same, none) = await disk.SyncAsync(solution, CancellationToken.None);
        Assert.Equal(0, none);
        Assert.Same(solution, same);

        File.WriteAllText(path, "class A2 { }");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(1));

        // Written again with the same text (a formatter that changed nothing): read, but not a change.
        File.WriteAllText(other, "class B { }");
        File.SetLastWriteTimeUtc(other, DateTime.UtcNow.AddMinutes(1));
        var (synced, changed) = await disk.SyncAsync(solution, CancellationToken.None);
        Assert.Equal(1, changed);
        foreach (var id in synced.GetDocumentIdsWithFilePath(path))
        {
            Assert.Equal("class A2 { }", (await synced.GetDocument(id)!.GetTextAsync()).ToString());
        }

        // The other file's documents are the ones it had (their projects keep their compilations where nothing else changed).
        foreach (var b in synced.GetDocumentIdsWithFilePath(other))
        {
            Assert.Same(await solution.GetDocument(b)!.GetTextAsync(), await synced.GetDocument(b)!.GetTextAsync());
        }
    }

    [Fact]
    public async Task AFixRun_WritesItsFiles_AsTheWorkspaceSavesThem()
    {
        var path = this.Write("A.cs", "class A { }", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var other = this.Write("B.cs", "class B { }");
        using var workspace = new AdhocWorkspace();
        var current = Linked(workspace.CurrentSolution, path, other);
        var disk = new DiskState();
        disk.Remember(current);

        // A run's view has one copy of A.cs; its fix reaches the other project's copy too.
        var view = current.RemoveProject(current.Projects.Single(p => p.Name == "Lib(net8.0)").Id);
        var a = view.GetDocumentIdsWithFilePath(path).Single();
        var text = await view.GetDocument(a)!.GetTextAsync();
        var formatted = view.WithDocumentText(a, text.WithChanges(new TextChange(new TextSpan(6, 1), "A2")));
        var (saved, written) = await disk.SaveAsync(current, view, formatted, CancellationToken.None);

        Assert.Equal([path], written);
        Assert.Equal([0xEF, 0xBB, 0xBF], File.ReadAllBytes(path).Take(3));
        Assert.Equal("class A2 { }", File.ReadAllText(path));
        foreach (var id in saved.GetDocumentIdsWithFilePath(path))
        {
            Assert.Equal("class A2 { }", (await saved.GetDocument(id)!.GetTextAsync()).ToString());
        }

        // What it wrote isn't read back as a change.
        Assert.Equal(0, (await disk.SyncAsync(saved, CancellationToken.None)).Changed);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(this.folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    // Two framework instances of one project, each with a copy of both files, read from disk as MSBuildWorkspace reads them.
    private static Solution Linked(Solution solution, params string[] paths)
    {
        foreach (var name in new[] { "Lib(net8.0)", "Lib(net10.0)" })
        {
            var id = ProjectId.CreateNewId();
            solution = solution.AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, "Lib", LanguageNames.CSharp, filePath: "/r/Lib/Lib.csproj"));
            foreach (var path in paths)
            {
                solution = solution.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(id), Path.GetFileName(path), loader: new FileTextLoader(path, null), filePath: path));
            }
        }

        return solution;
    }

    private string Write(string name, string text, Encoding? encoding = null)
    {
        var path = Path.Combine(this.folder, name);
        File.WriteAllText(path, text, encoding ?? new UTF8Encoding(false));
        return path;
    }

    private sealed class Fixer(params string[] ids) : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [.. ids];

        public override FixAllProvider? GetFixAllProvider() => null;

        public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
    }

    private sealed class OtherFixer(params string[] ids) : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [.. ids];

        public override FixAllProvider? GetFixAllProvider() => null;

        public override Task RegisterCodeFixesAsync(CodeFixContext context) => Task.CompletedTask;
    }

#pragma warning disable RS1001, RS1017, RS1036 // A test analyzer.
    private sealed class ClassAnalyzer(string id) : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor descriptor = new(id, "t", "{0}", "c", DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [this.descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => c.ReportDiagnostic(Diagnostic.Create(this.descriptor, ((ClassDeclarationSyntax)c.Node).Identifier.GetLocation(), ((ClassDeclarationSyntax)c.Node).Identifier.Text)), SyntaxKind.ClassDeclaration);
        }
    }
#pragma warning restore RS1001, RS1017, RS1036
}
