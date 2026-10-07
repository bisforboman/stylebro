using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1107 (first item on the line after the parenthesis) and BRO1108 (items all on one line or each on its own),
/// for parameter and argument lists. Each diagnostic is on the item that is out of place.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterLayoutAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.SplitParametersStartOnNewLine, Descriptors.ParametersOnSameOrSeparateLines);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                if (ParameterLayout.GetFirstItemToMove(c.Node, text, c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree)) is { } first)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.SplitParametersStartOnNewLine, first.GetLocation()));
                }

                if (ParameterLayout.GetFirstMisplacedItem(c.Node, text) is { } misplaced)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ParametersOnSameOrSeparateLines, misplaced.GetLocation()));
                }
            },
            ParameterLayout.ListKinds);
    }
}
