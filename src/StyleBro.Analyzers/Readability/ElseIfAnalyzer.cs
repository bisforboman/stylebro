using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1139: 'else if' on one line. The diagnostic is on the 'else'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ElseIfAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ElseIf);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var elseClause = (ElseClauseSyntax)c.Node;
                if (elseClause.Statement is IfStatementSyntax or BlockSyntax { Statements.Count: 1 }
                    && ElseIfs.IsReported(
                        elseClause,
                        c.Node.SyntaxTree.GetText(c.CancellationToken),
                        c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree),
                        id => Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, id, c.CancellationToken)))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ElseIf, elseClause.ElseKeyword.GetLocation()));
                }
            },
            SyntaxKind.ElseClause);
    }
}
