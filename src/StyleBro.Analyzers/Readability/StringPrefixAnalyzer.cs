using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1138: an unneeded '$', '@' or raw string. The diagnostic is on the literal.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringPrefixAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UnneededStringPrefix);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (StringPrefixes.GetChange((ExpressionSyntax)c.Node, c.SemanticModel, c.CancellationToken) is { } found)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.UnneededStringPrefix, c.Node.GetLocation(), found.Form));
                }
            },
            SyntaxKind.StringLiteralExpression,
            SyntaxKind.Utf8StringLiteralExpression,
            SyntaxKind.InterpolatedStringExpression);
    }
}
