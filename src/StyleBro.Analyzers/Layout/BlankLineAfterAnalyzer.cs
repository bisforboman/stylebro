using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1503 (blank line after an opening brace, on the brace) and BRO1504 (no blank line before a comment that
/// follows code, on the comment), found in one pass over the tree.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlankLineAfterAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlankLineAfterOpenBrace, Descriptors.BlankLineBeforeComment);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var options = start.Compilation.Options;
            start.RegisterSyntaxTreeAction(c => AnalyzeTree(c, options));
        });
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context, CompilationOptions options)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var text = context.Tree.GetText(context.CancellationToken);
        foreach (var token in root.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.OpenBraceToken) && BlankLines.GetBlankLinesBelow(token, text).Count > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAfterOpenBrace, token.GetLocation()));
            }
        }

        foreach (var trivia in root.DescendantTrivia())
        {
            if (BlankLines.NeedsBlankLineAbove(trivia, text, id => Severities.IsOn(options, context.Tree, id, context.CancellationToken)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineBeforeComment, trivia.GetLocation()));
            }
        }
    }
}
