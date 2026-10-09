using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1111 (StyleCop SA1127: generic type constraints on their own line).</summary>
internal static class ConstraintPlacement
{
    /// <summary>'own_line' (default, StyleCop's SA1127) or 'same_line' (every 'where' clause on the declaration's line).</summary>
    public const string PlacementKey = "stylebro_constraint_placement";

    /// <summary>The declarations that can have 'where' clauses.</summary>
    public static readonly SyntaxKind[] DeclarationKinds =
    [
        SyntaxKind.ClassDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.InterfaceDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.MethodDeclaration,
        SyntaxKind.DelegateDeclaration,
        SyntaxKind.LocalFunctionStatement,
    ];

    /// <summary>Whether <see cref="PlacementKey"/> is 'same_line'.</summary>
    public static bool IsSameLine(AnalyzerConfigOptions options)
    {
        return options.TryGetValue(PlacementKey, out var value) && value.Trim() == "same_line";
    }

    /// <summary>
    /// For 'same_line': the clauses that start a line, each with the edit that joins it onto the line before (one space).
    /// All or nothing per declaration: empty when a clause spans several lines, anything but whitespace sits before a
    /// clause, or the joined line (up to the last clause's end; a body or '=&gt;' after it isn't counted) would be longer
    /// than 'max_line_length' (no limit when unset). The '=&gt;' of an expression body stays where it is. When BRO1110
    /// is on and moves the ')' before the first clause to the last parameter's line, the ')' moves in the same edit, so
    /// both fixes give the same text in either order. The line is measured as the other fixes leave it
    /// (<see cref="SameLineJoins.GetColumn"/>).
    /// </summary>
    public static List<(TypeParameterConstraintClauseSyntax Clause, TextChange Change)> GetJoins(SyntaxNode declaration, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn, SemanticModel model, CancellationToken cancellationToken)
    {
        var joins = new List<(TypeParameterConstraintClauseSyntax Clause, TextChange Change)>();
        var clauses = GetClauses(declaration);
        if (clauses.Count == 0 || declaration.ContainsDiagnostics)
        {
            return joins;
        }

        var previous = clauses[0].WhereKeyword.GetPreviousToken();
        var start = previous.Span.End;
        var closeText = string.Empty;
        if (Line(text, previous.Span.End) != Line(text, clauses[0].WhereKeyword.SpanStart)
            && isOn(DiagnosticIds.CloseParenthesisOnLastItemLine)
            && ParenthesisPlacement.MovesCloseToLastItem(previous.Parent!, text, options, isOn))
        {
            if (!IsPlainGap(previous.GetPreviousToken(), previous))
            {
                return joins;
            }

            start = previous.GetPreviousToken().Span.End;
            closeText = previous.Text;
        }

        var length = closeText.Length;
        foreach (var clause in clauses)
        {
            var before = clause.WhereKeyword.GetPreviousToken();
            if (Line(text, clause.SpanStart) != Line(text, clause.Span.End) || !IsPlainGap(before, clause.WhereKeyword))
            {
                return [];
            }

            if (Line(text, before.Span.End) == Line(text, clause.WhereKeyword.SpanStart))
            {
                length += clause.WhereKeyword.SpanStart - before.Span.End + clause.Span.Length;
            }
            else
            {
                length += 1 + clause.Span.Length;
                joins.Add(joins.Count == 0 && closeText.Length > 0 && clause == clauses[0]
                    ? (clause, new TextChange(TextSpan.FromBounds(start, clause.WhereKeyword.SpanStart), closeText + " "))
                    : (clause, new TextChange(TextSpan.FromBounds(before.Span.End, clause.WhereKeyword.SpanStart), " ")));
            }
        }

        var maxLength = Indentation.GetMaxLineLength(options);
        return joins.Count > 0 && maxLength != int.MaxValue
            && SameLineJoins.GetColumn(declaration, start, closeText.Length > 0, text, options, isOn, model, cancellationToken) + length > maxLength ? [] : joins;
    }

    /// <summary>
    /// The 'where' clauses that share their line with the code before them. Skipped: a comment or directive right
    /// before the clause (the fix rewrites exactly that gap).
    /// </summary>
    public static IEnumerable<TypeParameterConstraintClauseSyntax> GetMisplacedClauses(SyntaxNode declaration, SourceText text)
    {
        foreach (var clause in GetClauses(declaration))
        {
            var previous = clause.WhereKeyword.GetPreviousToken();
            if (Line(text, previous.Span.End) == Line(text, clause.WhereKeyword.SpanStart) && IsPlainGap(previous, clause.WhereKeyword))
            {
                yield return clause;
            }
        }
    }

    /// <summary>
    /// The edits for one misplaced clause: a line break before 'where', indented one level deeper than the declaration's
    /// line. For the last clause of an expression-bodied member, the '=>' that follows it on the same line gets its own
    /// line too, so the body doesn't read as part of the constraint (like StyleCop's fix).
    /// </summary>
    public static IEnumerable<TextChange> GetChanges(TypeParameterConstraintClauseSyntax clause, SourceText text, string indentUnit)
    {
        var declaration = clause.Parent!;
        var identifierLine = text.Lines.GetLineFromPosition(GetIdentifier(declaration).SpanStart);
        var indentation = new string(text.ToString(identifierLine.Span).TakeWhile(c => c is ' ' or '\t').ToArray()) + indentUnit;
        var lineBreak = text.ToString(TextSpan.FromBounds(identifierLine.End, identifierLine.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        var previous = clause.WhereKeyword.GetPreviousToken();
        yield return new TextChange(TextSpan.FromBounds(previous.Span.End, clause.WhereKeyword.SpanStart), lineBreak + indentation);

        var clauses = GetClauses(declaration);
        if (clause == clauses[clauses.Count - 1]
            && GetExpressionBody(declaration) is { } body
            && Line(text, body.ArrowToken.SpanStart) == Line(text, clause.Span.End)
            && IsPlainGap(body.ArrowToken.GetPreviousToken(), body.ArrowToken))
        {
            yield return new TextChange(TextSpan.FromBounds(body.ArrowToken.GetPreviousToken().Span.End, body.ArrowToken.SpanStart), lineBreak + indentation);
        }
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> GetClauses(SyntaxNode declaration) => declaration switch
    {
        TypeDeclarationSyntax type => type.ConstraintClauses,
        MethodDeclarationSyntax method => method.ConstraintClauses,
        DelegateDeclarationSyntax @delegate => @delegate.ConstraintClauses,
        LocalFunctionStatementSyntax local => local.ConstraintClauses,
        _ => default,
    };

    private static SyntaxToken GetIdentifier(SyntaxNode declaration) => declaration switch
    {
        TypeDeclarationSyntax type => type.Identifier,
        MethodDeclarationSyntax method => method.Identifier,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier,
        LocalFunctionStatementSyntax local => local.Identifier,
        _ => declaration.GetFirstToken(),
    };

    private static ArrowExpressionClauseSyntax? GetExpressionBody(SyntaxNode declaration) => declaration switch
    {
        MethodDeclarationSyntax method => method.ExpressionBody,
        LocalFunctionStatementSyntax local => local.ExpressionBody,
        _ => null,
    };

    private static bool IsPlainGap(SyntaxToken before, SyntaxToken after) =>
        before.TrailingTrivia.Concat(after.LeadingTrivia)
            .All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
