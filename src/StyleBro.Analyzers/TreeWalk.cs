using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace StyleBro.Analyzers;

/// <summary>
/// One walk per tree, shared by the analyzers that look at every token, trivia or node of a file. Walking all of
/// Newtonsoft.Json's tokens costs about 35 ms; reading the cached array about 2 ms, so about ten analyzers that each
/// walked the tree paid that ten times (scripts/benchmark/README.md). Same items in the same order as
/// <c>DescendantTokens()</c>, <c>DescendantTrivia()</c> and <c>DescendantNodes()</c>; kept while the tree lives.
/// </summary>
internal static class TreeWalk
{
    private static readonly ConditionalWeakTable<SyntaxNode, SyntaxToken[]> TokenCache = new();
    private static readonly ConditionalWeakTable<SyntaxNode, SyntaxNode[]> NodeCache = new();

    /// <summary>The node's tokens, like <c>DescendantTokens()</c>.</summary>
    public static IReadOnlyList<SyntaxToken> Tokens(SyntaxNode root) => TokenCache.GetValue(root, r => r.DescendantTokens().ToArray());

    /// <summary>The node's trivia, like <c>DescendantTrivia()</c>: each token's leading, then its trailing trivia.</summary>
    public static IEnumerable<SyntaxTrivia> Trivia(SyntaxNode root)
    {
        foreach (var token in Tokens(root))
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                yield return trivia;
            }

            foreach (var trivia in token.TrailingTrivia)
            {
                yield return trivia;
            }
        }
    }

    /// <summary>The node's descendant nodes, like <c>DescendantNodes()</c>.</summary>
    public static IReadOnlyList<SyntaxNode> Nodes(SyntaxNode root) => NodeCache.GetValue(root, r => r.DescendantNodes().ToArray());
}
