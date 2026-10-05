using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers.Layout;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1139: <c>else if</c> on one line, for an <c>if</c> that follows <c>else</c> on the next line or
/// is the only statement of the else's block.
/// </summary>
internal static class ElseIfs
{
    /// <summary>Whether the else clause is reported (the fix can join it).</summary>
    public static bool IsReported(ElseClauseSyntax elseClause, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn) =>
        GetChanges(elseClause, text, options, isOn) is not null;

    /// <summary>
    /// The edits that join <c>else</c> and <c>if</c>, or null. The gap between them becomes one space, a block's
    /// <c>}</c> goes, and the if's other lines move left by as much as the <c>if</c> moved relative to the else's line.
    /// Every reported else clause nested in the same outermost one is fixed together (their lines move by the sum), so a
    /// single fix and Fix All give the same text. Skipped: comments or directives in the gaps (around the if), a block
    /// followed by another <c>else</c> (that <c>else</c> would then belong to the inner <c>if</c>), a line that isn't
    /// indented far enough to move, and a token spanning lines (a multi-line string) when lines move.
    /// <para>
    /// Joining <c>else { if (b) Y(); }</c> into the chain can make it inconsistent (braces on one clause, none on
    /// another), which turns BRO1514's findings into BRO1516's; 'dotnet format' may have run BRO1516 already. So when the
    /// brace rules (<paramref name="isOn"/>) want braces in the joined chains, the fix adds them too, as one edit of the
    /// whole chain, and the else clause isn't reported when they can't be added.
    /// </para>
    /// </summary>
    public static List<TextChange>? GetChanges(ElseClauseSyntax elseClause, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        // An 'else' BRO1143 removes (the 'if' branch ends in a jump) isn't joined: removing it flattens the code more, and
        // once joined, BRO1143 skips the 'else if' chain, so which one 'dotnet format' ran first decided the result.
        Part? Joinable(ElseClauseSyntax e) =>
            GetPart(e, text) is { } part
            && !(isOn(DiagnosticIds.ElseAfterJump) && e.Parent is IfStatementSyntax owner && ElseAfterJump.IsCandidate(owner, text, options, isOn))
                ? part
                : null;

        if (Joinable(elseClause) is null)
        {
            return null;
        }

        var top = elseClause;
        foreach (var ancestor in elseClause.Ancestors().OfType<ElseClauseSyntax>())
        {
            if (Joinable(ancestor) is not null)
            {
                top = ancestor;
            }
        }

        var parts = top.DescendantNodesAndSelf().OfType<ElseClauseSyntax>().Select(Joinable).OfType<Part>().ToList();
        var changes = new List<TextChange>();
        foreach (var part in parts)
        {
            changes.Add(new TextChange(TextSpan.FromBounds(part.Else.ElseKeyword.Span.End, part.If.SpanStart), " "));
            if (part.CloseBrace is { } close)
            {
                changes.Add(new TextChange(TextSpan.FromBounds(part.If.Span.End, close.Span.End), string.Empty));
            }
        }

        var lineEdits = new List<TextChange>();
        for (var number = parts[0].FirstLine; number <= parts[0].LastLine; number++)
        {
            var line = text.Lines[number];
            var shift = parts.Where(p => p.FirstLine <= number && number <= p.LastLine).Sum(p => p.Shift);
            var indent = 0;
            while (line.Start + indent < line.End && text[line.Start + indent] is ' ' or '\t')
            {
                indent++;
            }

            if (shift == 0 || line.Start + indent == line.End || changes.Any(c => c.Span.Start < line.Start && line.Start <= c.Span.End))
            {
                continue;
            }

            if (indent < shift)
            {
                return null;
            }

            lineEdits.Add(new TextChange(new TextSpan(line.Start, shift), string.Empty));
        }

        if (lineEdits.Count > 0 && parts[0].If.DescendantTokens().Any(t => text.Lines.GetLineFromPosition(t.SpanStart).LineNumber != text.Lines.GetLineFromPosition(t.Span.End).LineNumber))
        {
            return null;
        }

        changes.AddRange(lineEdits);
        return AddBraces(parts, changes, text, options, isOn);
    }

