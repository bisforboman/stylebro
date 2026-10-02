using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>BRO1615: the file starts with the XML copyright header. One diagnostic per file.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileHeaderAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.FileHeader);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            if (FileHeaderOptions.Read(c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree)) is not { } options)
            {
                return;
            }

            var root = c.Tree.GetRoot(c.CancellationToken);
            if (FileHeaders.GetFinding(root, c.Tree.GetText(c.CancellationToken), options) is { } finding)
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.FileHeader, Location.Create(c.Tree, new TextSpan(finding.Position, 0)), finding.Message));
            }
        });
    }
}
