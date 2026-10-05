using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1526: a blank line between switch sections, or none. The diagnostic is on the next section's first token.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchSectionBlankLinesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.BlankLineBetweenSwitchSections);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var compilationOptions = start.Compilation.Options;
            start.RegisterSyntaxNodeAction(
                c =>
                {
                    var node = (SwitchStatementSyntax)c.Node;
                    var mode = SwitchSectionBlankLines.ReadMode(c.Options.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree));
                    var text = node.SyntaxTree.GetText(c.CancellationToken);
                    foreach (var (location, change) in SwitchSectionBlankLines.GetFindings(node, text, mode, id => Severities.IsOn(compilationOptions, node.SyntaxTree, id, c.CancellationToken)))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineBetweenSwitchSections, Location.Create(node.SyntaxTree, location), change.NewText!.Length == 0 ? "Remove the blank line" : "Add a blank line"));
                    }
                },
                SyntaxKind.SwitchStatement);
        });
    }
}
