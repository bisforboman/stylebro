using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1136: <c>x =&gt; x</c> instead of <c>(x) =&gt; x</c>.</summary>
internal static class LambdaParentheses
{
    /// <summary>
    /// The edit that replaces the parameter list with the parameter's name, or null when the parentheses are needed: more
    /// or fewer than one parameter, a type (which a default value needs), modifier or attribute, a lambda with attributes
    /// or an explicit return type (both need the parentheses), or anything but whitespace inside them.
    /// </summary>
    public static TextChange? GetChange(ParenthesizedLambdaExpressionSyntax lambda)
    {
        if (lambda.ParameterList.Parameters.Count != 1
            || lambda.ParameterList.Parameters[0] is not { Type: null } parameter
            || parameter.Modifiers.Count > 0
            || parameter.AttributeLists.Count > 0
            || lambda.AttributeLists.Count > 0
            || lambda.ReturnType is not null
            || lambda.ParameterList.DescendantTrivia(lambda.ParameterList.Span)
                .Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return null;
        }

        return new TextChange(lambda.ParameterList.Span, parameter.Identifier.Text);
    }
}
