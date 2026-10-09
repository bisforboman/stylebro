using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1150: <c>source.Where(p).Count()</c> becomes <c>source.Count(p)</c>. Works on the <c>Where</c>
/// invocation alone (no diagnostic properties), so a fix for another analyzer's id reported on <c>Where</c> can reuse it.
/// </summary>
internal static class WhereCalls
{
    private static readonly string[] Terminals =
    {
        "Any", "Count", "LongCount", "First", "FirstOrDefault", "Last", "LastOrDefault", "Single", "SingleOrDefault",
    };

    /// <summary>
    /// The terminal call's name and the edits that pass the predicate to it, or null. Only System.Linq's Enumerable and
    /// Queryable methods, and only where the call with the predicate binds to their overload with it (so a type's own
    /// <c>Count(...)</c> or another extension in scope can't take over). Skipped: a comment or directive in the removed
    /// text, <c>?.</c>, explicit type arguments, <see langword="ref"/> arguments, and expression trees (a query provider sees
    /// another tree).
    /// </summary>
    public static (string Name, TextChange[] Changes)? GetChanges(InvocationExpressionSyntax where, SemanticModel model, CancellationToken cancellationToken)
    {
        if (where.Expression is not MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "Where" } whereName }
            || where.ArgumentList.Arguments.Count != 1
            || where.ArgumentList.Arguments[0] is not { RefKindKeyword.RawKind: 0 } argument
            || where.Parent is not MemberAccessExpressionSyntax { Name: IdentifierNameSyntax terminalName } terminalAccess
            || terminalAccess.Expression != where
            || terminalAccess.Parent is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } terminal
            || Array.IndexOf(Terminals, terminalName.Identifier.ValueText) < 0
            || !Trivia.IsBlank(terminal, TextSpan.FromBounds(whereName.Span.End, argument.SpanStart))
            || !Trivia.IsBlank(terminal, TextSpan.FromBounds(argument.Span.End, terminal.Span.End)))
        {
            return null;
        }

        var name = terminalName.Identifier.ValueText;
        if (GetLinqMethod(model.GetSymbolInfo(where, cancellationToken).Symbol) is null
            || GetLinqMethod(model.GetSymbolInfo(terminal, cancellationToken).Symbol) is not { Parameters.Length: 1 }
            || NullChecks.IsInExpressionTree(terminal, model, cancellationToken))
        {
            return null;
        }

        var text = where.SyntaxTree.GetText(cancellationToken);
        var replacement = name + "(" + text.ToString(argument.Span) + ")";
        var call = SyntaxFactory.ParseExpression(text.ToString(TextSpan.FromBounds(terminal.SpanStart, whereName.SpanStart)) + replacement);
        if (GetLinqMethod(Speculation.SymbolAfterReplacing(model, terminal, call, cancellationToken)) is null)
        {
            return null;
        }

        // Two edits ('Where' renamed, ').Count()' cut), so a 'Where' inside the predicate gets its own, separate edits.
        return (name, new[] { new TextChange(whereName.Span, name), new TextChange(TextSpan.FromBounds(argument.Span.End, terminal.Span.End), ")") });
    }

    /// <summary>The static form of an Enumerable or Queryable method, or null.</summary>
    private static IMethodSymbol? GetLinqMethod(ISymbol? symbol) =>
        symbol is IMethodSymbol method
        && (method.ReducedFrom ?? method) is { ContainingType: { Name: "Enumerable" or "Queryable", ContainingNamespace: { Name: "Linq", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } } } linq
            ? linq
            : null;
}
