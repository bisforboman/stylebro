using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Ordering;

/// <summary>BRO1003: 'get' before 'set'/'init'. BRO1004: 'add' before 'remove'. The diagnostic is on the first accessor's keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AccessorOrderAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.PropertyAccessorOrder, Descriptors.EventAccessorOrder);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (AccessorOrder.GetFinding((AccessorListSyntax)c.Node, c.Node.SyntaxTree.GetText(c.CancellationToken)) is { } finding)
                {
                    c.ReportDiagnostic(finding.Id == DiagnosticIds.PropertyAccessorOrder
                        ? Diagnostic.Create(Descriptors.PropertyAccessorOrder, finding.First.Keyword.GetLocation(), finding.First.Keyword.ValueText)
                        : Diagnostic.Create(Descriptors.EventAccessorOrder, finding.First.Keyword.GetLocation()));
                }
            },
            SyntaxKind.AccessorList);
    }
}
