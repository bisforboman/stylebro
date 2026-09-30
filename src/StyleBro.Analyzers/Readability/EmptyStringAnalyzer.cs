using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1106: reports "" where 'string.Empty' can be used.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyStringAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyString);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var literal = (LiteralExpressionSyntax)c.Node;
                if (EmptyStrings.ShouldReplace(literal))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyString, literal.GetLocation(), literal.Token.Text));
                }
            },
            SyntaxKind.StringLiteralExpression);
    }
}
