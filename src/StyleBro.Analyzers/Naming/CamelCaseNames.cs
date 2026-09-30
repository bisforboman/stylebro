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
    public static bool CanRename(SyntaxNode declaration, string oldName, string newName)
    {
        var scope = GetScope(declaration);
        if (scope.ContainsDirectives && scope.DescendantTrivia(descendIntoTrivia: true).Any(t => t.IsKind(SyntaxKind.IfDirectiveTrivia)))
        {
            return false;
        }

        foreach (var token in scope.DescendantTokens(descendIntoTrivia: true))
        {
            // XML element names in doc comments ('<param>') are identifier tokens too, but never bind to anything.
            if (!token.IsKind(SyntaxKind.IdentifierToken) || token.Parent is XmlNameSyntax)
            {
                continue;
            }

            var text = token.ValueText;
            if (text == newName
                || (text != oldName && GetNewName(text) == newName)
                || (text == oldName && IsInferredMemberName(token)))
            {
                return false;
            }
        }

        return true;
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

    private static bool IsInferredMemberName(SyntaxToken token)
    {
        return token.Parent is IdentifierNameSyntax name
            && (name.Parent is AnonymousObjectMemberDeclaratorSyntax { NameEquals: null }
                || name.Parent is ArgumentSyntax { NameColon: null, Parent: TupleExpressionSyntax });
    }
}
