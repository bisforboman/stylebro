using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1127-BRO1130: query clause layout. Each rule is reported once per keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QueryLayoutAnalyzer : DiagnosticAnalyzer
{
    private static readonly Dictionary<string, DiagnosticDescriptor> ById = new()
    {
        [DiagnosticIds.QueryClauseBlankLine] = Descriptors.QueryClauseBlankLine,
        [DiagnosticIds.QueryClausesOnSeparateLines] = Descriptors.QueryClausesOnSeparateLines,
        [DiagnosticIds.QueryClauseAfterMultiLineClause] = Descriptors.QueryClauseAfterMultiLineClause,
        [DiagnosticIds.MultiLineQueryClause] = Descriptors.MultiLineQueryClause,
    };

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.QueryClauseBlankLine, Descriptors.QueryClausesOnSeparateLines, Descriptors.QueryClauseAfterMultiLineClause, Descriptors.MultiLineQueryClause);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                var reported = new HashSet<(string, int)>();
                foreach (var finding in QueryLayout.GetFindings((QueryExpressionSyntax)c.Node, text, options))
                {
                    if (reported.Add((finding.Id, finding.Token.SpanStart)))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(ById[finding.Id], finding.Token.GetLocation()));
                    }
                }
            },
            SyntaxKind.QueryExpression);
    }
}
