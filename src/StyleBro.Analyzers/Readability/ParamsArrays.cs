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

/// <summary>
/// Shared logic for BRO1151: <c>M(x, new[] { a, b })</c> becomes <c>M(x, a, b)</c> when the array goes to a
/// <see langword="params"/> parameter. Works on the array creation alone (no diagnostic properties), so a fix for another
/// analyzer's id reported on the array can reuse it.
/// </summary>
internal static class ParamsArrays
{
    // The base library's params collections: .NET 9 on, no .NET Standard.
    private static readonly (Version Core, Version? Standard) ParamsCollections = (new Version(9, 0), null);

    /// <summary>
    /// The edits that pass the elements instead of the array, or null. The array (<c>new T[] { ... }</c>,
    /// <c>new[] { ... }</c> or a collection expression <c>[...]</c>) is the call's last argument, unnamed, on one line, of
    /// exactly the params parameter's type (an array of a derived type stays: the callee could rely on it), and the call
    /// with the elements binds to the same method (same type arguments). One element that converts to the array type
    /// itself (<see langword="null"/>, another array) is skipped: the call would pass it as the array. A params collection
    /// (<c>params ReadOnlySpan&lt;T&gt;</c>) in a project with a target framework before .NET 9 is skipped too: the base
    /// library has them from .NET 9 on (C# 13), so the older framework's copy binds the elements to nothing (trial 21,
    /// AngleSharp: <c>SearchValues.Create(['&lt;', '&amp;'])</c> broke net8.0).
    /// </summary>
    public static TextChange[]? GetChanges(ExpressionSyntax array, SemanticModel model, AnalyzerConfigOptions options, CancellationToken cancellationToken)
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
            || IsExtensionMember(method)
            || (parameter.Type is not IArrayTypeSymbol && !Modernize.MultiTargetSuppressor.AllHave(options, ParamsCollections))
            || MayBindElsewhere(method, options, cancellationToken)
            || list.Arguments.Count != method.Parameters.Length
            || !SymbolEqualityComparer.Default.Equals(GetType(model.GetTypeInfo(array, cancellationToken), array), parameter.Type)
            || elements.Any(e => IsTargetTyped(e, model, cancellationToken)))
        {
            return null;
        }

        var changes = removed.Select(span => new TextChange(span, string.Empty)).ToArray();
        var expanded = SyntaxFactory.ParseExpression(text.WithChanges(changes).ToString(new TextSpan(call.SpanStart, call.Span.Length - removed.Sum(s => s.Length))));
        return Speculation.Replace(model, call, expanded) is { } bound
            && SymbolEqualityComparer.Default.Equals(bound.Model.GetSymbolInfo(bound.Node, cancellationToken).Symbol, method)
            && BindsExpanded(bound.Model, bound.Node, list.Arguments.Count - 1, GetElementType(parameter.Type), cancellationToken)
                ? changes
                : null;
    }

    // A member of a C# 14 extension block (its containing type is the extension, TypeKind.Extension, which Roslyn 4.8 doesn't
    // name): the compiler checks the elements passed one by one against the params array's type for nullability and reports
    // CS8620 for each (trial 42, PolyShim: 'Task.WhenEach(a, b, c)' on its Task.WhenEach polyfill broke the build with warnings
    // as errors), so the array stays.
    private static bool IsExtensionMember(IMethodSymbol method) => (int)method.ContainingType.TypeKind == 14;

    // Multi-targeted: the fix from one framework's findings goes into the file every framework compiles, and another
    // framework can bind the elements to an overload this compilation doesn't see (trial 23: net8.0's expansions of
    // string.Join, Task.WhenAll and string.Format bound to .NET 9's 'params ReadOnlySpan' overloads on net10.0). So with
    // frameworks before and from .NET 9, a method from a library isn't expanded; and a method of the solution's own isn't
    // when its type has '#if' in it (an overload behind it).
    private static bool MayBindElsewhere(IMethodSymbol method, AnalyzerConfigOptions options, CancellationToken cancellationToken)
    {
        var frameworks = Modernize.MultiTargetSuppressor.GetFrameworks(options);
        if (frameworks.Count < 2)
        {
            return false;
        }

        var declarations = method.ContainingType.OriginalDefinition.DeclaringSyntaxReferences;
        return declarations.IsEmpty
            ? frameworks.Any(f => Modernize.MultiTargetSuppressor.Has(f, ParamsCollections)) && !Modernize.MultiTargetSuppressor.AllHave(options, ParamsCollections)
            : declarations.Any(d => HasConditionalDirective(d.GetSyntax(cancellationToken)));
    }

    private static bool HasConditionalDirective(SyntaxNode node)
    {
        for (var directive = node.ContainsDirectives ? node.GetFirstDirective() : null;
            directive is not null && directive.SpanStart < node.FullSpan.End;
            directive = directive.GetNextDirective())
        {
            if (directive.IsKind(SyntaxKind.IfDirectiveTrivia))
            {
                return true;
            }
        }

        return false;
    }

    // The element type of a params array or a params collection (C# 13: 'params ReadOnlySpan<T>', 'params List<T>'; a
    // collection expression goes to one, Jellyfin). Null for a collection type with no single type argument: not unwrapped.
    private static ITypeSymbol? GetElementType(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => array.ElementType,
        INamedTypeSymbol { TypeArguments.Length: 1 } named => named.TypeArguments[0],
        _ => null,
    };

    // Whether every argument from 'first' on converts to the params element type, i.e. the call binds in its expanded
    // form. A single element that converts to the array type itself ('default!', '(T[])null', an array variable, an
    // object[] for params object[]) binds in the normal form: it would be passed AS the array (trial 14: a one-element
    // array became null). Asked of the compiler, so no list of shapes can miss one.
    private static bool BindsExpanded(SemanticModel model, SyntaxNode call, int first, ITypeSymbol? elementType, CancellationToken cancellationToken)
    {
        if (elementType is null)
        {
            return false;
        }

        var arguments = call switch
        {
            InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments,
            BaseObjectCreationExpressionSyntax { ArgumentList: { } argumentList } => argumentList.Arguments,
            _ => default,
        };
        for (var i = first; i < arguments.Count; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(arguments[i].Expression, cancellationToken).ConvertedType, elementType))
            {
                return false;
            }
        }

        return true;
    }

    // An element whose meaning comes from the type it converts to: out of the array it could convert to the array type
    // itself (the call's normal form) or to another overload's parameter. 'new()', 'default', 'null', lambdas, anonymous
    // methods, collection expressions, switch expressions, and anything without a type of its own (method groups,
    // conditionals of target-typed parts, parenthesized ones).
    private static bool IsTargetTyped(ExpressionSyntax element, SemanticModel model, CancellationToken cancellationToken) =>
        element is ImplicitObjectCreationExpressionSyntax or AnonymousFunctionExpressionSyntax
            or CollectionExpressionSyntax or SwitchExpressionSyntax or LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression or (int)SyntaxKind.NullLiteralExpression }
        || model.GetTypeInfo(element, cancellationToken).Type is null;

    /// <summary>
    /// Whether the array goes to a <c>params object[]</c>, <c>params Array[]</c> or jagged <c>params T[][]</c> parameter, which Sonar's
    /// S3878 accepts (stylebro_params_arrays = except_object_and_jagged). Call it on an array <see cref="GetChanges"/> fixes.
    /// </summary>
    public static bool GoesToObjectOrJagged(ExpressionSyntax array, SemanticModel model, CancellationToken cancellationToken) =>
        array.Parent?.Parent?.Parent is ExpressionSyntax call
        && model.GetSymbolInfo(call, cancellationToken).Symbol is IMethodSymbol { Parameters.Length: > 0 } method
        && method.Parameters[method.Parameters.Length - 1].Type is IArrayTypeSymbol { ElementType: var element }
        && (element.SpecialType is SpecialType.System_Object or SpecialType.System_Array || element is IArrayTypeSymbol);

    /// <summary>Whether stylebro_params_arrays is except_object_and_jagged (default: all).</summary>
    public static bool ExceptObjectAndJagged(AnalyzerConfigOptions options) =>
        options.TryGetValue("stylebro_params_arrays", out var value) && value.Trim().Equals("except_object_and_jagged", StringComparison.OrdinalIgnoreCase);

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
