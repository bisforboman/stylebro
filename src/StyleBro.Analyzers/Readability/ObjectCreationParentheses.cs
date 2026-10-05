using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1141: an object creation with an initializer and no arguments is written <c>new T { ... }</c>
/// (<c>stylebro_object_creation_parentheses = omit</c>, the default) or <c>new T() { ... }</c> (<c>include</c>).
/// Target-typed <c>new() { ... }</c> needs its parentheses and is another kind of node.
/// </summary>
internal static class ObjectCreationParentheses
{
    /// <summary>The edit that removes or adds the parentheses, or null (also when a comment sits between the type and them).</summary>
    public static TextChange? GetChange(ObjectCreationExpressionSyntax creation, AnalyzerConfigOptions options)
    {
        if (creation.Initializer is null || creation.ContainsDiagnostics)
        {
            return null;
        }

        var include = options.TryGetValue("stylebro_object_creation_parentheses", out var value)
            && value.Trim().Equals("include", StringComparison.OrdinalIgnoreCase);
        if (creation.ArgumentList is not { } arguments)
        {
            return include ? new TextChange(new TextSpan(creation.Type.Span.End, 0), "()") : null;
        }

        var span = TextSpan.FromBounds(creation.Type.Span.End, arguments.Span.End);
        return include || arguments.Arguments.Count > 0 || !Trivia.IsBlank(creation, span) ? null : new TextChange(span, string.Empty);
    }
}
