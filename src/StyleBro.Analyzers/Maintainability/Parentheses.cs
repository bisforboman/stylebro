using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>
/// BRO1405 (SA1119): unnecessary parentheses around expressions, which ones follow StyleCop. BRO1410: around patterns.
/// The fix removes the pair.
/// </summary>
internal static class Parentheses
{
    /// <summary>Whether StyleCop's SA1119 reports these parentheses and removing them keeps the code's meaning.</summary>
    public static bool IsUnnecessary(ParenthesizedExpressionSyntax node, SourceText text) =>
        IsReportedByStyleCop(node, node) && ParsesTheSame(new[] { node }, text);

    /// <summary>
    /// Whether removing these pattern parentheses keeps the code's meaning. <paramref name="keepPrecedence"/>: whether
    /// BRO1407 is on and wants them where they separate 'and' from 'or' ('A or (B and C)'); asked only there.
    /// </summary>
    public static bool IsUnnecessary(ParenthesizedPatternSyntax pattern, SourceText text, Func<bool> keepPrecedence) =>
        !(DeclaresPrecedence(pattern) && keepPrecedence()) && ParsesTheSame(new[] { pattern }, text);

    /// <summary>
    /// The pairs nested right inside <paramref name="node"/> (removed with it) that StyleCop reports once the pairs around them
    /// are gone: in '((x * y))' as an argument only the outer pair is SA1119's, the inner one only on the next run (trial 27,
    /// PalCalc). The fix removes them with it, so one run is enough.
    /// </summary>
    public static IEnumerable<ParenthesizedExpressionSyntax> GetNested(ParenthesizedExpressionSyntax node)
    {
        for (var inner = node.Expression as ParenthesizedExpressionSyntax; inner is not null; inner = inner.Expression as ParenthesizedExpressionSyntax)
        {
            if (IsReportedByStyleCop(inner, node))
            {
                yield return inner;
            }
        }
    }

    /// <summary>The '(' and ')' of a parenthesized expression or pattern.</summary>
    public static (SyntaxToken Open, SyntaxToken Close) GetTokens(SyntaxNode node)
    {
        var (open, _, close) = Parts(node);
        return (open, close);
    }

    /// <summary>
    /// The pairs that can be removed together: each one is added only when the enclosing statement or member still parses
    /// the same with it and the ones before it removed (two removals that are fine alone can make a generic call together).
    /// </summary>
    public static List<TextChange> GetChanges(IEnumerable<SyntaxNode> nodes, SourceText text)
    {
        var changes = new List<TextChange>();
        foreach (var group in nodes.Distinct().GroupBy(Container))
        {
            var accepted = new List<SyntaxNode>();
            foreach (var node in group.OrderBy(n => n.SpanStart))
            {
                if (group.Key is not null && ParsesTheSame(accepted.Append(node).ToList(), text))
                {
                    accepted.Add(node);
                }
            }

            changes.AddRange(accepted.SelectMany(n => GetChanges(n, text)!));
        }

        return changes;
    }

    /// <summary>
    /// Removes '(' and ')' and the spaces inside them; a space stays where the tokens would run together. Comments inside
    /// stay; a gap with a comment or directive and a line break is skipped (the ')' line or a '//' comment would leave
    /// code in a bad place).
    /// </summary>
    private static List<TextChange>? GetChanges(SyntaxNode node, SourceText text)
    {
        var (open, inner, close) = Parts(node);
        if (open.IsMissing || close.IsMissing)
        {
            return null;
        }

        var innerFirst = inner.GetFirstToken();
        var innerLast = inner.GetLastToken();
        var openGap = open.TrailingTrivia.Concat(innerFirst.LeadingTrivia).ToList();
        var closeGap = innerLast.TrailingTrivia.Concat(close.LeadingTrivia).ToList();
        if (!IsRemovable(openGap) || !IsRemovable(closeGap))
        {
            return null;
        }

        // Only the whitespace between the token and the nearest comment goes.
        var openEnd = openGap.FirstOrDefault(t => !IsSpace(t)) is { RawKind: not 0 } firstKept ? firstKept.SpanStart : inner.SpanStart;
        var closeStart = closeGap.LastOrDefault(t => !IsSpace(t)) is { RawKind: not 0 } lastKept ? lastKept.Span.End : inner.Span.End;
        var before = open.GetPreviousToken();
        var after = close.GetNextToken();
        var openText = before.Span.End == open.SpanStart && Glues(before, innerFirst) ? " " : string.Empty;
        var closeText = after.SpanStart == close.Span.End && Glues(innerLast, after) ? " " : string.Empty;
        return new List<TextChange>
        {
            new(TextSpan.FromBounds(open.SpanStart, openEnd), openText),
            new(TextSpan.FromBounds(closeStart, close.Span.End), closeText),
        };
    }

