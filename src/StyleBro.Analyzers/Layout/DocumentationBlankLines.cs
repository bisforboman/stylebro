using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1511 (StyleCop SA1506: no blank line between a documentation comment and its element),
/// BRO1512 (SA1511: no blank line before the 'while' of 'do ... while') and BRO1513 (SA1514: a blank line before a
/// documentation comment).
/// </summary>
internal static class DocumentationBlankLines
{
    /// <summary>The member's '///' documentation comment, or default.</summary>
    public static SyntaxTrivia GetDocumentation(MemberDeclarationSyntax member) =>
        member.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

    /// <summary>
    /// BRO1511: the blank lines between the documentation comment and the element (its first attribute or keyword),
    /// including blank lines between two '///' blocks of one comment, or null. Only whitespace and documentation may
    /// sit there; a comment or directive is left alone. The edit removes all of them; the span starts at the first.
    /// </summary>
    public static (TextSpan First, IReadOnlyList<TextChange> Changes)? GetBlankLinesAfterDocumentation(MemberDeclarationSyntax member, SourceText text)
    {
        var documentation = GetDocumentation(member);
        if (documentation.RawKind == 0)
        {
            return null;
        }

        var trivia = member.GetLeadingTrivia();
        var after = trivia.Skip(trivia.IndexOf(documentation)).ToList();
        if (!after.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia) || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
        {
            return null;
        }

        // Whole blank lines from the documentation's first line to the element's line.
        var changes = new List<TextChange>();
        var from = text.Lines.GetLineFromPosition(documentation.FullSpan.Start).LineNumber + 1;
        var to = text.Lines.GetLineFromPosition(member.SpanStart).LineNumber;
        for (var line = from; line < to; line++)
        {
            if (text.ToString(text.Lines[line].Span).Trim().Length == 0)
            {
                changes.Add(new TextChange(text.Lines[line].SpanIncludingLineBreak, string.Empty));
            }
        }

        return changes.Count == 0 ? null : (changes[0].Span, changes);
    }

    /// <summary>BRO1512: the blank lines before the 'while' of a 'do ... while', or null.</summary>
    public static TextSpan? GetBlankLinesBeforeWhile(DoStatementSyntax statement, SourceText text)
    {
        var before = statement.WhileKeyword.GetPreviousToken();
        if (!before.TrailingTrivia.Concat(statement.WhileKeyword.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        var from = text.Lines.GetLineFromPosition(before.SpanStart).LineNumber + 1;
        var to = text.Lines.GetLineFromPosition(statement.WhileKeyword.SpanStart).LineNumber;
        return to > from ? TextSpan.FromBounds(text.Lines[from].Start, text.Lines[to].Start) : null;
    }

    /// <summary>
    /// BRO1513: where a line break goes (the start of the documentation comment's first line) when the line above it
    /// isn't blank, or null. Not reported, like StyleCop: right after an opening brace or at the start of the file. Not
    /// reported either (unlike StyleCop): right below a comment, where the blank line would break BRO1506's "no blank
    /// line below a comment" and the two fixes would undo each other.
    /// </summary>
    public static int? GetMissingBlankLine(MemberDeclarationSyntax member, SourceText text)
    {
        var documentation = GetDocumentation(member);
        if (documentation.RawKind == 0)
        {
            return null;
        }

        // A documentation comment's Span starts after its first '///'; FullSpan starts at it.
        var line = text.Lines.GetLineFromPosition(documentation.FullSpan.Start);
        return text.ToString(TextSpan.FromBounds(line.Start, documentation.FullSpan.Start)).Trim().Length == 0
            && WantsBlankLineAbove(line, text)
            ? line.Start
            : null;
    }

    /// <summary>
    /// Whether a documentation comment starting <paramref name="line"/> wants a blank line above it (BRO1513): the line
    /// above holds code. Also asked by BRO1601's fix, which inserts '/// &lt;inheritdoc/&gt;' and adds the blank line
    /// in the same run.
    /// </summary>
    public static bool WantsBlankLineAbove(TextLine line, SourceText text)
    {
        if (line.LineNumber == 0)
        {
            return false;
        }

        var above = text.ToString(text.Lines[line.LineNumber - 1].Span).Trim();

        // Like StyleCop, a directive above counts as code only when it ends or marks a block (#endif, #region,
        // #endregion); after #if, #else or #pragma the documentation is the first thing in its block.
        var directive = above.StartsWith("#") ? above.TrimStart('#').TrimStart() : null;
        if (above.Length == 0 || above.EndsWith("{") || above.StartsWith("//") || above.StartsWith("/*") || above.EndsWith("*/")
            || (directive is not null && !(directive.StartsWith("endif") || directive.StartsWith("region") || directive.StartsWith("endregion"))))
        {
            return false;
        }

        return true;
    }
}
