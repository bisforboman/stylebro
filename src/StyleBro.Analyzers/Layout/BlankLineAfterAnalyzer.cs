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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlankLineAfterOpenBrace, Descriptors.BlankLineBeforeComment);

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
            if (token.IsKind(SyntaxKind.OpenBraceToken) && BlankLines.GetBlankLinesBelow(token, text).Count > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAfterOpenBrace, token.GetLocation()));
            }
        }

        foreach (var trivia in root.DescendantTrivia())
        {
            if (BlankLines.NeedsBlankLineAbove(trivia, text))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineBeforeComment, trivia.GetLocation()));
            }
        }
    }
}
