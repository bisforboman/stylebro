using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1617 (a cref writes type arguments as '{T}', not '&amp;lt;T&amp;gt;'), BRO1618 (a C# keyword
/// alone in '&lt;c&gt;' is '&lt;see langword="..."/&gt;') and BRO1619 (top-level documentation elements in a fixed
/// order). All three are text edits inside one documentation comment, found per comment.
/// </summary>
internal static class DocumentationStyle
{
    /// <summary>
    /// The top-level elements BRO1619 orders, in that order: Visual Studio's '///' stub (summary, typeparam, param,
    /// returns), then the order of the sections on Microsoft Learn's API pages (property value, exceptions), then
    /// remarks, example and seealso. A comment with any other top-level element isn't checked.
    /// </summary>
    public static readonly string[] ElementOrder = ["summary", "typeparam", "param", "returns", "value", "exception", "remarks", "example", "seealso"];

    /// <summary>
    /// Contextual keywords BRO1618 also takes (Meziantou MA0154's list): the ones unlikely to be an identifier. The
    /// reserved keywords come from the compiler.
    /// </summary>
    private static readonly HashSet<string> ContextualKeywords = new(StringComparer.Ordinal)
    {
        "async", "await", "dynamic", "init", "nameof", "nint", "nuint", "partial", "record", "required", "scoped", "var",
    };

    /// <summary>Every finding in one documentation comment: the rule, where it's reported, the message argument, the edits.</summary>
    public static IEnumerable<Finding> GetFindings(DocumentationCommentTriviaSyntax documentation, SourceText text)
    {
        foreach (var node in documentation.DescendantNodes(n => !IsCode(n)))
        {
            if (node is XmlCrefAttributeSyntax cref && GetGenericCrefChanges(cref) is { } crefChanges)
            {
                yield return new Finding(DiagnosticIds.GenericCrefBraces, cref.Cref.Span, cref.Cref.ToString(), crefChanges);
            }
            else if (node is XmlElementSyntax element && GetKeyword(element) is { } keyword)
            {
                yield return new Finding(
                    DiagnosticIds.LangwordElement,
                    element.Span,
                    keyword,
                    [new TextChange(element.Span, "<see langword=\"" + keyword + "\"/>")]);
            }
        }

        if (GetOrderFinding(documentation, text) is { } order)
        {
            yield return order;
        }
    }

    /// <summary>
    /// BRO1617: the edits that turn a cref's '&amp;lt;'/'&amp;gt;' type argument brackets into braces, or null. Only when
    /// every angle bracket in the cref is such an entity around a type argument or type parameter list (not an
    /// operator like 'operator &amp;lt;') and the cref parsed without errors.
    /// </summary>
    private static TextChange[]? GetGenericCrefChanges(XmlCrefAttributeSyntax attribute)
    {
        var cref = attribute.Cref;
        if (cref.ContainsDiagnostics)
        {
            return null;
        }

        var changes = new List<TextChange>();
        foreach (var token in cref.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.LessThanToken) && !token.IsKind(SyntaxKind.GreaterThanToken))
            {
                continue;
            }

            var entity = token.IsKind(SyntaxKind.LessThanToken) ? "&lt;" : "&gt;";
            if (token.Text != entity || token.Parent is not (TypeArgumentListSyntax or TypeParameterListSyntax))
            {
                return null;
            }

            changes.Add(new TextChange(token.Span, token.IsKind(SyntaxKind.LessThanToken) ? "{" : "}"));
        }

        return changes.Count == 0 ? null : changes.ToArray();
    }

    /// <summary>BRO1618: the keyword when the element is '&lt;c&gt;' without attributes holding just a C# keyword.</summary>
    private static string? GetKeyword(XmlElementSyntax element)
    {
        if (element.StartTag.Name.LocalName.ValueText != "c"
            || element.StartTag.Attributes.Count > 0
            || element.EndTag.Name.LocalName.ValueText != "c"
            || element.Content.Count != 1
            || element.Content[0] is not XmlTextSyntax { TextTokens.Count: 1 } content)
        {
            return null;
        }

        var word = content.TextTokens[0].Text;
        return (SyntaxFacts.GetKeywordKind(word) != SyntaxKind.None && !word.StartsWith("__", StringComparison.Ordinal)) || ContextualKeywords.Contains(word)
            ? word
            : null;
    }

    /// <summary>
    /// BRO1619: the edits that put the top-level elements in <see cref="ElementOrder"/>, or null. Elements of one kind
    /// keep their order among themselves (BRO1611 and BRO1613 order the tags by parameter). Each element must have its
    /// '///' lines to itself; the lines in between stay where they are. Not checked: a comment with text outside the
    /// elements, or with an element not in the list. A '/** */' comment's lines don't start with '///', so it's never
    /// on its own lines.
    /// </summary>
    private static Finding? GetOrderFinding(DocumentationCommentTriviaSyntax documentation, SourceText text)
    {
        var elements = new List<(XmlNodeSyntax Node, string Name, int Rank)>();
        foreach (var node in documentation.Content)
        {
            if (node is XmlTextSyntax plain)
            {
                if (plain.TextTokens.Any(t => t.IsKind(SyntaxKind.XmlTextLiteralToken) && t.Text.Trim().Length > 0))
                {
                    return null;
                }

                continue;
            }

            var name = node switch
            {
                XmlElementSyntax element => element.StartTag.Name.LocalName.ValueText,
                XmlEmptyElementSyntax empty => empty.Name.LocalName.ValueText,
                _ => null,
            };
            var rank = name is null ? -1 : Array.IndexOf(ElementOrder, name);
            if (rank < 0)
            {
                return null;
            }

            elements.Add((node, name!, rank));
        }

        var ordered = elements.OrderBy(e => e.Rank).ToList();
        var first = 0;
        while (first < elements.Count && elements[first].Node == ordered[first].Node)
        {
            first++;
        }

        if (first == elements.Count || !elements.All(e => ParameterDocumentation.IsOnOwnLines(e.Node, text)))
        {
            return null;
        }

        var changes = new List<TextChange>();
        for (var i = first; i < elements.Count; i++)
        {
            if (elements[i].Node != ordered[i].Node)
            {
                changes.Add(new TextChange(ParameterDocumentation.GetLines(elements[i].Node, text), text.ToString(ParameterDocumentation.GetLines(ordered[i].Node, text))));
            }
        }

        var moved = elements[first].Node;
        return new Finding(
            DiagnosticIds.DocumentationElementOrder,
            TextSpan.FromBounds(DocumentationTags.GetStart(moved), moved is XmlElementSyntax movedElement ? movedElement.StartTag.Span.End : moved.Span.End),
            "Put '<" + ordered[first].Name + ">' before '<" + elements[first].Name + ">'",
            changes.ToArray());
    }

    private static bool IsCode(SyntaxNode node) =>
        node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: "code" };

    /// <summary>One finding: the rule, where it's reported, the message argument and the edits that fix it.</summary>
    public sealed class Finding
    {
        public Finding(string id, TextSpan span, string argument, TextChange[] changes)
        {
            Id = id;
            Span = span;
            Argument = argument;
            Changes = changes;
        }

        public string Id { get; }

        public TextSpan Span { get; }

        public string Argument { get; }

        public TextChange[] Changes { get; }
    }
}
