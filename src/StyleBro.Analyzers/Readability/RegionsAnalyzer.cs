using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1112 (a region between members or types) and BRO1113 (a region inside a code element). The diagnostic is on the
/// '#region' line; the fix removes it and its '#endregion'.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegionsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NoRegions, Descriptors.NoRegionsInCodeElements);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            foreach (var (region, _, inCodeElement) in Regions.GetRegions(c.Tree.GetRoot(c.CancellationToken)))
            {
                c.ReportDiagnostic(Diagnostic.Create(
                    inCodeElement ? Descriptors.NoRegionsInCodeElements : Descriptors.NoRegions,
                    region.GetLocation()));
            }
        });
    }
}
