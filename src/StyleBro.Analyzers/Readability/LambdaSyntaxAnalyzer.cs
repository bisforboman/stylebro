using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1125: lambda syntax instead of anonymous methods (on 'delegate'). BRO1403: no empty parentheses after
/// 'delegate' (on the parentheses), only where BRO1125 doesn't turn the method into a lambda anyway.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LambdaSyntaxAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LambdaSyntax, Descriptors.EmptyDelegateParentheses);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var method = (AnonymousMethodExpressionSyntax)c.Node;
                if (UsesLambda(method, c.SemanticModel, c.CancellationToken))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.LambdaSyntax, method.DelegateKeyword.GetLocation()));
                }
                else if (LambdaSyntax.GetEmptyParameterList(method, c.SemanticModel, c.CancellationToken) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyDelegateParentheses, method.ParameterList!.GetLocation()));
                }
            },
            SyntaxKind.AnonymousMethodExpression);
    }

    /// <summary>Whether BRO1125 is on and turns this method into a lambda (then BRO1403 has nothing left to do).</summary>
    internal static bool UsesLambda(AnonymousMethodExpressionSyntax method, SemanticModel model, System.Threading.CancellationToken cancellationToken) =>
        Severities.IsOn(model.Compilation.Options, method.SyntaxTree, DiagnosticIds.LambdaSyntax, cancellationToken)
        && LambdaSyntax.GetLambda(method, model, cancellationToken) is not null;
}
