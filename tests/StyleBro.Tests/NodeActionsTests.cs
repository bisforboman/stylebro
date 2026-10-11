using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers;

namespace StyleBro.Tests;

/// <summary>
/// <see cref="NodeActions"/> replaces the compiler's syntax node actions in every analyzer: it must call the action for
/// exactly the nodes the compiler would, with a semantic model that answers the same.
/// </summary>
public class NodeActionsTests
{
    private const string Source = """
        [assembly: System.CLSCompliant(false)]
        namespace App
        {
            using System;

            public enum Color { Red, Green }

            public partial class Shape<T> : IDisposable where T : class
            {
                private int count = 1, total;

                public int Area { get => count; set { total = value; } }

                public void Dispose()
                {
                    var square = (Func<int, int>)(x => x * x);
                    if (count > 0) { total = square(count); } else { total = 0; }
                    int Local() => total;
                }
            }
        }
        """;

    [Fact]
    public async Task VisitsTheSameNodesAsSyntaxNodeActions()
    {
        var tree = CSharpSyntaxTree.ParseText(Source);
        var kinds = tree.GetRoot().DescendantNodesAndSelf().Select(n => n.Kind()).Distinct().ToArray();
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var analyzer = new Recorder(kinds);
        await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)).GetAnalyzerDiagnosticsAsync();

        Assert.Equal(tree.GetRoot().DescendantNodesAndSelf().Count(), analyzer.Compiler.Count);
        Assert.Equal(Sorted(analyzer.Compiler), Sorted(analyzer.Ours));
        Assert.Contains(analyzer.Ours, n => n.Kind == SyntaxKind.CompilationUnit);
    }

    private static List<(SyntaxKind, int, int, string?)> Sorted(ConcurrentBag<(SyntaxKind Kind, int Start, int Length, string? Symbol)> nodes) =>
        nodes.OrderBy(n => n.Start).ThenBy(n => n.Length).ThenBy(n => n.Kind).Select(n => (n.Kind, n.Start, n.Length, n.Symbol)).ToList();

#pragma warning disable RS1036 // A test analyzer, never loaded by a build.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class Recorder : DiagnosticAnalyzer
    {
        private readonly SyntaxKind[] kinds;

        public Recorder(SyntaxKind[] kinds)
        {
            this.kinds = kinds;
        }

        public ConcurrentBag<(SyntaxKind Kind, int Start, int Length, string? Symbol)> Compiler { get; } = new();

        public ConcurrentBag<(SyntaxKind Kind, int Start, int Length, string? Symbol)> Ours { get; } = new();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(new DiagnosticDescriptor("T0001", "t", "t", "t", DiagnosticSeverity.Warning, true));

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => Compiler.Add(Record(c)), kinds);
            context.RegisterNodeAction(c => Ours.Add(Record(c)), kinds);
        }

        private static (SyntaxKind, int, int, string?) Record(SyntaxNodeAnalysisContext c) =>
            (c.Node.Kind(), c.Node.SpanStart, c.Node.Span.Length, c.SemanticModel.GetDeclaredSymbol(c.Node)?.ToDisplayString());
    }
#pragma warning restore RS1036
}
