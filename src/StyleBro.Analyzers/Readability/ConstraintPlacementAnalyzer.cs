using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1111: every 'where' clause on its own line (or, with stylebro_constraint_placement = same_line, on the declaration's
/// line). The diagnostic is on the clause.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstraintPlacementAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstraintOnOwnLine);

    /// <inheritdoc/>
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
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Node.SyntaxTree);
                if (ConstraintPlacement.IsSameLine(options))
                {
                    foreach (var (clause, _) in ConstraintPlacement.GetJoins(c.Node, text, options, id => Severities.IsOn(c.Compilation.Options, c.Node.SyntaxTree, id, c.CancellationToken)))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintOnOwnLine, clause.GetLocation(), clause.Name.Identifier.ValueText, "to the declaration's line"));
                    }

                    return;
                }

                foreach (var clause in ConstraintPlacement.GetMisplacedClauses(c.Node, text))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintOnOwnLine, clause.GetLocation(), clause.Name.Identifier.ValueText, "to its own line"));
                }
            },
            ConstraintPlacement.DeclarationKinds);
    }
}
