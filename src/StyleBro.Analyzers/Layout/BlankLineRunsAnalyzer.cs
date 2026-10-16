using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1517-BRO1519: blank lines in a row, before '}', and missing after '}'.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlankLineRunsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.MultipleBlankLines, Descriptors.BlankLineBeforeCloseBrace, Descriptors.BlankLineAfterCloseBrace);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context = context.SkipForeignFiles();
        context.RegisterCompilationStartAction(start =>
        {
            var options = start.Compilation.Options;
            start.RegisterSyntaxTreeAction(c =>
            {
                var text = c.Tree.GetText(c.CancellationToken);
                foreach (var (id, span, _) in BlankLineRuns.GetFindings(c.Tree.GetRoot(c.CancellationToken), text, i => Severities.IsOn(options, c.Tree, i, c.CancellationToken), SwitchSectionBlankLines.ReadMode(c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree)), TrailingBlankLines.RemovesStandalone(c.Options.AnalyzerConfigOptionsProvider.GetOptions(c.Tree))))
                {
                    var descriptor = id == DiagnosticIds.MultipleBlankLines ? Descriptors.MultipleBlankLines
                        : id == DiagnosticIds.BlankLineBeforeCloseBrace ? Descriptors.BlankLineBeforeCloseBrace
                        : Descriptors.BlankLineAfterCloseBrace;
                    c.ReportDiagnostic(Diagnostic.Create(descriptor, Location.Create(c.Tree, span)));
                }
            });
        });
    }
}
