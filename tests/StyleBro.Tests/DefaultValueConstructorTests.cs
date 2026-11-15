using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Readability;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.DefaultValueConstructorAnalyzer, StyleBro.CodeFixes.Readability.DefaultValueConstructorCodeFixProvider>;

namespace StyleBro.Tests;

public class DefaultValueConstructorTests
{
    private const string NativeIntegers = """
        using System;

        class C
        {
            void M()
            {
                nint a = {|BRO1104:new nint()|};
                nuint b = {|BRO1104:new nuint()|};
                var c = {|BRO1104:new IntPtr()|};
                var d = {|BRO1104:new UIntPtr()|};
                nint e = {|BRO1104:new()|};
            }
        }
        """;

    [Fact]
    public Task ValueTypes_BecomeDefault() => VerifyFixAsync(
        """
        using System;

        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = {|BRO1104:new int()|};
                var b = {|BRO1104:new DateTime()|};
                var c = {|BRO1104:new Plain()|};
                var d = {|BRO1104:new int?()|};
                Plain e = {|BRO1104:new()|};
                var f = {|BRO1104:new Plain()|}.X;
            }
        }
        """,
        """
        using System;

        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = default(int);
                var b = default(DateTime);
                var c = default(Plain);
                var d = default(int?);
                Plain e = default(Plain);
                var f = default(Plain).X;
            }
        }
        """);

