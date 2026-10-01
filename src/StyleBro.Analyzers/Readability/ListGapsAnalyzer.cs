using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1116-BRO1119: gaps in parameter and argument lists (see <see cref="ListGaps"/>).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ListGapsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        Descriptors.EmptyListOnOneLine, Descriptors.CommaOnItemLine, Descriptors.FirstItemFollowsOpening, Descriptors.ItemFollowsComma);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                foreach (var finding in ListGaps.GetFindings(c.Node, c.Node.SyntaxTree.GetText(c.CancellationToken)))
                {
                    c.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics.First(d => d.Id == finding.Id), finding.Location));
                }
            },
            ParameterLayout.ListKinds);
    }
}