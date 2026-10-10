using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Ordering;

/// <summary>BRO1008: using directives inside or outside the namespace. The diagnostic is on each misplaced directive.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsingPlacementAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UsingPlacement);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSemanticModelAction(
            c =>
            {
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.SemanticModel.SyntaxTree);
                var mode = UsingPlacement.GetMode(options);
                var root = (CompilationUnitSyntax)c.SemanticModel.SyntaxTree.GetRoot(c.CancellationToken);
                var misplaced = UsingPlacement.GetMisplaced(root, mode);
                if (misplaced.IsEmpty
                    || UsingPlacement.GetChanges(c.SemanticModel, mode, Indentation.GetUnit(options), c.CancellationToken) is null)
                {
                    return;
                }

                var where = mode == UsingPlacementMode.Outside ? "outside" : "inside";
                foreach (var directive in misplaced)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.UsingPlacement, directive.GetLocation(), where));
                }
            });
    }
}
