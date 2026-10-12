using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Naming;
using StyleBro.CodeFixes.Naming;
using StyleBro.CodeFixes.Ordering;
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

    [Theory]
    [InlineData("typeof(C).GetField(\"_blockedUntil\", 0)")]
    [InlineData("c.GetType().GetRuntimeField(\"_blockedUntil\")")]
    [InlineData("Expression.Field(null, typeof(C), \"_blockedUntil\")")]
    [InlineData("AccessTools.FieldRefAccess<C, int>(\"_blockedUntil\")")]
    public async Task AFieldNamedInAReflectionCall_IsKept(string read)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "class C\n{\n    private int _blockedUntil;\n\n    public int Get() => _blockedUntil;\n}\n"),
            ("Tests.cs", $"class Tests\n{{\n    object Read(C c) => {read};\n}}\n"));

        Assert.Equal(KeptReason.NameInString, reason);
    }

    [Theory]
    [InlineData("Assert.Equal(\"_blockedUntil\", c)")] // Orleans: words in messages ('options') kept 692 renames
    [InlineData("new System.ArgumentNullException(\"_blockedUntil\")")]
    [InlineData("Settings[\"_blockedUntil\"]")] // Flow.Launcher: 'type', a JSON key in another plugin
    [InlineData("System.Text.Json.JsonDocument.Parse(\"{}\").RootElement.GetProperty(\"_blockedUntil\")")] // Orleans: JSON, not a Type
    [InlineData("DeleteMembershipEntries(\"_blockedUntil\")")] // 'Member' inside 'Membership' isn't a word
    [InlineData("FieldsetName(\"_blockedUntil\")")] // nor 'Field' inside 'Fieldset'
    public async Task AFieldWhoseNameIsAnOrdinaryStringElsewhere_IsRenamed(string use)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "class C\n{\n    private int _blockedUntil;\n\n    public int Get() => _blockedUntil;\n}\n"),
            ("Tests.cs", $"class Tests\n{{\n    object Use(C c) => {use};\n}}\n"));

        Assert.Null(reason);
    }

    // sourcegit: a public field of a private nested class is private in effect, so a word in another project's strings
    // doesn't keep it (only a reflection-shaped string would); an internal type's field is data a serializer writes.
    [Theory]
    [InlineData("private", false)]
    [InlineData("internal", true)]
    public async Task APublicFieldOfAPrivateType_IsGuardedLikeAPrivateField(string access, bool kept)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Engine.cs", $"public class Engine {{ {access} class Variable {{ public string name = string.Empty; }} }}"),
            ("Xshd.cs", "class Xshd { string Write() => \"<Color name='x' />\"; }"));

        Assert.Equal(kept ? KeptReason.NameInString : null, reason);
    }

    [Fact]
    public void RepositoryNames_ListReflectionShapedStrings()
    {
        var root = CSharpSyntaxTree.ParseText("""
            class T
            {
                [MemberData("Cases")]
                [InlineData("inline")]
                [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "count")]
                void M(object o)
                {
                    o.GetType().GetProperty("Total");
                    typeof(T)?.GetMethod("Run");
                    Type.GetType("App.Shape");
                    Assert.Equal("options", o);
                    GetPrivateField(o, "_secret");
                }
            }
            """).GetRoot();

        var lines = RepositoryNames.Lines(root, "T.cs").Where(l => l.StartsWith("R", StringComparison.Ordinal)).ToList();

        Assert.Equal(new[] { "R\tT.cs(3)\tCases", "R\tT.cs(5)\tcount", "R\tT.cs(8)\tTotal", "R\tT.cs(9)\tRun", "R\tT.cs(10)\tApp.Shape", "R\tT.cs(12)\t_secret" }, lines);
    }

    [Fact]
    public void RepositoryNames_WriteAndReadALoneSurrogate()
    {
        // Orleans: '"jobs/\uD800"' in a test; File.WriteAllLines threw EncoderFallbackException before any formatting.
        var root = CSharpSyntaxTree.ParseText("class T { string S => \"jobs/\\uD800\"; string P => \"\\uD83D\\uDE00\\\\u0041\"; }").GetRoot();
        var file = Path.Combine(Path.GetTempPath(), $"stylebro-names-test-{Guid.NewGuid():N}.tsv");
        try
        {
            File.WriteAllLines(file, RepositoryNames.Lines(root, "T.cs"));
            var texts = File.ReadAllLines(file).Select(RepositoryNames.Parse).Where(e => e!.Kind == RepositoryNames.StringKind).Select(e => e!.Text).ToList();

            Assert.Equal(new[] { "jobs/\uD800", "😀\\u0041" }, texts);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void KeptFinding_WritesALoneSurrogateAsAReplacementCharacter()
    {
        var file = Path.Combine(Path.GetTempPath(), $"stylebro-kept-test-{Guid.NewGuid():N}.tsv");
        try
        {
            File.AppendAllText(file, new KeptFinding("C.cs", 1, 1, "BRO1106", KeptReason.NameInString, "Use 'jobs/\uD800'", "T.cs(1)") + "\n");

            Assert.Equal("Use 'jobs/�'", KeptFinding.Parse(File.ReadAllLines(file).Single())!.Message);
        }
        finally
        {
            File.Delete(file);
        }
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
            ("Tests.cs", "class Tests\n{\n    object Field => typeof(C).GetField(\"_blockedUntil\");\n}\n"));
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
            WriteKept(Path.Combine(root, "C.cs"));
            return 0;
        });

        Assert.Equal(FormatCommand.NotCleanExitCode, code);
        Assert.Equal(FormatCommand.MaxRuns, run);
        Assert.Equal(new[] { 0, 11, 11 }, restores); // only the first run restores; the later ones check first, once
        Assert.Contains(lines, l => l.StartsWith("Not clean: still changing", StringComparison.Ordinal));

        // The kept findings are listed too (trial 8: with TreatWarningsAsErrors they fail the build either way).
        Assert.Contains("  C.cs(3,17): BRO1303 Rename '_count' to 'count'. Kept: " + KeptFinding.Describe(KeptReason.NameInString) + ".", lines);
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
        var tests = "class ZqxCounterTests\n{\n    object zqxField => typeof(Counter).GetField(\"_zqxCount\");\n}\n"; // only zqx names: other tests run alongside

        Assert.Null(await GetReasonAsync(DiagnosticIds.PrivateFieldNaming, files));
        Assert.Equal(KeptReason.NameInString, await WithRepositoryNamesAsync(("Tests/CounterTests.cs", tests), () => GetReasonAsync(DiagnosticIds.PrivateFieldNaming, files)));
    }

    // Names serializers write (public fields, properties, types) still count any string, also inside one: test JSON.
    [Fact]
    public async Task ADataFieldInAPlainStringInAProjectThisRunDoesntLoad_IsKept()
    {
        var files = new[] { ("Order.cs", "internal class ZqxOrder\n{\n    public int zqx_total;\n}\n") };
        var tests = """
            class ZqxOrderTests
            {
                string zqxJson => "{ \"zqx_total\": 1 }";
            }
            """;

        Assert.Null(await GetReasonAsync(DiagnosticIds.FieldUnderscore, files));
        Assert.Equal(KeptReason.NameInString, await WithRepositoryNamesAsync(("Tests/OrderTests.cs", tests), () => GetReasonAsync(DiagnosticIds.FieldUnderscore, files)));
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
    public async Task ATypeAProjectThisRunDoesntLoadReadsByPosition_KeepsItsOrder()
    {
        // A new solution for each call: the names are collected once per solution.
        static async Task<string?> ReadAsync()
        {
            var project = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(project);
            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(project, "App", "App", LanguageNames.CSharp)
                .AddMetadataReference(project, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddDocument(documentId, "ZqxEvent.cs", SourceText.From("class ZqxEvent\n{\n    private int zqxA;\n    public int ZqxB;\n}\n"), filePath: Path.Combine(Path.GetTempPath(), "ZqxEvent.cs"));
            var document = solution.GetDocument(documentId)!;
            var type = (await document.GetSyntaxRootAsync())!.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>().Single();
            return await MemberOrderingCodeFixProvider.GetReadByPositionAsync(document, type, null, CancellationToken.None);
        }

        var tests = ("Tests/ZqxEventTests.cs", "class ZqxEventTests\n{\n    object zqxRead(ZqxEvent e) => e.GetType().GetFields()[0];\n}\n");

        Assert.Null(await ReadAsync());
        Assert.EndsWith("ZqxEventTests.cs(3)", await WithRepositoryNamesAsync(tests, ReadAsync));
    }

    [Fact]
    public void RepositoryNames_ListTheTypesReadByPosition()
    {
        var root = CSharpSyntaxTree.ParseText("""
            class T
            {
                Event subject;

                void M()
                {
                    var fields = subject.GetType().GetFields();
                    var a = fields[0];
                    var b = typeof(Other).GetProperties().ElementAt(1);
                    var c = new Third().GetType().GetMembers().First();
                    var d = typeof(NotByPosition).GetFields().Length;
                    var e = this.GetType().GetEvents()[0];
                }
            }
            """).GetRoot();

        var lines = RepositoryNames.Lines(root, "T.cs").Where(l => l.StartsWith("P", StringComparison.Ordinal)).ToList();

        Assert.Equal(new[] { "P\tT.cs(7)\tEvent GetFields", "P\tT.cs(9)\tOther GetProperties", "P\tT.cs(10)\tThird GetMembers", "P\tT.cs(12)\tT GetEvents" }, lines);
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

    // The trial: '--files' loads only the files' project, so it renamed a field the whole-repository run kept for a test
    // project's GetField. The run gets the repository's names, as a per-framework run does.
    [Fact]
    public void Format_Files_GivesTheRunTheNamesOfTheWholeRepository()
    {
        var root = NewFolder();
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(Path.Combine(root, "src", "A"));
        Directory.CreateDirectory(Path.Combine(root, "tests", "T"));
        File.WriteAllText(Path.Combine(root, "src", "A", "A.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(root, "src", "A", "X.cs"), "class X\n{\n    private int _zqxCount;\n}\n");
        File.WriteAllText(Path.Combine(root, "tests", "T", "T.cs"), "class T\n{\n    object F => typeof(X).GetField(\"_zqxCount\");\n}\n");
        var entries = new List<RepositoryNames.Entry>();

        var code = FormatCommand.Run(new[] { "--files", Path.Combine(root, "src", "A", "X.cs") }, _ => { }, (_, _) =>
        {
            entries.AddRange(File.ReadAllLines(Environment.GetEnvironmentVariable(RepositoryNames.Variable)!).Select(RepositoryNames.Parse).OfType<RepositoryNames.Entry>());
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Contains(entries, e => e.Kind == RepositoryNames.ReflectionKind && e.Text == "_zqxCount" && e.Where.EndsWith("T.cs(3)", StringComparison.Ordinal));
        Assert.Null(Environment.GetEnvironmentVariable(RepositoryNames.Variable));
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
