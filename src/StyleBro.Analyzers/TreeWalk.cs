using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StyleBro.Analyzers;

/// <summary>
/// One walk per tree, shared by the analyzers that look at every token, trivia or node of a file. Walking all of
/// Newtonsoft.Json's tokens costs about 35 ms; reading the cached array about 2 ms, so about ten analyzers that each
/// walked the tree paid that ten times (scripts/benchmark/README.md). Same items in the same order as
/// <c>DescendantTokens()</c> and <c>DescendantNodes()</c> (trivia: without whitespace); kept while the tree lives.
/// </summary>
internal static class TreeWalk
{
    private static readonly ConditionalWeakTable<SyntaxNode, Walk> WalkCache = new();
    private static readonly ConditionalWeakTable<SyntaxNode, SyntaxTrivia[]> TriviaCache = new();

    /// <summary>The node's tokens, like <c>DescendantTokens()</c>.</summary>
    public static IReadOnlyList<SyntaxToken> Tokens(SyntaxNode root) => WalkCache.GetValue(root, r => new Walk(r)).Tokens;

    /// <summary>
    /// The node's trivia except whitespace and line breaks (comments, documentation, directives, disabled code, skipped
    /// tokens), in the order of <c>DescendantTrivia()</c>. Every analyzer that walks trivia only wants these, and most
    /// trivia of a file are whitespace and line breaks.
    /// </summary>
    public static IReadOnlyList<SyntaxTrivia> Trivia(SyntaxNode root) => TriviaCache.GetValue(root, GetTrivia);

    /// <summary>The node's descendant nodes, like <c>DescendantNodes()</c>.</summary>
    public static IReadOnlyList<SyntaxNode> Nodes(SyntaxNode root) => WalkCache.GetValue(root, r => new Walk(r)).Nodes;

    private static SyntaxTrivia[] GetTrivia(SyntaxNode root)
    {
        var result = new List<SyntaxTrivia>();
        foreach (var token in Tokens(root))
        {
            Add(token.LeadingTrivia, result);
            Add(token.TrailingTrivia, result);
        }

        return result.ToArray();
    }

    private static void Add(SyntaxTriviaList list, List<SyntaxTrivia> result)
    {
        foreach (var trivia in list)
        {
            if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                result.Add(trivia);
            }
        }
    }

    /// <summary>Tokens and nodes from one walk: every tree that needs one also needs the other, and a walk is the cost.</summary>
    private sealed class Walk
    {
        public Walk(SyntaxNode root)
        {
            var tokens = new List<SyntaxToken>();
            var nodes = new List<SyntaxNode>();
            foreach (var item in root.DescendantNodesAndTokens())
            {
                if (item.IsToken)
                {
                    tokens.Add(item.AsToken());
                }
                else
                {
                    nodes.Add(item.AsNode()!);
                }
            }

            Tokens = tokens.ToArray();
            Nodes = nodes.ToArray();
        }

        public SyntaxToken[] Tokens { get; }

        public SyntaxNode[] Nodes { get; }
    }
}
