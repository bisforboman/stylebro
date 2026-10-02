using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1123: tuple syntax instead of ValueTuple. The diagnostic is on the type, the 'new' expression, or
/// 'ValueTuple.Create' (like StyleCop).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TupleSyntaxAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TupleSyntax);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var finding = c.Node is GenericNameSyntax name
                    ? TupleSyntax.GetType(name, c.SemanticModel, c.CancellationToken)
                    : TupleSyntax.GetCreation((ExpressionSyntax)c.Node, c.SemanticModel, c.CancellationToken);
                if (finding is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.TupleSyntax, finding.Location, finding.Replacement));
                }
            },
            SyntaxKind.GenericName,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.InvocationExpression);
    }
}
