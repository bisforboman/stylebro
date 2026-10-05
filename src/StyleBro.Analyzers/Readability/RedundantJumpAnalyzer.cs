using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1137: a 'return;' or 'yield break;' that ends a body. The diagnostic is on the statement.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RedundantJumpAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.RedundantJump);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var statement = (StatementSyntax)c.Node;
                if (statement.Parent is BlockSyntax && RedundantJumps.GetChange(statement, c.Node.SyntaxTree.GetText(c.CancellationToken)) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.RedundantJump, statement.GetLocation(), statement is ReturnStatementSyntax ? "return;" : "yield break;"));
                }
            },
            SyntaxKind.ReturnStatement,
            SyntaxKind.YieldBreakStatement);
    }
}
