using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1517 (SA1507) no two blank lines in a row, BRO1518 (SA1508) no blank line before '}', BRO1519 (SA1513) a blank
/// line after '}'. Which places count follows StyleCop; a place another StyleBro blank-line rule already fixes is left to
/// that rule when it's on (after '{': BRO1503; below a comment: BRO1506; a comment, documentation or a member after
/// '}': BRO1504, BRO1513, BRO1505), so two fixes never edit the same lines.
/// </summary>
internal static class BlankLineRuns
{
    /// <summary>Every finding in the tree: the rule, the diagnostic's span, and the edit that fixes it.</summary>
    public static IEnumerable<(string Id, TextSpan Location, TextChange Change)> GetFindings(SyntaxNode root, SourceText text, Func<string, bool> isOn)
    {
        // Asked per token: read each rule's severity once per file.
        var known = new Dictionary<string, bool>();
        var lookup = isOn;
        isOn = id => known.TryGetValue(id, out var on) ? on : known[id] = lookup(id);

        if (TreeWalk.Tokens(root).All(t => t.IsKind(SyntaxKind.EndOfFileToken)) && text.ToString().Trim().Length == 0)
        {
            yield break;
        }

        var (multipleOn, beforeOn, afterOn) = (isOn(DiagnosticIds.MultipleBlankLines), isOn(DiagnosticIds.BlankLineBeforeCloseBrace), isOn(DiagnosticIds.BlankLineAfterCloseBrace));
        foreach (var token in TreeWalk.Tokens(root))
        {
            if (multipleOn)
            {
                foreach (var multiple in MultipleBlankLines(token, text, isOn))
                {
                    yield return (DiagnosticIds.MultipleBlankLines, multiple.Location, multiple.Change);
                }
            }

            if (token.IsKind(SyntaxKind.CloseBraceToken))
            {
                if (beforeOn && BlankLinesBeforeCloseBrace(token, text, isOn) is { } before)
                {
                    yield return (DiagnosticIds.BlankLineBeforeCloseBrace, token.Span, before);
                }

                if (afterOn && MissingBlankLineAfterCloseBrace(token, text, isOn) is { } after)
                {
                    yield return (DiagnosticIds.BlankLineAfterCloseBrace, after.Location, after.Change);
                }
            }
        }
    }

    /// <summary>
    /// Whether BRO1519 wants a blank line between a '}' and the next token on a later line, apart from where the '}' is:
    /// also used by fixes that create such a '}' (BRO1514 wrapping a statement, BRO1508/BRO1509 expanding a one-line
    /// block), so they add the blank line in the same run. <paramref name="owner"/> is what the '}' closes (the
    /// statement for a block a fix is about to add), <paramref name="brace"/> the '}' when it exists already.
    /// <paramref name="gapIsReplaced"/>: the caller rewrites everything between the two tokens, so blank lines already
    /// there don't count.
    /// </summary>
    public static bool WantsBlankLineAfter(SyntaxNode owner, SyntaxToken brace, SyntaxToken next, Func<string, bool> isOn, bool gapIsReplaced = false)
    {
        if (!isOn(DiagnosticIds.BlankLineAfterCloseBrace)
            || owner.IsKind(SyntaxKind.Interpolation)
            || owner is DoStatementSyntax or BlockSyntax { Parent: DoStatementSyntax }
            || next.IsKind(SyntaxKind.EndOfFileToken))
        {
            return false;
        }

        // Only what follows with nothing but indentation before it: comments, documentation and directives there are
        // other rules' (BRO1504, BRO1513) or exempt.
        if (!next.LeadingTrivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || (gapIsReplaced && t.IsKind(SyntaxKind.EndOfLineTrivia))))
        {
            return false;
        }

        if (next.Kind() is SyntaxKind.DotToken or SyntaxKind.CloseBraceToken or SyntaxKind.CatchKeyword or SyntaxKind.FinallyKeyword
            or SyntaxKind.ElseKeyword or SyntaxKind.CommaToken or SyntaxKind.CloseParenToken or SyntaxKind.ColonToken
            or SyntaxKind.EqualsGreaterThanToken or SyntaxKind.AmpersandAmpersandToken or SyntaxKind.BarBarToken
            or SyntaxKind.CloseBracketToken or SyntaxKind.AddKeyword or SyntaxKind.RemoveKeyword or SyntaxKind.GetKeyword
            or SyntaxKind.SetKeyword or SyntaxKind.InitKeyword)
        {
            return false;
        }

