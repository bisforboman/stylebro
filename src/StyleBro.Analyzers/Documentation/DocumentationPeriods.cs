using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1603 (StyleCop SA1629: documentation text ends with a period). Needs the documentation comments
/// parsed as XML, which the compiler does when the project generates documentation (like StyleCop's SA0001 says).
/// </summary>
internal static class DocumentationPeriods
{
    private static readonly HashSet<string> TextElements = new()
    {
        "summary", "remarks", "param", "typeparam", "returns", "value", "exception",
    };

    /// <summary>
    /// Block elements: text that ends with one of them (a list, a code sample) isn't a sentence, and inherited or included
    /// text ('&lt;param name="key"&gt;&lt;inheritdoc cref="Get" path="/param"/&gt;&lt;/param&gt;') is checked where it's written.
    /// </summary>
    private static readonly HashSet<string> BlockElements = new()
    {
        "list", "code", "table", "br", "inheritdoc", "include",
    };

    /// <summary>Elements that hold sentences of their own: when the text ends with one, its own last text counts.</summary>
    private static readonly HashSet<string> ContainerElements = new()
    {
        "para", "remarks", "summary", "value", "returns", "example", "note", "description", "item", "term", "placeholder",
    };

    /// <summary>
    /// The positions where a period is missing: after the last text of each top-level summary, remarks, param,
    /// typeparam, returns, value and exception element. Not reported: text that ends with '.', '?', '!' or ':' (a
    /// question, an exclamation or an introduction is a finished sentence), with a block element such as a list or a
    /// code sample, or elements without text.
    /// </summary>
    public static IEnumerable<int> GetMissingPeriods(SyntaxNode root)
    {
        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: false))
        {
            if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (var element in documentation.Content.OfType<XmlElementSyntax>())
            {
                if (TextElements.Contains(element.StartTag.Name.LocalName.ValueText) && GetInsertionPoint(element) is { } position)
                {
                    yield return position;
                }
            }
        }
    }

    private static int? GetInsertionPoint(XmlElementSyntax element)
    {
        // The last piece of content that isn't whitespace or a line's '///'.
        for (var i = element.Content.Count - 1; i >= 0; i--)
        {
            switch (element.Content[i])
            {
                case XmlTextSyntax text:
                    var last = text.TextTokens.LastOrDefault(t => t.IsKind(SyntaxKind.XmlTextLiteralToken) && t.Text.Trim().Length > 0);
                    if (last == default)
                    {
                        continue;
                    }

                    var trimmed = last.Text.TrimEnd();
                    return trimmed[trimmed.Length - 1] is '.' or '?' or '!' or ':' ? null : last.SpanStart + trimmed.Length;
                case XmlElementSyntax child:
                    var name = child.StartTag.Name.LocalName.ValueText;
                    return BlockElements.Contains(name) ? null
                        : ContainerElements.Contains(name) ? GetInsertionPoint(child)
                        : child.Span.End;
                case XmlEmptyElementSyntax empty:
                    return BlockElements.Contains(empty.Name.LocalName.ValueText) ? null : empty.Span.End;
                default:
                    return null;
            }
        }

        return null;
    }
}
