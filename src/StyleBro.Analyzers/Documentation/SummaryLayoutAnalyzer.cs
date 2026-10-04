using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Documentation;

/// <summary>BRO1616: a summary written on one line or with its tags on lines of their own. The diagnostic is on the start tag.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SummaryLayoutAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.SummaryLayout);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree);
            foreach (var (startTag, join, _) in SummaryLayout.GetFindings(c.Tree.GetRoot(c.CancellationToken), c.Tree.GetText(c.CancellationToken), options))
            {
                c.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.SummaryLayout,
                    startTag.GetLocation(),
                    join ? "Write the summary on one line" : "Put the summary's tags on lines of their own"));
            }
        });
    }
}
