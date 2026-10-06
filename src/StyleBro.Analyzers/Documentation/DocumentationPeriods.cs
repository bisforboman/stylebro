using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1603 (StyleCop SA1629: documentation text ends with a period). Needs the documentation comments
/// parsed as XML, which the compiler does when the project generates documentation (like StyleCop's SA0001 says).
/// </summary>
internal static class DocumentationPeriods
{
    /// <summary>Tags whose text isn't checked, comma-separated: StyleCop's excludeFromPunctuationCheck.</summary>
    public const string ExcludeKey = "stylebro_exclude_from_punctuation_check";

    /// <summary>StyleCop's default for excludeFromPunctuationCheck.</summary>
    public const string DefaultExcluded = "seealso";

    /// <summary>Closing punctuation that may follow a sentence's period: '(see above.)', '"done."'.</summary>
    private static readonly char[] ClosingPunctuation = [')', ']', '"', '\''];

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
    /// code sample, or elements without text, or with a period followed only by closing punctuation ('(see above.)').
    /// Elements named in <see cref="ExcludeKey"/> aren't checked.
    /// </summary>
    public static IEnumerable<int> GetMissingPeriods(IEnumerable<SyntaxTrivia> trivia, AnalyzerConfigOptions options)
    {
        var excluded = GetExcluded(options);
        foreach (var comment in trivia)
        {
            if (comment.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
            {
                continue;
            }

            foreach (var element in documentation.Content.OfType<XmlElementSyntax>())
            {
                var name = element.StartTag.Name.LocalName.ValueText;
                if (TextElements.Contains(name) && !excluded.Contains(name) && GetInsertionPoint(element, excluded) is { } position)
                {
                    yield return position;
                }
            }
        }
    }

    /// <summary>
    /// The edit for BRO1603: a period at <paramref name="position"/>. Spaces between it and a closing tag on the same line
    /// go ('true &lt;/returns&gt;' -> 'true.&lt;/returns&gt;'), else they'd stay after the period.
    /// </summary>
    public static TextChange GetPeriodChange(int position, SourceText text)
    {
        var end = position;
        while (end < text.Length && text[end] is ' ' or '\t')
        {
            end++;
        }

        return new TextChange(TextSpan.FromBounds(position, end < text.Length && text[end] == '<' ? end : position), ".");
    }

    /// <summary>The tags <see cref="ExcludeKey"/> names (StyleCop's default when it isn't set).</summary>
    public static HashSet<string> GetExcluded(AnalyzerConfigOptions options)
    {
        var value = options.TryGetValue(ExcludeKey, out var configured) ? configured : DefaultExcluded;
        return new HashSet<string>(value.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0), StringComparer.Ordinal);
    }

    /// <summary>
    /// A trailing <c>&lt;c&gt;"User logged in."&lt;/c&gt;</c>: a quoted sentence whose period sits inside the quote, so
    /// another one after the element would be doubled (StyleCop #2784).
    /// </summary>
    private static bool IsQuotedSentence(XmlElementSyntax element)
    {
        if (element.StartTag.Name.LocalName.ValueText != "c")
        {
            return false;
        }

        var text = string.Concat(element.Content.OfType<XmlTextSyntax>().SelectMany(t => t.TextTokens).Select(t => t.ValueText)).TrimEnd();
        return text.Length > 1 && text[text.Length - 1] is '"' or '\'' && text.TrimEnd(ClosingPunctuation).EndsWith(".", StringComparison.Ordinal);
    }

    private static int? GetInsertionPoint(XmlElementSyntax element, HashSet<string> excluded)
    {
        // The last piece of content that isn't whitespace or a line's '///'.
        for (var i = element.Content.Count - 1; i >= 0; i--)
        {
            switch (element.Content[i])
            {
                case XmlTextSyntax text:
                    // Entities count as text ('List&lt;T&gt;': the period goes after '&gt;').
                    var last = text.TextTokens.LastOrDefault(t => t.Kind() is SyntaxKind.XmlTextLiteralToken or SyntaxKind.XmlEntityLiteralToken
                        && t.Text.Trim().Length > 0);
                    if (last == default)
                    {
                        continue;
                    }

                    // A period may be followed by closing punctuation: '(see above.)', '"done."'.
                    var ending = string.Concat(text.TextTokens.TakeWhile(t => t != last).Append(last).Select(t => t.ValueText))
                        .TrimEnd().TrimEnd(ClosingPunctuation);
                    return ending.Length > 0 && ending[ending.Length - 1] is '.' or '?' or '!' or ':' ? null
                        : last.IsKind(SyntaxKind.XmlEntityLiteralToken) ? last.Span.End
                        : last.SpanStart + last.Text.TrimEnd().Length;
                case XmlElementSyntax child:
                    var name = child.StartTag.Name.LocalName.ValueText;
                    return BlockElements.Contains(name) || excluded.Contains(name) || IsQuotedSentence(child) ? null
                        : ContainerElements.Contains(name) ? GetInsertionPoint(child, excluded)
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
