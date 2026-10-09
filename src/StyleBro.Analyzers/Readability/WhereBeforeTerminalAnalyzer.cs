using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1150: 'source.Count(p)' instead of 'source.Where(p).Count()'. The diagnostic is on 'Where'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WhereBeforeTerminalAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.WhereBeforeTerminal);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var where = (InvocationExpressionSyntax)c.Node;
                if (where.Expression is MemberAccessExpressionSyntax { Name: { Identifier.ValueText: "Where" } name }
                    && where.Parent is MemberAccessExpressionSyntax
                    && WhereCalls.GetChanges(where, c.SemanticModel, c.CancellationToken) is { } change)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.WhereBeforeTerminal, name.GetLocation(), change.Name));
                }
            },
            SyntaxKind.InvocationExpression);
    }
}
