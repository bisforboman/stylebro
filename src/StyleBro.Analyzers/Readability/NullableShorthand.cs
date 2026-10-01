using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1115 (StyleCop SA1125: <c>int?</c> instead of <c>Nullable&lt;int&gt;</c>). Like StyleCop, also
/// <c>System.Nullable&lt;int&gt;</c>, <c>global::System.Nullable&lt;int&gt;</c>, nested type arguments, <c>typeof</c> and
/// <c>default</c>; not <c>typeof(Nullable&lt;&gt;)</c>, <c>nameof</c> or <c>cref</c>.
/// </summary>
internal static class NullableShorthand
{
    /// <summary>
    /// The type syntax to replace (the whole qualified name) and its type argument, or null. Only in places where only a
    /// type can stand, where <c>T?</c> means the same: not the left side of a member access (<c>Nullable&lt;int&gt;.Equals</c>).
    /// </summary>
    public static (TypeSyntax Node, TypeSyntax Argument)? GetFinding(GenericNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
    {
        if (name.Identifier.ValueText != "Nullable" || name.TypeArgumentList.Arguments.Count != 1
            || name.TypeArgumentList.Arguments[0] is OmittedTypeArgumentSyntax || name.ContainsDiagnostics)
        {
            return null;
        }

        TypeSyntax node = name;
        while (node.Parent is QualifiedNameSyntax qualified && qualified.Right == node)
        {
            node = qualified;
        }

        if (node.Parent is AliasQualifiedNameSyntax alias && alias.Name == node)
        {
            node = alias;
            while (node.Parent is QualifiedNameSyntax qualified && qualified.Right == node)
            {
                node = qualified;
            }
        }

        // Only where nothing but a type can stand. This also leaves out 'is'/'as' operands, patterns, using aliases and
        // crefs (checked by tests; explicit checks for them were dead code).
        if (!SyntaxFacts.IsInTypeOnlyContext(node))
        {
            return null;
        }

        return model.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }
            ? (node, name.TypeArgumentList.Arguments[0])
            : null;
    }

    /// <summary>
    /// The replacement for a finding: the type argument followed by '?', with any <c>Nullable&lt;T&gt;</c> inside the
    /// argument shortened too, so nested ones are done in one edit.
    /// </summary>
    public static TextChange GetChange((TypeSyntax Node, TypeSyntax Argument) finding, SemanticModel model, SourceText text, CancellationToken cancellationToken)
    {
        var argument = finding.Argument;
        var inner = argument.DescendantNodesAndSelf().OfType<GenericNameSyntax>()
            .Select(g => GetFinding(g, model, cancellationToken))
            .Where(f => f is not null)
            .Select(f => f!.Value)
            .ToList();
        var outermost = inner.Where(f => !inner.Any(o => o.Node != f.Node && o.Node.Span.Contains(f.Node.Span))).ToList();

        var replacement = text.ToString(argument.Span);
        foreach (var f in outermost.OrderByDescending(f => f.Node.SpanStart))
        {
            var change = GetChange(f, model, text, cancellationToken);
            var start = f.Node.SpanStart - argument.SpanStart;
            replacement = replacement.Substring(0, start) + change.NewText + replacement.Substring(start + f.Node.Span.Length);
        }

        return new TextChange(finding.Node.Span, replacement + "?");
    }
}
