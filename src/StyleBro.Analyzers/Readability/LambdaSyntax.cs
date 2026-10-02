using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1125 (StyleCop SA1130: lambda syntax instead of anonymous methods) and BRO1403 (SA1410: no
/// empty parentheses after 'delegate').
/// </summary>
internal static class LambdaSyntax
{
    /// <summary>
    /// BRO1125: the anonymous method as a lambda, like StyleCop's fix: parameter types are dropped
    /// (<c>delegate (object s, EventArgs e) { }</c> becomes <c>(s, e) =&gt; { }</c>), a missing parameter list gets the
    /// delegate's parameter names (made unique), and a cast's operand gets parentheses. Null unless the lambda converts
    /// to the same delegate type and, as an argument, calls the same overload (checked by binding it in place). Skipped:
    /// parameters with modifiers, attributes or defaults, comments in the header, ref/out parameters it would have to
    /// invent names for.
    /// </summary>
    public static TextChange? GetLambda(AnonymousMethodExpressionSyntax method, SemanticModel model, CancellationToken cancellationToken)
    {
        if (method.ContainsDiagnostics || model.GetTypeInfo(method, cancellationToken).ConvertedType is not INamedTypeSymbol { DelegateInvokeMethod: { } invoke })
        {
            return null;
        }

        var header = TextSpan.FromBounds(method.SpanStart, method.Block.SpanStart);
        if (method.DescendantTrivia(header).Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        List<string> names;
        if (method.ParameterList is { } list)
        {
            if (list.Parameters.Any(p => p.Modifiers.Count > 0 || p.AttributeLists.Count > 0 || p.Default is not null))
            {
                return null;
            }

            names = list.Parameters.Select(p => p.Identifier.ValueText).ToList();
        }
        else
        {
            if (invoke.Parameters.Any(p => p.RefKind != RefKind.None || p.IsParams))
            {
                return null;
            }

            // Names the body doesn't use and nothing in scope has (the body can't refer to the parameters anyway).
            var taken = new HashSet<string>(method.Block.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText));
            names = new List<string>();
            foreach (var parameter in invoke.Parameters)
            {
                var name = parameter.Name;
                for (var i = 1; taken.Contains(name) || names.Contains(name) || model.LookupSymbols(method.SpanStart, name: name).Length > 0; i++)
                {
                    name = parameter.Name + i;
                }

                names.Add(SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name);
            }
        }

        var parameters = names.Count == 1 ? names[0] : "(" + string.Join(", ", names) + ")";
        var modifiers = method.ToString().Substring(0, method.DelegateKeyword.SpanStart - method.SpanStart);
        var beforeBody = method.Block.GetFirstToken().GetPreviousToken();
        var gap = method.SyntaxTree.GetText(cancellationToken).ToString(TextSpan.FromBounds(beforeBody.Span.End, method.Block.SpanStart));
        var lambda = modifiers + parameters + " =>" + (gap.Contains("\n") ? gap : " ") + method.Block;
        if (method.Parent is not (ArgumentSyntax or EqualsValueClauseSyntax or AssignmentExpressionSyntax or ReturnStatementSyntax
            or ArrowExpressionClauseSyntax or ParenthesizedExpressionSyntax or YieldStatementSyntax or InitializerExpressionSyntax))
        {
            lambda = "(" + lambda + ")";
        }

        return Speculation.BindsTheSame(model, method, lambda, cancellationToken) ? new TextChange(method.Span, lambda) : null;
    }

    /// <summary>
    /// BRO1403: the edit that removes an empty parameter list after 'delegate' (<c>delegate() { }</c>), or null. Like
    /// StyleCop, kept when removing it would make an overloaded call ambiguous.
    /// </summary>
    public static TextChange? GetEmptyParameterList(AnonymousMethodExpressionSyntax method, SemanticModel model, CancellationToken cancellationToken)
    {
        if (method.ContainsDiagnostics || method.ParameterList is not { Parameters.Count: 0 } list
            || list.DescendantTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        var text = method.SyntaxTree.GetText(cancellationToken);
        // 'delegate (){' keeps a space: 'delegate {'.
        var change = new TextChange(TextSpan.FromBounds(method.DelegateKeyword.Span.End, list.Span.End), list.GetTrailingTrivia().Any() ? string.Empty : " ");
        var withoutList = text.ToString(TextSpan.FromBounds(method.SpanStart, method.DelegateKeyword.Span.End)) + change.NewText
            + text.ToString(TextSpan.FromBounds(list.Span.End, method.Span.End));
        return Speculation.BindsTheSame(model, method, withoutList, cancellationToken) ? change : null;
    }
}