    [Fact]
    public Task WellKnownTypesAndEnums_UseTheirEmptyMember() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }
        enum Flags { A = 1, B = 2 }

        class C
        {
            void M()
            {
                var a = {|BRO1104:new CancellationToken()|};
                var b = {|BRO1104:new Guid()|};
                var c = {|BRO1104:new IntPtr()|};
                var d = {|BRO1104:new Color()|};
                var e = {|BRO1104:new Flags()|};
            }
        }
        """,
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }
        enum Flags { A = 1, B = 2 }

        class C
        {
            void M()
            {
                var a = CancellationToken.None;
                var b = Guid.Empty;
                var c = IntPtr.Zero;
                var d = Color.Red;
                var e = default(Flags);
            }
        }
        """);

    [Fact]
    public Task ParameterDefaults_StayConstant() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }

        class C
        {
            void M(Guid g = {|BRO1104:new Guid()|}, CancellationToken ct = {|BRO1104:new CancellationToken()|}, Color c = {|BRO1104:new Color()|})
            {
            }
        }
        """,
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }

        class C
        {
            void M(Guid g = default(Guid), CancellationToken ct = default(CancellationToken), Color c = Color.Red)
            {
            }
        }
        """);

    [Fact]
    public Task CodeThatDefaultWouldSkip_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        struct WithConstructor
        {
            public int X;
            public WithConstructor() { X = 1; }
        }

        struct Plain { public int X; }

        class C
        {
            void M<T>() where T : struct
            {
                var a = new WithConstructor();
                var b = new T();
                new Plain();
            }
        }
        """);

    [Fact]
    public Task ArgumentsInitializersAndClasses_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = new Plain { X = 1 };
                var b = new Plain() { };
                var c = new System.DateTime(2000, 1, 1);
                var d = new C();
            }
        }
        """);

    // 'nint.Zero' only compiles with C# 11 on .NET 7+ (StyleCop 1.3 checks for that): 'default(nint)' elsewhere.
    [Theory]
    [InlineData(true, LanguageVersion.CSharp11, "nint.Zero", "nuint.Zero")]
    [InlineData(true, LanguageVersion.CSharp10, "default(nint)", "default(nuint)")]
    [InlineData(false, LanguageVersion.CSharp11, "default(nint)", "default(nuint)")]
    public Task NativeIntegers_BecomeZeroOnNet7WithCSharp11_DefaultElsewhere(bool net8, LanguageVersion version, string nint, string nuint)
    {
        var test = new CSharpCodeFixTest<DefaultValueConstructorAnalyzer, DefaultValueConstructorCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = net8 ? ReferenceAssemblies.Net.Net80 : ReferenceAssemblies.NetStandard.NetStandard20,
            TestCode = NativeIntegers,
            FixedCode = Fixed(nint, nuint),
        };
        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(
            projectId,
            ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithLanguageVersion(version)));
        return test.RunAsync();
    }

    // A multi-targeted project compiles the same file once per target framework, and every copy must get the same
    // text: 'nint.Zero' only when it compiles in all of them, whichever copy the fix starts from.
    [Theory]
    [InlineData(true, 0, "nint.Zero", "nuint.Zero")]
    [InlineData(false, 0, "default(nint)", "default(nuint)")]
    [InlineData(false, 1, "default(nint)", "default(nuint)")]
    public async Task NativeIntegers_InSeveralTargets_BecomeZeroOnlyWhenAllCompileIt(bool allNet8, int startFrom, string nint, string nuint)
    {
        var source = NativeIntegers.Replace("{|BRO1104:", string.Empty, StringComparison.Ordinal).Replace("|}", string.Empty, StringComparison.Ordinal);
        var path = Path.Combine(Path.GetTempPath(), "NativeIntegers.cs");
        var solution = new AdhocWorkspace().CurrentSolution;
        var ids = new List<DocumentId>();
        foreach (var assemblies in new[] { ReferenceAssemblies.Net.Net80, allNet8 ? ReferenceAssemblies.Net.Net80 : ReferenceAssemblies.NetStandard.NetStandard20 })
        {
            var project = ProjectId.CreateNewId();
            var document = DocumentId.CreateNewId(project);
            solution = solution
                .AddProject(project, "p" + ids.Count, "p" + ids.Count, LanguageNames.CSharp)
                .WithProjectParseOptions(project, new CSharpParseOptions(LanguageVersion.CSharp11))
                .WithProjectCompilationOptions(project, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(project, await assemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None))
                .AddDocument(document, "NativeIntegers.cs", SourceText.From(source), filePath: path);
            ids.Add(document);
        }

        var analyzer = new DefaultValueConstructorAnalyzer();
        var fixer = new DefaultValueConstructorCodeFixProvider();
        var first = solution.GetDocument(ids[startFrom])!;
        var diagnostics = await new FixAllDiagnostics(analyzer).GetDocumentDiagnosticsAsync(first, CancellationToken.None);
        Assert.Equal(5, diagnostics.Count());
        CodeAction? action = null;
        await fixer.RegisterCodeFixesAsync(new CodeFixContext(first, diagnostics.First(), (a, _) => action ??= a, CancellationToken.None));
        var context = new FixAllContext(first, fixer, FixAllScope.Solution, action!.EquivalenceKey, new[] { "BRO1104" }, new FixAllDiagnostics(analyzer), CancellationToken.None);
        var operations = await (await fixer.GetFixAllProvider()!.GetFixAsync(context))!.GetOperationsAsync(CancellationToken.None);
        var fixedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;

        foreach (var id in ids)
        {
            Assert.Equal(Fixed(nint, nuint), (await fixedSolution.GetDocument(id)!.GetTextAsync()).ToString());
        }
    }

    [Fact]
    public Task PreferSimpleDefault_WritesTheLiteralWhereItBindsTheSame() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        struct Plain { public int X; }

        class C
        {
            TimeSpan field = {|BRO1104:new TimeSpan()|};

            int Property => {|BRO1104:new int()|};

            void M(Plain p = {|BRO1104:new Plain()|}, CancellationToken token = {|BRO1104:new CancellationToken()|})
            {
                DateTime a = {|BRO1104:new DateTime()|};
                a = {|BRO1104:new DateTime()|};
                Take({|BRO1104:new int()|});
                Func<int> f = () => {|BRO1104:new int()|};
                var g = {|BRO1104:new Guid()|};
                Take(token == {|BRO1104:new CancellationToken()|} ? 1 : 2);
                Pair({|BRO1104:new int()|}, {|BRO1104:new long()|});
            }

            static void Take(int x)
            {
            }

            static void Pair(int x, long y)
            {
            }
        }
        """,
        """
        using System;
        using System.Threading;

        struct Plain { public int X; }

        class C
        {
            TimeSpan field = default;

            int Property => default;

            void M(Plain p = default, CancellationToken token = default)
            {
                DateTime a = default;
                a = default;
                Take(default);
                Func<int> f = () => default;
                var g = Guid.Empty;
                Take(token == CancellationToken.None ? 1 : 2);
                Pair(default, default);
            }

            static void Take(int x)
            {
            }

            static void Pair(int x, long y)
            {
            }
        }
        """,
        "csharp_prefer_simple_default_expression = true:warning\n");

    // 'var', a conversion (boxing, nullable), an overload the literal can't pick, a member access on it, and creations
    // or 'default(T)'s that can't all become 'default' in one statement ('Two(default, default)' is ambiguous where
    // each alone isn't) keep 'default(T)'.
    [Fact]
    public Task PreferSimpleDefault_KeepsTheTypeWhereTheLiteralWouldChangeTheMeaning() => VerifyFixAsync(
        """
        class C
        {
            void M()
            {
                var a = {|BRO1104:new int()|};
                object o = {|BRO1104:new int()|};
                int? n = {|BRO1104:new int()|};
                Take({|BRO1104:new int()|});
                Two({|BRO1104:new int()|}, {|BRO1104:new long()|});
                Two(default(int), {|BRO1104:new long()|});
                var s = {|BRO1104:new int()|}.ToString();
            }

            static void Take(int x)
            {
            }

            static void Take(string x)
            {
            }

            static void Two(int x, long y)
            {
            }

            static void Two(string x, string y)
            {
            }
        }
        """,
        """
        class C
        {
            void M()
            {
                var a = default(int);
                object o = default(int);
                int? n = default(int);
                Take(default(int));
                Two(default(int), default(long));
                Two(default(int), default(long));
                var s = default(int).ToString();
            }

            static void Take(int x)
            {
            }

            static void Take(string x)
            {
            }

            static void Two(int x, long y)
            {
            }

            static void Two(string x, string y)
            {
            }
        }
        """,
        "csharp_prefer_simple_default_expression = true\n");

    [Theory]
    [InlineData("csharp_prefer_simple_default_expression = false\n", LanguageVersion.Latest)]
    [InlineData("csharp_prefer_simple_default_expression = true\n", LanguageVersion.CSharp7)]
    public Task PreferSimpleDefault_OffOrBeforeCSharp71_KeepsTheType(string editorConfig, LanguageVersion version)
    {
        var test = new CSharpCodeFixTest<DefaultValueConstructorAnalyzer, DefaultValueConstructorCodeFixProvider, DefaultVerifier>
        {
            TestCode = "class C\n{\n    int field = {|BRO1104:new int()|};\n}\n",
            FixedCode = "class C\n{\n    int field = default(int);\n}\n",
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(
            projectId,
            ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithLanguageVersion(version)));
        return test.RunAsync();
    }

    // Trial 25 (MimeKit): the SDK's formatter puts the space there too, so 'default(S)' changed again on the next run.
    [Fact]
    public Task SpaceBeforeCallParentheses_IsFollowed() => VerifyFixAsync(
        """
        struct S { }

        class C
        {
            S field = {|BRO1104:new S ()|};
        }
        """,
        """
        struct S { }

        class C
        {
            S field = default (S);
        }
        """,
        "csharp_space_between_method_call_name_and_opening_parenthesis = true\n");

    private sealed class FixAllDiagnostics : FixAllContext.DiagnosticProvider
    {
        private readonly DiagnosticAnalyzer analyzer;

        public FixAllDiagnostics(DiagnosticAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            var diagnostics = await compilation!.WithAnalyzers(ImmutableArray.Create(analyzer)).GetAnalyzerDiagnosticsAsync(cancellationToken);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return diagnostics.Where(d => d.Location.SourceTree == tree).OrderBy(d => d.Location.SourceSpan.Start);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());
    }

    private static string Fixed(string nint, string nuint) => NativeIntegers
        .Replace("{|BRO1104:new()|}", nint, StringComparison.Ordinal)
        .Replace("{|BRO1104:new nint()|}", nint, StringComparison.Ordinal)
        .Replace("{|BRO1104:new nuint()|}", nuint, StringComparison.Ordinal)
        .Replace("{|BRO1104:new IntPtr()|}", "IntPtr.Zero", StringComparison.Ordinal)
        .Replace("{|BRO1104:new UIntPtr()|}", "UIntPtr.Zero", StringComparison.Ordinal);
}
