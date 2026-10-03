using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>BRO1405 (SA1119): unnecessary parentheses. Which ones follow StyleCop; the fix removes the pair.</summary>
internal static class Parentheses
{
    /// <summary>Whether StyleCop's SA1119 reports these parentheses and removing them keeps the code's meaning.</summary>
    public static bool IsUnnecessary(ParenthesizedExpressionSyntax node, SourceText text) =>
        !node.IsPartOfStructuredTrivia() && IsReportedByStyleCop(node) && ParsesTheSame(new[] { node }, text);

    /// <summary>
    /// The pairs that can be removed together: each one is added only when the enclosing statement or member still parses
    /// the same with it and the ones before it removed (two removals that are fine alone can make a generic call together).
    /// </summary>
    public static List<TextChange> GetChanges(IEnumerable<ParenthesizedExpressionSyntax> nodes, SourceText text)
    {
        var changes = new List<TextChange>();
        foreach (var group in nodes.Distinct().GroupBy(Container))
        {
            var accepted = new List<ParenthesizedExpressionSyntax>();
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

    /// <summary>Removes '(' and ')' and the spaces inside them; a space stays where the tokens would run together.</summary>
    private static List<TextChange>? GetChanges(ParenthesizedExpressionSyntax node, SourceText text)
    {
        var open = node.OpenParenToken;
        var close = node.CloseParenToken;
        if (open.IsMissing || close.IsMissing
            || !open.TrailingTrivia.Concat(close.LeadingTrivia).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        var inner = node.Expression;
        var before = open.GetPreviousToken();
        var after = close.GetNextToken();
        var openText = before.Span.End == open.SpanStart && Glues(before, inner.GetFirstToken()) ? " " : string.Empty;
        var closeText = after.SpanStart == close.Span.End && Glues(inner.GetLastToken(), after) ? " " : string.Empty;
        return new List<TextChange>
        {
            new(TextSpan.FromBounds(open.SpanStart, inner.SpanStart), openText),
            new(TextSpan.FromBounds(inner.Span.End, close.Span.End), closeText),
        };
    }

    private static bool Glues(SyntaxToken left, SyntaxToken right) =>
        left.Text.Length > 0 && right.Text.Length > 0 && IsWordChar(left.Text[left.Text.Length - 1]) && IsWordChar(right.Text[0]);

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '"' or '$';

    /// <summary>
    /// The edit can't change the code: the enclosing statement or member, parsed again without the parentheses, is the
    /// same tree with the parenthesized expression replaced by its content (catches 'F((a &lt; b), (c &gt; (d)))' turning
    /// into a generic call, and casts like '(T)(-x)').
    /// </summary>
    private static bool ParsesTheSame(IReadOnlyList<ParenthesizedExpressionSyntax> nodes, SourceText text)
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

        var expected = container.ReplaceNodes(nodes, (original, rewritten) => ((ParenthesizedExpressionSyntax)rewritten).Expression);
        return parsed.IsEquivalentTo(expected, topLevel: false);
    }

    private static SyntaxNode? Container(SyntaxNode node) =>
        node.Ancestors().FirstOrDefault(a => a is StatementSyntax or MemberDeclarationSyntax);

    private static bool IsReportedByStyleCop(ParenthesizedExpressionSyntax node)
    {
        var expression = node.Expression;
        var parent = node.Parent;
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
                var outer = node.AncestorsAndSelf().TakeWhile(a => a is ParenthesizedExpressionSyntax).Last();
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

        if (parent is AssignmentExpressionSyntax assignment && assignment.Left == node)
        {
            return false;
        }

        return parent switch
        {
            MemberAccessExpressionSyntax access => access.Expression != node,
            CheckedExpressionSyntax => true,
            EqualsValueClauseSyntax equals => equals.Value == node,
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
}
