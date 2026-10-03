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
    /// "multi-line" is what the SDK's IDE0011 counts as such; with false, nothing is.
    /// </summary>
    public static IEnumerable<(StatementSyntax Child, string Id)> GetFindings(
        SyntaxNode node,
        SourceText text,
        Func<string, bool> isOn,
        bool allowConsecutiveUsings = true,
        BracePreference preference = BracePreference.Always)
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

            if (id is not null)
            {
                yield return (child, id);
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
            if (line.Span.IsEmpty || text.ToString(line.Span).Trim().Length == 0 || wraps.Any(w => SetsIndent(w.Gap, line) || SetsIndent(w.Close, line)))
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

    /// <summary>The <see cref="ConsecutiveUsingsKey"/> setting.</summary>
    public static bool AllowConsecutiveUsings(AnalyzerConfigOptions options) =>
        !(options.TryGetValue(ConsecutiveUsingsKey, out var value) && bool.TryParse(value.Trim(), out var allowed) && !allowed);

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

    /// <summary>Whether an edit of this span already writes the line's indentation.</summary>
    private static bool SetsIndent(TextSpan span, TextLine line) => span.Start < line.Start && line.Start <= span.End;

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
