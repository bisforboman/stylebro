using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1124: tuple elements are referred to by name. The diagnostic is on 'ItemN'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TupleElementNameAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.TupleElementName);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var name = (IdentifierNameSyntax)c.Node;
                if (TupleElementNames.GetName(name, c.SemanticModel, c.CancellationToken) is { } newName)
                {
                    // After '?.', on '.Item1' like StyleCop.
                    var location = name.Parent is MemberBindingExpressionSyntax binding ? binding.GetLocation() : name.GetLocation();
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.TupleElementName, location, newName, name.Identifier.ValueText));
                }
            },
            SyntaxKind.IdentifierName);
    }
}
