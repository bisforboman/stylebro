using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Layout;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1149: an <see langword="if"/> that is the only statement of an enclosing <see langword="if"/> (its block's
/// only statement, or its embedded statement), neither with an <see langword="else"/>, joins it: <c>if (a &amp;&amp; b)</c>.
/// Everything here works on an inner <see langword="if"/> and the text alone (no diagnostic properties), so a fix for another
/// analyzer's id that reports the same inner <see langword="if"/> can reuse <see cref="GetChange"/>.
/// </summary>
internal static class NestedIfs
{
    /// <summary>
    /// The enclosing <see langword="if"/> that <paramref name="inner"/> can be merged into, or null: neither has an
    /// <see langword="else"/>, and the text that goes (<c>) {</c>, <c>if (</c>, the closing <c>}</c>) holds no comment or directive.
    /// </summary>
    public static IfStatementSyntax? GetOuter(IfStatementSyntax inner) =>
        (inner.Parent is BlockSyntax { Parent: IfStatementSyntax owner } ? owner : inner.Parent as IfStatementSyntax) is { } outer
        && GetInner(outer) == inner
            ? outer
            : null;

    /// <summary>The <see langword="if"/> that can be merged into <paramref name="outer"/>, or null (see <see cref="GetOuter"/>).</summary>
    public static IfStatementSyntax? GetInner(IfStatementSyntax outer)
    {
        var inner = outer.Statement switch
        {
            IfStatementSyntax embedded => embedded,
            BlockSyntax { Statements.Count: 1 } block => block.Statements[0] as IfStatementSyntax,
            _ => null,
        };

        // Directives anywhere in the outer 'if' (also in the body), or right after it (an 'else' only some target frameworks'
        // copies see): each copy could see other code.
        return inner is null
            || outer.Else is not null
            || inner.Else is not null
            || outer.ContainsDirectives
            || outer.GetLastToken().GetNextToken().LeadingTrivia.Any(t => t.IsDirective)
            || outer.ContainsDiagnostics
            || !Trivia.IsBlank(outer, TextSpan.FromBounds(outer.Condition.Span.End, inner.Condition.SpanStart))
            || !Trivia.IsBlank(outer, TextSpan.FromBounds(inner.Span.End, outer.Span.End))
            ? null
            : inner;
    }

    /// <summary>
    /// The edit that merges <paramref name="inner"/> into its enclosing <see langword="if"/>, or null when it isn't merged.
    /// Every <see langword="if"/> merged into one condition is fixed with the whole nest (the outermost merged chain around it,
    /// with the chains in its body), so a single fix and Fix All give the same text. When the brace rules are on
    /// (<paramref name="isOn"/>), the statements they want in braces in the merged chains get them in the same edit:
    /// a merge can turn which rule reports a statement of an if/else chain (BRO1514 into BRO1516), and 'dotnet format'
    /// may have run that rule already.
    /// </summary>
    public static TextChange? GetChange(IfStatementSyntax inner, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        if (GetOuter(inner) is null)
        {
            return null;
        }

        // The chains around it, outermost first: the first that merges this 'if' decides.
        foreach (var top in inner.Ancestors().OfType<IfStatementSyntax>().Where(i => GetOuter(i) is null && GetInner(i) is not null).Reverse())
        {
            if (GetChange(top, inner, text, options, isOn) is { } change)
            {
                return change;
            }
        }

        return null;
    }

    private static TextChange? GetChange(IfStatementSyntax top, IfStatementSyntax inner, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        if (Render(top, text) is not { } rendered || !rendered.Merged.Contains(inner))
        {
            return null;
        }

        if (Braces.GetPreference(options) == BracePreference.Never
            || !(isOn(DiagnosticIds.BracesOmitted) || isOn(DiagnosticIds.BracesMultiLine) || isOn(DiagnosticIds.BracesConsistent)))
        {
            return rendered.Change;
        }

        // The brace rules' findings after the merge, in the merged 'if' (and the else chain it belongs to): all of them get
        // their braces now, wherever which rule reports a statement changed (or a single-line block that left one to
        // BRO1508/BRO1509's expansion went away).
        var merged = text.WithChanges(rendered.Change);
        if (top.SyntaxTree.WithChangedText(merged).GetRoot().FindToken(top.SpanStart).Parent is not IfStatementSyntax mergedTop
            || mergedTop.SpanStart != top.SpanStart)
        {
            return null;
        }

        var root = Braces.GetChain(mergedTop.Statement) ?? mergedTop;
        var children = mergedTop.Statement.DescendantNodesAndSelf().Where(n => Braces.Kinds.Contains(n.Kind())).Prepend(root)
            .SelectMany(n => Braces.GetFindings(n, merged, isOn, Braces.AllowConsecutiveUsings(options), Braces.GetPreference(options), Braces.AllowSingleLineJumps(options)))
            .Where(f => !Braces.IsLeftToExpansion(f.Child, merged, options, isOn))
            .Select(f => f.Child)
            .Distinct()
            .ToList();
        if (children.Count == 0)
        {
            return rendered.Change;
        }

        if (Braces.GetChanges(children, merged, options, isOn) is not { } wraps
            || wraps.Any(w => w.Span.Start < root.SpanStart || w.Span.End > mergedTop.Span.End))
        {
            return null;
        }

        // One edit from the first changed character to the last, so a merge in an earlier clause's block keeps its own edit.
        var final = merged.WithChanges(wraps);
        var before = text.ToString(TextSpan.FromBounds(root.SpanStart, top.Span.End));
        var after = final.ToString(new TextSpan(root.SpanStart, before.Length + final.Length - text.Length));
        var start = 0;
        while (start < before.Length && start < after.Length && before[start] == after[start])
        {
            start++;
        }

        var end = 0;
        while (end < before.Length - start && end < after.Length - start && before[before.Length - 1 - end] == after[after.Length - 1 - end])
        {
            end++;
        }

        return new TextChange(new TextSpan(root.SpanStart + start, before.Length - start - end), after.Substring(start, after.Length - start - end));
    }

    /// <summary>
    /// The merged chain starting at <paramref name="top"/>: one edit from its condition to its end, with the chains in the
    /// deepest body merged too. The body moves left by as much as its 'if' is indented deeper than the top one's line.
    /// </summary>
    private static Rendered? Render(IfStatementSyntax top, SourceText text)
    {
        var ifs = new List<IfStatementSyntax> { top };
        while (GetInner(ifs[ifs.Count - 1]) is { } next)
        {
            ifs.Add(next);
        }

        var deepest = ifs[ifs.Count - 1];
        var body = deepest.Statement;
        var topIndent = IndentOf(text, top.SpanStart);
        var shift = IndentOf(text, deepest.SpanStart) - topIndent;
        if (ifs.Count < 2 || shift < 0 || !Trivia.IsBlank(deepest, TextSpan.FromBounds(deepest.Condition.Span.End, deepest.CloseParenToken.SpanStart))
            || WidensAScope(top, ifs))
        {
            return null;
        }

        var builder = new StringBuilder();
        foreach (var current in ifs)
        {
            var condition = Dedent(current.Condition, text, current == top ? 0 : IndentOf(text, current.SpanStart) - topIndent);
            if (condition is null)
            {
                return null;
            }

            builder.Append(current == top ? string.Empty : " && ").Append(NeedsParentheses(current.Condition) ? "(" + condition + ")" : condition);
        }

        builder.Append(')');

        var gap = text.ToString(TextSpan.FromBounds(deepest.CloseParenToken.Span.End, body.SpanStart));
        var lastBreak = gap.LastIndexOf('\n');
        if (lastBreak >= 0)
        {
            if (gap.Length - lastBreak - 1 < shift || gap.Skip(lastBreak + 1).Take(shift).Any(c => c is not (' ' or '\t')))
            {
                return null;
            }

            gap = gap.Remove(lastBreak + 1, shift);
        }

        builder.Append(gap);

        // The chains in the body, merged first (each a top whose own merge works).
        var merged = new List<IfStatementSyntax>(ifs.Skip(1));
        var nested = new List<TextChange>();
        void Collect(SyntaxNode node)
        {
            if (node is IfStatementSyntax candidate && GetOuter(candidate) is null && GetInner(candidate) is not null && Render(candidate, text) is { } inside)
            {
                nested.Add(new TextChange(new TextSpan(inside.Change.Span.Start - body.SpanStart, inside.Change.Span.Length), inside.Change.NewText!));
                merged.AddRange(inside.Merged);
                return;
            }

            foreach (var child in node.ChildNodes())
            {
                Collect(child);
            }
        }

        Collect(body);
        var bodyText = Dedent(body, SourceText.From(text.ToString(body.Span)).WithChanges(nested).ToString(), text, shift);
        if (bodyText is null)
        {
            return null;
        }

        builder.Append(bodyText);
        return new Rendered(new TextChange(TextSpan.FromBounds(top.Condition.SpanStart, top.Span.End), builder.ToString()), merged);
    }

    /// <summary>
    /// Whether a variable declared in an inner condition (<c>out var x</c>, <c>is string s</c>) would collide once it moves into
    /// the top condition: a variable declared in an <see langword="if"/> condition is in scope in the whole enclosing block
    /// (switch block), not only in the <see langword="if"/>, so the same name elsewhere there (a later local) would no longer
    /// compile or would mean another variable.
    /// </summary>
    private static bool WidensAScope(IfStatementSyntax top, List<IfStatementSyntax> ifs)
    {
        var names = ifs.Skip(1).SelectMany(i => i.Condition.DescendantNodes()).OfType<SingleVariableDesignationSyntax>()
            .Select(d => d.Identifier.ValueText)
            .ToList();
        if (names.Count == 0)
        {
            return false;
        }

        var scope = top.Parent switch
        {
            BlockSyntax block => block,
            SwitchSectionSyntax section => section.Parent,
            GlobalStatementSyntax global => global.Parent,
            _ => null,
        };
        return scope is not null
            && scope.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && !top.Span.Contains(t.Span) && names.Contains(t.ValueText));
    }

    /// <summary>Operands that bind looser than '&amp;&amp;' get parentheses.</summary>
    private static bool NeedsParentheses(ExpressionSyntax condition) =>
        condition is ConditionalExpressionSyntax or AssignmentExpressionSyntax or LambdaExpressionSyntax or QueryExpressionSyntax
        || condition.IsKind(SyntaxKind.LogicalOrExpression)
        || condition.IsKind(SyntaxKind.CoalesceExpression);

    private static int IndentOf(SourceText text, int position)
    {
        var line = text.Lines.GetLineFromPosition(position);
        var indent = 0;
        while (line.Start + indent < line.End && text[line.Start + indent] is ' ' or '\t')
        {
            indent++;
        }

        return indent;
    }

    private static string? Dedent(SyntaxNode node, SourceText text, int shift) =>
        Dedent(node, text.ToString(node.Span), text, shift);

    /// <summary>
    /// <paramref name="value"/> (the text of <paramref name="node"/>, maybe changed) with every line after the first moved left by
    /// <paramref name="shift"/>; null when a line isn't indented that far or a token of the node spans lines (a string would change).
    /// </summary>
    private static string? Dedent(SyntaxNode node, string value, SourceText text, int shift)
    {
        if (shift == 0)
        {
            return value;
        }

        if (node.DescendantTokens().Any(t => text.Lines.GetLineFromPosition(t.SpanStart).LineNumber != text.Lines.GetLineFromPosition(t.Span.End).LineNumber))
        {
            return null;
        }

        var lines = value.Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim().Length == 0)
            {
                continue;
            }

            if (lines[i].Length < shift || lines[i].Take(shift).Any(c => c is not (' ' or '\t')))
            {
                return null;
            }

            lines[i] = lines[i].Substring(shift);
        }

        return string.Join("\n", lines);
    }

    private sealed class Rendered
    {
        public Rendered(TextChange change, List<IfStatementSyntax> merged)
        {
            this.Change = change;
            this.Merged = merged;
        }

        public TextChange Change { get; }

        /// <summary>Gets the 'if' statements merged into the one above them.</summary>
        public List<IfStatementSyntax> Merged { get; }
    }
}