        if (next.Kind() is SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword && next.Parent is AccessorDeclarationSyntax)
        {
            return false;
        }

        if (owner.AncestorsAndSelf().Any(a => a is QueryExpressionSyntax)
            && next.Parent is QueryClauseSyntax or SelectOrGroupClauseSyntax or QueryContinuationSyntax or JoinIntoClauseSyntax)
        {
            return false;
        }

        if (next.IsKind(SyntaxKind.SemicolonToken)
            && owner.AncestorsAndSelf().Any(a => a is VariableDeclaratorSyntax or YieldStatementSyntax or ArrowExpressionClauseSyntax
                or EqualsValueClauseSyntax or AssignmentExpressionSyntax or ReturnStatementSyntax or ThrowStatementSyntax or ObjectCreationExpressionSyntax))
        {
            return false;
        }

        if (next.IsKind(SyntaxKind.IdentifierToken)
            && owner.FirstAncestorOrSelf<RecursivePatternSyntax>() is { } pattern
            && next.Parent!.FirstAncestorOrSelf<RecursivePatternSyntax>() == pattern)
        {
            return false;
        }

        // A '}' that ends a member, type or namespace followed by the next one: BRO1505's (element separation).
        return !(brace.RawKind != 0 && isOn(DiagnosticIds.ElementsSeparatedByBlankLine)
            && owner.AncestorsAndSelf().Any(a => a is MemberDeclarationSyntax m && m.GetLastToken() == brace));
    }

    /// <summary>
    /// Whether BRO1519 judges the gap between this '}' and the next token: the block spans lines, the '}' ends its line
    /// with no comment after it, and <see cref="WantsBlankLineAfter"/>. With <paramref name="gapIsReplaced"/> blank lines
    /// already in the gap don't count, so it also says where BRO1519 keeps a blank line (BRO1526 leaves those places to it).
    /// </summary>
    public static bool JudgesGapAfter(SyntaxToken brace, SyntaxToken next, SourceText text, Func<string, bool> isOn, bool gapIsReplaced)
    {
        // The cheap checks first: most '}' are followed by another '}' (WantsBlankLineAfter's first checks).
        return brace.TrailingTrivia.LastOrDefault(t => !t.IsKind(SyntaxKind.WhitespaceTrivia)) is { RawKind: (int)SyntaxKind.EndOfLineTrivia }
            && !brace.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
            && WantsBlankLineAfter(brace.Parent!, brace, next, isOn, gapIsReplaced)
            && OpeningLine(brace, text) != text.Lines.GetLineFromPosition(brace.SpanStart).LineNumber;
    }

    /// <summary>
    /// SA1507: every run of two or more blank lines in a token's leading trivia (several when directives or comments
    /// separate them, e.g. empty regions in a row); the fix keeps one blank line of each.
    /// </summary>
    private static IEnumerable<(TextSpan Location, TextChange Change)> MultipleBlankLines(SyntaxToken token, SourceText text, Func<string, bool> isOn)
    {
        if (token.IsKind(SyntaxKind.EndOfFileToken))
        {
            // Blank lines at the end are BRO1507's.
            yield break;
        }

        var trivia = token.LeadingTrivia;
        var start = 0;
        var end = -1;
        var count = 0;
        for (var i = 0; i <= trivia.Count; i++)
        {
            var kind = i < trivia.Count ? trivia[i].Kind() : SyntaxKind.None;
            if (kind == SyntaxKind.WhitespaceTrivia)
            {
                continue;
            }

            if (kind == SyntaxKind.EndOfLineTrivia)
            {
                end = i;
                count++;
                continue;
            }

            if (Run(token, trivia, start, end, count, text, isOn) is { } found)
            {
                yield return found;
            }

            start = i + 1;
            count = 0;
        }
    }

    private static (TextSpan Location, TextChange Change)? Run(SyntaxToken token, SyntaxTriviaList trivia, int start, int end, int count, SourceText text, Func<string, bool> isOn)
    {
        if (end <= start)
        {
            return null;
        }

        var afterComment = start > 0 && trivia[start - 1].Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia;
        if (afterComment)
        {
            // The first line break ends the comment's line.
            start++;
            count--;

            // Blank lines below a '//' comment are BRO1506's.
            if (trivia[start - 2].IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment))
            {
                return null;
            }
        }

        if (count < 2 || trivia[start].SpanStart == 0)
        {
            // Blank lines at the start of the file aren't SA1507's.
            return null;
        }

        // Directly after '{' (BRO1503) or before '}' (BRO1518): those rules remove all of them.
        var nothingBefore = start == 0 && !afterComment;
        var nothingAfter = trivia.Skip(end + 1).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
        if ((nothingBefore && token.GetPreviousToken().IsKind(SyntaxKind.OpenBraceToken) && isOn(DiagnosticIds.BlankLineAfterOpenBrace))
            || (nothingAfter && token.IsKind(SyntaxKind.CloseBraceToken) && isOn(DiagnosticIds.BlankLineBeforeCloseBrace)))
        {
            return null;
        }

        var first = text.Lines.GetLineFromPosition(trivia[start].SpanStart);
        var last = text.Lines.GetLineFromPosition(trivia[end].SpanStart);
        var location = TextSpan.FromBounds(trivia[start].SpanStart, trivia[end].Span.End);
        return (location, new TextChange(TextSpan.FromBounds(first.EndIncludingLineBreak, last.EndIncludingLineBreak), string.Empty));
    }

    /// <summary>SA1508: blank lines between '}' and what comes before it; the fix removes them.</summary>
    private static TextChange? BlankLinesBeforeCloseBrace(SyntaxToken brace, SourceText text, Func<string, bool> isOn)
    {
        // A blank line needs a line break in the brace's leading trivia (the previous token's trailing trivia ends at its
        // first one): most braces have none, and this skips finding the previous token.
        if (!brace.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia))
        {
            return null;
        }

        var previous = brace.GetPreviousToken();
        if (previous.IsKind(SyntaxKind.None))
        {
            return null;
        }

        // Back from the brace over whitespace and line breaks, to the last comment, directive or token.
        var separating = previous.TrailingTrivia.Concat(brace.LeadingTrivia).ToList();
        var index = separating.Count - 1;
        while (index >= 0 && separating[index].Kind() is SyntaxKind.WhitespaceTrivia or SyntaxKind.EndOfLineTrivia)
        {
            index--;
        }

        var stop = index >= 0 ? separating[index] : default;
        if (stop.IsKind(SyntaxKind.SingleLineCommentTrivia) && isOn(DiagnosticIds.BlankLineAfterComment))
        {
            // Below a '//' comment: BRO1506's.
            return null;
        }

        if (index < 0 && previous.IsKind(SyntaxKind.OpenBraceToken) && isOn(DiagnosticIds.BlankLineAfterOpenBrace))
        {
            // '{', blank lines, '}': BRO1503's.
            return null;
        }

        // Like StyleCop, count the line breaks between, not the lines: a directive ends with its own line break, so one blank
        // line after '#endif' isn't reported (OpenTelemetry enforces SA1508 and has those).
        if (separating.Skip(index + 1).Count(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) < 2)
        {
            return null;
        }

        var stopEnd = index >= 0 ? stop.Span.End : previous.Span.End;
        var stopLine = text.Lines.GetLineFromPosition(stopEnd).LineNumber;
        var braceLine = text.Lines.GetLineFromPosition(brace.SpanStart).LineNumber;
        if (braceLine - stopLine < 2)
        {
            return null;
        }

        for (var i = stopLine + 1; i < braceLine; i++)
        {
            if (text.ToString(text.Lines[i].Span).Trim().Length != 0)
            {
                return null;
            }
        }

        return new TextChange(TextSpan.FromBounds(text.Lines[stopLine + 1].Start, text.Lines[braceLine].Start), string.Empty);
    }

    /// <summary>SA1513: no blank line between '}' and the next statement; the fix adds one.</summary>
    private static (TextSpan Location, TextChange Change)? MissingBlankLineAfterCloseBrace(SyntaxToken brace, SourceText text, Func<string, bool> isOn)
    {
        var next = brace.GetNextToken(includeZeroWidth: true, includeSkipped: true);
        if (!JudgesGapAfter(brace, next, text, isOn, gapIsReplaced: false))
        {
            return null;
        }

        var eol = SingleLineBlocks.LineBreak(text, brace.SpanStart);
        return (TextSpan.FromBounds(brace.Span.End, next.FullSpan.Start), new TextChange(new TextSpan(next.FullSpan.Start, 0), eol));
    }

    private static int OpeningLine(SyntaxToken brace, SourceText text)
    {
        var open = brace.Parent!.ChildTokens().FirstOrDefault(t => t.IsKind(SyntaxKind.OpenBraceToken));
        return open.RawKind == 0 ? -1 : text.Lines.GetLineFromPosition(open.Span.End).LineNumber;
    }
}
