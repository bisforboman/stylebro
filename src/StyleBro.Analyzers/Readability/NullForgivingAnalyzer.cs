using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1147: a null-forgiving '!' on an operand that is already not null. The diagnostic is on the '!'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullForgivingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.RedundantNullForgiving);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var node = (PostfixUnaryExpressionSyntax)c.Node;
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
                if (NullForgiving.GetChange(node, c.SemanticModel, options, c.CancellationToken) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.RedundantNullForgiving, node.OperatorToken.GetLocation()));
                }
            },
            SyntaxKind.SuppressNullableWarningExpression);
    }
}
