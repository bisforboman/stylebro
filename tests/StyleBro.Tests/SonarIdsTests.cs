using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.CodeFixes.Readability;
using static StyleBro.Tests.DocExamplesTests;

namespace StyleBro.Tests;

/// <summary>
/// BRO1149-BRO1151's fixes also fix Sonar's S1066, S2971 and S3878 where their shared logic accepts the place. A stub
/// stands in for SonarAnalyzer.CSharp: it reports the ids where Sonar does (observed with 10.35.0.4138: S1066 on the
/// inner 'if' keyword, S2971 on the 'Where' name, S3878 on the array), also at places StyleBro skips.
/// scripts/sonar-interop.ps1 checks the real package.
/// </summary>
public class SonarIdsTests
{
    [Fact]
    public Task S1066_MergedWhereBro1149Merges() => VerifyAsync<NestedIfCodeFixProvider>(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a)
                {
                    {|S1066:if|} (b)
                    {
                        M(b, a);
                    }
                }

                if (a)
                {
                    // Kept: a comment in the removed text.
                    {|S1066:if|} (b)
                    {
                        M(b, a);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a && b)
                {
                    M(b, a);
                }

                if (a)
                {
                    // Kept: a comment in the removed text.
                    {|S1066:if|} (b)
                    {
                        M(b, a);
                    }
                }
            }
        }
        """);

    [Fact]
    public Task S2971_FixedForWhereBeforeATerminalCall() => VerifyAsync<WhereBeforeTerminalCodeFixProvider>(
        """
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public object M(List<object> items)
            {
                var count = items.{|S2971:Where|}(i => i != null).Count();
                var cast = items.{|S2971:Where|}(i => i is string).Select(i => (string)i);
                var property = items.{|S2971:Count|}();
                return (count, cast, property);
            }
        }
        """,
        """
        using System.Collections.Generic;
        using System.Linq;

        public class C
        {
            public object M(List<object> items)
            {
                var count = items.Count(i => i != null);
                var cast = items.{|S2971:Where|}(i => i is string).Select(i => (string)i);
                var property = items.{|S2971:Count|}();
                return (count, cast, property);
            }
        }
        """);

    [Fact]
    public Task S3878_FixedWhereTheCallBindsTheSame() => VerifyAsync<ParamsArrayCodeFixProvider>(
        """
        public class C
        {
            public static int Sum(int first, params int[] rest) => first + rest.Length;

            public static int Count(params object[] values) => values.Length;

            public int M() => Sum(1, {|S3878:new[] { 2, 3 }|}) + Count({|S3878:new string[] { "a", "b" }|});
        }
        """,
        """
        public class C
        {
            public static int Sum(int first, params int[] rest) => first + rest.Length;

            public static int Count(params object[] values) => values.Length;

            public int M() => Sum(1, 2, 3) + Count({|S3878:new string[] { "a", "b" }|});
        }
        """);

    // A place the shared logic skips gets no code action (no light bulb that does nothing).
    [Fact]
    public async Task S1066_NoActionWhereBro1149Skips()
    {
        var document = CreateDocument(
            """
            public class C
            {
                public void M(bool a, bool b)
                {
                    if (a)
                    {
                        // Kept.
                        if (b)
                        {
                            M(b, a);
                        }
                    }
                }
            }
            """,
            null);
        var diagnostic = Assert.Single(await GetDiagnosticsAsync(document, ImmutableArray.Create<DiagnosticAnalyzer>(new SonarStub()), "S1066"));
        var actions = new List<CodeAction>();
        await new NestedIfCodeFixProvider().RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (a, _) => actions.Add(a), CancellationToken.None));
        Assert.Empty(actions);
    }

    // BRO1149 and S1066 sit on the same 'if': Fix All for either, in either order, gives the same text and leaves neither.
    [Theory]
    [InlineData("BRO1149", "S1066")]
    [InlineData("S1066", "BRO1149")]
    public async Task S1066_AndBro1149_ConvergeInEitherOrder(string first, string second)
    {
        const string Source = """
            public class C
            {
                public void M(bool a, bool b, bool c)
                {
                    if (a)
                    {
                        if (b)
                            if (c)
                                M(c, b, a);
                    }
                }
            }
            """;
        const string Expected = """
            public class C
            {
                public void M(bool a, bool b, bool c)
                {
                    if (a && b && c)
                    {
                        M(c, b, a);
                    }
                }
            }
            """;
        var document = CreateDocument(Source, null);
        foreach (var id in new[] { first, second })
        {
            var diagnostics = await GetDiagnosticsAsync(document, Analyzers(id), id);
            if (diagnostics.Length > 0)
            {
                document = await FixAllAsync(document, new NestedIfCodeFixProvider(), Analyzers(id), id, diagnostics);
            }
        }

        Assert.Equal(Expected, (await document.GetTextAsync()).ToString());
        Assert.Empty(await GetDiagnosticsAsync(document, Analyzers("S1066"), "S1066"));
        Assert.Empty(await GetDiagnosticsAsync(document, Analyzers("BRO1149"), "BRO1149"));
    }

    // The places StyleBro skips keep their Sonar warning (markup in the fixed code).
    private static Task VerifyAsync<TCodeFix>(string source, string fixedSource)
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new Verifier<SonarStub, TCodeFix>.Test { TestCode = source, FixedCode = fixedSource };
        test.FixedState.MarkupHandling = Microsoft.CodeAnalysis.Testing.MarkupMode.Allow;
        return test.RunAsync();
    }

    private static ImmutableArray<DiagnosticAnalyzer> Analyzers(string id) =>
        id.StartsWith('S') ? ImmutableArray.Create<DiagnosticAnalyzer>(new SonarStub()) : FindAnalyzer(id);

#pragma warning disable RS1001, RS2008 // A stand-in for SonarAnalyzer.CSharp, never loaded as an analyzer.
    /// <summary>Reports S1066/S2971/S3878 where Sonar does, by their shape alone (broader than StyleBro's checks).</summary>
    internal sealed class SonarStub : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor S1066 = new("S1066", "S1066", "Merge", "Sonar", DiagnosticSeverity.Warning, true);
        private static readonly DiagnosticDescriptor S2971 = new("S2971", "S2971", "Simplify", "Sonar", DiagnosticSeverity.Warning, true);
        private static readonly DiagnosticDescriptor S3878 = new("S3878", "S3878", "Pass the elements", "Sonar", DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(S1066, S2971, S3878);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    var outer = (IfStatementSyntax)c.Node;
                    var inner = outer.Statement as IfStatementSyntax ?? (outer.Statement as BlockSyntax is { Statements.Count: 1 } block ? block.Statements[0] as IfStatementSyntax : null);
                    if (outer.Else is null && inner is { Else: null })
                    {
                        c.ReportDiagnostic(Diagnostic.Create(S1066, inner.IfKeyword.GetLocation()));
                    }
                },
                SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    // 'Where(p).Count()'-like and 'Where(p).Select(cast)' on 'Where'; 'list.Count()' on 'Count'.
                    var call = (InvocationExpressionSyntax)c.Node;
                    if (call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Where" or "Count" } access
                        && (access.Name.Identifier.ValueText == "Count" ? call.ArgumentList.Arguments.Count == 0 && access.Expression is IdentifierNameSyntax : call.Parent is MemberAccessExpressionSyntax))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(S2971, access.Name.GetLocation()));
                    }
                },
                SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    var list = (ArgumentListSyntax)c.Node;
                    if (list.Arguments.LastOrDefault()?.Expression is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or CollectionExpressionSyntax)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(S3878, list.Arguments.Last().Expression.GetLocation()));
                    }
                },
                SyntaxKind.ArgumentList);
        }
    }
#pragma warning restore RS1001, RS2008
}
