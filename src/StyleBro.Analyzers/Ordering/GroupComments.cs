using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// A '//' comment that probably introduces a group of members rather than the one below it: a blank line separates it
/// from that member, or the next member follows that member without a blank line ('// Test interfaces and classes'
/// above two interfaces and the classes after them, Scrutor). The sort moves a comment with its member, so it would
/// end up above part of the group. The container is skipped when the sort would move such a member.
/// </summary>
internal static class GroupComments
{
    /// <summary>Whether the sort (<paramref name="order"/>: new index -> old index) moves a member led by a group comment.</summary>
    public static bool MovesGroupComment(SyntaxList<MemberDeclarationSyntax> members, int[] order)
    {
        var position = new int[order.Length];
        var moved = false;
        for (var i = 0; i < order.Length; i++)
        {
            position[order[i]] = i;
            moved |= order[i] != i;
        }

        if (!moved)
        {
            return false;
        }

        for (var i = 0; i < members.Count; i++)
        {
            if (Moves(position, i) && HasGroupComment(members, i))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether any other member changes sides: before the member, and after it once sorted, or the other way.</summary>
    private static bool Moves(int[] position, int i)
    {
        for (var j = 0; j < position.Length; j++)
        {
            if (j != i && (j < i) != (position[j] < position[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasGroupComment(SyntaxList<MemberDeclarationSyntax> members, int i)
    {
        // The last '//' comment after the last directive: what is above a directive (a region line) stays in place.
        var trivia = members[i].GetLeadingTrivia();
        var comment = -1;
        for (var k = 0; k < trivia.Count; k++)
        {
            if (trivia[k].IsDirective)
            {
                comment = -1;
            }
            else if (trivia[k].IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                comment = k;
            }
        }

        if (comment < 0)
        {
            return false;
        }

        var lineBreaks = 0;
        for (var k = comment + 1; k < trivia.Count; k++)
        {
            lineBreaks += trivia[k].IsKind(SyntaxKind.EndOfLineTrivia) ? 1 : 0;
        }

        return lineBreaks > 1 || (i + 1 < members.Count && !HasBlankLine(members[i + 1].GetLeadingTrivia()));
    }

    /// <summary>Whether the trivia holds a blank line: a line break with nothing but whitespace since the previous one or the start.</summary>
    private static bool HasBlankLine(SyntaxTriviaList trivia)
    {
        var lineStart = true;
        foreach (var t in trivia)
        {
            if (t.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                if (lineStart)
                {
                    return true;
                }

                lineStart = true;
            }
            else if (!t.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                lineStart = false;
            }
        }

        return false;
    }
}
