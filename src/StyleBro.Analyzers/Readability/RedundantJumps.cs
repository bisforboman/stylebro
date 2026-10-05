using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1137: a <c>return;</c> or <c>yield break;</c> that ends a body does nothing.</summary>
internal static class RedundantJumps
{
    /// <summary>
    /// The edit that removes the statement, or null when it isn't redundant or can't be removed safely. Redundant: a
    /// <c>return;</c> without a value, or a <c>yield break;</c>, as the last statement of the block that is the body of a
    /// method, constructor, destructor, operator, accessor, local function, lambda or anonymous method (a last statement of a
    /// nested block isn't: code after the block would run). Skipped: a <c>yield break;</c> that is the body's only yield
    /// (it makes the method an iterator), and a comment or directive on the statement (a directive that could make another
    /// statement last is always in its leading trivia). A labeled statement isn't directly in the block, so it's never reported.
    /// </summary>
    public static TextChange? GetChange(StatementSyntax statement, SourceText text)
    {
        if (statement is not (ReturnStatementSyntax { Expression: null } or YieldStatementSyntax { RawKind: (int)SyntaxKind.YieldBreakStatement })
            || statement.Parent is not BlockSyntax block
            || block.Statements.Last() != statement
            || block.Parent is not (BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)
            || statement.DescendantTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia))
            || (statement is YieldStatementSyntax && !block.DescendantNodes(n => n == block || !IsFunction(n)).OfType<YieldStatementSyntax>().Skip(1).Any()))
        {
            return null;
        }

        var previous = statement.GetFirstToken().GetPreviousToken();
        var next = statement.GetLastToken().GetNextToken();
        var lineStart = text.Lines.GetLineFromPosition(statement.SpanStart).Start;
        var line = text.Lines.GetLineFromPosition(statement.Span.End);
        var aloneOnItsLines = IsBlank(text, lineStart, statement.SpanStart) && IsBlank(text, statement.Span.End, line.End)
            && text.Lines.GetLineFromPosition(next.SpanStart).LineNumber > line.LineNumber;
        if (!aloneOnItsLines)
        {
            // '{ return; }', or code on the same line as the statement: remove it with the spaces before it.
            var start = statement.SpanStart;
            while (start > previous.Span.End && text[start - 1] is ' ' or '\t')
            {
                start--;
            }

            return new TextChange(TextSpan.FromBounds(start, statement.Span.End), string.Empty);
        }

        // The statement's lines, and the blank lines above them: a blank line before the body's '}' would be left.
        var first = text.Lines.GetLineFromPosition(lineStart).LineNumber;
        while (first > 0 && text.Lines[first - 1].Start > previous.Span.End && IsBlank(text, text.Lines[first - 1].Start, text.Lines[first - 1].End))
        {
            first--;
        }

        return new TextChange(TextSpan.FromBounds(text.Lines[first].Start, line.EndIncludingLineBreak), string.Empty);
    }

    private static bool IsFunction(SyntaxNode node) => node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;

    private static bool IsBlank(SourceText text, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (text[i] is not (' ' or '\t'))
            {
                return false;
            }
        }

        return true;
    }
}
