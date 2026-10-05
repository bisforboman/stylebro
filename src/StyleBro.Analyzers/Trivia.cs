using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers;

/// <summary>Checks on the trivia inside a span that a fix rewrites.</summary>
internal static class Trivia
{
    /// <summary>Whether every trivia of the node inside the span is whitespace or a line break (no comment or directive).</summary>
    public static bool IsBlank(SyntaxNode node, TextSpan span) =>
        node.DescendantTrivia(span).All(t => !span.Contains(t.Span) || t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));
}
