using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
    /// another tree). <paramref name="options"/> decides between the LINQ call and a List's or array's own method
    /// (<see cref="GetCollectionChanges"/>). Name is the call written, Terminal the call removed.
    /// </summary>
    public static (string Name, string Terminal, TextChange[] Changes)? GetChanges(InvocationExpressionSyntax where, SemanticModel model, AnalyzerConfigOptions options, CancellationToken cancellationToken)
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

        if (GetCollectionChanges(where, terminal, name, argument, model, options, text, cancellationToken) is { } collection)
        {
            return (collection.Name, name, collection.Changes);
        }

        // Two edits ('Where' renamed, ').Count()' cut), so a 'Where' inside the predicate gets its own, separate edits.
        return (name, name, new[] { new TextChange(whereName.Span, name), new TextChange(TextSpan.FromBounds(argument.Span.End, terminal.Span.End), ")") });
    }

    /// <summary>
    /// The owner's decision (2026-10-10): where Sonar's S6605 (<c>Exists</c> instead of <c>Any</c>) or S6602 (<c>Find</c>
    /// instead of <c>FirstOrDefault</c>) is on, <c>Any</c>/<c>FirstOrDefault</c> on a <c>List&lt;T&gt;</c> becomes the
    /// list's <c>Exists</c>/<c>Find</c> and on an array <c>Array.Exists</c>/<c>Array.Find</c> (the same results; the LINQ
    /// call would get Sonar's warning, which breaks builds with TreatWarningsAsErrors). Null otherwise, or when that call
    /// wouldn't bind (a <c>Func</c> predicate isn't a <c>Predicate</c>; no <c>using System;</c> for <c>Array</c>).
    /// </summary>
    private static (string Name, TextChange[] Changes)? GetCollectionChanges(
        InvocationExpressionSyntax where,
        InvocationExpressionSyntax terminal,
        string name,
        ArgumentSyntax argument,
        SemanticModel model,
        AnalyzerConfigOptions options,
        SourceText text,
        CancellationToken cancellationToken)
    {
        var method = name switch
        {
            "Any" => "Exists",
            "FirstOrDefault" => "Find",
            _ => null,
        };
        var access = (MemberAccessExpressionSyntax)where.Expression;
        var type = method is null ? null : model.GetTypeInfo(access.Expression, cancellationToken).Type;
        var isArray = type is IArrayTypeSymbol { IsSZArray: true };
        if (method is null
            || (!isArray && !IsList(type))
            || !SonarRules.IsOn(model, options, name == "Any" ? "S6605" : "S6602", cancellationToken))
        {
            return null;
        }

        var receiver = access.Expression;
        var predicate = text.ToString(argument.Span);
        var call = isArray
            ? "Array." + method + "(" + text.ToString(receiver.Span) + ", " + predicate + ")"
            : text.ToString(TextSpan.FromBounds(terminal.SpanStart, access.Name.SpanStart)) + method + "(" + predicate + ")";
        if (Speculation.SymbolAfterReplacing(model, terminal, SyntaxFactory.ParseExpression(call), cancellationToken) is not IMethodSymbol symbol
            || !(isArray ? symbol.ContainingType.SpecialType == SpecialType.System_Array : IsList(symbol.ContainingType)))
        {
            return null;
        }

        var tail = new TextChange(TextSpan.FromBounds(argument.Span.End, terminal.Span.End), ")");
        if (!isArray)
        {
            return (method, new[] { new TextChange(access.Name.Span, method), tail });
        }

        // 'items.Where(p).Any()' -> 'Array.Exists(items, p)': the receiver moves into the call, so the text between it and
        // 'Where' goes too (a comment there would be lost).
        var gap = TextSpan.FromBounds(receiver.Span.End, argument.SpanStart);
        return Trivia.IsBlank(terminal, TextSpan.FromBounds(receiver.Span.End, access.Name.SpanStart))
            ? ("Array." + method, new[] { new TextChange(new TextSpan(receiver.SpanStart, 0), "Array." + method + "("), new TextChange(gap, ", "), tail })
            : null;
    }

    private static bool IsList(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.OriginalDefinition is { Name: "List", Arity: 1, ContainingNamespace: { Name: "Generic", ContainingNamespace: { Name: "Collections", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } } })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The static form of an Enumerable or Queryable method, or null.</summary>
    private static IMethodSymbol? GetLinqMethod(ISymbol? symbol) =>
        symbol is IMethodSymbol method
        && (method.ReducedFrom ?? method) is { ContainingType: { Name: "Enumerable" or "Queryable", ContainingNamespace: { Name: "Linq", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } } } linq
            ? linq
            : null;
}
