using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Ordering;

/// <summary>
/// A '//' comment that probably introduces a group of members rather than the one below it: the next member is of the
/// same kind ('// Test interfaces and classes' above two interfaces and the classes after them, Scrutor). The sort moves a
/// comment with its member, so it would end up above part of the group. The container is skipped when the sort would
/// move such a member. Judged by kinds, not blank lines: other rules add and remove blank lines between members in the
/// same 'dotnet format' run (BRO1505, BRO1506, BRO1509), so a blank-line test would decide differently on a second run.
/// </summary>
internal static class GroupComments
{
    /// <summary>Whether the sort (<paramref name="order"/>: new index -> old index) moves a member led by a group comment.</summary>
    public static bool MovesGroupComment(SyntaxList<MemberDeclarationSyntax> members, MemberKey[] keys, int[] order)
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

        for (var i = 0; i + 1 < members.Count; i++)
        {
            if (keys[i + 1].Kind == keys[i].Kind && Moves(position, i) && HasComment(members[i]))
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

    /// <summary>Whether a '//' comment leads the member, after the last directive (what is above a region line stays in place).</summary>
    private static bool HasComment(MemberDeclarationSyntax member)
    {
        var found = false;
        foreach (var trivia in member.GetLeadingTrivia())
        {
            if (trivia.IsDirective)
            {
                found = false;
            }
            else if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                found = true;
            }
        }

        return found;
    }
}