    private static bool IsRemovable(List<SyntaxTrivia> gap) =>
        gap.All(IsSpace) || !gap.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia) || t.HasStructure);

    private static bool IsSpace(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia);

    private static bool Glues(SyntaxToken left, SyntaxToken right) =>
        left.Text.Length > 0 && right.Text.Length > 0 && IsWordChar(left.Text[left.Text.Length - 1]) && IsWordChar(right.Text[0]);

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '"' or '$';

    /// <summary>
    /// The edit can't change the code: the enclosing statement or member, parsed again without the parentheses, is the
    /// same tree with the parenthesized expression replaced by its content (catches 'F((a &lt; b), (c &gt; (d)))' turning
    /// into a generic call, and casts like '(T)(-x)').
    /// </summary>
    private static bool ParsesTheSame(IReadOnlyList<SyntaxNode> nodes, SourceText text)
    {
        var container = Container(nodes[0]);
        var changes = new List<TextChange>();
        foreach (var node in nodes)
        {
            if (GetChanges(node, text) is not { } edits)
            {
                return false;
            }

            changes.AddRange(edits);
        }

        if (container is null)
        {
            return false;
        }

        var span = container.FullSpan;
        var shifted = changes.Select(c => new TextChange(new TextSpan(c.Span.Start - span.Start, c.Span.Length), c.NewText!));
        var newText = SourceText.From(text.ToString(span)).WithChanges(shifted).ToString();
        var options = (CSharpParseOptions)container.SyntaxTree.Options;
        SyntaxNode? parsed = container is StatementSyntax
            ? SyntaxFactory.ParseStatement(newText, options: options)
            : SyntaxFactory.ParseMemberDeclaration(newText, options: options);
        if (parsed is null || parsed.ContainsDiagnostics || parsed.FullSpan.Length != newText.Length)
        {
            return false;
        }

        var expected = container.ReplaceNodes(nodes, (original, rewritten) => Parts(rewritten).Inner);
        return parsed.IsEquivalentTo(expected, topLevel: false);
    }

    private static SyntaxNode? Container(SyntaxNode node) =>
        node.Ancestors().FirstOrDefault(a => a is StatementSyntax or MemberDeclarationSyntax);

    /// <summary>Whether SA1119 reports <paramref name="node"/> at the place of <paramref name="place"/> (itself or a pair around it).</summary>
    private static bool IsReportedByStyleCop(ParenthesizedExpressionSyntax node, ExpressionSyntax place)
    {
        var expression = node.Expression;
        var parent = place.Parent;
        if (expression is not (BinaryExpressionSyntax or AssignmentExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax)
            && !expression.IsKind(SyntaxKind.CastExpression)
            && !expression.IsKind(SyntaxKind.ConditionalExpression)
            && !expression.IsKind(SyntaxKind.IsExpression)
            && !expression.IsKind(SyntaxKind.IsPatternExpression)
            && !expression.IsKind(SyntaxKind.SimpleLambdaExpression)
            && !expression.IsKind(SyntaxKind.ParenthesizedLambdaExpression)
            && !expression.IsKind(SyntaxKind.ArrayCreationExpression)
            && !expression.IsKind(SyntaxKind.CoalesceExpression)
            && !expression.IsKind(SyntaxKind.QueryExpression)
            && !expression.IsKind(SyntaxKind.AwaitExpression)
            && !expression.IsKind(SyntaxKind.RangeExpression))
        {
            // A simple expression: always reported, except where the parentheses change how it's parsed.
            if (expression.IsKind(SyntaxKind.ConditionalAccessExpression) && parent is ElementAccessExpressionSyntax or MemberAccessExpressionSyntax or ConditionalAccessExpressionSyntax)
            {
                return false;
            }

            if (expression is SwitchExpressionSyntax or WithExpressionSyntax)
            {
                var outer = place.AncestorsAndSelf().TakeWhile(a => a is ParenthesizedExpressionSyntax).Last();
                if (outer.Parent is AwaitExpressionSyntax or CastExpressionSyntax or MemberAccessExpressionSyntax or ConditionalAccessExpressionSyntax or ElementAccessExpressionSyntax or InvocationExpressionSyntax)
                {
                    return false;
                }
            }

            return !(expression.Kind() is SyntaxKind.StackAllocArrayCreationExpression or SyntaxKind.ImplicitStackAllocArrayCreationExpression && parent is EqualsValueClauseSyntax);
        }

        // An operator expression: only where nothing around it binds tighter (a statement, an argument, an initializer,
        // the right side of an assignment, checked(...)).
        if (parent is InterpolationSyntax && HasConditional(expression))
        {
            return false;
        }

        if (parent is AssignmentExpressionSyntax assignment && assignment.Left == place)
        {
            return false;
        }

        return parent switch
        {
            MemberAccessExpressionSyntax access => access.Expression != place,
            CheckedExpressionSyntax => true,
            EqualsValueClauseSyntax equals => equals.Value == place,
            AssignmentExpressionSyntax => true,
            ExpressionSyntax => false,
            _ => true,
        };
    }

    private static bool HasConditional(ExpressionSyntax expression) => expression switch
    {
        ConditionalExpressionSyntax => true,
        AssignmentExpressionSyntax assignment => HasConditional(assignment.Left) || HasConditional(assignment.Right),
        _ => false,
    };

    /// <summary>
    /// The outermost pair around an 'and' inside 'or' or the other way round, which BRO1407 would add again. Inner pairs
    /// ('A or ((B and C))') are redundant.
    /// </summary>
    private static bool DeclaresPrecedence(ParenthesizedPatternSyntax node)
    {
        var inner = node.Pattern;
        while (inner is ParenthesizedPatternSyntax nested)
        {
            inner = nested.Pattern;
        }

        return inner is BinaryPatternSyntax binary && node.Parent is BinaryPatternSyntax outer && !outer.IsKind(binary.Kind());
    }

    private static (SyntaxToken Open, SyntaxNode Inner, SyntaxToken Close) Parts(SyntaxNode node) => node switch
    {
        ParenthesizedExpressionSyntax e => (e.OpenParenToken, e.Expression, e.CloseParenToken),
        ParenthesizedPatternSyntax p => (p.OpenParenToken, p.Pattern, p.CloseParenToken),
        _ => throw new ArgumentException("Not a parenthesized expression or pattern.", nameof(node)),
    };
}
