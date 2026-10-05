using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1141: an object creation with an initializer has its empty parentheses or not (stylebro_object_creation_parentheses). The diagnostic is on the parentheses, or on the type when they are missing.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObjectCreationParenthesesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ObjectCreationParentheses);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var creation = (ObjectCreationExpressionSyntax)c.Node;
                if (ObjectCreationParentheses.GetChange(creation, c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree)) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.ObjectCreationParentheses,
                        creation.ArgumentList is { } arguments ? arguments.GetLocation() : creation.Type.GetLocation(),
                        creation.ArgumentList is null ? "Add" : "Remove"));
                }
            },
            SyntaxKind.ObjectCreationExpression);
    }
}
