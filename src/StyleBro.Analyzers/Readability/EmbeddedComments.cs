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
/// Shared logic for BRO1132 (SA1108): comments between a statement's header and the '{' of its block, and BRO1134
/// (StyleCop issue #605): the same between a declaration's header and its '{'. Which blocks and which comments are
/// StyleCop's: the blocks of if/else/for/foreach/while/do/lock/try/catch/finally/checked/unchecked/fixed and a switch
/// statement's braces; '//' (not '////') and '/* */' comments in the trailing trivia of the token before '{' or in the
/// leading trivia of '{'. The fix moves them into the block, each on its own line right after '{'.
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
    /// BRO1134: the '{' of a type, a block-scoped namespace, a property's, indexer's or event's accessor list, or the body
    /// of a method, constructor, operator, finalizer, accessor or local function; or default.
    /// </summary>
    public static SyntaxToken GetDeclarationOpenBrace(SyntaxNode node) => node switch
    {
        BaseTypeDeclarationSyntax type => type.OpenBraceToken,
        NamespaceDeclarationSyntax ns => ns.OpenBraceToken,
        AccessorListSyntax accessors => accessors.OpenBraceToken,
        BlockSyntax block when block.Parent is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax => block.OpenBraceToken,
        _ => default,
    };

    /// <summary>
    /// The comments to move, empty when there are none or the fix can't move them safely (for a declaration, a comment
    /// ending the header's line above '{' isn't one to move): a header spanning several lines
    /// (the comment usually explains its last line, not the block: a user decision, see docs/decisions.md), a directive
    /// between the header and '{', a comment spanning lines, or code after '{' on its line (a single-line block, nowhere
    /// to put a line).
    /// </summary>
    public static ImmutableArray<SyntaxTrivia> GetComments(SyntaxToken openBrace, SourceText text)
    {
        if (!openBrace.IsKind(SyntaxKind.OpenBraceToken) || openBrace.IsMissing)
        {
            return ImmutableArray<SyntaxTrivia>.Empty;
        }

        var previous = openBrace.GetPreviousToken();
        if (previous.IsMissing || !(previous.TrailingTrivia.Any(IsComment) || openBrace.LeadingTrivia.Any(IsComment)))
        {
            return ImmutableArray<SyntaxTrivia>.Empty;
        }

        // The statement, clause or declaration that owns the block; its first token after any attributes starts the header
        // ('else', 'catch', the 'if' of an 'else if', 'switch', a modifier, 'class', ...).
        var owner = (openBrace.Parent is BlockSyntax or AccessorListSyntax ? openBrace.Parent.Parent : openBrace.Parent)!;
        var gap = previous.TrailingTrivia.Concat(openBrace.LeadingTrivia).ToList();
        var comments = gap.Where(IsComment).ToImmutableArray();

        // BRO1134: a comment that ends the header's line, with '{' on a later line, is a note on the header and stays
        // (a user decision, see docs/decisions.md); comments on lines of their own still move.
        if (GetDeclarationOpenBrace(openBrace.Parent!) == openBrace && Line(text, openBrace.SpanStart) != Line(text, previous.SpanStart))
        {
            comments = comments.RemoveAll(c => previous.TrailingTrivia.Contains(c));
        }

        if (comments.IsEmpty
            || Line(text, GetHeaderStart(owner).SpanStart) != Line(text, previous.SpanStart)
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
    /// after '{' (below any blank lines there), one per line, indented like the block's first line (or one unit deeper
    /// than '{' in an empty block).
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

        // Below blank lines right after '{': those are BRO1503's to remove; above them, the comment would get BRO1506's
        // "no blank line below a comment" instead, which a run that already applied BRO1506 leaves behind.
        var after = braceLine;
        while (text.Lines[after.LineNumber + 1] is var line && line.Start < next.SpanStart && text.ToString(line.Span).Trim().Length == 0)
        {
            after = line;
        }

        yield return new TextChange(new TextSpan(after.End, 0), insert.ToString());
    }

    /// <summary>
    /// The rule whose fix moves this comment (BRO1132 or BRO1134), or null. BRO1504 leaves such a comment alone while that
    /// rule is on: its blank line above a comment on its own line before '{' would stay behind as a blank line before '{'
    /// once the comment moves.
    /// </summary>
    public static string? GetMovingRule(SyntaxTrivia comment, SourceText text)
    {
        var brace = GetOpenBrace(comment);
        return !GetComments(brace, text).Contains(comment) ? null
            : GetOpenBrace(brace.Parent!) == brace ? DiagnosticIds.EmbeddedComment
            : DiagnosticIds.DeclarationComment;
    }

    /// <summary>The checked '{' (a statement's or a declaration's) whose gap holds this trivia (trailing the token before it or leading it), or default.</summary>
    public static SyntaxToken GetOpenBrace(SyntaxTrivia trivia)
    {
        var brace = trivia.Token.IsKind(SyntaxKind.OpenBraceToken) && trivia.SpanStart < trivia.Token.SpanStart
            ? trivia.Token
            : trivia.Token.GetNextToken();
        return brace.Parent is { } parent && (GetOpenBrace(parent) == brace || GetDeclarationOpenBrace(parent) == brace) ? brace : default;
    }

    /// <summary>The first token of a header: the owner's first token after its attribute lists.</summary>
    private static SyntaxToken GetHeaderStart(SyntaxNode owner)
    {
        var token = owner.GetFirstToken();
        while (token.Parent is AttributeListSyntax list && list.Parent == owner)
        {
            token = list.GetLastToken().GetNextToken();
        }

        return token;
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
