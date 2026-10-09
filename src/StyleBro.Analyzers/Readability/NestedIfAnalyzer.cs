using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1149: merge an 'if' into the enclosing 'if'. The diagnostic is on the inner 'if' keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NestedIfAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NestedIf);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var inner = (IfStatementSyntax)c.Node;
                if (inner.Parent is IfStatementSyntax or BlockSyntax { Statements.Count: 1, Parent: IfStatementSyntax }
                    && NestedIfs.GetChange(
                        inner,
                        c.Node.SyntaxTree.GetText(c.CancellationToken),
                        c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree),
                        id => Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, id, c.CancellationToken)) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.NestedIf, inner.IfKeyword.GetLocation()));
                }
            },
            SyntaxKind.IfStatement);
    }
}
