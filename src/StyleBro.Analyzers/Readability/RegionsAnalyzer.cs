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
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NoRegions, Descriptors.NoRegionsInCodeElements);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context = context.SkipForeignFiles();
        context.RegisterCompilationStartAction(start =>
        {
            var snippets = Regions.GetSnippetNames(start.Options.AnalyzerConfigOptionsProvider.GlobalOptions);
            start.RegisterSyntaxTreeAction(c =>
            {
                foreach (var (region, _, inCodeElement) in Regions.GetRegions(c.Tree.GetRoot(c.CancellationToken)))
                {
                    // A MarkdownSnippets snippet: the docs name the region, and without it the build fails "Missing snippet"
                    // (trial 37, shouldly; owner's decision 2026-10-20).
                    if (snippets.Contains(Regions.GetName(region)))
                    {
                        continue;
                    }

                    c.ReportDiagnostic(Diagnostic.Create(
                        inCodeElement ? Descriptors.NoRegionsInCodeElements : Descriptors.NoRegions,
                        region.GetLocation()));
                }
            });
        });
    }
}
