using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1106: reports "" where 'string.Empty' can be used, or 'string.Empty' with stylebro_empty_string_style = literal.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmptyStringAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EmptyString);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var literal = (LiteralExpressionSyntax)c.Node;
                if (EmptyStrings.ShouldReplace(literal)
                    && !EmptyStrings.PrefersLiteral(c.Options.AnalyzerConfigOptionsProvider.GetOptions(literal.SyntaxTree)))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyString, literal.GetLocation(), "string.Empty", literal.Token.Text));
                }
            },
            SyntaxKind.StringLiteralExpression);
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var access = (MemberAccessExpressionSyntax)c.Node;
                if (access.Name.Identifier.ValueText == "Empty"
                    && EmptyStrings.PrefersLiteral(c.Options.AnalyzerConfigOptionsProvider.GetOptions(access.SyntaxTree))
                    && EmptyStrings.ShouldReplaceWithLiteral(access, c.SemanticModel, c.CancellationToken))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.EmptyString, access.GetLocation(), "\"\"", access.ToString()));
                }
            },
            SyntaxKind.SimpleMemberAccessExpression);
    }
}
