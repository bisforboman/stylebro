using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Spacing;

/// <summary>BRO1006: no space between '#' and a preprocessor keyword ('# if' -> '#if'). The diagnostic is on the keyword.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectiveSpacingAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptors.DirectiveSpacing);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(c =>
        {
            foreach (var (keyword, _) in GetSpaces(c.Tree.GetRoot(c.CancellationToken)))
            {
                c.ReportDiagnostic(Diagnostic.Create(Descriptors.DirectiveSpacing, keyword.GetLocation(), keyword.Text));
            }
        });
    }

    /// <summary>
    /// Like StyleCop's SA1006: a '#' followed by whitespace (not a line break) and then a token: the keyword and the
    /// whitespace to remove.
    /// </summary>
    internal static IEnumerable<(SyntaxToken Keyword, TextSpan Space)> GetSpaces(SyntaxNode root)
    {
        // Only the directives, not every token of the file (most files have none).
        if (!root.ContainsDirectives)
        {
            yield break;
        }

        // The tree's shared trivia (TreeWalk): GetNextDirective searched the tree from each directive again.
        foreach (var trivia in TreeWalk.Trivia(root))
        {
            if (!trivia.IsDirective)
            {
                continue;
            }

            var token = ((DirectiveTriviaSyntax)trivia.GetStructure()!).HashToken;
            if (!token.HasTrailingTrivia || token.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
            {
                continue;
            }

            var keyword = token.GetNextToken(includeDirectives: true);
            if (!keyword.IsKind(SyntaxKind.None) && !keyword.IsMissing && keyword.SpanStart > token.Span.End)
            {
                yield return (keyword, TextSpan.FromBounds(token.Span.End, keyword.SpanStart));
            }
        }
    }
}
