using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>
/// BRO1405/BRO1410: unnecessary parentheses around an expression or a pattern. The diagnostic is on the parenthesized
/// node; a hidden companion on each '(' and ')' lets an IDE fade them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParenthesesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.UnnecessaryParentheses,
        Descriptors.UnnecessaryParenthesesFade,
        Descriptors.UnnecessaryPatternParentheses,
        Descriptors.UnnecessaryPatternParenthesesFade);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var node = (ParenthesizedExpressionSyntax)c.Node;
                if (Parentheses.IsUnnecessary(node, node.SyntaxTree.GetText(c.CancellationToken)))
                {
                    Report(c, Descriptors.UnnecessaryParentheses, Descriptors.UnnecessaryParenthesesFade);
                }
            },
            SyntaxKind.ParenthesizedExpression);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var node = (ParenthesizedPatternSyntax)c.Node;
                var tree = node.SyntaxTree;
                if (Parentheses.IsUnnecessary(
                    node,
                    tree.GetText(c.CancellationToken),
                    () => Severities.IsOn(c.Compilation.Options, tree, DiagnosticIds.ConditionalPrecedence, c.CancellationToken)
                        && Precedence.IsWanted(DiagnosticIds.ConditionalPrecedence, c.Options.AnalyzerConfigOptionsProvider.GetOptions(tree))))
                {
                    Report(c, Descriptors.UnnecessaryPatternParentheses, Descriptors.UnnecessaryPatternParenthesesFade);
                }
            },
            SyntaxKind.ParenthesizedPattern);
    }

    // The fading only where the rule itself is on: with the rule off, nothing says the parentheses are unnecessary.
    private static void Report(SyntaxNodeAnalysisContext c, DiagnosticDescriptor rule, DiagnosticDescriptor fade)
    {
        c.ReportDiagnostic(Diagnostic.Create(rule, c.Node.GetLocation()));
        if (Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, rule.Id, c.CancellationToken))
        {
            var (open, close) = Parentheses.GetTokens(c.Node);
            c.ReportDiagnostic(Diagnostic.Create(fade, open.GetLocation()));
            c.ReportDiagnostic(Diagnostic.Create(fade, close.GetLocation()));
        }
    }
}
