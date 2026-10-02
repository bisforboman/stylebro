using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1123 (StyleCop SA1141): tuple syntax instead of <c>ValueTuple</c>. Types
/// (<c>ValueTuple&lt;int, string&gt;</c> becomes <c>(int, string)</c>) wherever only a type can stand, and the
/// creations <c>new ValueTuple&lt;int, string&gt;(1, "a")</c> and <c>ValueTuple.Create(1, "a")</c>, which become
/// <c>(1, "a")</c>.
/// </summary>
internal static class TupleSyntax
{
    /// <summary>A finding: the node to report and replace, and its replacement.</summary>
    public sealed class Finding
    {
        public Finding(SyntaxNode node, Location location, string replacement)
        {
            Node = node;
            Location = location;
            Replacement = replacement;
        }

        public SyntaxNode Node { get; }

        public Location Location { get; }

        public string Replacement { get; }

        public TextChange Change => new(Node.Span, Replacement);
    }

    /// <summary>
    /// A <c>ValueTuple&lt;...&gt;</c> type with 2 to 7 elements in a type-only context, the outermost one when they're
    /// nested (its replacement converts the inner ones too). The type of a <c>new</c> is
    /// <see cref="GetCreation"/>'s: <c>new (int, string)(1, "a")</c> doesn't compile.
    /// </summary>
    public static Finding? GetType(GenericNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
    {
        var node = Outermost(name);
        if (!IsSupported(name) || !SyntaxFacts.IsInTypeOnlyContext(node) || node.Parent is ObjectCreationExpressionSyntax
            || ToTuple(name, model, cancellationToken) is not { } replacement)
        {
            return null;
        }

        // Inside another ValueTuple that is converted (as a type or as a creation), the outer replacement covers this one.
        for (var parent = node.Parent; parent is TypeArgumentListSyntax { Parent: GenericNameSyntax outer }; parent = Outermost(outer).Parent)
        {
            var outerNode = Outermost(outer);
            if (outerNode.Parent is ObjectCreationExpressionSyntax creation
                ? GetCreation(creation, model, cancellationToken) is not null
                : ToTuple(outer, model, cancellationToken) is not null && SyntaxFacts.IsInTypeOnlyContext(outerNode))
            {
                return null;
            }
        }

        return new Finding(node, node.GetLocation(), replacement);
    }

    /// <summary>
    /// <c>new ValueTuple&lt;A, B&gt;(a, b)</c> or <c>ValueTuple.Create(a, b)</c> with 2 to 7 plain arguments, as a tuple
    /// literal. Not in expression trees (no tuple literals there), and not when an argument would give the element a
    /// name (<c>(x, y)</c> names its elements x and y, so <c>t.Item1</c> would then be BRO1124's on a second run).
    /// </summary>
    public static Finding? GetCreation(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        if (expression.ContainsDiagnostics || !IsSupported(expression))
        {
            return null;
        }

        ArgumentListSyntax? arguments;
        Location location;
        if (expression is ObjectCreationExpressionSyntax { Initializer: null } creation
            && Inner(creation.Type) is GenericNameSyntax name && ToTuple(name, model, cancellationToken) is not null)
        {
            arguments = creation.ArgumentList;
            location = creation.GetLocation();
            if (arguments?.Arguments.Count != name.TypeArgumentList.Arguments.Count)
            {
                return null;
            }
        }
        else if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Create" } access } invocation
            && model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol
            {
                ContainingType: { Name: "ValueTuple", Arity: 0, ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } },
            })
        {
            arguments = invocation.ArgumentList;
            location = access.GetLocation();
            if (arguments.Arguments.Count is < 2 or > 7)
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        var items = new string[arguments.Arguments.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var argument = arguments.Arguments[i];
            if (argument.NameColon is not null || !argument.RefKindKeyword.IsKind(SyntaxKind.None) || InfersName(argument.Expression))
            {
                return null;
            }

            var type = model.GetTypeInfo(argument.Expression, cancellationToken);
            if (type.ConvertedType is null)
            {
                return null;
            }

            items[i] = argument.Expression.ToString();
            if (!SymbolEqualityComparer.Default.Equals(type.Type, type.ConvertedType))
            {
                // The literal's element types come from the arguments, so an argument converted to the element type
                // gets a cast (like StyleCop). Only for literals: anything else would need parentheses.
                if (argument.Expression is not LiteralExpressionSyntax)
                {
                    return null;
                }

                items[i] = "(" + type.ConvertedType.ToMinimalDisplayString(model, argument.SpanStart) + ")" + items[i];
            }
        }

        return IsInExpressionTree(expression, model, cancellationToken)
            ? null
            : new Finding(expression, location, "(" + string.Join(", ", items) + ")");
    }

    /// <summary>Tuple syntax needs C# 7.</summary>
    private static bool IsSupported(SyntaxNode node) =>
        node.SyntaxTree.Options is CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp7 };

    /// <summary>'(int, string)' for a ValueTuple type with 2 to 7 elements, converting nested ones; or null.</summary>
    private static string? ToTuple(GenericNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
    {
        var arguments = name.TypeArgumentList.Arguments;
        if (name.Identifier.ValueText != "ValueTuple" || arguments.Count is < 2 or > 7 || arguments.Any(a => a.IsKind(SyntaxKind.OmittedTypeArgument))
            || model.GetSymbolInfo(Outermost(name), cancellationToken).Symbol is not INamedTypeSymbol { IsTupleType: true })
        {
            return null;
        }

        return "(" + string.Join(", ", arguments.Select(a => Inner(a) is GenericNameSyntax inner && ToTuple(inner, model, cancellationToken) is { } nested ? nested : a.ToString())) + ")";
    }

    /// <summary>The whole name for 'System.ValueTuple&lt;...&gt;' or 'global::System.ValueTuple&lt;...&gt;'.</summary>
    private static TypeSyntax Outermost(GenericNameSyntax name)
    {
        TypeSyntax node = name;
        while (node.Parent is QualifiedNameSyntax qualified && qualified.Right == node || node.Parent is AliasQualifiedNameSyntax alias && alias.Name == node)
        {
            node = (TypeSyntax)node.Parent;
        }

        return node;
    }

    private static SimpleNameSyntax? Inner(TypeSyntax type) => type switch
    {
        QualifiedNameSyntax qualified => qualified.Right,
        AliasQualifiedNameSyntax alias => alias.Name,
        SimpleNameSyntax simple => simple,
        _ => null,
    };

    /// <summary>Whether the expression gives a tuple element its name, like a variable or a member (C# 7.1).</summary>
    private static bool InfersName(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax => true,
        MemberAccessExpressionSyntax => true,
        ConditionalAccessExpressionSyntax conditional => conditional.WhenNotNull is MemberBindingExpressionSyntax or MemberAccessExpressionSyntax or ConditionalAccessExpressionSyntax,
        ParenthesizedExpressionSyntax parenthesized => InfersName(parenthesized.Expression),
        _ => false,
    };

    private static bool IsInExpressionTree(SyntaxNode node, SemanticModel model, CancellationToken cancellationToken) =>
        node.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().Any(f =>
            model.GetTypeInfo(f, cancellationToken).ConvertedType is INamedTypeSymbol type
            && type.Name == "Expression" && type.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions");
}
