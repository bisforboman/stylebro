using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1151: <c>M(x, new[] { a, b })</c> becomes <c>M(x, a, b)</c> when the array goes to a
/// <see langword="params"/> parameter. Works on the array creation alone (no diagnostic properties), so a fix for another
/// analyzer's id reported on the array can reuse it.
/// </summary>
internal static class ParamsArrays
{
    /// <summary>
    /// The edits that pass the elements instead of the array, or null. The array (<c>new T[] { ... }</c>,
    /// <c>new[] { ... }</c> or a collection expression <c>[...]</c>) is the call's last argument, unnamed, on one line, of
    /// exactly the params parameter's type (an array of a derived type stays: the callee could rely on it), and the call
    /// with the elements binds to the same method (same type arguments). One element that converts to the array type
    /// itself (<see langword="null"/>, another array) is skipped: the call would pass it as the array.
    /// </summary>
    public static TextChange[]? GetChanges(ExpressionSyntax array, SemanticModel model, CancellationToken cancellationToken)
    {
        if (GetElements(array) is not { } elements
            || array.Parent is not ArgumentSyntax { NameColon: null, RefKindKeyword.RawKind: 0 } argument
            || argument.Parent is not ArgumentListSyntax list
            || list.Arguments[list.Arguments.Count - 1] != argument
            || list.Parent is not (InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
            || array.ContainsDirectives)
        {
            return null;
        }

        // On one line; and with two or more elements, on the first argument's line: elements on the line of an argument
        // that starts its own line would break BRO1108's (SA1117) "each on its own line".
        var text = array.SyntaxTree.GetText(cancellationToken);
        var line = text.Lines.GetLineFromPosition(array.SpanStart).LineNumber;
        if (line != text.Lines.GetLineFromPosition(array.Span.End).LineNumber
            || (elements.Count > 1 && line != text.Lines.GetLineFromPosition(list.Arguments[0].SpanStart).LineNumber))
        {
            return null;
        }

        // The edits: the text before the first element and after the last goes (a trailing comma too); an empty array
        // takes the comma before it along.
        TextSpan[] removed;
        if (elements.Count == 0)
        {
            var index = list.Arguments.IndexOf(argument);
            removed = new[] { TextSpan.FromBounds(index == 0 ? array.SpanStart : list.Arguments[index - 1].Span.End, array.Span.End) };
        }
        else
        {
            removed = new[]
            {
                TextSpan.FromBounds(array.SpanStart, elements[0].SpanStart),
                TextSpan.FromBounds(elements[elements.Count - 1].Span.End, array.Span.End),
            };
        }

        var call = (ExpressionSyntax)list.Parent;
        if (removed.Any(span => !Trivia.IsBlank(call, span))
            || model.GetSymbolInfo(call, cancellationToken).Symbol is not IMethodSymbol { Parameters.Length: > 0 } method
            || method.Parameters[method.Parameters.Length - 1] is not { IsParams: true } parameter
            || list.Arguments.Count != method.Parameters.Length
            || !SymbolEqualityComparer.Default.Equals(GetType(model.GetTypeInfo(array, cancellationToken), array), parameter.Type)
            || (elements.Count == 1 && model.ClassifyConversion(elements[0], parameter.Type).IsImplicit))
        {
            return null;
        }

        var changes = removed.Select(span => new TextChange(span, string.Empty)).ToArray();
        var expanded = SyntaxFactory.ParseExpression(text.WithChanges(changes).ToString(new TextSpan(call.SpanStart, call.Span.Length - removed.Sum(s => s.Length))));
        return SymbolEqualityComparer.Default.Equals(Speculation.SymbolAfterReplacing(model, call, expanded, cancellationToken), method) ? changes : null;
    }

    // A collection expression has no type of its own; an array's own type must be the parameter's (no covariance).
    private static ITypeSymbol? GetType(TypeInfo info, ExpressionSyntax array) =>
        array is CollectionExpressionSyntax ? info.ConvertedType : info.Type;

    private static IReadOnlyList<ExpressionSyntax>? GetElements(ExpressionSyntax array) => array switch
    {
        ArrayCreationExpressionSyntax { Initializer: { } initializer } creation
            when creation.Type.RankSpecifiers.Count == 1 && creation.Type.RankSpecifiers[0].Sizes.All(s => s is OmittedArraySizeExpressionSyntax) => initializer.Expressions.ToList(),
        ImplicitArrayCreationExpressionSyntax { Commas.Count: 0 } implicitCreation => implicitCreation.Initializer.Expressions.ToList(),
        CollectionExpressionSyntax collection when collection.Elements.All(e => e is ExpressionElementSyntax) =>
            collection.Elements.Select(e => ((ExpressionElementSyntax)e).Expression).ToList(),
        _ => null,
    };
}
