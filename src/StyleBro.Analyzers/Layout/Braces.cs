using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1514 (SA1503) braces not omitted, BRO1515 (SA1519) not from a multi-line child statement, BRO1516 (SA1520) used
/// consistently in an if/else chain. Which statements count, and which rule reports one, follow StyleCop.
/// </summary>
/// <summary>The SDK's csharp_prefer_braces.</summary>
internal enum BracePreference
{
    /// <summary>'true', or not set: StyleCop's rules (SA1503, SA1519, SA1520).</summary>
    Always,

    /// <summary>'when_multiline': only multi-line statements (as the SDK's IDE0011 counts them) and inconsistent chains.</summary>
    WhenMultiline,

    /// <summary>'false': braces aren't required.</summary>
    Never,
}

internal static class Braces
{
    /// <summary>StyleCop's allowConsecutiveUsings: 'using (a) using (b) { }' shares one block (default true).</summary>
    public const string ConsecutiveUsingsKey = "stylebro_allow_consecutive_usings";

    /// <summary>StyleCop issue #2252: 'if (x) return;' (a jump statement on the 'if' line) without braces (default false).</summary>
    public const string SingleLineJumpsKey = "stylebro_allow_single_line_jump_statements";

    public static readonly SyntaxKind[] Kinds =
    {
        SyntaxKind.IfStatement, SyntaxKind.DoStatement, SyntaxKind.WhileStatement, SyntaxKind.ForStatement,
        SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement, SyntaxKind.FixedStatement,
        SyntaxKind.UsingStatement, SyntaxKind.LockStatement,
    };

    /// <summary>
    /// The child statements without braces and the rule that reports each, like StyleCop: a multi-line one is SA1519's,
    /// one in an if/else chain where another clause has braces is SA1520's, the rest SA1503's; a rule that is off leaves
    /// its statements to the next one. <paramref name="isOn"/> says whether a rule is on. With the SDK's
    /// csharp_prefer_braces = when_multiline, single-line statements in a consistent chain aren't reported, and
    /// "multi-line" is what the SDK's IDE0011 counts as such; with false, nothing is. With
    /// <paramref name="allowSingleLineJumps"/>, a jump statement on its 'if' line is fine unless BRO1516 wants braces.
    /// </summary>
    public static IEnumerable<(StatementSyntax Child, string Id)> GetFindings(
        SyntaxNode node,
        SourceText text,
        Func<string, bool> isOn,
        bool allowConsecutiveUsings = true,
        BracePreference preference = BracePreference.Always,
        bool allowSingleLineJumps = false)
    {
        if (preference == BracePreference.Never)
        {
            yield break;
        }

        List<StatementSyntax> children;
        switch (node)
        {
            case IfStatementSyntax { Parent: ElseClauseSyntax }:
                // Analyzed with the chain's first 'if'.
                yield break;

            case IfStatementSyntax ifStatement:
                children = new List<StatementSyntax>();
                for (var current = ifStatement; current is not null; current = current.Else?.Statement as IfStatementSyntax)
                {
                    children.Add(current.Statement);
                    if (current.Else is { Statement: not IfStatementSyntax } @else)
                    {
                        children.Add(@else.Statement);
                    }
                }

                break;

            case UsingStatementSyntax { Statement: UsingStatementSyntax } when allowConsecutiveUsings:
                yield break;

            case CommonForEachStatementSyntax forEach:
                children = new List<StatementSyntax> { forEach.Statement };
                break;

            default:
                children = new List<StatementSyntax> { GetChild(node) };
                break;
        }

        var inconsistent = node is IfStatementSyntax && children.Any(c => c is BlockSyntax);
        List<StatementSyntax>? allowed = null;
        var reported = false;
        foreach (var child in children)
        {
            if (child is BlockSyntax)
            {
                continue;
            }

            var multiLine = preference == BracePreference.WhenMultiline ? IsMultiLineForSdk(child, text) : IsMultiLine(child, text);
            if (preference == BracePreference.WhenMultiline && !multiLine && !inconsistent)
            {
                continue;
            }

            string? id = null;
            if (multiLine && isOn(DiagnosticIds.BracesMultiLine))
            {
                id = DiagnosticIds.BracesMultiLine;
            }
            else if (inconsistent && isOn(DiagnosticIds.BracesConsistent))
            {
                id = DiagnosticIds.BracesConsistent;
            }
            else if (isOn(DiagnosticIds.BracesOmitted))
            {
                id = DiagnosticIds.BracesOmitted;
            }

            if (id == DiagnosticIds.BracesOmitted && allowSingleLineJumps && IsSingleLineJump(child, text))
            {
                (allowed ??= new List<StatementSyntax>()).Add(child);
                continue;
            }

            if (id is not null)
            {
                reported = true;
                yield return (child, id);
            }
        }

        // Braces on another clause make the chain inconsistent: the allowed jumps get theirs in the same run.
        if (allowed is not null && reported && node is IfStatementSyntax && isOn(DiagnosticIds.BracesConsistent))
        {
            foreach (var child in allowed)
            {
                yield return (child, DiagnosticIds.BracesConsistent);
            }
        }
    }

