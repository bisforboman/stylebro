using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>BRO1111: every 'where' clause on its own line. The diagnostic is on the clause.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstraintPlacementAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstraintOnOwnLine);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                if (c.Node.ContainsDiagnostics)
                {
                    return;
                }

                var text = c.Node.SyntaxTree.GetText(c.CancellationToken);
                foreach (var clause in ConstraintPlacement.GetMisplacedClauses(c.Node, text))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintOnOwnLine, clause.GetLocation(), clause.Name.Identifier.ValueText));
                }
            },
            ConstraintPlacement.DeclarationKinds);
    }
}
