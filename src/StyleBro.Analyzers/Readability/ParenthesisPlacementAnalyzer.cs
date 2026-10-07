using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1109 (opening parenthesis or bracket on the name's line) and BRO1110 (closing parenthesis or bracket on the last
/// item's line, or on its own line in a split list with stylebro_closing_parenthesis_placement = own_line), for parameter lists, argument lists and attribute arguments. The diagnostic is on the misplaced token.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParenthesisPlacementAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.OpenParenthesisOnNameLine, Descriptors.CloseParenthesisOnLastItemLine);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (c.Node.ContainsDiagnostics)
                {
                    return;
                }

                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                if (ParenthesisPlacement.GetMisplacedOpen(c.Node, text) is { } open)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.OpenParenthesisOnNameLine, open.GetLocation(), open.Text));
                }

                var ownLine = ParenthesisPlacement.IsOwnLine(c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree));
                var openMoves = ownLine && Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, DiagnosticIds.OpenParenthesisOnNameLine, c.CancellationToken);
                if (ParenthesisPlacement.GetCloseFix(c.Node, text, ownLine, openMoves) is { } close)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.CloseParenthesisOnLastItemLine,
                        close.Close.GetLocation(),
                        close.Close.Text,
                        close.Change.NewText!.Trim().Length == 0 ? "to its own line" : "to the end of the last item"));
                }
            },
            ParameterLayout.ListKinds);
    }
}
