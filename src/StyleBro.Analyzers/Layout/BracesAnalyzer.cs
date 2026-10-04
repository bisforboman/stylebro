using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>BRO1514-BRO1516: a child statement without braces. The diagnostic is on the statement.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BracesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BracesOmitted, Descriptors.BracesMultiLine, Descriptors.BracesConsistent);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            c =>
            {
                var tree = c.Node.SyntaxTree;
                var text = tree.GetText(c.CancellationToken);
                var options = c.Options.AnalyzerConfigOptionsProvider.GetOptions(tree);
                foreach (var (child, id) in Braces.GetFindings(c.Node, text, i => Severities.IsOn(c.Compilation.Options, tree, i, c.CancellationToken), Braces.AllowConsecutiveUsings(options), Braces.GetPreference(options), Braces.AllowSingleLineJumps(options)))
                {
                    if (Braces.GetChanges(new[] { child }, text, options) is not null)
                    {
                        var descriptor = id == DiagnosticIds.BracesMultiLine ? Descriptors.BracesMultiLine
                            : id == DiagnosticIds.BracesConsistent ? Descriptors.BracesConsistent
                            : Descriptors.BracesOmitted;
                        c.ReportDiagnostic(Diagnostic.Create(descriptor, child.GetLocation()));
                    }
                }
            },
            Braces.Kinds);
    }
}