    /// <summary>
    /// The edits that put each statement in braces: '{' and '}' on their own lines at the owning statement's indentation
    /// (or '{' at the end of its line when csharp_new_line_before_open_brace leaves out control blocks), the statement one
    /// level deeper. Nested statements are handled together: the indentation of every line accounts for all braces added
    /// around it. Null when any statement can't be wrapped safely (comments or directives where the braces go, a line
    /// break inside a token, other code after it on its line).
    /// </summary>
    public static List<TextChange>? GetChanges(IReadOnlyCollection<StatementSyntax> statements, SourceText text, AnalyzerConfigOptions options, Func<string, bool>? isOn = null)
    {
        var unit = Indentation.GetUnit(options);
        var braceOnNewLine = SingleLineBlocks.NewLineBeforeBrace(SyntaxFactory.Block(), options);
        var wraps = new List<Wrap>();
        foreach (var statement in statements.Distinct())
        {
            if (Wrap.Create(statement, text) is not { } wrap)
            {
                return null;
            }

            wraps.Add(wrap);
        }

        // The new indentation of a line: set by a gap edit when a wrapped statement starts it, shifted when it's inside one.
        var cache = new Dictionary<int, string?>();
        string? NewIndent(int lineNumber)
        {
            if (cache.TryGetValue(lineNumber, out var known))
            {
                return known;
            }

            var line = text.Lines[lineNumber];
            var old = LeadingWhitespace(text, line);
            string? result;
            if (wraps.FirstOrDefault(w => w.StartsLine && w.FirstLine == lineNumber) is { } starting)
            {
                result = Target(starting);
            }
            else if (wraps.Where(w => w.Governs(line)).OrderBy(w => w.Statement.Span.Length).FirstOrDefault() is { } inside)
            {
                var prefix = inside.StartsLine ? LeadingWhitespace(text, text.Lines[inside.FirstLine]) : LeadingWhitespace(text, text.Lines[inside.OwnerLine]);
                result = Target(inside) is { } target && old.StartsWith(prefix, StringComparison.Ordinal)
                    ? target + old.Substring(prefix.Length)
                    : null;
            }
            else
            {
                result = old;
            }

            cache[lineNumber] = result;
            return result;
        }

        string? Target(Wrap wrap) => NewIndent(wrap.OwnerLine) is { } owner ? owner + unit : null;

        var changes = new List<TextChange>();
        foreach (var wrap in wraps)
        {
            if (NewIndent(wrap.OwnerLine) is not { } owner || Target(wrap) is not { } target)
            {
                return null;
            }

            var eol = SingleLineBlocks.LineBreak(text, wrap.Statement.SpanStart);
            var open = braceOnNewLine ? eol + owner + "{" : " {";
            changes.Add(new TextChange(wrap.Gap, open + eol + target));
        }

        // Lines inside a wrapped statement move with it; lines a gap edit starts are already set.
        foreach (var lineNumber in wraps.SelectMany(w => w.InnerLines(text)).Distinct())
        {
            var line = text.Lines[lineNumber];
            if (line.Span.IsEmpty || text.ToString(line.Span).Trim().Length == 0)
            {
                continue;
            }

            if (NewIndent(lineNumber) is not { } indent)
            {
                return null;
            }

            var old = LeadingWhitespace(text, line);
            if (old != indent)
            {
                changes.Add(new TextChange(new TextSpan(line.Start, old.Length), indent));
            }
        }

        // Closing braces: statements ending at the same place close innermost first.
        foreach (var group in wraps.GroupBy(w => w.Close.Start))
        {
            var ordered = group.OrderBy(w => w.Statement.Span.Length).ToList();
            var eol = SingleLineBlocks.LineBreak(text, ordered[0].Statement.SpanStart);
            var closing = string.Concat(ordered.Select(w => eol + NewIndent(w.OwnerLine) + "}"));
            var outer = ordered[ordered.Count - 1];
            var suffix = outer.Following switch
            {
                SyntaxKind.WhileKeyword => " ",
                SyntaxKind.ElseKeyword when !NewLineBeforeElse(options) => " ",
                SyntaxKind.ElseKeyword => eol + NewIndent(outer.OwnerLine),

                // The new '}' followed by a statement: BRO1519's blank line now, so one run converges.
                _ when isOn is not null && BlankLineRuns.WantsBlankLineAfter(outer.Statement.Parent!, default, outer.Statement.GetLastToken().GetNextToken(), isOn) => eol,
                _ => string.Empty,
            };
            changes.Add(new TextChange(outer.Close, closing + suffix));
        }

        return changes;
    }

