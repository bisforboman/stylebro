using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1524: a split conditional expression with '?' or ':' sharing a line with both neighbors. The diagnostic is on the token.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConditionalLayoutAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.ConditionalLayout);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context = context.SkipForeignFiles();
        context.RegisterCompilationStartAction(start =>
        {
            var compilationOptions = start.Compilation.Options;
            start.RegisterSyntaxTreeAction(c =>
            {
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree);
                var mirror = !Severities.IsOn(compilationOptions, c.Tree, DiagnosticIds.OperatorPlacement, c.CancellationToken);
                foreach (var (token, beginning, _) in ConditionalLayout.GetFindings(c.Tree.GetRoot(c.CancellationToken), c.Tree.GetText(c.CancellationToken), options, mirror))
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.ConditionalLayout, token.GetLocation(), beginning ? "before" : "after", token.Text));
                }
            });
        });
    }
}
