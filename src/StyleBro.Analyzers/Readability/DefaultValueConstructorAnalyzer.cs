using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1104: reports 'new T()' for value types, such as 'new int()' or 'new Guid()'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefaultValueConstructorAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DefaultValueConstructor);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var creation = (BaseObjectCreationExpressionSyntax)c.Node;
                if (DefaultValueConstructors.GetReplacement(creation, c.SemanticModel, c.Options.AnalyzerConfigOptionsProvider.GetOptions(creation.SyntaxTree), c.CancellationToken) is { } replacement)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.DefaultValueConstructor, creation.GetLocation(), replacement));
                }
            },
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression);
    }
}
