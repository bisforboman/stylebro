using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers;

/// <summary>
/// The names in a repository's C# files that the renaming fixes check beyond the solution they see: string literals
/// (reflection: <c>GetField("count")</c>), <see langword="nameof"/> names, and each file's identifiers. 'stylebro-migrate format'
/// formats a multi-targeted repository one target framework at a time, and a run loads only the projects that target
/// that framework: Scrutor's netstandard2.0 run loaded the library without its tests, which read a private field with
/// GetField, and the field was renamed. So format writes these names for every C# file to the file
/// <see cref="Variable"/> names, and the fixes check them too (one line per entry, see <see cref="Lines"/>).
/// </summary>
public static class RepositoryNames
{
    /// <summary>The environment variable that names the file; not set (the IDE, plain 'dotnet format'), nothing extra is checked.</summary>
    public const string Variable = "STYLEBRO_REPOSITORY_NAMES";

    /// <summary>A string literal's text (<see cref="Entry.Where"/>: 'path(line)').</summary>
    public const char StringKind = 'S';

    /// <summary>A name in <c>nameof(...)</c>.</summary>
    public const char NameofKind = 'N';

    /// <summary>A file's identifiers, space-separated (<see cref="Entry.Where"/>: the path).</summary>
    public const char IdentifierKind = 'I';

    /// <summary>The string literals and <see langword="nameof"/> names in a tree (the text, and the 0-based line).</summary>
    public static IEnumerable<(char Kind, string Text, int Line)> Collect(SyntaxNode root)
    {
        foreach (var token in TreeWalk.Tokens(root))
        {
            if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
            {
                yield return (StringKind, token.ValueText, token.GetLocation().GetLineSpan().StartLinePosition.Line);
            }
            else if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "nameof"
                && token.Parent?.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation)
            {
                yield return (NameofKind, invocation.ArgumentList.Arguments[0].Expression.GetLastToken().ValueText, 0);
            }
        }
    }

    /// <summary>The lines for one file: its strings and nameof names, then one line with its identifiers.</summary>
    public static IEnumerable<string> Lines(SyntaxNode root, string path)
    {
        foreach (var (kind, text, line) in Collect(root))
        {
            yield return kind == StringKind ? Line(kind, $"{path}({line + 1})", text) : Line(kind, string.Empty, text);
        }

        var identifiers = new SortedSet<string>(TreeWalk.Tokens(root).Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText), System.StringComparer.Ordinal);
        yield return Line(IdentifierKind, path, string.Join(" ", identifiers));
    }

    /// <summary>A line of the file, or null when it isn't one.</summary>
    public static Entry? Parse(string line)
    {
        var first = line.IndexOf('\t');
        var second = first < 0 ? -1 : line.IndexOf('\t', first + 1);
        return first == 1 && second > 0 ? new Entry(line[0], Unescape(line.Substring(2, second - 2)), Unescape(line.Substring(second + 1))) : null;
    }

    private static string Line(char kind, string where, string text) => kind + "\t" + Escape(where) + "\t" + Escape(text);

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");

    private static string Unescape(string text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                i++;
                result.Append(text[i] switch { 't' => '\t', 'r' => '\r', 'n' => '\n', _ => text[i] });
            }
            else
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>One line of the file.</summary>
    public sealed class Entry
    {
        /// <summary>Initializes a new instance of the <see cref="Entry"/> class.</summary>
        public Entry(char kind, string where, string text)
        {
            Kind = kind;
            Where = where;
            Text = text;
        }

        /// <summary>Gets the kind (<see cref="StringKind"/>, <see cref="NameofKind"/> or <see cref="IdentifierKind"/>).</summary>
        public char Kind { get; }

        /// <summary>Gets where it is: 'path(line)' for a string, the path for identifiers.</summary>
        public string Where { get; }

        /// <summary>Gets the string's text, the name, or the identifiers.</summary>
        public string Text { get; }
    }
}
