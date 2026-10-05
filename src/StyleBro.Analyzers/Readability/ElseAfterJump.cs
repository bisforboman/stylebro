using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Layout;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// BRO1143 (Meziantou MA0071, Roslynator RCS1211): no 'else' after an 'if' branch that ends in a jump. The fix removes
/// the 'else' (and the braces of its block) and moves the body one level out, below the 'if' statement. Text edits, so
/// candidates nested in each other's 'else' body are fixed in one pass: each line is unindented once per 'else' body it
/// leaves.
/// </summary>
internal static class ElseAfterJump
{
    /// <summary>
    /// Whether the 'else' of this 'if' can go. Not for: an 'if' that isn't a statement of a block, 'else if' chains,
    /// empty 'else' blocks, comments or directives between the 'if' branch and the first statement of the 'else' body or
    /// between its last statement and '}', a body that spans lines but starts on the 'else' line, lines that aren't
    /// indented one level deeper than the 'if', strings or comments spanning lines, 'using' declarations (they would be
    /// disposed later), and names declared in the body that the enclosing block also uses (they would clash). Also not an
    /// 'if' branch without braces that BRO1516 wants braces on (the 'else' block makes the chain inconsistent): without
    /// the 'else' it would become BRO1514's, which may already have run; BRO1516 adds them, the next run removes the 'else'.
    /// </summary>
    public static bool IsCandidate(IfStatementSyntax node, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        if (node.Parent is not BlockSyntax block
            || node.Else is not { Statement: not IfStatementSyntax } elseClause
            || !EndsInJump(node.Statement)
            || node.ContainsDiagnostics
            || node.ContainsDirectives
            || GetBody(elseClause.Statement) is not { } body)
        {
            return false;
        }

        if (!OnlyWhitespace(elseClause.ElseKeyword.GetPreviousToken(), body.First.GetFirstToken())
            || (elseClause.Statement is BlockSyntax closing && !OnlyWhitespace(body.Last.GetLastToken(), closing.CloseBraceToken)))
        {
            return false;
        }

        if (node.Statement is not BlockSyntax && elseClause.Statement is BlockSyntax
            && Braces.GetFindings(node, text, isOn, Braces.AllowConsecutiveUsings(options), Braces.GetPreference(options), Braces.AllowSingleLineJumps(options))
                .Any(f => f.Child == node.Statement && f.Id == DiagnosticIds.BracesConsistent))
        {
            return false;
        }

        var unit = Indentation.GetUnit(options);
        var ifLine = text.Lines.GetLineFromPosition(node.SpanStart);
        var indent = Leading(text, ifLine);
        var firstLine = text.Lines.GetLineFromPosition(body.First.SpanStart);
        var lastLine = text.Lines.GetLineFromPosition(body.Last.Span.End).LineNumber;
        if (ifLine.Start + indent.Length != node.SpanStart)
        {
            return false;
        }

        if (firstLine.Start + Leading(text, firstLine).Length != body.First.SpanStart)
        {
            // The body starts on the 'else' (or '{') line: only when it ends there too.
            if (lastLine != firstLine.LineNumber)
            {
                return false;
            }
        }
        else
        {
            for (var i = firstLine.LineNumber; i <= lastLine; i++)
            {
                var line = text.Lines[i];
                if (!text.ToString(line.Span).StartsWith(indent + unit, StringComparison.Ordinal) && text.ToString(line.Span).Trim().Length != 0)
                {
                    return false;
                }
            }

            if (elseClause.Statement.DescendantTokens().Any(t => SpansLines(text, t.Span))
                || elseClause.Statement.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.MultiLineCommentTrivia) && SpansLines(text, t.Span)))
            {
                return false;
            }
        }

        var statements = elseClause.Statement is BlockSyntax b ? b.Statements : SyntaxFactory.SingletonList(elseClause.Statement);
        if (statements.Any(s => s is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not 0 }))
        {
            return false;
        }

        var declared = new HashSet<string>(DeclaredNames(elseClause.Statement));
        return declared.Count == 0
            || !block.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && !elseClause.Statement.FullSpan.Contains(t.SpanStart) && declared.Contains(t.ValueText));
    }

    /// <summary>
    /// The edits that remove the 'else' of every given candidate together: the gap from the 'if' branch to the body's first
    /// statement becomes a line break (and BRO1519's blank line after a '}' when it wants one), the '}' of an 'else' block
    /// goes, and every other line of the body loses one indentation unit per 'else' body it was in.
    /// </summary>
    public static List<TextChange> GetChanges(IReadOnlyList<IfStatementSyntax> candidates, SourceText text, string unit, Func<string, bool> isOn)
    {
        var items = candidates.Select(c => new Item(c, text)).ToList();
        var eol = items.Count == 0 ? "\n" : SingleLineBlocks.LineBreak(text, items[0].Node.SpanStart);
        int Levels(int line) => items.Count(i => i.Unindented(line));

        var owners = items.Where(i => i.StartsLine).GroupBy(i => i.FirstLine).ToDictionary(g => g.Key, g => g.First());
        string FinalIndent(int line)
        {
            if (owners.TryGetValue(line, out var owner))
            {
                return FinalIndent(owner.IfLine);
            }

            var lead = Leading(text, text.Lines[line]);
            return lead.Substring(0, Math.Max(0, lead.Length - (Levels(line) * unit.Length)));
        }

        var changes = new List<TextChange>();
        foreach (var item in items)
        {
            var blank = item.Node.Statement is BlockSyntax branch
                && text.Lines.GetLineFromPosition(branch.OpenBraceToken.SpanStart).LineNumber != text.Lines.GetLineFromPosition(branch.CloseBraceToken.SpanStart).LineNumber
                && BlankLineRuns.WantsBlankLineAfter(branch, branch.CloseBraceToken, item.First.GetFirstToken(), isOn, gapIsReplaced: true);
            changes.Add(new TextChange(item.Gap, eol + (blank ? eol : string.Empty) + FinalIndent(item.IfLine)));
            if (item.Close is { } close)
            {
                changes.Add(new TextChange(close, string.Empty));
            }
        }

        foreach (var line in items.SelectMany(i => Enumerable.Range(i.FirstLine + 1, Math.Max(0, i.LastLine - i.FirstLine))).Distinct())
        {
            var textLine = text.Lines[line];
            var lead = Leading(text, textLine);
            var levels = Levels(line) * unit.Length;

            // A line that starts inside a gap or a '}' edit (the 'else' line, the body's first line, the '}' line) gets its
            // indentation from that edit; its own edit overlaps and LinkedFileFixAllProvider.Merge drops it.
            if (levels > 0 && lead.Length != textLine.Span.Length && lead.Length >= levels)
            {
                changes.Add(new TextChange(new TextSpan(textLine.Start + lead.Length - levels, levels), string.Empty));
            }
        }

        return changes;
    }

    private static bool EndsInJump(StatementSyntax statement) => statement switch
    {
        BlockSyntax block => block.Statements.Count > 0 && EndsInJump(block.Statements.Last()),
        ReturnStatementSyntax or ThrowStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax or GotoStatementSyntax => true,
        YieldStatementSyntax yield => yield.IsKind(SyntaxKind.YieldBreakStatement),
        _ => false,
    };

    private static (StatementSyntax First, StatementSyntax Last)? GetBody(StatementSyntax statement) => statement switch
    {
        BlockSyntax { Statements.Count: 0 } => null,
        BlockSyntax block => (block.Statements.First(), block.Statements.Last()),
        _ => (statement, statement),
    };

    /// <summary>Only whitespace and line breaks in the trivia from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static bool OnlyWhitespace(SyntaxToken from, SyntaxToken to)
    {
        static bool Plain(SyntaxTriviaList trivia) => trivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));

        for (var token = from; token != to; token = token.GetNextToken())
        {
            if (!Plain(token.TrailingTrivia) || !Plain(token.GetNextToken().LeadingTrivia))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> DeclaredNames(StatementSyntax body)
    {
        foreach (var node in body.DescendantNodesAndSelf())
        {
            var name = node switch
            {
                VariableDeclaratorSyntax n => n.Identifier,
                SingleVariableDesignationSyntax n => n.Identifier,
                LocalFunctionStatementSyntax n => n.Identifier,
                ForEachStatementSyntax n => n.Identifier,
                CatchDeclarationSyntax n => n.Identifier,
                LabeledStatementSyntax n => n.Identifier,
                ParameterSyntax n => n.Identifier,
                FromClauseSyntax n => n.Identifier,
                LetClauseSyntax n => n.Identifier,
                JoinClauseSyntax n => n.Identifier,
                JoinIntoClauseSyntax n => n.Identifier,
                QueryContinuationSyntax n => n.Identifier,
                _ => default,
            };
            if (name.RawKind != 0)
            {
                yield return name.ValueText;
            }
        }
    }

    private static bool SpansLines(SourceText text, TextSpan span) =>
        text.Lines.GetLineFromPosition(span.Start).LineNumber != text.Lines.GetLineFromPosition(span.End).LineNumber;

    private static string Leading(SourceText text, TextLine line)
    {
        var end = line.Start;
        while (end < line.End && text[end] is ' ' or '\t')
        {
            end++;
        }

        return text.ToString(TextSpan.FromBounds(line.Start, end));
    }

    private sealed class Item
    {
        public Item(IfStatementSyntax node, SourceText text)
        {
            Node = node;
            var elseClause = node.Else!;
            var (first, last) = GetBody(elseClause.Statement)!.Value;
            First = first;
            Gap = TextSpan.FromBounds(elseClause.ElseKeyword.GetPreviousToken().Span.End, first.SpanStart);
            if (elseClause.Statement is BlockSyntax block)
            {
                Close = TextSpan.FromBounds(last.GetLastToken().Span.End, block.CloseBraceToken.Span.End);
            }

            IfLine = text.Lines.GetLineFromPosition(node.SpanStart).LineNumber;
            var firstLine = text.Lines.GetLineFromPosition(first.SpanStart);
            FirstLine = firstLine.LineNumber;
            LastLine = text.Lines.GetLineFromPosition(last.Span.End).LineNumber;
            StartsLine = firstLine.Start + Leading(text, firstLine).Length == first.SpanStart;
        }

        public IfStatementSyntax Node { get; }

        public StatementSyntax First { get; }

        public TextSpan Gap { get; }

        public TextSpan? Close { get; }

        public int IfLine { get; }

        public int FirstLine { get; }

        public int LastLine { get; }

        /// <summary>Gets a value indicating whether the body's first statement starts its line (the gap edit then sets that line's indentation).</summary>
        public bool StartsLine { get; }

        /// <summary>Whether this candidate's 'else' body moves the line one level out (its lines after the first).</summary>
        public bool Unindented(int line) => StartsLine && line > FirstLine && line <= LastLine;
    }
}
