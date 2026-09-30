using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1109 (opening parenthesis or bracket on the name's line) and BRO1110 (closing parenthesis or bracket on the last
/// item's line), for parameter lists, argument lists and attribute arguments. The diagnostic is on the misplaced token.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParenthesisPlacementAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.OpenParenthesisOnNameLine, Descriptors.CloseParenthesisOnLastItemLine);

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

                if (ParenthesisPlacement.GetMisplacedClose(c.Node, text) is { } close)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.CloseParenthesisOnLastItemLine, close.GetLocation(), close.Text));
                }
            },
            ParameterLayout.ListKinds);
    }
}
