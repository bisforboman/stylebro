using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;
using StyleBro.CodeFixes.Naming;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>
/// Findings a fix keeps on purpose: why (the reason the renamer's own checks give), that 'stylebro-migrate format' lists
/// them, and that they don't make it fail. The tests that set <see cref="KeptFinding.Variable"/> live in this class, so
/// they don't run in parallel with each other.
/// </summary>
public class KeptFindingsTests
{
    [Fact]
    public async Task AFieldReadByReflection_IsKept_TheNameIsInAString()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "class C\n{\n    private int _blockedUntil;\n\n    public int Get() => _blockedUntil;\n}\n"),
            ("Tests.cs", "class Tests\n{\n    object Read(C c) => typeof(C).GetField(\"_blockedUntil\", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(c);\n}\n"));

        Assert.Equal(KeptReason.NameInString, reason);
    }

    [Fact]
    public async Task AFieldInNameofElsewhere_IsKept_TheNameIsInANameof()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldUnderscore,
            ("C.cs", "class C\n{\n    internal static int Max_Count = 1;\n}\n"),
            ("D.cs", "class D\n{\n    string Name => nameof(C.Max_Count);\n}\n"));

        Assert.Equal(KeptReason.NameInNameof, reason);
    }

    [Fact]
    public async Task AFieldADerivedMemberWouldHide_IsKept_TheNewNameMeansSomethingElse()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Base.cs", "internal class Base\n{\n    internal static readonly int limit = 1;\n}\n"),
            ("Derived.cs", "internal class Derived : Base\n{\n    public int Limit => 2;\n\n    public int Get() => limit;\n}\n"));

        Assert.Equal(KeptReason.NewNameMeansSomethingElse, reason);
    }

    [Fact]
    public async Task AFieldGeneratedCodeUses_IsKept_GeneratedReference()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Dto.cs", "internal class Dto\n{\n    internal int total;\n}\n"),
            ("Page.g.cs", "internal class Page\n{\n    public int Read(Dto dto) => dto.total;\n}\n"));

        Assert.Equal(KeptReason.GeneratedReference, reason);
    }

    [Fact]
    public async Task AFieldWithNothingInTheWay_IsRenamed_NoReason()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "class C\n{\n    private int _count;\n\n    public int Get() => _count;\n}\n"));

        Assert.Null(reason);
    }

    [Fact]
    public async Task FixAll_WritesTheKeptFindings_WhenTheVariableIsSet()
    {
        var (solution, document, diagnostic) = await FindAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "class C\n{\n    private int _blockedUntil;\n\n    public int Get() => _blockedUntil;\n}\n"),
            ("Tests.cs", "class Tests\n{\n    string Name => \"_blockedUntil\";\n}\n"));
        var file = Path.Combine(Path.GetTempPath(), $"stylebro-kept-test-{Guid.NewGuid():N}.tsv");
        Environment.SetEnvironmentVariable(KeptFinding.Variable, file);
        try
        {
            var renamed = await CamelCaseRenamer.RenameAsync(solution, new[] { (document, diagnostic) }, CancellationToken.None);

            Assert.Equal((await document.GetTextAsync()).ToString(), (await renamed.GetDocument(document.Id)!.GetTextAsync()).ToString());
            var kept = File.ReadAllLines(file).Select(KeptFinding.Parse).OfType<KeptFinding>().Single(k => k.Path.EndsWith("C.cs", StringComparison.Ordinal));
            Assert.Equal((3, 17, "BRO1303", KeptReason.NameInString), (kept.Line, kept.Column, kept.Id, kept.Reason));
            Assert.Equal("Rename '_blockedUntil' to 'blockedUntil'", kept.Message);

            // Where the string is, so a person can check it (the trial: "the name is in a string", but not which).
            Assert.EndsWith("Tests.cs(3)", kept.Where);
        }
        finally
        {
            Environment.SetEnvironmentVariable(KeptFinding.Variable, null);
            File.Delete(file);
        }
    }

    [Fact]
    public void KeptFinding_RoundTrips()
    {
        var finding = new KeptFinding(Path.Combine("src", "C.cs"), 12, 5, "BRO1303", KeptReason.NameInNameof, "Rename '_a' to 'a'");

        var parsed = KeptFinding.Parse(finding.ToString())!;

        Assert.Equal((finding.Path, 12, 5, "BRO1303", KeptReason.NameInNameof, "Rename '_a' to 'a'"), (parsed.Path, parsed.Line, parsed.Column, parsed.Id, parsed.Reason, parsed.Message));
        Assert.Null(KeptFinding.Parse("not a finding"));

        var withWhere = KeptFinding.Parse(new KeptFinding("C.cs", 1, 1, "BRO1303", KeptReason.NameInString, "m", Path.Combine("tests", "T.cs") + "(9)").ToString())!;
        Assert.Equal(Path.Combine("tests", "T.cs") + "(9)", withWhere.Where);
    }

    [Fact]
    public void KeptSummary_SaysWhereTheString_Is()
    {
        var root = NewFolder();
        var finding = new KeptFinding(Path.Combine(root, "C.cs"), 3, 17, "BRO1303", KeptReason.NameInString, "Rename '_count' to 'count'", Path.Combine(root, "tests", "T.cs") + "(12)");

        Assert.Contains("  C.cs(3,17): BRO1303 Rename '_count' to 'count'. Kept: " + KeptFinding.Describe(KeptReason.NameInString) + " (first: tests/T.cs(12)).", FormatCommand.KeptSummary(new[] { finding }, root));
    }

    [Fact]
    public void Verify_SomethingFixableLeft_MarksTheKeptFindings()
    {
        var root = NewFolder();
        var kept = $"{Path.Combine(root, "C.cs")}(3,17): warning BRO1303: Rename '_count' to 'count' [{Path.Combine(root, "App.csproj")}]";
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke(kept);
                output?.Invoke("C.cs(1,1): warning BRO1505: Add a blank line");
                return 2;
            }

            WriteKept(Path.Combine(args[0], "C.cs"));
            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C\n{\n}\n");
            return 0;
        });

        Assert.Equal(2, code);
        Assert.Contains(kept.Replace("warning BRO1303", "kept BRO1303", StringComparison.Ordinal), lines);
        Assert.DoesNotContain(kept, lines);
        Assert.Contains("C.cs(1,1): warning BRO1505: Add a blank line", lines);
        Assert.Contains(lines, l => l.StartsWith("1 of these is kept on purpose (marked 'kept')", StringComparison.Ordinal));
    }

    [Fact]
    public void Format_OnlyKeptFindingsLeft_IsClean_AndListsThem()
    {
        var root = NewFolder();
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root }, lines.Add, (args, _) =>
        {
            WriteKept(Path.Combine(root, "C.cs"));
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Contains("Run 1: 0 files changed, clean.", lines);
        Assert.Contains(lines, l => l.StartsWith("Clean: ", StringComparison.Ordinal) && l.Contains("kept on purpose (listed below)", StringComparison.Ordinal));
        Assert.Contains("  C.cs(3,17): BRO1303 Rename '_count' to 'count'. Kept: " + KeptFinding.Describe(KeptReason.NameInString) + ".", lines);
    }

    [Fact]
    public void Format_StillChangingAfterTheLastRun_IsNotClean()
    {
        var root = NewFolder();
        var lines = new List<string>();
        var run = 0;
        var restores = new List<int>();

        var code = FormatCommand.Run(new[] { root }, lines.Add, (args, _) =>
        {
            restores.Add(args.Count(a => a == "--no-restore") + (10 * args.Count(a => a == FormatCommand.CheckFirstOption)));
            File.WriteAllText(Path.Combine(root, "C.cs"), new string('x', ++run));
            return 0;
        });

        Assert.Equal(FormatCommand.NotCleanExitCode, code);
        Assert.Equal(FormatCommand.MaxRuns, run);
        Assert.Equal(new[] { 0, 11, 11 }, restores); // only the first run restores; the later ones check first, once
        Assert.Contains(lines, l => l.StartsWith("Not clean: still changing", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_OnlyKeptFindingsLeft_ExitsZero_AndHidesTheirWarnings()
    {
        var root = NewFolder();
        var warning = $"{Path.Combine(root, "C.cs")}(3,17): warning BRO1303: Rename '_count' to 'count' [{Path.Combine(root, "App.csproj")}]";
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke(warning);
                return 2;
            }

            // The run on the copy: writes nothing, keeps the finding.
            WriteKept(Path.Combine(args[0], "C.cs"));
            return 0;
        });

        Assert.Equal(0, code);
        Assert.DoesNotContain(warning, lines);
        Assert.Contains("  C.cs(3,17): BRO1303 Rename '_count' to 'count'. Kept: " + KeptFinding.Describe(KeptReason.NameInString) + ".", lines);
        Assert.Equal("class C { }\n", File.ReadAllText(Path.Combine(root, "C.cs")));
    }

    [Fact]
    public void Verify_SomethingFixableLeft_FailsWithFormatsCode_AndTouchesNothing()
    {
        var root = NewFolder();
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke("C.cs(1,1): warning BRO1505: Add a blank line");
                return 2;
            }

            File.WriteAllText(Path.Combine(args[0], "C.cs"), "class C\n{\n}\n");
            return 0;
        });

        Assert.Equal(2, code);
        Assert.Contains("C.cs(1,1): warning BRO1505: Add a blank line", lines);
        Assert.Contains("Not clean: formatting would change 1 file. Run 'stylebro-migrate format'.", lines);
        Assert.Equal("class C { }\n", File.ReadAllText(Path.Combine(root, "C.cs")));
    }

    [Fact]
    public async Task AFieldReadByReflectionInAProjectThisRunDoesntLoad_IsKept_LikeScrutor()
    {
        // Scrutor: the netstandard2.0 run loads only the library; its tests read the field with GetField.
        var files = new[] { ("Counter.cs", "class Counter\n{\n    private int _zqxCount;\n\n    public int Get() => _zqxCount;\n}\n") };
        var tests = "class ZqxCounterTests\n{\n    string zqxName => \"_zqxCount\";\n}\n"; // as in GetField(name); only zqx names: other tests run alongside

        Assert.Null(await GetReasonAsync(DiagnosticIds.PrivateFieldNaming, files));
        Assert.Equal(KeptReason.NameInString, await WithRepositoryNamesAsync(("Tests/CounterTests.cs", tests), () => GetReasonAsync(DiagnosticIds.PrivateFieldNaming, files)));
    }

    [Fact]
    public async Task AFieldAProjectThisRunDoesntLoadUses_IsKept_UsedInProjectNotLoaded()
    {
        var files = new[] { ("Dto.cs", "internal class ZqxDto\n{\n    internal int zqxTotal;\n}\n") };
        var tests = "class ZqxDtoTests\n{\n    int zqxRead(ZqxDto zqxD) => zqxD.zqxTotal;\n}\n";

        Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync(("Tests/DtoTests.cs", tests), () => GetReasonAsync(DiagnosticIds.FieldPascalCase, files)));

        // Its own files count as loaded; a private field can't be used elsewhere (only reached by a string).
        Assert.Null(await WithRepositoryNamesAsync(("Dto.cs", tests), () => GetReasonAsync(DiagnosticIds.FieldPascalCase, files)));
        var privateField = new[] { ("C.cs", "class C\n{\n    private int _zqxOther;\n\n    public int Get() => _zqxOther;\n}\n") };
        Assert.Null(await WithRepositoryNamesAsync(("Tests/T.cs", "class ZqxT { int _zqxOther; }\n"), () => GetReasonAsync(DiagnosticIds.PrivateFieldNaming, privateField)));
    }

    [Fact]
    public async Task ANamespaceOrTupleElementAProjectThisRunDoesntLoadUses_IsKept()
    {
        var (solution, _, _) = await FindAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "namespace ZqxApp.zqxData\n{\n    class C\n    {\n        private int _zqxN;\n\n        public (int zqxFirst, int zqxSecond) Get() => (1, 0);\n\n        public int GetN() => _zqxN;\n    }\n}\n"));
        var tests = ("Tests/T.cs", "namespace ZqxTests\n{\n    using ZqxApp.zqxData;\n\n    class ZqxT\n    {\n        int zqxRead(C zqxC) => zqxC.Get().zqxFirst;\n    }\n}\n");

        Assert.Null((await NamespaceRenamer.GetChangesAsync(solution, "ZqxApp.zqxData", "ZqxData", CancellationToken.None)).Reason);
        Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync(tests, async () => (await NamespaceRenamer.GetChangesAsync(solution, "ZqxApp.zqxData", "ZqxData", CancellationToken.None)).Reason));
        Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync(tests, () => TupleElementRenamer.GetKeptReasonAsync(solution, "zqxFirst", "ZqxFirst", CancellationToken.None)));
    }

    [Fact]
    public void ReadKept_PrefersTheReasonOfARunThatLoadsEveryUse()
    {
        var root = NewFolder();
        var file = Path.Combine(root, "kept.tsv");
        var path = Path.Combine(root, "C.cs");
        File.WriteAllLines(file, new[]
        {
            new KeptFinding(path, 3, 17, "BRO1303", KeptReason.UsedInProjectNotLoaded, "Rename").ToString(),
            new KeptFinding(path, 3, 17, "BRO1303", KeptReason.NameInString, "Rename").ToString(),
        });

        Assert.Equal(KeptReason.NameInString, Assert.Single(FormatCommand.ReadKept(file, root, root)).Reason);
    }

    [Fact]
    public void Format_Once_PrintsTheRunAndTheKeptFindings()
    {
        var root = NewFolder();
        var lines = new List<string>();
        var runs = 0;

        var code = FormatCommand.Run(new[] { root, FormatCommand.OnceOption }, lines.Add, (args, _) =>
        {
            runs++;
            Assert.DoesNotContain(FormatCommand.OnceOption, args);
            File.WriteAllText(Path.Combine(root, "C.cs"), "class Changed { }");
            WriteKept(Path.Combine(root, "C.cs"));
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Equal(1, runs); // one run, though it changed a file
        Assert.Contains("Run 1: 1 file changed:", lines);
        Assert.Contains(lines, l => l.StartsWith("1 finding kept on purpose", StringComparison.Ordinal)); // not "Clean:": the run changed files
        Assert.Contains("  C.cs(3,17): BRO1303 Rename '_count' to 'count'. Kept: " + KeptFinding.Describe(KeptReason.NameInString) + ".", lines);
    }

    /// <summary>Runs <paramref name="action"/> with a repository-names file of one more file, as 'stylebro-migrate format' writes it.</summary>
    private static async Task<T> WithRepositoryNamesAsync<T>((string Path, string Text) other, Func<Task<T>> action)
    {
        var file = Path.Combine(Path.GetTempPath(), $"stylebro-names-test-{Guid.NewGuid():N}.tsv");
        var tree = CSharpSyntaxTree.ParseText(other.Text);
        File.WriteAllLines(file, RepositoryNames.Lines(tree.GetRoot(), Path.Combine(Path.GetTempPath(), other.Path)));
        Environment.SetEnvironmentVariable(RepositoryNames.Variable, file);
        try
        {
            return await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(RepositoryNames.Variable, null);
            File.Delete(file);
        }
    }

    private static string NewFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "stylebro-kept-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "C.cs"), "class C { }\n");
        return root;
    }

    /// <summary>What the fix writes for a kept rename, to the file 'stylebro-migrate format' named.</summary>
    private static void WriteKept(string path) =>
        File.AppendAllText(
            Environment.GetEnvironmentVariable(KeptFinding.Variable)!,
            new KeptFinding(path, 3, 17, "BRO1303", KeptReason.NameInString, "Rename '_count' to 'count'") + "\n");

    private static async Task<KeptReason?> GetReasonAsync(string id, params (string Path, string Text)[] files)
    {
        var (solution, document, diagnostic) = await FindAsync(id, files);
        return await CamelCaseRenamer.GetKeptReasonAsync(solution, document, diagnostic, CancellationToken.None);
    }

    /// <summary>A one-project solution of the files, and the first diagnostic of <paramref name="id"/> in the first file.</summary>
    private static async Task<(Solution Solution, Document Document, Diagnostic Diagnostic)> FindAsync(string id, params (string Path, string Text)[] files)
    {
        var workspace = new AdhocWorkspace();
        var project = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(project, "App", "App", LanguageNames.CSharp)
            .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .AddMetadataReference(project, MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var ids = new List<DocumentId>();
        foreach (var (path, text) in files)
        {
            var document = DocumentId.CreateNewId(project);
            solution = solution.AddDocument(document, path, SourceText.From(text), filePath: Path.Combine(Path.GetTempPath(), path));
            ids.Add(document);
        }

        var first = solution.GetDocument(ids[0])!;
        var compilation = (await first.Project.GetCompilationAsync())!;
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FieldNamingAnalyzer());
        var tree = await first.GetSyntaxTreeAsync();
        var diagnostic = (await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync()).First(d => d.Id == id && d.Location.SourceTree == tree);
        return (solution, first, diagnostic);
    }
}
