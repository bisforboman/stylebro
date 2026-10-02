using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1131: 'this.' instead of 'base.' when the type has no member of its own. The diagnostic is on 'base'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BaseCallsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BaseCall);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (BaseCalls.CanUseThis((BaseExpressionSyntax)c.Node, c.SemanticModel, c.CancellationToken))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.BaseCall, c.Node.GetLocation()));
                }
            },
            SyntaxKind.BaseExpression);
    }
}
