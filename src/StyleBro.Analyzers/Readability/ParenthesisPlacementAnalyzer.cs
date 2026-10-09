using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1109 (opening parenthesis or bracket on the name's line) and BRO1110 (closing parenthesis or bracket on the last
/// item's line, or on its own line in a split list with stylebro_closing_parenthesis_placement = own_line), for
/// parameter lists, argument lists and attribute arguments. The diagnostic is on the misplaced token.
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
        context.RegisterCompilationStartAction(start =>
        {
            var compilationOptions = start.Compilation.Options;
            start.RegisterSyntaxTreeAction(c => AnalyzeTree(c, compilationOptions));
        });
    }

    // One pass per file over TreeWalk's cached nodes, so the settings are read once per file, not for every list.
    private static void AnalyzeTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
        var ownLine = ParenthesisPlacement.IsOwnLine(options);
        bool IsOn(string id) => Severities.IsOn(compilationOptions, context.Tree, id, context.CancellationToken);
        var openMoves = ownLine && IsOn(DiagnosticIds.OpenParenthesisOnNameLine);
        var text = context.Tree.GetText(context.CancellationToken);
        foreach (var node in TreeWalk.Nodes(context.Tree.GetRoot(context.CancellationToken)))
        {
            if (!IsList(node) || node.ContainsDiagnostics)
            {
                continue;
            }

            if (ParenthesisPlacement.GetMisplacedOpen(node, text) is { } open)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.OpenParenthesisOnNameLine, open.GetLocation(), open.Text));
            }

            if (ParenthesisPlacement.GetCloseFix(node, text, ownLine, openMoves, options, IsOn) is { } close)
            {
                var newText = close.Change.NewText!;
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.CloseParenthesisOnLastItemLine,
                    close.Close.GetLocation(),
                    close.Close.Text,
                    newText.Trim().Length > 0 ? "to the end of the last item"
                        : newText.IndexOf('\n') >= 0 ? "to its own line"
                        : "to the indentation of the line with the opening token"));
            }
        }
    }

    private static bool IsList(SyntaxNode node) => node.Kind() is SyntaxKind.ParameterList or SyntaxKind.BracketedParameterList
        or SyntaxKind.ArgumentList or SyntaxKind.BracketedArgumentList or SyntaxKind.AttributeArgumentList or SyntaxKind.ArrayRankSpecifier;
}
