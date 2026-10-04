using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1506 (blank line after a single-line comment, on the comment) and BRO1507 (blank lines at the end of the file,
/// at the end of the last code or comment), in one pass over the tree.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TrailingBlankLinesAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlankLineAfterComment, Descriptors.BlankLinesAtEndOfFile);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            var root = c.Tree.GetRoot(c.CancellationToken);
            var text = c.Tree.GetText(c.CancellationToken);
            foreach (var trivia in TreeWalk.Trivia(root))
            {
                if (TrailingBlankLines.GetBlankLinesAfterComment(trivia, text).Count > 0)
                {
                    c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLineAfterComment, trivia.GetLocation()));
                }
            }

            if (TrailingBlankLines.GetExtraEnding(text) is not null)
            {
                var end = TrailingBlankLines.GetEndOfContent(text);
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.BlankLinesAtEndOfFile, Location.Create(c.Tree, new TextSpan(end, 0))));
            }
        });
    }
}
