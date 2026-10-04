using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// BRO1520 (binary and conditional operators), BRO1521 ('=>' of expression bodies and switch expression arms) and
/// BRO1522 ('=' of assignments and initializers): where the token goes when the line breaks right before or after it.
/// Only the token moves: the edit rewrites the two gaps around it, keeps the line break and the indentation of the
/// continuation line, and puts one space between the token and the code on its line.
/// </summary>
internal static class WrappingPlacement
{
    /// <summary>The SDK's key for operators (no SDK rule enforces it).</summary>
    public const string OperatorKey = "dotnet_style_operator_placement_when_wrapping";

    /// <summary>StyleBro's key for '=>'.</summary>
    public const string ArrowKey = "stylebro_arrow_placement_when_wrapping";

    /// <summary>StyleBro's key for '='.</summary>
    public const string EqualsKey = "stylebro_equals_placement_when_wrapping";

    private static readonly char[] LineBreaks = ['\r', '\n'];

    /// <summary>Every finding in the tree: the rule, the token, whether it belongs at the line's beginning, and the edit.</summary>
    public static IEnumerable<(string Id, SyntaxToken Token, bool Beginning, TextChange Change)> GetFindings(SyntaxNode root, SourceText text, AnalyzerConfigOptions options)
    {
        var operators = Read(options, OperatorKey, beginning: true);
        var arrows = Read(options, ArrowKey, beginning: false);
        var equals = Read(options, EqualsKey, beginning: false);
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case BinaryExpressionSyntax binary:
                    if (Find(binary.OperatorToken, operators, text) is { } b)
                    {
                        yield return (DiagnosticIds.OperatorPlacement, binary.OperatorToken, operators, b);
                    }

                    break;
                case ConditionalExpressionSyntax conditional:
                    if (Find(conditional.QuestionToken, operators, text) is { } q)
                    {
                        yield return (DiagnosticIds.OperatorPlacement, conditional.QuestionToken, operators, q);
                    }

                    if (Find(conditional.ColonToken, operators, text) is { } c)
                    {
                        yield return (DiagnosticIds.OperatorPlacement, conditional.ColonToken, operators, c);
                    }

                    break;

                // After a 'where' constraint, BRO1111 places the '=>' (on its own line, like StyleCop's fix).
                case ArrowExpressionClauseSyntax arrow when !arrow.ArrowToken.GetPreviousToken().Parent!.AncestorsAndSelf().Any(a => a is TypeParameterConstraintClauseSyntax):
                    if (Find(arrow.ArrowToken, arrows, text) is { } a)
                    {
                        yield return (DiagnosticIds.ArrowPlacement, arrow.ArrowToken, arrows, a);
                    }

                    break;
                case SwitchExpressionArmSyntax arm:
                    if (Find(arm.EqualsGreaterThanToken, arrows, text) is { } s)
                    {
                        yield return (DiagnosticIds.ArrowPlacement, arm.EqualsGreaterThanToken, arrows, s);
                    }

                    break;
                case AssignmentExpressionSyntax assignment:
                    if (Find(assignment.OperatorToken, equals, text) is { } e)
                    {
                        yield return (DiagnosticIds.EqualsPlacement, assignment.OperatorToken, equals, e);
                    }

                    break;
                case EqualsValueClauseSyntax clause:
                    if (Find(clause.EqualsToken, equals, text) is { } i)
                    {
                        yield return (DiagnosticIds.EqualsPlacement, clause.EqualsToken, equals, i);
                    }

                    break;
            }
        }
    }

    /// <summary>A placement key: beginning_of_line or end_of_line ('end_of_line:warning' style values too).</summary>
    public static bool Read(AnalyzerConfigOptions options, string key, bool beginning) =>
        !options.TryGetValue(key, out var value) ? beginning
        : value.Split(':')[0].Trim().ToLowerInvariant() switch
        {
            "beginning_of_line" => true,
            "end_of_line" => false,
            _ => beginning,
        };

    /// <summary>
    /// The edit that moves <paramref name="token"/> to the wanted side of the line break, or null when it's already there,
    /// there's no line break next to it, or the case is skipped: a line break on both sides, anything but whitespace and
    /// line breaks around it (comments, directives), syntax errors, a '{'/'[' after it (an initializer or collection
    /// expression on its own line), or a query after it (BRO1127-BRO1130 indent a query's clauses by whether 'from'
    /// starts its line, so moving the token there would make them re-indent the query).
    /// </summary>
    private static TextChange? Find(SyntaxToken token, bool beginning, SourceText text)
    {
        var previous = token.GetPreviousToken(includeZeroWidth: true);
        var next = token.GetNextToken(includeZeroWidth: true);
        if (token.IsMissing || previous.IsMissing || next.IsMissing
            || next.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.OpenBracketToken
            || next.Parent is FromClauseSyntax { Parent: QueryExpressionSyntax })
        {
            return null;
        }

        var before = previous.TrailingTrivia.Concat(token.LeadingTrivia).ToList();
        var after = token.TrailingTrivia.Concat(next.LeadingTrivia).ToList();
        if (!before.Concat(after).All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        var breakBefore = before.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        var breakAfter = after.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (breakBefore == breakAfter || breakBefore == beginning)
        {
            return null;
        }

        // The gap with the line break keeps everything from the line break on (blank lines, the indentation); the
        // other gap, on one line, becomes a single space.
        if (beginning)
        {
            var gap = text.ToString(TextSpan.FromBounds(token.Span.End, next.SpanStart));
            return new TextChange(TextSpan.FromBounds(previous.Span.End, next.SpanStart), gap.Substring(gap.IndexOfAny(LineBreaks)) + token.Text + " ");
        }
        else
        {
            var gap = text.ToString(TextSpan.FromBounds(previous.Span.End, token.SpanStart));
            return new TextChange(TextSpan.FromBounds(previous.Span.End, next.SpanStart), " " + token.Text + gap.Substring(gap.IndexOfAny(LineBreaks)));
        }
    }
}
