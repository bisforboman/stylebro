using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1148: 'x.HasValue' instead of a null check. The diagnostic is on 'x.HasValue' or '!x.HasValue'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HasValueAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.HasValueNullCheck);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var node = (MemberAccessExpressionSyntax)c.Node;
                if (node.Name.Identifier.ValueText != "HasValue")
                {
                    return;
                }

                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree);
                if (HasValueChecks.GetFix(node, c.SemanticModel, options, c.CancellationToken) is { } fix)
                {
                    var target = node.Parent.IsKind(SyntaxKind.LogicalNotExpression) ? node.Parent! : node;
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.HasValueNullCheck, target.GetLocation(), fix.Form));
                }
            },
            SyntaxKind.SimpleMemberAccessExpression);
    }
}