    /// <summary>
    /// Whether the statement is left to BRO1508/BRO1509: its block is on a single line and that rule (on) expands it, adding
    /// the braces in the same edit (<see cref="AddToExpansion"/>). Wrapping it inside the single-line block first would
    /// leave a half-expanded block, and the block's '}' on its line hides the rest of the statements until the block
    /// is expanded, so 'dotnet format' would need a second run.
    /// </summary>
    public static bool IsLeftToExpansion(StatementSyntax child, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn) =>
        child.Ancestors().OfType<BlockSyntax>().FirstOrDefault() is { } block
        && SingleLineBlocks.GetBraces(block) is { } braces
        && isOn(braces.IsElement ? DiagnosticIds.SingleLineElement : DiagnosticIds.SingleLineStatementBlock)
        && SingleLineBlocks.IsReported(block, text, options);

    /// <summary>
    /// Adds BRO1514-BRO1516's braces to the expansion of a single-line block (<paramref name="expansion"/>, BRO1508/
    /// BRO1509's edits): <c>void M(bool x) { if (x) return; }</c> gets its method body and the braces in one run, in any
    /// fix order. The findings and their edits are computed on the expanded text, for the statements directly in the
    /// block (a nested single-line block's are its own expansion's), and mapped back; nothing is added when an edit
    /// falls outside the statements.
    /// </summary>
    public static void AddToExpansion(BlockSyntax block, SourceText text, List<TextChange> expansion, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        if (!block.Statements.Any(s => s.DescendantNodesAndSelf().Any(n => Kinds.Contains(n.Kind()))))
        {
            return;
        }

        var sorted = expansion.OrderBy(c => c.Span.Start).ToList();
        var expanded = text.WithChanges(sorted);
        var root = block.SyntaxTree.WithChangedText(expanded).GetRoot();
        var items = new List<(StatementSyntax Old, StatementSyntax New, int Shift)>();
        foreach (var statement in block.Statements)
        {
            var shift = sorted.Where(c => c.Span.End <= statement.SpanStart).Sum(c => c.NewText!.Length - c.Span.Length);
            if (root.FindNode(new TextSpan(statement.SpanStart + shift, statement.Span.Length)) is not StatementSyntax found
                || !found.IsKind(statement.Kind()) || found.Span.Length != statement.Span.Length)
            {
                return;
            }

            items.Add((statement, found, shift));
        }

        var newBlock = items[0].New.Parent;
        var children = items
            .SelectMany(i => i.New.DescendantNodesAndSelf())
            .Where(n => Kinds.Contains(n.Kind()) && n.Ancestors().OfType<BlockSyntax>().FirstOrDefault() == newBlock)
            .SelectMany(n => GetFindings(n, expanded, isOn, AllowConsecutiveUsings(options), GetPreference(options), AllowSingleLineJumps(options)))
            .Select(f => f.Child)
            .Where(c => GetChanges(new[] { c }, expanded, options) is not null)
            .ToList();
        if (children.Count == 0 || GetChanges(children, expanded, options, isOn) is not { } wraps)
        {
            return;
        }

        // Every edit is inside a statement or at its end (a '}' inserted there goes before the expansion's line break:
        // an insertion sorts before a replacement at the same position).
        var mapped = new List<TextChange>();
        foreach (var change in wraps)
        {
            var item = items.FirstOrDefault(i => change.Span.Start >= i.New.SpanStart && change.Span.End <= i.New.Span.End);
            if (item.Old is null)
            {
                return;
            }

            mapped.Add(new TextChange(new TextSpan(change.Span.Start - item.Shift, change.Span.Length), change.NewText!));
        }

        expansion.AddRange(mapped);
    }

