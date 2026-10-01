using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1402: '[Obsolete]' instead of '[Obsolete()]'. The diagnostic is on the parentheses.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyAttributeParenthesesAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyAttributeParentheses);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var attribute = (AttributeSyntax)c.Node;
                if (EmptyAttributeParentheses.GetChange(attribute) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyAttributeParentheses, attribute.ArgumentList!.GetLocation()));
                }
            },
            SyntaxKind.Attribute);
    }
}