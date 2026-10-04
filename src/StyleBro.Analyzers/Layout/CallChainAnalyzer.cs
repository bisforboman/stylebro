using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1523: a call of a split call chain that doesn't start its own line. The diagnostic is on its '.' or '?'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CallChainAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.CallChainLayout);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            foreach (var (link, _) in CallChains.GetFindings(c.Tree.GetRoot(c.CancellationToken), c.Tree.GetText(c.CancellationToken)))
            {
                // '.Name', or '?.Name' (the '?' token, then the '.' token)
                var next = link.GetNextToken();
                var member = next.IsKind(SyntaxKind.DotToken) ? "?." + next.GetNextToken().Text : "." + next.Text;
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.CallChainLayout, link.GetLocation(), member));
            }
        });
    }
}
