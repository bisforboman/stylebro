using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Spacing;

/// <summary>
/// BRO1002: reports '//comment' without a space. Only real comments count: text in disabled '#if' regions is not
/// comment trivia, and '///' documentation comments are a different kind of trivia.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentSpacingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CommentSpacing);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            foreach (var trivia in c.Tree.GetRoot(c.CancellationToken).DescendantTrivia())
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && CommentSpacing.GetFixedText(trivia.ToString()) is not null)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.CommentSpacing, trivia.GetLocation()));
                }
            }
        });
    }
}
