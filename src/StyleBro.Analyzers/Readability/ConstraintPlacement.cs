using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1111 (StyleCop SA1127: generic type constraints on their own line).</summary>
internal static class ConstraintPlacement
{
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
