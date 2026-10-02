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
/// Shared logic for the query clause layout rules, read from StyleCop's SA110xQueryClauses: BRO1127 (SA1102, no blank
/// line between clauses), BRO1128 (SA1103, clauses all on one line or each on its own), BRO1129 (SA1104, a clause after
/// a multi-line clause starts on a new line) and BRO1130 (SA1105, a multi-line clause starts on a new line). Every fix
/// only rewrites the whitespace before a clause keyword, so the rules' edits never conflict.
/// </summary>
internal static class QueryLayout
{
    /// <summary>
    /// The problems in one query. Like StyleCop: a clause after <c>into</c> is compared with <c>into</c>. Unlike
    /// StyleCop, BRO1128 is also reported next to BRO1129/BRO1130 when other clauses share a line (StyleCop leaves them
    /// to a second run, where the query is still mixed); its finding is on the <c>from</c> keyword. Also unlike StyleCop: gaps with a comment or directive are skipped (the fix would have to move it), and so
    /// are blank lines with a comment line between them (removing only the blank ones wouldn't satisfy the rule); a
    /// query whose clauses share a line across a comment isn't BRO1128's (the fix couldn't make it consistent). BRO1128
    /// has a finding per clause it moves, all on the 'from' keyword.
    /// </summary>
    public static List<Finding> GetFindings(QueryExpressionSyntax query, SourceText text, AnalyzerConfigOptions options)
    {
        var findings = new List<Finding>();
        if (query.ContainsDiagnostics)
        {
            return findings;
        }

        var tokens = new List<SyntaxToken>();
        AddClause(query.FromClause, tokens);
        AddBody(query.Body, tokens);

        var indent = GetIndent(query, text, options);
        var lineBreak = SingleLineBlocks.LineBreak(text, query.SpanStart);
        var sameLine = new List<SyntaxToken>();
        bool allSame = true, allSeparate = true, blocked = false;
        for (var i = 0; i < tokens.Count - 1; i++)
        {
            SyntaxToken first = tokens[i], second = tokens[i + 1];
            if (second.Parent is QueryContinuationSyntax)
            {
                continue;
            }

            // The clause before may contain the whole rest (a continuation); then its keyword marks the line.
            var firstSpan = first.Parent is QueryContinuationSyntax ? first.Span : first.Parent!.Span;
            var firstEnd = Line(text, firstSpan.End);
            var secondStart = Line(text, second.SpanStart);
            var previous = second.GetPreviousToken();
            var plain = previous.TrailingTrivia.Concat(second.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));

            if (secondStart - firstEnd > 1 && plain)
            {
                var from = Line(text, previous.Span.End) + 1;
                findings.Add(new Finding(DiagnosticIds.QueryClauseBlankLine, second, new TextChange(TextSpan.FromBounds(text.Lines[from].Start, text.Lines[secondStart].Start), string.Empty)));
            }

            var onSameLine = firstEnd == secondStart;
            blocked |= onSameLine && !plain;
            if (onSameLine && plain)
            {
                var change = new TextChange(TextSpan.FromBounds(previous.Span.End, second.SpanStart), lineBreak + indent);
                if (!(first.Parent is QueryContinuationSyntax) && SpansLines(text, first.Parent!.Span))
                {
                    findings.Add(new Finding(DiagnosticIds.QueryClauseAfterMultiLineClause, second, change));
                }
                else if (SpansLines(text, second.Parent!.Span))
                {
                    findings.Add(new Finding(DiagnosticIds.MultiLineQueryClause, second, change));
                }
                else
                {
                    sameLine.Add(second);
                }
            }

            allSame &= onSameLine;
            allSeparate &= !onSameLine;
        }

        // Mixed: every clause that shares a line with the one before moves to its own line (StyleCop's fix can also
        // join them all on one line; under 'dotnet format' it picks whichever action comes first).
        if (!allSame && !allSeparate && !blocked && sameLine.Count > 0)
        {
            findings.AddRange(sameLine.Select(t => new Finding(
                DiagnosticIds.QueryClausesOnSeparateLines,
                query.FromClause.FromKeyword,
                new TextChange(TextSpan.FromBounds(t.GetPreviousToken().Span.End, t.SpanStart), lineBreak + indent))));
        }

        return findings;
    }

    /// <summary>
    /// StyleCop's query indentation: the indentation of the line the <c>from</c> is on, one unit deeper when the
    /// <c>from</c> isn't the first thing on that line (unless that's an opening parenthesis).
    /// </summary>
    private static string GetIndent(QueryExpressionSyntax query, SourceText text, AnalyzerConfigOptions options)
    {
        var line = text.Lines.GetLineFromPosition(query.FromClause.FromKeyword.SpanStart);
        var lineText = text.ToString(line.Span);
        var leading = lineText.Substring(0, lineText.Length - lineText.TrimStart().Length);
        var firstOnLine = line.Start + leading.Length;
        var first = query.SyntaxTree.GetRoot().FindToken(firstOnLine);
        return first == query.FromClause.FromKeyword || first.IsKind(SyntaxKind.OpenParenToken) ? leading : leading + Indentation.GetUnit(options);
    }

    private static void AddBody(QueryBodySyntax body, List<SyntaxToken> tokens)
    {
        foreach (var clause in body.Clauses)
        {
            AddClause(clause, tokens);
        }

        if (body.SelectOrGroup is SelectClauseSyntax { IsMissing: false } select)
        {
            tokens.Add(select.SelectKeyword);
        }
        else if (body.SelectOrGroup is GroupClauseSyntax group)
        {
            tokens.Add(group.GroupKeyword);
        }

        if (body.Continuation is { } continuation)
        {
            tokens.Add(continuation.IntoKeyword);
            AddBody(continuation.Body, tokens);
        }
    }

    private static void AddClause(QueryClauseSyntax clause, List<SyntaxToken> tokens)
    {
        switch (clause)
        {
            case FromClauseSyntax from:
                tokens.Add(from.FromKeyword);
                break;
            case LetClauseSyntax let:
                tokens.Add(let.LetKeyword);
                break;
            case WhereClauseSyntax where:
                tokens.Add(where.WhereKeyword);
                break;
            case JoinClauseSyntax join:
                tokens.Add(join.JoinKeyword);
                break;
            case OrderByClauseSyntax orderBy:
                tokens.Add(orderBy.OrderByKeyword);
                break;
        }
    }

    private static bool SpansLines(SourceText text, TextSpan span) => Line(text, span.Start) != Line(text, span.End);

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

    /// <summary>One problem: the rule, the keyword it's reported on, and the edit.</summary>
    public sealed class Finding
    {
        public Finding(string id, SyntaxToken token, TextChange change)
        {
            Id = id;
            Token = token;
            Change = change;
        }

        public string Id { get; }

        public SyntaxToken Token { get; }

        public TextChange Change { get; }
    }
}
