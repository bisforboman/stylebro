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
        context = context.SkipForeignFiles();
        context.RegisterNodeAction(
            c =>
            {
                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                bool JoinsEmptyLists() => Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, DiagnosticIds.EmptyListOnOneLine, c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                if (ParameterLayout.GetFirstItemToMove(c.Node, text, options, JoinsEmptyLists) is { } first)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.SplitParametersStartOnNewLine, first.GetLocation()));
                }

                if (ParameterLayout.GetFirstMisplacedItem(c.Node, text, JoinsEmptyLists, options) is { } misplaced)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ParametersOnSameOrSeparateLines, misplaced.GetLocation()));
                }
            },
            ParameterLayout.ListKinds);
    }
}
