using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1151: pass the elements, not an array, to a params parameter. The diagnostic is on the array.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParamsArrayAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ParamsArray);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var array = (ExpressionSyntax)c.Node;
                if (array.Parent is ArgumentSyntax && ParamsArrays.GetChanges(array, c.SemanticModel, c.CancellationToken) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ParamsArray, array.GetLocation()));
                }
            },
            SyntaxKind.ArrayCreationExpression,
            SyntaxKind.ImplicitArrayCreationExpression,
            SyntaxKind.CollectionExpression);
    }
}
