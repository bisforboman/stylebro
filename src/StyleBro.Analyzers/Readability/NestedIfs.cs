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
    /// <summary>Nested ifs with a dynamic condition are fine (default false): Sonar's S1066 skips them.</summary>
    public const string AllowDynamicKey = "stylebro_allow_nested_dynamic_ifs";

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

        // Conditional directives anywhere in the outer 'if' (also in the body), or right after it (an 'else' only some target
        // frameworks' copies see): each copy could see other code. Not '#region' or '#pragma' lines: BRO1112/BRO1113 remove
        // regions, and when an '#endregion' right after the 'if' kept it, the merge came on the next run (Flow.Launcher's
        // ShellContextMenu). In the text the merge removes they still count (the gap checks below).
        return inner is null
            || outer.Else is not null
            || inner.Else is not null
            || (outer.ContainsDirectives && HasConditionalDirective(outer.DescendantTrivia()))
            || HasConditionalDirective(outer.GetLastToken().GetNextToken().LeadingTrivia)
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
    /// may have run that rule already. With <see cref="AllowDynamicKey"/>, a condition with a part of type dynamic anywhere in
    /// the merge leaves the nest alone (like Sonar's S1066; it needs the semantic model, so it's an option).
    /// </summary>
    public static TextChange? GetChange(IfStatementSyntax inner, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn, SemanticModel model)
    {
        if (GetOuter(inner) is null)
        {
            return null;
        }

        // The chains around it, outermost first: the first that merges this 'if' decides.
        foreach (var top in inner.Ancestors().OfType<IfStatementSyntax>().Where(i => GetOuter(i) is null && GetInner(i) is not null).Reverse())
        {
            if (GetChange(top, inner, text, options, isOn, model) is { } change)
            {
                return change;
            }
        }

        return null;
    }

    private static bool IsDynamic(ExpressionSyntax condition, SemanticModel model) =>
        condition.DescendantNodesAndSelf().OfType<ExpressionSyntax>().Any(e => model.GetTypeInfo(e).Type is { TypeKind: TypeKind.Dynamic });

    private static bool HasConditionalDirective(IEnumerable<SyntaxTrivia> trivia) =>
        trivia.Any(t => t.IsKind(SyntaxKind.IfDirectiveTrivia) || t.IsKind(SyntaxKind.ElifDirectiveTrivia) || t.IsKind(SyntaxKind.ElseDirectiveTrivia)
            || t.IsKind(SyntaxKind.EndIfDirectiveTrivia) || t.IsKind(SyntaxKind.DisabledTextTrivia));

    private static TextChange? GetChange(IfStatementSyntax top, IfStatementSyntax inner, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn, SemanticModel model)
    {
        var layout = CSharpierLayout.PrintWidth(options) is { } width ? (width, Indentation.GetUnit(options)) : ((int, string)?)null;
        if (Render(top, text, OperatorAtBeginning(top, text, options, isOn), layout) is not { } rendered || !rendered.Merged.Contains(inner)
            || (options.TryGetValue(AllowDynamicKey, out var allow) && bool.TryParse(allow.Trim(), out var allowed) && allowed
                && rendered.Merged.Any(m => IsDynamic(m.Condition, model) || IsDynamic(GetOuter(m)!.Condition, model))))
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
            .SelectMany(n => Braces.GetFindings(n, merged, isOn, options, afterFix: true))
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
    /// Where a wrapped '&amp;&amp;' goes: where dotnet_style_operator_placement_when_wrapping says (the beginning by default)
    /// when BRO1520 enforces it. Otherwise at the end of the line when the chain's conditions already wrap after an operator
    /// (sourcegit sets beginning_of_line but writes 254 of 264 operators at the end; init turns BRO1520 off there), else
    /// still where the key says: AzureEventGridSimulator (CSharpier, which turns BRO1520 off) puts all 189 at the beginning.
    /// </summary>
    private static bool OperatorAtBeginning(IfStatementSyntax top, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        var beginning = WrappingPlacement.Read(options, WrappingPlacement.OperatorKey, beginning: true);
        if (isOn(DiagnosticIds.OperatorPlacement))
        {
            return beginning;
        }

        for (var current = top; current is not null; current = GetInner(current))
        {
            if (current.Condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()
                .Any(b => text.Lines.GetLineFromPosition(b.OperatorToken.Span.End).LineNumber < text.Lines.GetLineFromPosition(b.Right.SpanStart).LineNumber))
            {
                return false;
            }
        }

        return beginning;
    }

    /// <summary>
    /// The merged chain starting at <paramref name="top"/>: one edit from its condition to its end, with the chains in the
    /// deepest body merged too. The body moves left by as much as its 'if' is indented deeper than the top one's line. Once
    /// the condition so far spans lines, the next one starts a line of its own, aligned with the top condition, with
    /// '&amp;&amp;' where <paramref name="beginning"/> says (dotnet_style_operator_placement_when_wrapping), and its other
    /// lines keep their indentation relative to it (Flow.Launcher's 'HandleMenuMsg(' went to the end of the wrapped
    /// condition's last line, its arguments below at the old place).
    /// </summary>
    private static Rendered? Render(IfStatementSyntax top, SourceText text, bool beginning, (int Width, string Unit)? csharpier)
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
        var topColumn = top.Condition.SpanStart - text.Lines.GetLineFromPosition(top.Condition.SpanStart).Start;
        var topLine = text.Lines.GetLineFromPosition(top.SpanStart);
        var lineBreak = SingleLineBlocks.LineBreak(text, top.SpanStart) + text.ToString(new TextSpan(topLine.Start, topIndent)) + new string(' ', System.Math.Max(0, topColumn - topIndent));
        foreach (var current in ifs)
        {
            var parenthesized = NeedsParentheses(current.Condition);
            var ownLine = current != top && builder.ToString().IndexOf('\n') >= 0;
            var condition = Dedent(current.Condition, text, current == top ? 0 : IndentOf(text, current.SpanStart) - (ownLine ? topColumn : topIndent));
            if (condition is null)
            {
                return null;
            }

            builder.Append(current == top ? string.Empty : !ownLine ? " && " : beginning ? lineBreak + "&& " : " &&" + lineBreak)
                .Append(parenthesized ? "(" + condition + ")" : condition);
        }

        // CSharpier lays the code out: the condition as CSharpier writes it, from the '(' (the top one may already be broken).
        var start = top.Condition.SpanStart;
        if (csharpier is { } layout && CSharpierCondition(ifs, text, top, layout.Width, layout.Unit) is { } laidOut)
        {
            builder.Clear().Append(laidOut);
            start = top.OpenParenToken.Span.End;
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
            if (node is IfStatementSyntax candidate && GetOuter(candidate) is null && GetInner(candidate) is not null && Render(candidate, text, beginning, csharpier) is { } inside)
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
        return new Rendered(new TextChange(TextSpan.FromBounds(start, top.Span.End), builder.ToString()), merged);
    }

    /// <summary>
    /// The merged condition the way CSharpier writes it (<see cref="CSharpierLayout"/>), up to the closing ')': on the 'if' line
    /// when it fits in <paramref name="width"/>, else every '&amp;&amp;' operand on a line of its own, one level deeper, the
    /// operator first, and ')' on a line of its own. An operand that doesn't fit its line and is an '||' chain (parenthesized
    /// by the merge) breaks inside its parentheses: '&amp;&amp; (', each '||' operand on a line of its own one level deeper,
    /// ')' (EntityGraphQL's GraphQLParser: an inner condition CSharpier had already broken that way). Null (the usual layout)
    /// when another operand, or an '||' operand, spans lines or doesn't fit on its line: CSharpier would break it further.
    /// Without this, CSharpier relaid every merge whose condition got too long (StabilityMatrix: 10 of the 16 files it rejected
    /// after StyleBro's format).
    /// </summary>
    private static string? CSharpierCondition(List<IfStatementSyntax> ifs, SourceText text, IfStatementSyntax top, int width, string unit)
    {
        var operands = new List<(string Flat, List<string>? Ors)>();
        foreach (var current in ifs)
        {
            foreach (var operand in NeedsParentheses(current.Condition) ? new[] { current.Condition } : AndOperands(current.Condition))
            {
                var parts = (operand.IsKind(SyntaxKind.LogicalOrExpression) ? OrOperands(operand) : new[] { operand }).Select(o => text.ToString(o.Span)).ToList();
                if (parts.Any(p => p.IndexOf('\n') >= 0))
                {
                    return null;
                }

                var value = string.Join(" || ", parts);
                operands.Add(NeedsParentheses(operand) ? ("(" + value + ")", parts.Count > 1 ? parts : null) : (value, null));
            }
        }

        var line = text.Lines.GetLineFromPosition(top.SpanStart);
        var flat = string.Join(" && ", operands.Select(o => o.Flat));
        if (top.OpenParenToken.Span.End - line.Start + flat.Length + 1 <= width
            && text.Lines.GetLineFromPosition(top.OpenParenToken.SpanStart).LineNumber == line.LineNumber)
        {
            return flat;
        }

        var lineBreak = SingleLineBlocks.LineBreak(text, top.SpanStart);
        var indent = text.ToString(new TextSpan(line.Start, IndentOf(text, top.SpanStart)));
        var builder = new StringBuilder();
        for (var i = 0; i < operands.Count; i++)
        {
            var prefix = indent + unit + (i == 0 ? string.Empty : "&& ");
            if ((prefix + operands[i].Flat).Length <= width)
            {
                builder.Append(lineBreak).Append(prefix).Append(operands[i].Flat);
                continue;
            }

            var ors = operands[i].Ors;
            if (ors is null)
            {
                return null;
            }

            builder.Append(lineBreak).Append(prefix).Append('(');
            for (var j = 0; j < ors.Count; j++)
            {
                var orLine = indent + unit + unit + (j == 0 ? string.Empty : "|| ") + ors[j];
                if (orLine.Length > width)
                {
                    return null;
                }

                builder.Append(lineBreak).Append(orLine);
            }

            builder.Append(lineBreak).Append(indent).Append(unit).Append(')');
        }

        return builder.Append(lineBreak).Append(indent).ToString();
    }

    /// <summary>The operands of an '||' chain.</summary>
    private static IEnumerable<ExpressionSyntax> OrOperands(ExpressionSyntax condition) =>
        condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } or
            ? OrOperands(or.Left).Concat(OrOperands(or.Right))
            : new[] { condition };

    /// <summary>The operands of an '&amp;&amp;' chain (the expression itself when it isn't one).</summary>
    private static IEnumerable<ExpressionSyntax> AndOperands(ExpressionSyntax condition) =>
        condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalAndExpression } and
            ? AndOperands(and.Left).Concat(AndOperands(and.Right))
            : new[] { condition };

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
    /// <paramref name="shift"/> (right for a negative one); null when a line isn't indented that far or a token of the node spans
    /// lines (a string would change).
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

            if (shift < 0 && lines[i][0] != '\t')
            {
                lines[i] = new string(' ', -shift) + lines[i];
                continue;
            }

            if (shift < 0 || lines[i].Length < shift || lines[i].Take(shift).Any(c => c is not (' ' or '\t')))
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
