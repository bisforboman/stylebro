using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StyleBro.Analyzers.Naming;

/// <summary>
/// Names in code excluded by '#if': a rename can't see them (the compiler doesn't bind disabled code), so a name used
/// there keeps its old spelling while the declaration changes, and another target framework's build breaks (Bogus:
/// type parameter 'V' renamed to 'TV', the inactive '#else' branch kept '(Action&lt;T, V&gt;)'). 'stylebro-migrate format'
/// formats one target framework at a time, so the linked copies that would show that branch as active aren't there.
/// The naming analyzers don't report a symbol whose old name is a word in disabled code where it can be referenced.
/// </summary>
internal static class DisabledCode
{
    private static readonly ConditionalWeakTable<Compilation, List<string>> Texts = new();

    /// <summary>Whether <paramref name="name"/> is a word in disabled code anywhere in the compilation (for members and types other files can use).</summary>
    public static bool Mentions(Compilation compilation, string name)
    {
        foreach (var text in Texts.GetValue(compilation, Collect))
        {
            if (IsWordIn(text, name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether <paramref name="name"/> is a word in disabled code inside one of the symbol's declarations (a type's, a method's).</summary>
    public static bool Mentions(ISymbol owner, string name, System.Threading.CancellationToken cancellationToken)
    {
        foreach (var reference in owner.DeclaringSyntaxReferences)
        {
            var declaration = reference.GetSyntax(cancellationToken);
            if (!declaration.ContainsDirectives)
            {
                continue;
            }

            foreach (var trivia in TreeWalk.Trivia(declaration.SyntaxTree.GetRoot(cancellationToken)))
            {
                if (trivia.IsKind(SyntaxKind.DisabledTextTrivia)
                    && declaration.FullSpan.Contains(trivia.Span)
                    && IsWordIn(trivia.ToString(), name))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Whether <paramref name="name"/> occurs in <paramref name="text"/> with no identifier character on either side.</summary>
    private static bool IsWordIn(string text, string name)
    {
        for (var i = text.IndexOf(name, System.StringComparison.Ordinal); i >= 0; i = text.IndexOf(name, i + 1, System.StringComparison.Ordinal))
        {
            var end = i + name.Length;
            if ((i == 0 || !IsIdentifierChar(text[i - 1])) && (end == text.Length || !IsIdentifierChar(text[end])))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    // The disabled code of every tree with directives, read once per compilation; names are looked up in it only for the
    // few symbols that are about to be reported (a word set of Newtonsoft.Json's disabled code cost ~30 ms).
    private static List<string> Collect(Compilation compilation)
    {
        var texts = new List<string>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            if (!root.ContainsDirectives)
            {
                continue;
            }

            foreach (var trivia in TreeWalk.Trivia(root))
            {
                if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                {
                    texts.Add(trivia.ToString());
                }
            }
        }

        return texts;
    }
}
