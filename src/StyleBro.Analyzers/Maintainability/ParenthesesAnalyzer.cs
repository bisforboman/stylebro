using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1405: unnecessary parentheses. The diagnostic is on the parenthesized expression.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParenthesesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UnnecessaryParentheses);

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
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.UnnecessaryParentheses, node.GetLocation()));
                }
            },
            SyntaxKind.ParenthesizedExpression);
    }
}
