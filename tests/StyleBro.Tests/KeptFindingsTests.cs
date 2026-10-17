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
public sealed class KeptFindingsTests : IDisposable
{
    private readonly List<string> folders = [];

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

    // Jint (trial 23): a script reads 'host.ByteField' of a private test class, so a public field is data whatever its type's
    // accessibility (this reverses sourcegit's exception for private types); a protected one is guarded like a private one.
    [Theory]
    [InlineData("private", "public", true)]
    [InlineData("internal", "public", true)]
    [InlineData("private", "internal", true)]
    [InlineData("internal", "protected internal", true)]
    public async Task APublicFieldOfAnyType_IsMatchedInsideStrings(string access, string fieldAccess, bool kept)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Engine.cs", $"public class Engine {{ {access} class Host {{ {fieldAccess} byte byteField; }} }}"),
            ("Script.cs", "class Script { string Run() => \"host.byteField\"; }"));

        Assert.Equal(kept ? KeptReason.NameInString : null, reason);
    }

    // Jint (trial 23): '((dynamic)t).MemberKey.Field' binds by name at run time; renaming 'Field' compiled and broke the test.
    [Theory]
    [InlineData("((dynamic)t).Key.byteField")]
    [InlineData("(t as dynamic).byteField")]
    [InlineData("d.byteField", "dynamic d")]
    [InlineData("d.Get().byteField", "dynamic d")]
    [InlineData("d[0].byteField", "dynamic d")]
    public async Task AFieldUsedThroughDynamic_IsKept(string use, string parameter = "object t")
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Host.cs", "public class Engine { private class Host { public byte byteField; } }"),
            ("Use.cs", $"class Use {{ object Read({parameter}) => {use}; }}"));

        Assert.Equal(KeptReason.UsedThroughDynamic, reason);
    }

    [Theory]
    [InlineData("((object)t).ToString()")]
    [InlineData("t.byteField", "Engine.Host t")]
    public async Task AFieldWithoutADynamicUse_IsRenamed(string use, string parameter = "object t")
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Host.cs", "public class Engine { internal class Host { public byte byteField; } }"),
            ("Use.cs", $"class Use {{ object Read({parameter}) => {use}; }}"));

        Assert.Null(reason);
    }

    [Fact]
    public void RepositoryNames_ListDynamicMemberNames()
    {
        var root = CSharpSyntaxTree.ParseText("""
            class T
            {
                dynamic field;
                dynamic Prop => null;
                object M(object o, dynamic p, Other other)
                {
                    dynamic local = o;
                    foreach (dynamic item in new object[0]) { _ = item.Each; }
                    return ((dynamic)o).Cast.Chain + (o as dynamic).As + local.Local + p.Param + field.Field + Prop.Property + o.GetHashCode() + other.Plain;
                }
            }
            """).GetRoot();

        var lines = RepositoryNames.Lines(root, "T.cs").Where(l => l.StartsWith("D", StringComparison.Ordinal)).ToList();

        Assert.Equal(new[] { "Each", "Cast", "Chain", "As", "Local", "Param", "Field", "Property" }, lines.Select(l => l.Split('\t')[2]));
        Assert.Equal("D\tT.cs(8)\tEach", lines[0]);
    }

    // Trial 23 (xunit.v3 + Verify): snapshot files hold what a serializer wrote, so they count like strings (owner's choice
    // 2026-10-15); renaming 'customerName' broke the snapshot test.
    [Theory]
    [InlineData("Tests.Summary.verified.txt", KeptReason.NameInSnapshotFile)]
    [InlineData("Tests.Summary.received.json", KeptReason.NameInSnapshotFile)]
    [InlineData("Approvals.Run.approved.txt", KeptReason.NameInSnapshotFile)]
    [InlineData("notes.txt", null)]
    public async Task APropertyInASnapshotFile_IsKept(string file, KeptReason? expected)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.ElementPascalCase,
            ("Summary.cs", "internal sealed class Summary\n{\n    public string customerName { get; set; } = string.Empty;\n}\n"),
            (file, "{\n  customerName: Ada\n}\n"));

        Assert.Equal(expected, reason);
    }

    // A private field isn't serialized: a word in a snapshot doesn't keep it.
    [Fact]
    public async Task APrivateFieldInASnapshotFile_IsRenamed()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "class C\n{\n    private int _total;\n\n    public int Get() => _total;\n}\n"),
            ("C.Run.verified.txt", "{ _total: 1 }"));

        Assert.Null(reason);
    }

    [Fact]
    public async Task ASnapshotFileOnDisk_IsReadFromTheProjectsRepository()
    {
        // The tests' snapshot sits beside the library, under the same git root; one in obj/ doesn't count.
        Assert.Equal(KeptReason.NameInSnapshotFile, await GetReasonOnDiskAsync(Path.Combine("tests", "SnapshotTests.Summary.verified.txt")));
        Assert.Null(await GetReasonOnDiskAsync(Path.Combine("tests", "obj", "SnapshotTests.Summary.verified.txt")));

        // Binary and huge files aren't read.
        Assert.Null(await GetReasonOnDiskAsync(Path.Combine("tests", "SnapshotTests.Image.verified.png"), "\0totalAmount"));
        Assert.Null(await GetReasonOnDiskAsync(Path.Combine("tests", "SnapshotTests.Big.verified.txt"), new string(' ', 1 << 20) + "totalAmount"));

        static async Task<KeptReason?> GetReasonOnDiskAsync(string snapshot, string text = "{\n  totalAmount: 2.50\n}\n")
        {
            var root = Path.Combine(Path.GetTempPath(), "stylebro-snapshot-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, ".git"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, snapshot))!);
            try
            {
                File.WriteAllText(Path.Combine(root, snapshot), text);
                var (solution, document, diagnostic) = await FindAsync(DiagnosticIds.ElementPascalCase, ("Summary.cs", "internal sealed class Summary\n{\n    public decimal totalAmount { get; set; }\n}\n"));
                solution = solution.WithProjectFilePath(document.Project.Id, Path.Combine(root, "src", "App", "App.csproj"));
                return await CamelCaseRenamer.GetKeptReasonAsync(solution, solution.GetDocument(document.Id)!, diagnostic, CancellationToken.None);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    // Trial 26: BRO1309 renamed a view model's 'title' while MainWindow.xaml bound '{Binding title}': the binding went empty
    // at run time. Identifiers in XAML attribute values count like strings (owner's choice 2026-10-16); element and
    // attribute names and namespace declarations don't.
    [Theory]
    [InlineData("MainWindow.xaml", "<Window Title=\"{Binding title}\" />", KeptReason.NameInXamlFile)]
    [InlineData("Views/Main.axaml", "<ListBox DisplayMemberPath='title' />", KeptReason.NameInXamlFile)]
    [InlineData("Page.xaml", "<TextBlock\n    Text=\"{Binding Path=Model.title, Mode=OneWay}\" />", KeptReason.NameInXamlFile)]
    [InlineData("Page.xaml", "<title xmlns:x=\"clr-namespace:title\" title=\"Title\" />", null)]
    [InlineData("Page.xaml.txt", "<Window Title=\"{Binding title}\" />", null)]
    public async Task APropertyInAXamlFile_IsKept(string file, string xaml, KeptReason? expected)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.ElementPascalCase,
            ("ShellModel.cs", "internal sealed class ShellModel\n{\n    public string title { get; set; } = string.Empty;\n}\n"),
            (file, xaml));

        Assert.Equal(expected, reason);
    }

    // Trial 27: element and attribute names from the project's own namespaces name its types and properties: renaming
    // them broke the build (MC3074, MC3072). The framework's names and geometry values ('Data="M0,0 z"') don't count.
    [Theory]
    [InlineData("<Application xmlns:local=\"clr-namespace:Repro\"><local:boolConverter x:Key=\"BC\" /></Application>", "internal class boolConverter\n{\n}\n", KeptReason.NameInXamlFile)]
    [InlineData("<Window xmlns:local=\"using:Repro\" local:tagBehavior.Key=\"main\" />", "internal static class tagBehavior\n{\n}\n", KeptReason.NameInXamlFile)]
    [InlineData("<Window xmlns:local=\"clr-namespace:Repro\"><local:Badge caption=\"hi\" /></Window>", "internal class Badge\n{\n    public string caption { get; set; } = string.Empty;\n}\n", KeptReason.NameInXamlFile)]
    [InlineData("<Window xmlns:local=\"clr-namespace:Repro\"><local:Badge><local:Badge.caption>hi</local:Badge.caption></local:Badge></Window>", "internal class Badge\n{\n    public string caption { get; set; } = string.Empty;\n}\n", KeptReason.NameInXamlFile)]
    [InlineData("<Window xmlns=\"clr-namespace:Repro\"><Badge caption=\"hi\" /></Window>", "internal class Badge\n{\n    public string caption { get; set; } = string.Empty;\n}\n", KeptReason.NameInXamlFile)]
    [InlineData("<Window xmlns:local=\"clr-namespace:Repro\"><TextBlock caption=\"hi\" /><caption /></Window>", "internal class Badge\n{\n    public string caption { get; set; } = string.Empty;\n}\n", null)]
    [InlineData("<Window><Path Data=\"M0,0 L1,1 caption\" /></Window>", "internal class Badge\n{\n    public string caption { get; set; } = string.Empty;\n}\n", null)]
    public async Task ANameInAXamlFilesElementOrAttributeName_IsKept(string xaml, string code, KeptReason? expected)
    {
        var reason = await GetReasonAsync(DiagnosticIds.ElementPascalCase, ("Code.cs", "namespace Repro;\n\n" + code), ("MainWindow.xaml", xaml));

        Assert.Equal(expected, reason);
    }

    // Trial 27 (PalCalc): the assembly attributes MSBuild writes under obj/ were the evidence for keeping a public field 'c'.
    [Theory]
    [InlineData("obj/Debug/net8.0/App.AssemblyInfo.cs", null)]
    [InlineData("src/App.AssemblyInfo.cs", KeptReason.NameInString)]
    public async Task BuildMetadataUnderObj_HoldsNoNames(string path, KeptReason? expected)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.FieldPascalCase,
            ("Point.cs", "internal sealed class Point\n{\n    public int c;\n}\n"),
            (path, "[assembly: System.Reflection.AssemblyCompanyAttribute(\"c\")]\n"));

        Assert.Equal(expected, reason);
    }

    // An event handler is a method XAML names; a private field isn't reachable from XAML, so a word there doesn't keep it.
    [Fact]
    public async Task AnEventHandlerInAXamlFile_IsKept_APrivateFieldIsNot()
    {
        const string Xaml = "<Button Click=\"onSave\" Content=\"total\" />";
        Assert.Equal(KeptReason.NameInXamlFile, await GetReasonAsync(
            DiagnosticIds.ElementPascalCase,
            ("Window.cs", "internal partial class Window\n{\n    private void onSave(object sender, System.EventArgs e) { }\n}\n"),
            ("Window.xaml", Xaml)));
        Assert.Null(await GetReasonAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("Window.cs", "internal class Window\n{\n    private int _total;\n\n    public int Get() => _total;\n}\n"),
            ("Window.xaml", Xaml.Replace("total", "_total"))));
    }

    [Fact]
    public async Task AXamlFileOnDisk_IsReadFromTheProjectsRepository()
    {
        var root = Path.Combine(Path.GetTempPath(), "stylebro-xaml-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".git"));
        Directory.CreateDirectory(Path.Combine(root, "src", "App.Wpf"));
        try
        {
            File.WriteAllText(Path.Combine(root, "src", "App.Wpf", "MainWindow.xaml"), "<Window>\n    <TextBlock Text=\"{Binding totalAmount}\" />\n</Window>\n");
            var (solution, document, diagnostic) = await FindAsync(DiagnosticIds.ElementPascalCase, ("Summary.cs", "internal sealed class Summary\n{\n    public decimal totalAmount { get; set; }\n}\n"));
            solution = solution.WithProjectFilePath(document.Project.Id, Path.Combine(root, "src", "App", "App.csproj"));
            Assert.Equal(KeptReason.NameInXamlFile, await CamelCaseRenamer.GetKeptReasonAsync(solution, solution.GetDocument(document.Id)!, diagnostic, CancellationToken.None));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Trial 23: a snake_case property ('line_count', a wire name) isn't BRO1309's, so its renamed siblings mixed the DTO's casing.
    [Fact]
    public async Task APropertyNextToASnakeCaseOne_IsKept()
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.ElementPascalCase,
            ("Summary.cs", "internal sealed class Summary\n{\n    public string customerName { get; set; } = string.Empty;\n\n    public int line_count { get; set; }\n}\n"));

        Assert.Equal(KeptReason.OtherPropertyKept, reason);
    }

    // Owner (trial 23): "Leave VB alone". VB is case-insensitive, so the reference check said the new name meant something else.
    [Fact]
    public async Task ATypeVisualBasicUses_IsKept_UsedFromVisualBasic()
    {
        var (solution, document, diagnostic) = await FindAsync(DiagnosticIds.ElementPascalCase, ("Tariff.cs", "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Legacy\")]\n\nnamespace Billing\n{\n    internal static class tariffHelper\n    {\n        public static string Describe() => \"x\";\n    }\n}\n"));
        var vb = ProjectId.CreateNewId();
        solution = solution
            .AddProject(vb, "Legacy", "Legacy", LanguageNames.VisualBasic)
            .AddMetadataReference(vb, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddProjectReference(vb, new ProjectReference(document.Project.Id))
            .AddDocument(DocumentId.CreateNewId(vb), "Invoice.vb", SourceText.From("Public Class Invoice\n    Public Function Total() As String\n        Return Billing.tariffHelper.Describe()\n    End Function\nEnd Class\n"));

        Assert.Equal(KeptReason.UsedFromVisualBasic, await CamelCaseRenamer.GetKeptReasonAsync(solution, solution.GetDocument(document.Id)!, diagnostic, CancellationToken.None));
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

    // Mapperly: its restore runs 'dotnet husky install', which fails outside a git repository, so the copy's run failed and
    // verify said "Not clean" with only kept findings left. The copy of a git repository is one too.
    [Fact]
    public void Verify_TheCopyOfAGitRepository_IsAGitRepository()
    {
        var root = NewFolder();
        PreviewCommand.Git(root, "init", "-q");
        var lines = new List<string>();

        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (args, output) =>
        {
            if (args.Contains("--verify-no-changes"))
            {
                output?.Invoke($"{Path.Combine(root, "C.cs")}(3,17): warning BRO1303: Rename '_count' to 'count' [{Path.Combine(root, "App.csproj")}]");
                return 2;
            }

            WriteKept(Path.Combine(args[0], "C.cs"));
            return Directory.Exists(Path.Combine(args[0], ".git")) ? 0 : 1;
        });

        Assert.Equal(0, code);
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
        Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync(tests, () => TupleElementRenamer.GetKeptReasonAsync(solution, "zqxFirst", "ZqxFirst", "C.cs", CancellationToken.None)));
    }

    // Trial 21: the elements of a tuple type are renamed together; the one that blocks gives its reason, the other says so.
    [Fact]
    public async Task ATupleElementKeptWithAnother_SaysSo()
    {
        var (solution, _, _) = await FindAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "namespace ZqxApp\n{\n    class C\n    {\n        private int _zqxN;\n\n        public (int zqxFirst, int zqxSecond) Get() => (1, 0);\n\n        public int GetN() => _zqxN;\n    }\n}\n"));
        var tests = ("Tests/T.cs", "namespace ZqxTests\n{\n    using ZqxApp;\n\n    class ZqxT\n    {\n        int zqxRead(C zqxC) => zqxC.Get().zqxFirst;\n    }\n}\n");

        Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync(tests, () => TupleElementRenamer.GetKeptReasonAsync(solution, "zqxFirst", "ZqxFirst", "C.cs", CancellationToken.None)));
        Assert.Equal(KeptReason.KeptWithOtherElement, await WithRepositoryNamesAsync(tests, () => TupleElementRenamer.GetKeptReasonAsync(solution, "zqxSecond", "ZqxSecond", "C.cs", CancellationToken.None)));
    }

    // AngleSharp (trial 21): static properties aren't data a serializer writes, so only a reflection-shaped string keeps
    // them; 'nbc' was kept for "https://www.nbc.com" and "Pages.nbc.html".
    [Theory]
    [InlineData("\"https://www.nbc.com\"", null)]
    [InlineData("\"nbc\"", null)]
    [InlineData("typeof(Assets).GetProperty(\"nbc\")", KeptReason.NameInString)]
    public async Task AStaticProperty_IsKeptOnlyForAReflectionShapedString(string use, KeptReason? expected)
    {
        var reason = await GetReasonAsync(
            DiagnosticIds.ElementPascalCase,
            ("Assets.cs", "internal static class Assets\n{\n    public static string nbc => Read(\"Pages.nbc.html\");\n\n    private static string Read(string name) => name;\n}\n"),
            ("Bench.cs", $"class Bench\n{{\n    object Use() => {use};\n}}\n"));

        Assert.Equal(expected, reason);
    }

    // eShop: a tuple element in a private member's signature (or a body) can't be seen by code the run doesn't load; a
    // use of that name in another project (the MAUI app outside the solution filter) doesn't keep it.
    [Fact]
    public async Task ATupleElementOnlyThisProjectSees_IsRenamedWhateverOutsideCodeIsNamed()
    {
        var (solution, _, _) = await FindAsync(
            DiagnosticIds.PrivateFieldNaming,
            ("C.cs", "namespace ZqxApp\n{\n    public class C\n    {\n        private int _zqxN;\n\n        private (int zqxUser, int zqxClaims) Find() => (1, 0);\n\n        public int Get()\n        {\n            (int zqxUser, int zqxClaims) pair = Find();\n            return pair.zqxUser + pair.zqxClaims + _zqxN;\n        }\n    }\n}\n"));
        var app = ("ClientApp/T.cs", "namespace ZqxClient\n{\n    class ZqxT\n    {\n        int zqxRead((int zqxUser, int zqxId) zqxP) => zqxP.zqxUser;\n    }\n}\n");

        Assert.Null(await WithRepositoryNamesAsync(app, () => TupleElementRenamer.GetKeptReasonAsync(solution, "zqxUser", "ZqxUser", "C.cs", CancellationToken.None)));
    }

    // EntityGraphQL: an internal method's '(bool IsSet, object? value)' was kept for a lambda's '(_, value) =>' in an example
    // project. Only member uses count ('.value', 'value:', another tuple's element), and for an internal member only in a
    // project InternalsVisibleTo names (the nearest project file's name).
    [Fact]
    public async Task AnInternalTupleElement_IsKeptOnlyForMemberUsesInProjectsThatSeeIt()
    {
        var files = new[]
        {
            ("C.cs", """
                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ZqxTests, PublicKey=00")]

                namespace ZqxApp
                {
                    public class C
                    {
                        private int _zqxN;

                        internal static (bool IsSet, object? zqxValue) Build() => (true, null);

                        public int GetN() => _zqxN;
                    }
                }

                """),
        };
        var lambda = """
            class ZqxT
            {
                System.Func<int, int, int> zqxF = (_, zqxValue) => zqxValue;
            }

            """;
        var member = """
            class ZqxT
            {
                object zqxRead() => ZqxApp.C.Build().zqxValue;
            }

            """;
        var folder = Path.Combine(Path.GetTempPath(), "stylebro-ivt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Examples"));
        Directory.CreateDirectory(Path.Combine(folder, "ZqxTests"));
        File.WriteAllText(Path.Combine(folder, "Examples", "Examples.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(folder, "ZqxTests", "ZqxTests.csproj"), "<Project />");
        try
        {
            // A new solution each time: the names are collected once per solution.
            async Task<KeptReason?> Reason() => await TupleElementRenamer.GetKeptReasonAsync((await FindAsync(DiagnosticIds.PrivateFieldNaming, files)).Solution, "zqxValue", "ZqxValue", "C.cs", CancellationToken.None);

            Assert.Null(await WithRepositoryNamesAsync(("Tests/T.cs", lambda), Reason));
            Assert.Null(await WithRepositoryNamesAsync((Path.Combine(folder, "Examples", "T.cs"), member), Reason));
            Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync((Path.Combine(folder, "ZqxTests", "T.cs"), member), Reason));
            Assert.Equal(KeptReason.UsedInProjectNotLoaded, await WithRepositoryNamesAsync(("Tests/T.cs", member), Reason));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
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

    [Fact]
    public void Format_ARunThatChangedOnlyLineEndings_IsTheLast()
    {
        // Bencodex: end_of_line and charset changed 5 files, and a second run checked every framework again to say clean.
        var root = NewFolder();
        File.WriteAllText(Path.Combine(root, ".editorconfig"), "root = true\n\n[*.cs]\nend_of_line = crlf\n");
        var lines = new List<string>();
        var runs = 0;

        var code = FormatCommand.Run(new[] { root }, lines.Add, (_, _) =>
        {
            runs++;
            File.WriteAllText(Path.Combine(root, "C.cs"), "﻿class C { }\r\n");
            WriteKept(Path.Combine(root, "C.cs"));
            return 0;
        });

        Assert.Equal(0, code);
        Assert.Equal(1, runs);
        Assert.Contains("Run 1: 1 file changed, line endings or byte order marks only (no run needed after it), clean:", lines);
        Assert.Contains(lines, l => l.StartsWith("Clean: 1 finding kept on purpose", StringComparison.Ordinal));

        // Anything else changed: another run.
        runs = 0;
        lines.Clear();
        FormatCommand.Run(new[] { root }, lines.Add, (_, _) =>
        {
            File.WriteAllText(Path.Combine(root, "C.cs"), $"class C{++runs} {{ }}\r\n");
            return 0;
        });
        Assert.Equal(FormatCommand.MaxRuns, runs);
    }

    [Fact]
    public void Verify_SaysTheVerdictAfterTheChecksAtTheEnd()
    {
        // Trial 26: "CSharpier: checking..." came before the "Checking StyleBro's rules" header. The checks after the runs (a
        // generator project's other configurations, CSharpier) come after the runs' output, and the verdict after them.
        var root = NewFolder();
        var lines = new List<string>();
        Directory.CreateDirectory(Path.Combine(root, "Missing"));

        // One left from an earlier command doesn't count.
        FormatCommand.RecordUnchecked(root, [(Path.Combine(root, "Other", "Other.csproj"), "Release")]);
        FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (_, _) => 0);
        Assert.DoesNotContain(lines, l => l.Contains("Other in Release", StringComparison.Ordinal));
        lines.Clear();

        // What a run with a generator project records (RunOnce).
        var code = FormatCommand.Run(new[] { root, "--verify-no-changes" }, lines.Add, (_, output) =>
        {
            output!("Checking StyleBro's rules (1 ids), changing nothing.");
            FormatCommand.RecordUnchecked(root, [(Path.Combine(root, "Missing", "Missing.csproj"), "Release")]);
            return 0;
        });

        // Its build fails (no project file): it checked nothing, so not clean (trial 27), and the verdict comes after it.
        Assert.Equal(FormatCommand.NotCleanExitCode, code);
        var header = lines.IndexOf("Checking StyleBro's rules (1 ids), changing nothing.");
        var build = lines.FindIndex(l => l.StartsWith("Building Missing in Release to check it", StringComparison.Ordinal));
        Assert.True(header >= 0 && build > header, string.Join("\n", lines));
        Assert.Equal("Formatting is clean. But:", lines[^2]);
        Assert.StartsWith("Not clean: the build of Missing in Release failed", lines[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WithAVerdictThatWaits_KeepsTheKeptFindingsBelowIt()
    {
        // The verdict waits for the checks at the end, and the kept findings it lists wait with it (not above it).
        var root = NewFolder();
        File.WriteAllText(Path.Combine(root, ".editorconfig"), "root = true\n\n[*.cs]\ndotnet_analyzer_diagnostic.severity = silent\n");
        var lines = new List<string>();

        FormatCommand.Run(new[] { root, FormatCommand.OnceOption }, lines.Add, (_, _) =>
        {
            WriteKept(Path.Combine(root, "C.cs"));
            return 0;
        });

        var verdict = lines.FindIndex(l => l.StartsWith("Formatting is clean: 1 finding kept on purpose", StringComparison.Ordinal));
        var finding = lines.FindIndex(l => l.StartsWith("  C.cs(3,17): BRO1303", StringComparison.Ordinal));
        var but = lines.FindIndex(l => l.StartsWith("Not clean: .editorconfig's bulk severity", StringComparison.Ordinal));
        Assert.True(verdict >= 0 && finding > verdict && but > finding, string.Join("\n", lines));
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

    /// <summary>The folders <see cref="NewFolder"/> made, deleted after the test (thousands were left in the temp folder).</summary>
    public void Dispose() => folders.ForEach(f => PreviewCommand.Delete(f));

    private string NewFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "stylebro-kept-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        folders.Add(root);
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

    // akka.net: an analyzer ProjectReference that isn't built yet is an unresolved analyzer reference, and the SDK's Roslyn
    // throws on it in every SymbolFinder call. The renames are computed without it and applied to the solution as it was
    // ('dotnet format' can't apply a change to a project's analyzer references).
    [Fact]
    public async Task UnresolvedAnalyzerReference_RenamesAndStaysInTheSolution()
    {
        var (solution, document, diagnostic) = await FindAsync("BRO1309", ("C.cs", "internal class C\n{\n    private void doWork() { }\n\n    public void Run() => doWork();\n}\n"));
        var unresolved = new UnresolvedAnalyzerReference(Path.Combine(Path.GetTempPath(), "missing", "Gen.dll"));
        solution = solution.AddAnalyzerReference(document.Project.Id, unresolved);

        var renamed = await CamelCaseRenamer.RenameAsync(solution, new[] { (solution.GetDocument(document.Id)!, diagnostic) }, CancellationToken.None);

        Assert.Contains(unresolved, renamed.GetProject(document.Project.Id)!.AnalyzerReferences);
        Assert.Contains("public void Run() => DoWork();", (await renamed.GetDocument(document.Id)!.GetTextAsync()).ToString(), StringComparison.Ordinal);
        Assert.Null(await CamelCaseRenamer.GetKeptReasonAsync(solution, solution.GetDocument(document.Id)!, diagnostic, CancellationToken.None));
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
            if (!path.EndsWith(".cs", StringComparison.Ordinal))
            {
                solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(project), path, SourceText.From(text), filePath: Path.Combine(Path.GetTempPath(), path));
                continue;
            }

            var document = DocumentId.CreateNewId(project);
            solution = solution.AddDocument(document, path, SourceText.From(text), filePath: Path.Combine(Path.GetTempPath(), path));
            ids.Add(document);
        }

        var first = solution.GetDocument(ids[0])!;
        var compilation = (await first.Project.GetCompilationAsync())!;
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FieldNamingAnalyzer(), new PascalCaseNamingAnalyzer());
        var tree = await first.GetSyntaxTreeAsync();
        var diagnostic = (await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync()).First(d => d.Id == id && d.Location.SourceTree == tree);
        return (solution, first, diagnostic);
    }
}
