using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// Shared logic for BRO1301 (StyleCop SA1312: variable names begin with a lower-case letter) and BRO1302 (SA1313:
/// parameter names begin with a lower-case letter): the new name, and whether renaming is safe.
/// </summary>
internal static class CamelCaseNames
{
    /// <summary>
    /// A scope's identifiers, read once: every variable and parameter of a member asks about the same scope, and walking
    /// a big method once per renamed name cost ~10 ms per edit in a 350 KB file.
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SyntaxNode, ScopeNames> Scopes = new();

    /// <summary>
    /// The camelCase name for <paramref name="name"/>, or null when the name already begins with a lower-case letter
    /// or has no safe replacement. Leading underscores go; a leading run of capitals is lowered as a whole, except the
    /// capital that starts the next word: 'Value' -> 'value', '_value' -> 'value', 'URL' -> 'url',
    /// 'HTMLParser' -> 'htmlParser'. Null for names that are only underscores, would start with a digit, or would
    /// become a keyword ('Class' -> 'class').
    /// </summary>
    public static string? GetNewName(string name)
    {
        if (name.Length == 0 || char.IsLower(name[0]))
        {
            return null;
        }

        var chars = name.TrimStart('_').ToCharArray();
        if (chars.Length == 0 || !char.IsLetter(chars[0]))
        {
            return null;
        }

        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
            {
                break;
            }

            if (i > 0 && i + 1 < chars.Length && !char.IsUpper(chars[i + 1]))
            {
                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        var result = new string(chars);
        return result != name && SyntaxFacts.IsValidIdentifier(result) && SyntaxFacts.GetKeywordKind(result) == SyntaxKind.None
            ? result
            : null;
    }

    /// <summary>
    /// Whether renaming the variable or parameter declared by <paramref name="declaration"/> can't change what any
    /// name binds to. Checked by syntax, conservatively, over the member that contains the declaration (for a primary
    /// constructor parameter, the whole type): no identifier there may already be <paramref name="newName"/> or get
    /// the same new name from another rename, the old name may not be an inferred tuple element or anonymous type
    /// member name (the rename would change it), and the member may not contain '#if' (code that isn't compiled
    /// can't be renamed reliably).
    /// </summary>
    public static bool CanRename(SyntaxNode declaration, string oldName, string newName, Func<string, string?>? getNewName = null)
    {
        var scope = GetScope(declaration);
        if (scope.ContainsDirectives && scope.DescendantTrivia(descendIntoTrivia: true).Any(t => t.IsKind(SyntaxKind.IfDirectiveTrivia)))
        {
            return false;
        }

        var names = Scopes.GetValue(scope, ScopeNames.Collect);
        var map = getNewName ?? GetNewName;
        return !names.Identifiers.Contains(newName)
            && !names.Inferred.Contains(oldName)
            && !names.Identifiers.Any(text => text != oldName && map(text) == newName);
    }

    /// <summary>
    /// BRO1313: whether the parameter's name reaches run time from its member's code: <c>nameof(key)</c>, or an argument
    /// a <c>[CallerArgumentExpression]</c> parameter captures (<c>ArgumentNullException.ThrowIfNull(key)</c>). Both
    /// usually end up as an exception's ParamName, which callers and tests compare (Newtonsoft.Json's
    /// <c>JObject.ContainsKey(null)</c> test expects "propertyName").
    /// </summary>
    public static bool IsNameObservable(SyntaxNode member, string name, SemanticModel model, System.Threading.CancellationToken cancellationToken)
    {
        foreach (var identifier in member.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.ValueText != name || identifier.Parent is not ArgumentSyntax argument)
            {
                continue;
            }

            if (argument.Parent?.Parent is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } }
                || (model.GetOperation(argument, cancellationToken) is Microsoft.CodeAnalysis.Operations.IArgumentOperation { Parameter: { } target }
                    && target.ContainingSymbol is IMethodSymbol method
                    && method.Parameters.Any(p => p.GetAttributes().Any(a =>
                        a.AttributeClass?.Name == "CallerArgumentExpressionAttribute"
                        && a.ConstructorArguments.Length == 1
                        && a.ConstructorArguments[0].Value as string == target.Name))))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The member whose code can refer to a variable or parameter by its simple name.</summary>
    public static SyntaxNode GetScope(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is GlobalStatementSyntax)
            {
                return current.Parent ?? current;
            }

            if (current is MemberDeclarationSyntax)
            {
                return current;
            }
        }

        return node.SyntaxTree.GetRoot();
    }

    /// <summary>Whether the name is an inferred anonymous type member or tuple element name ('new { Total }').</summary>
    public static bool IsInferredMemberName(SyntaxToken token)
    {
        return token.Parent is IdentifierNameSyntax name
            && (name.Parent is AnonymousObjectMemberDeclaratorSyntax { NameEquals: null }
                || name.Parent is ArgumentSyntax { NameColon: null, Parent: TupleExpressionSyntax });
    }

    private sealed class ScopeNames
    {
        public System.Collections.Generic.HashSet<string> Identifiers { get; } = new(StringComparer.Ordinal);

        /// <summary>Gets the names used as inferred tuple element or anonymous type member names.</summary>
        public System.Collections.Generic.HashSet<string> Inferred { get; } = new(StringComparer.Ordinal);

        public static ScopeNames Collect(SyntaxNode scope)
        {
            var names = new ScopeNames();
            foreach (var token in scope.DescendantTokens(descendIntoTrivia: true))
            {
                // XML element names in doc comments ('<param>') are identifier tokens too, but never bind to anything.
                if (token.IsKind(SyntaxKind.IdentifierToken) && token.Parent is not XmlNameSyntax)
                {
                    names.Identifiers.Add(token.ValueText);
                    if (IsInferredMemberName(token))
                    {
                        names.Inferred.Add(token.ValueText);
                    }
                }
            }

            return names;
        }
    }
}
