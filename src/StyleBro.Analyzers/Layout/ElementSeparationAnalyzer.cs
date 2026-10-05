using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1505: reports an element that isn't separated from the previous one by a blank line.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ElementSeparationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ElementsSeparatedByBlankLine);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var compilationOptions = start.Compilation.Options;
            start.RegisterSyntaxTreeAction(c =>
            {
                var text = c.Tree.GetText(c.CancellationToken);
                var autoAccessorLines = Severities.IsOn(compilationOptions, c.Tree, DiagnosticIds.AutoAccessorsOnOneLine, c.CancellationToken)
                    ? c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree)
                    : null;
                foreach (var (previous, current) in ElementSeparation.GetViolations(c.Tree.GetRoot(c.CancellationToken), text, autoAccessorLines))
                {
                    if (ElementSeparation.GetChange(previous, current, text) is not null)
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.ElementsSeparatedByBlankLine, ElementSeparation.GetLocation(current)));
                    }
                }
            });
        });
    }
}
