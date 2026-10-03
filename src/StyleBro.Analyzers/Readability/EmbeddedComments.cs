using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1132 (SA1108): comments between a statement's header and the '{' of its block. Which blocks and
/// which comments are StyleCop's: the blocks of if/else/for/foreach/while/do/lock/try/catch/finally/checked/unchecked/
/// fixed and a switch statement's braces; '//' (not '////') and '/* */' comments in the trailing trivia of the token
/// before '{' or in the leading trivia of '{'. The fix moves them into the block, each on its own line right after '{'.
/// </summary>
internal static class EmbeddedComments
{
    private static readonly SyntaxKind[] BlockOwners =
    {
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForStatement,
        SyntaxKind.WhileStatement,
        SyntaxKind.DoStatement,
        SyntaxKind.IfStatement,
        SyntaxKind.ElseClause,
        SyntaxKind.LockStatement,
        SyntaxKind.TryStatement,
        SyntaxKind.CatchClause,
        SyntaxKind.FinallyClause,
        SyntaxKind.CheckedStatement,
        SyntaxKind.UncheckedStatement,
        SyntaxKind.FixedStatement,
    };

    /// <summary>The '{' of a checked block or switch statement, or default.</summary>
    public static SyntaxToken GetOpenBrace(SyntaxNode node) => node switch
    {
        BlockSyntax block when block.Parent is not null && BlockOwners.Any(block.Parent.IsKind) => block.OpenBraceToken,
        SwitchStatementSyntax switchStatement => switchStatement.OpenBraceToken,
        _ => default,
    };

    /// <summary>
    /// The comments to move, empty when there are none or the fix can't move them safely: a directive between the header
    /// and '{', a comment spanning lines, or code after '{' on its line (a single-line block, nowhere to put a line).
    /// </summary>
    public static ImmutableArray<SyntaxTrivia> GetComments(SyntaxToken openBrace, SourceText text)
    {
        if (!openBrace.IsKind(SyntaxKind.OpenBraceToken) || openBrace.IsMissing)
        {
            return ImmutableArray<SyntaxTrivia>.Empty;
        }

        var previous = openBrace.GetPreviousToken();
        if (previous.IsMissing)
        {
            return ImmutableArray<SyntaxTrivia>.Empty;
        }

        var gap = previous.TrailingTrivia.Concat(openBrace.LeadingTrivia).ToList();
        var comments = gap.Where(IsComment).ToImmutableArray();
        if (comments.IsEmpty
            || gap.Any(t => t.IsDirective)
            || comments.Any(c => Line(text, c.Span.Start) != Line(text, c.Span.End))
            || Line(text, openBrace.GetNextToken().SpanStart) == Line(text, openBrace.SpanStart))
        {
            return ImmutableArray<SyntaxTrivia>.Empty;
        }

        return comments;
    }

    /// <summary>
    /// Removes the comments where they are (a line left blank goes completely, as in BRO1101's fix) and inserts them
    /// after '{', one per line, indented like the block's first line (or one unit deeper than '{' in an empty block).
    /// </summary>
    public static IEnumerable<TextChange> GetChanges(SyntaxToken openBrace, ImmutableArray<SyntaxTrivia> comments, SourceText text, string indentUnit)
    {
        foreach (var change in EmptyStatements.GetChanges(text, comments.Select(c => c.Span)))
        {
            yield return change;
        }

        var braceLine = text.Lines.GetLineFromPosition(openBrace.SpanStart);
        var next = openBrace.GetNextToken();
        var indent = next.IsKind(SyntaxKind.CloseBraceToken)
            ? Indent(text, braceLine) + indentUnit
            : Indent(text, text.Lines.GetLineFromPosition(next.SpanStart));
        var lineBreak = text.ToString(TextSpan.FromBounds(braceLine.End, braceLine.EndIncludingLineBreak));
        var insert = new StringBuilder();
        foreach (var comment in comments)
        {
            insert.Append(lineBreak).Append(indent).Append(comment.ToString());
        }

        yield return new TextChange(new TextSpan(braceLine.End, 0), insert.ToString());
    }

    /// <summary>
    /// Whether BRO1132's fix moves this comment. BRO1504 leaves such a comment alone while BRO1132 is on: its blank line
    /// above a comment on its own line before '{' would stay behind as a blank line before '{' once the comment moves.
    /// </summary>
    public static bool IsMoved(SyntaxTrivia comment, SourceText text) => GetComments(GetOpenBrace(comment), text).Contains(comment);

    /// <summary>The checked '{' whose gap holds this trivia (trailing the token before it or leading it), or default.</summary>
    public static SyntaxToken GetOpenBrace(SyntaxTrivia trivia)
    {
        var brace = trivia.Token.IsKind(SyntaxKind.OpenBraceToken) && trivia.SpanStart < trivia.Token.SpanStart
            ? trivia.Token
            : trivia.Token.GetNextToken();
        return brace.Parent is { } parent && GetOpenBrace(parent) == brace ? brace : default;
    }

    private static bool IsComment(SyntaxTrivia trivia) =>
        (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) && !trivia.ToString().StartsWith("////", System.StringComparison.Ordinal))
        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

    private static string Indent(SourceText text, TextLine line)
    {
        var lineText = text.ToString(line.Span);
        return lineText.Substring(0, lineText.Length - lineText.TrimStart(' ', '\t').Length);
    }
}