    /// <summary>The join's edits, or one edit of the whole chain with the braces the brace rules want afterwards.</summary>
    private static List<TextChange>? AddBraces(List<Part> parts, List<TextChange> changes, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        if (Braces.GetPreference(options) == BracePreference.Never
            || !(isOn(DiagnosticIds.BracesOmitted) || isOn(DiagnosticIds.BracesMultiLine) || isOn(DiagnosticIds.BracesConsistent))
            || Braces.GetChain(parts[0].Else.Statement) is not { } chain)
        {
            return changes;
        }

        var sorted = changes.OrderBy(c => c.Span.Start).ToList();
        int Map(int position) => position + sorted.Where(c => c.Span.End <= position).Sum(c => c.NewText!.Length - c.Span.Length);
        IEnumerable<(StatementSyntax Child, string Id)> Findings(IfStatementSyntax chainRoot, SourceText chainText) =>
            Braces.GetFindings(chainRoot, chainText, isOn, Braces.AllowConsecutiveUsings(options), Braces.GetPreference(options), Braces.AllowSingleLineJumps(options))
                .Where(f => !Braces.IsLeftToExpansion(f.Child, chainText, options, isOn));

        // The brace rules' findings before the join (the outer chains, and the inner chains that were in a block) ...
        var before = parts.Select(p => Braces.GetChain(p.Else.Statement)).Concat(parts.Select(p => p.If).Where(i => i.Parent is BlockSyntax))
            .OfType<IfStatementSyntax>().Distinct().SelectMany(c => Findings(c, text))
            .ToDictionary(f => Map(f.Child.SpanStart), f => f.Id);

        // ... and after it. When any changed, the joined chains get all their braces now.
        var joined = text.WithChanges(sorted);

        // Incremental: a full parse of the joined file cost ~10 ms per candidate in a 100 KB file (per edit in the IDE).
        var root = chain.SyntaxTree.WithChangedText(joined).GetRoot();
        var after = parts.Select(p => Map(p.If.SpanStart))
            .Select(start => root.FindToken(start).Parent is IfStatementSyntax joinedIf && joinedIf.SpanStart == start ? Braces.GetChain(joinedIf) : null)
            .OfType<IfStatementSyntax>().Distinct().SelectMany(c => Findings(c, joined)).ToList();
        if (after.All(f => before.TryGetValue(f.Child.SpanStart, out var id) && id == f.Id))
        {
            return changes;
        }

        if (Braces.GetChanges(after.Select(f => f.Child).ToList(), joined, options, isOn) is not { } wraps)
        {
            return null;
        }

        var braced = joined.WithChanges(wraps);
        var length = chain.Span.Length + braced.Length - text.Length;
        return new List<TextChange> { new(chain.Span, braced.ToString(new TextSpan(chain.SpanStart, length))) };
    }

    private static Part? GetPart(ElseClauseSyntax elseClause, SourceText text)
    {
        IfStatementSyntax inner;
        SyntaxToken? close = null;
        IEnumerable<SyntaxTrivia> gaps;
        if (elseClause.Statement is IfStatementSyntax ifStatement)
        {
            inner = ifStatement;
            gaps = elseClause.ElseKeyword.TrailingTrivia.Concat(inner.GetLeadingTrivia());
            if (text.Lines.GetLineFromPosition(inner.SpanStart).LineNumber == text.Lines.GetLineFromPosition(elseClause.ElseKeyword.SpanStart).LineNumber)
            {
                return null;
            }
        }
        else if (elseClause.Statement is BlockSyntax { Statements.Count: 1 } block && block.Statements[0] is IfStatementSyntax only
            && !block.CloseBraceToken.GetNextToken().IsKind(SyntaxKind.ElseKeyword))
        {
            inner = only;
            close = block.CloseBraceToken;
            gaps = elseClause.ElseKeyword.TrailingTrivia.Concat(block.OpenBraceToken.LeadingTrivia).Concat(block.OpenBraceToken.TrailingTrivia)
                .Concat(inner.GetLeadingTrivia()).Concat(inner.GetTrailingTrivia()).Concat(block.CloseBraceToken.LeadingTrivia);
        }
        else
        {
            return null;
        }

        if (elseClause.ContainsDiagnostics
            || gaps.Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        // How far the if's lines move: the if's indentation minus the else line's, when the if starts its line.
        var elseLine = text.Lines.GetLineFromPosition(elseClause.ElseKeyword.SpanStart);
        var ifLine = text.Lines.GetLineFromPosition(inner.SpanStart);
        var shift = 0;
        if (ifLine.LineNumber != elseLine.LineNumber && text.ToString(TextSpan.FromBounds(ifLine.Start, inner.SpanStart)).Trim().Length == 0)
        {
            var elseIndent = text.ToString(elseLine.Span).Length - text.ToString(elseLine.Span).TrimStart().Length;
            shift = inner.SpanStart - ifLine.Start - elseIndent;
        }

        return shift < 0 ? null : new Part(elseClause, inner, close, shift, ifLine.LineNumber + 1, text.Lines.GetLineFromPosition(inner.Span.End).LineNumber);
    }

    private sealed class Part
    {
        public Part(ElseClauseSyntax @else, IfStatementSyntax @if, SyntaxToken? closeBrace, int shift, int firstLine, int lastLine)
        {
            this.Else = @else;
            this.If = @if;
            this.CloseBrace = closeBrace;
            this.Shift = shift;
            this.FirstLine = firstLine;
            this.LastLine = lastLine;
        }

        public ElseClauseSyntax Else { get; }

        public IfStatementSyntax If { get; }

        public SyntaxToken? CloseBrace { get; }

        public int Shift { get; }

        /// <summary>Gets the first line that moves (the one after the if's).</summary>
        public int FirstLine { get; }

        public int LastLine { get; }
    }
}
