using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1520-BRO1522: an operator, '=>' or '=' on the wrong side of a line break. The diagnostic is on the token.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WrappingPlacementAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.OperatorPlacement, Descriptors.ArrowPlacement, Descriptors.EqualsPlacement);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree);
            foreach (var (id, token, beginning, _) in WrappingPlacement.GetFindings(c.Tree.GetRoot(c.CancellationToken), c.Tree.GetText(c.CancellationToken), options))
            {
                var descriptor = id == DiagnosticIds.OperatorPlacement ? Descriptors.OperatorPlacement
                    : id == DiagnosticIds.ArrowPlacement ? Descriptors.ArrowPlacement
                    : Descriptors.EqualsPlacement;
                c.ReportDiagnostic(Diagnostic.Create(descriptor, token.GetLocation(), token.Text, beginning ? "beginning" : "end"));
            }
        });
    }
}
