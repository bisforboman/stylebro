using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1501 (blank line before an opening brace) and BRO1502 (blank line before 'else', 'catch' or 'finally'), found
/// in one pass over the tokens. The diagnostic is on the token; the code fix recomputes the blank lines from it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlankLineBeforeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlankLineBeforeOpenBrace, Descriptors.BlankLineBeforeChainedBlock);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var text = context.Tree.GetText(context.CancellationToken);
        foreach (var token in root.DescendantTokens())
        {
            var descriptor = BlankLines.IsCheckedOpenBrace(token) ? Descriptors.BlankLineBeforeOpenBrace
                : BlankLines.IsChainedBlockKeyword(token) ? Descriptors.BlankLineBeforeChainedBlock
                : null;

            if (descriptor is not null && BlankLines.GetBlankLinesAbove(token, text).Count > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, token.GetLocation(), token.Text));
            }
        }
    }
}
