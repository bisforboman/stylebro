using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1103: reports comparisons with a constant on the left, such as '1 == x' or 'null != o'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstantOnLeftAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstantOnLeft);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var binary = (BinaryExpressionSyntax)c.Node;
                if (ConstantComparisons.ShouldSwap(binary, c.SemanticModel, c.CancellationToken))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstantOnLeft, binary.GetLocation(), binary.Left.ToString()));
                }
            },
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.LessThanExpression,
            SyntaxKind.GreaterThanExpression,
            SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.GreaterThanOrEqualExpression);
    }
}
