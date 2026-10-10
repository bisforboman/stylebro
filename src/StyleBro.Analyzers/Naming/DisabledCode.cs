using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
    private static readonly ConditionalWeakTable<Compilation, HashSet<string>> Words = new();
    private static readonly Regex Word = new(@"[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}]*", RegexOptions.CultureInvariant);

    /// <summary>Whether <paramref name="name"/> is a word in disabled code anywhere in the compilation (for members and types other files can use).</summary>
    public static bool Mentions(Compilation compilation, string name) => Words.GetValue(compilation, Collect).Contains(name);

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

    private static bool IsWordIn(string text, string name)
    {
        if (text.IndexOf(name, System.StringComparison.Ordinal) < 0)
        {
            return false;
        }

        foreach (Match match in Word.Matches(text))
        {
            if (match.Value == name)
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> Collect(Compilation compilation)
    {
        var words = new HashSet<string>(System.StringComparer.Ordinal);
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
                    foreach (Match match in Word.Matches(trivia.ToString()))
                    {
                        words.Add(match.Value);
                    }
                }
            }
        }

        return words;
    }
}
