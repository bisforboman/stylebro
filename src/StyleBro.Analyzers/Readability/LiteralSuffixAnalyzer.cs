using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1122: '1L' instead of '(long)1', on the cast; BRO1135: '1L' instead of '1l', on the literal.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LiteralSuffixAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LiteralSuffix, Descriptors.LiteralSuffixCase);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (LiteralSuffixes.GetChange((CastExpressionSyntax)c.Node, c.SemanticModel, c.CancellationToken) is { } change)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.LiteralSuffix, c.Node.GetLocation(), change.NewText));
                }
            },
            SyntaxKind.CastExpression);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (LiteralSuffixes.GetUpperCaseSuffix(((LiteralExpressionSyntax)c.Node).Token) is { } newText)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.LiteralSuffixCase, c.Node.GetLocation(), newText));
                }
            },
            SyntaxKind.NumericLiteralExpression);
    }
}