    /// <summary>The <see cref="ConsecutiveUsingsKey"/> setting.</summary>
    public static bool AllowConsecutiveUsings(AnalyzerConfigOptions options) =>
        !(options.TryGetValue(ConsecutiveUsingsKey, out var value) && bool.TryParse(value.Trim(), out var allowed) && !allowed);

    /// <summary>The <see cref="SingleLineJumpsKey"/> setting.</summary>
    public static bool AllowSingleLineJumps(AnalyzerConfigOptions options) =>
        options.TryGetValue(SingleLineJumpsKey, out var value) && bool.TryParse(value.Trim(), out var allowed) && allowed;

    /// <summary>The <see cref="BracePreference"/> from csharp_prefer_braces ('true:warning' style values too).</summary>
    public static BracePreference GetPreference(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue("csharp_prefer_braces", out var value))
        {
            return BracePreference.Always;
        }

        return value.Split(':')[0].Trim().ToLowerInvariant() switch
        {
            "when_multiline" => BracePreference.WhenMultiline,
            "false" => BracePreference.Never,
            _ => BracePreference.Always,
        };
    }

    /// <summary>The first 'if' of the if/else chain that <paramref name="statement"/> is a clause of, or null.</summary>
    public static IfStatementSyntax? GetChain(StatementSyntax statement)
    {
        if ((statement.Parent is ElseClauseSyntax @else ? @else.Parent : statement.Parent) is not IfStatementSyntax chain)
        {
            return null;
        }

        while (chain.Parent is ElseClauseSyntax { Parent: IfStatementSyntax outer })
        {
            chain = outer;
        }

        return chain;
    }

    private static StatementSyntax GetChild(SyntaxNode node) => node switch
    {
        DoStatementSyntax s => s.Statement,
        WhileStatementSyntax s => s.Statement,
        ForStatementSyntax s => s.Statement,
        FixedStatementSyntax s => s.Statement,
        UsingStatementSyntax s => s.Statement,
        LockStatementSyntax s => s.Statement,
        _ => throw new ArgumentException(node.Kind().ToString()),
    };

    /// <summary>
    /// The SDK's IDE0011 'multi-line': the statement doesn't fit on one line, and the part before the child statement, the
    /// child statement, or the part after it (a do statement's 'while', not an 'else') spans lines.
    /// </summary>
    private static bool IsMultiLineForSdk(StatementSyntax child, SourceText text)
    {
        var owner = child.Parent!;
        bool SameLine(SyntaxToken a, SyntaxToken b) => Line(text, a.SpanStart) == Line(text, b.Span.End);

        if (SameLine(owner.GetFirstToken(), owner.GetLastToken()))
        {
            return false;
        }

        if (!SameLine(owner.GetFirstToken(), child.GetFirstToken().GetPreviousToken()) || !SameLine(child.GetFirstToken(), child.GetLastToken()))
        {
            return true;
        }

        return owner.GetLastToken() != child.GetLastToken()
            && !(owner is IfStatementSyntax ifStatement && ifStatement.Statement == child)
            && !SameLine(child.GetLastToken().GetNextToken(), owner.GetLastToken());
    }

    /// <summary>
    /// An 'if' statement's (not an 'else' clause's) child that is a jump statement ('return', 'throw', 'break',
    /// 'continue', 'goto', 'yield break') and ends on the line of the 'if' keyword.
    /// </summary>
    private static bool IsSingleLineJump(StatementSyntax child, SourceText text) =>
        child.Parent is IfStatementSyntax ifStatement
        && child is ReturnStatementSyntax or ThrowStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax or GotoStatementSyntax
            or YieldStatementSyntax { RawKind: (int)SyntaxKind.YieldBreakStatement }
        && Line(text, ifStatement.IfKeyword.SpanStart) == Line(text, child.Span.End);

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

    private static bool IsMultiLine(SyntaxNode node, SourceText text) =>
        text.Lines.GetLineFromPosition(node.SpanStart).LineNumber != text.Lines.GetLineFromPosition(node.Span.End).LineNumber;

    private static string LeadingWhitespace(SourceText text, TextLine line)
    {
        var end = line.Start;
        while (end < line.End && text[end] is ' ' or '\t')
        {
            end++;
        }

        return text.ToString(TextSpan.FromBounds(line.Start, end));
    }

    private static bool NewLineBeforeElse(AnalyzerConfigOptions options) =>
        !(options.TryGetValue("csharp_new_line_before_else", out var value) && value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase));

    private sealed class Wrap
    {
        private Wrap(StatementSyntax statement, TextSpan gap, TextSpan close, SyntaxKind following, int ownerLine, int firstLine, bool startsLine)
        {
            Statement = statement;
            Gap = gap;
            Close = close;
            Following = following;
            OwnerLine = ownerLine;
            FirstLine = firstLine;
            StartsLine = startsLine;
        }

        public StatementSyntax Statement { get; }

        /// <summary>Gets the span between the token before the statement and the statement, which becomes the '{' and the line break.</summary>
        public TextSpan Gap { get; }

        /// <summary>Gets the span where the '}' goes: before the line break ending the statement's line, or the space before 'else'/'while'.</summary>
        public TextSpan Close { get; }

        /// <summary>Gets the keyword ('else' or 'while') that follows the statement and goes after the '}'.</summary>
        public SyntaxKind Following { get; }

        /// <summary>Gets the line of the owning statement's (or 'else' clause's) first token, whose indentation the braces get.</summary>
        public int OwnerLine { get; }

        public int FirstLine { get; }

        public bool StartsLine { get; }

        public static Wrap? Create(StatementSyntax statement, SourceText text)
        {
            var parent = statement.Parent;
            if (parent is null || statement is BlockSyntax || statement.ContainsDirectives || parent.ContainsDirectives)
            {
                return null;
            }

            var first = statement.GetFirstToken();
            var before = first.GetPreviousToken();
            if (!before.TrailingTrivia.Concat(first.LeadingTrivia).All(IsSpace))
            {
                return null;
            }

            // A line break inside a token (a multi-line string) can't be reindented.
            if (statement.DescendantTokens().Any(t => text.Lines.GetLineFromPosition(t.SpanStart).LineNumber != text.Lines.GetLineFromPosition(t.Span.End).LineNumber))
            {
                return null;
            }

            var last = statement.GetLastToken();
            var next = last.GetNextToken();
            var lastLine = text.Lines.GetLineFromPosition(last.Span.End).LineNumber;
            TextSpan close;
            var following = SyntaxKind.None;

            // The statement's own 'else' follows the '}' as csharp_new_line_before_else says, wherever it was; a do
            // statement's 'while' keeps its line ('} while' only when it shared the statement's line).
            var nextOnLine = !next.IsKind(SyntaxKind.None) && text.Lines.GetLineFromPosition(next.SpanStart).LineNumber == lastLine;
            var ownKeyword = (next.IsKind(SyntaxKind.ElseKeyword) && parent is IfStatementSyntax { Else: { } @else } && @else.ElseKeyword == next)
                || (next.IsKind(SyntaxKind.WhileKeyword) && parent is DoStatementSyntax && nextOnLine);
            if (ownKeyword && last.TrailingTrivia.Concat(next.LeadingTrivia).All(IsSpace))
            {
                close = TextSpan.FromBounds(last.Span.End, next.SpanStart);
                following = next.Kind();
            }
            else if (!nextOnLine)
            {
                if (last.TrailingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) is not { RawKind: not 0 } lineBreak)
                {
                    return null;
                }

                close = new TextSpan(lineBreak.SpanStart, 0);
            }
            else
            {
                return null;
            }

            var ownerToken = parent.GetFirstToken();
            var line = text.Lines.GetLineFromPosition(statement.SpanStart);
            var startsLine = text.ToString(TextSpan.FromBounds(line.Start, statement.SpanStart)).Trim().Length == 0;
            return new Wrap(
                statement,
                TextSpan.FromBounds(before.Span.End, statement.SpanStart),
                close,
                following,
                text.Lines.GetLineFromPosition(ownerToken.SpanStart).LineNumber,
                line.LineNumber,
                startsLine);
        }

        /// <summary>Whether a line starts inside the statement (not the line the statement starts on).</summary>
        public bool Governs(TextLine line) => line.LineNumber > FirstLine && line.Start < Statement.Span.End;

        public IEnumerable<int> InnerLines(SourceText text)
        {
            var lastLine = text.Lines.GetLineFromPosition(Statement.Span.End).LineNumber;
            for (var i = FirstLine + 1; i <= lastLine; i++)
            {
                yield return i;
            }
        }

        private static bool IsSpace(SyntaxTrivia trivia) =>
            trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia);
    }
}
