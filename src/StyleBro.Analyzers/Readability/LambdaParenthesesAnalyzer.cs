using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1136: 'x => x' instead of '(x) => x'. The diagnostic is on the parameter list.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LambdaParenthesesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LambdaParentheses);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var lambda = (ParenthesizedLambdaExpressionSyntax)c.Node;
                if (LambdaParentheses.GetChange(lambda) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.LambdaParentheses, lambda.ParameterList.GetLocation(), lambda.ParameterList.Parameters[0].Identifier.ValueText));
                }
            },
            SyntaxKind.ParenthesizedLambdaExpression);
    }
}
