using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1525: blank lines after an attribute list. The diagnostic is at the start of the first blank line.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeBlankLinesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.BlankLineAfterAttributes);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            var text = c.Tree.GetText(c.CancellationToken);
            foreach (var blankLines in AttributeBlankLines.GetFindings(c.Tree.GetRoot(c.CancellationToken), text))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAfterAttributes, Location.Create(c.Tree, new TextSpan(blankLines.Start, 0))));
            }
        });
    }
}
