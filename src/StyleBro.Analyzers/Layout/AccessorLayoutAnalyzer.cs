using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1510: a property's, indexer's or event's accessors are all single-line or all multi-line. The diagnostic is on the first accessor.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AccessorLayoutAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.AccessorsConsistentLayout);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var list = (AccessorListSyntax)c.Node;
                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                if (AccessorLayout.GetFinding(list, text, options) is { } first)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.AccessorsConsistentLayout, first.Keyword.GetLocation()));
                }
            },
            SyntaxKind.AccessorList);
    }
}
