using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1133: a null check in the form stylebro_null_check_style doesn't ask for. The diagnostic is on the whole check.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullCheckAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.NullCheckStyle);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                if (NullChecks.GetFix((ExpressionSyntax)c.Node, c.SemanticModel, options, c.CancellationToken) is { } fix)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.NullCheckStyle, c.Node.GetLocation(), fix.Form));
                }
            },
            SyntaxKind.EqualsExpression,
            SyntaxKind.NotEqualsExpression,
            SyntaxKind.IsPatternExpression);
    }
}
