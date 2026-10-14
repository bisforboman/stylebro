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

    private static readonly System.Text.RegularExpressions.Regex NumberOnly = new(@"^[-+]?(?:0[xX][0-9a-fA-F_]+|0[bB][01_]+|[0-9][0-9_,]*(?:\.[0-9]+)?)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    // A quoted literal that reads as code: followed by ';' (',';), with an escape ("\r\n"), or a punctuation char (',').
    // A quoted word ('the word "done"') is prose and gets its period.
    private static readonly System.Text.RegularExpressions.Regex QuotedLiteral = new(
        @"(?:(?:'(?:[^'\\]|\\[^']{1,5})'|""[^""]*"");|'(?:[^'\\\w]|\\[^']{1,5})'|""[^""]*\\[^""]*"")$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex VersionLike = new(@"[\p{L}\p{N}]\.[0-9]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly char[] CodeCharacters = ['{', '}', '[', ']', '(', ')', '=', ';', ':', '/', '\\', '|', '$', '#', '_', '`'];

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
    /// Inline elements: the sentence's last word ('Returns &lt;see langword="true"/&gt;'), so the period goes after them.
    /// StyleCop's (c, see, paramref, typeparamref) plus HTML's inline elements. Any other element (a tool's own
    /// '&lt;devremarks&gt;', '&lt;para/&gt;') is passed over, like StyleCop: the text before it counts.
    /// </summary>
    private static readonly HashSet<string> InlineElements = new()
    {
        "c", "see", "paramref", "typeparamref", "a", "b", "i", "u", "em", "strong", "sub", "sup", "span", "tt",
    };

    /// <summary>
    /// The positions where a period is missing: after the last text of each top-level summary, remarks, param,
    /// typeparam, returns, value and exception element. Not reported: text that ends with '.', '?', '!' or ':' (a
    /// question, an exclamation or an introduction is a finished sentence) or with ',' or ';' (a line of code or a list
    /// that goes on: '1033 => "en",' became '"en",.' in trial 20), with a block element such as a list or a
    /// code sample, or elements without text, or with a period followed only by closing punctuation ('(see above.)').
    /// Elements named in <see cref="ExcludeKey"/> aren't checked.
    /// </summary>
    public static IEnumerable<int> GetMissingPeriods(IEnumerable<SyntaxTrivia> trivia, AnalyzerConfigOptions options)
    {
        var excluded = GetExcluded(options);
        foreach (var comment in trivia)
        {
            if (comment.GetStructure() is not DocumentationCommentTriviaSyntax documentation || IsUnchecked(comment))
            {
                continue;
            }

            // Malformed XML is checked once there's a finding: most comments have none.
            bool? malformed = null;
            foreach (var element in documentation.Content.OfType<XmlElementSyntax>())
            {
                var name = element.StartTag.Name.LocalName.ValueText;
                if (TextElements.Contains(name) && !excluded.Contains(name) && GetInsertionPoint(element, excluded) is { } position)
                {
                    malformed ??= DocumentationComments.IsMalformed(documentation);
                    if (malformed.Value)
                    {
                        break;
                    }

                    yield return position;
                }
            }
        }
    }

    /// <summary>
    /// The edit for BRO1603: a period at <paramref name="position"/>. Spaces between it and a closing tag on the same line
    /// go ('true &lt;/returns&gt;' -> 'true.&lt;/returns&gt;'), and so do spaces ending the line (GuardClauses: 'text ' became
    /// 'text. ', the line still ending in whitespace); else they'd stay after the period.
    /// </summary>
    public static TextChange GetPeriodChange(int position, SourceText text)
    {
        var end = position;
        while (end < text.Length && text[end] is ' ' or '\t')
        {
            end++;
        }

        var drop = end == text.Length || text[end] is '\r' or '\n' || (end + 1 < text.Length && text[end] == '<' && text[end + 1] == '/');
        return new TextChange(TextSpan.FromBounds(position, drop ? end : position), ".");
    }

    /// <summary>The tags <see cref="ExcludeKey"/> names (StyleCop's default when it isn't set).</summary>
    public static HashSet<string> GetExcluded(AnalyzerConfigOptions options)
    {
        var value = options.TryGetValue(ExcludeKey, out var configured) ? configured : DefaultExcluded;
        return new HashSet<string>(value.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0), StringComparer.Ordinal);
    }

    /// <summary>
    /// Whether the comment documents an event or a finalizer: StyleCop's SA1629 doesn't check those (probed with
    /// 1.2.0-beta.556; SSH.NET, StyleCop-clean, had 31 event summaries without a period).
    /// </summary>
    private static bool IsUnchecked(SyntaxTrivia comment)
    {
        var member = comment.Token.Parent?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return member is EventFieldDeclarationSyntax or EventDeclarationSyntax or DestructorDeclarationSyntax
            && member.GetFirstToken() == comment.Token;
    }

    /// <summary>
    /// An inline element whose own text ends a sentence: <c>&lt;paramref name="name"&gt;The name of the request.&lt;/paramref&gt;</c>
    /// (SSH.NET, trial 21), or a quoted one in code: <c>&lt;c&gt;"User logged in."&lt;/c&gt;</c> (StyleCop #2784). Another
    /// period after it would be doubled.
    /// </summary>
    private static bool EndsWithSentence(XmlElementSyntax element)
    {
        if (!InlineElements.Contains(element.StartTag.Name.LocalName.ValueText))
        {
            return false;
        }

        // '<c>' holds code ('Calls <c>Done.</c>' still gets its period): only a quoted sentence in it counts.
        var text = string.Concat(element.Content.SelectMany(c => c.DescendantTokens())
            .Where(t => t.IsKind(SyntaxKind.XmlTextLiteralToken) || t.IsKind(SyntaxKind.XmlEntityLiteralToken))
            .Select(t => t.ValueText)).TrimEnd();
        var sentence = text.TrimEnd(ClosingPunctuation).TrimEnd();
        return (element.StartTag.Name.LocalName.ValueText != "c" || (text.Length > 0 && text[text.Length - 1] is '"' or '\''))
            && sentence.Length > 0 && sentence[sentence.Length - 1] is '.' or '?' or '!';
    }

    /// <summary>
    /// Whether the text ends in code rather than a sentence: its last word has a bracket, brace, '=', ';', ':', a slash,
    /// '|', '$', '#', '_' or a backtick in it (Stateless: 'the prefix text would be digraph {', spreadcheetah: 'Example:
    /// A1:B10'). A period there reads as part of the code. A closing ')' or ']' whose opener isn't in the word ends a
    /// parenthetical ('(in ms)') and doesn't count, nor do a closing quote or a trailing ',' or ';'. A quoted char or
    /// string literal at the end is code too (CsvHelper: 'Default is ',';'), and so is a word ending in a period and digits
    /// ('HTML 5.0', AngleSharp's 'processes it.0': a period after it reads as another part of the number). StyleCop's SA1629
    /// has no such exception: a deviation.
    /// </summary>
    private static bool EndsInCode(string text)
    {
        var word = text.Substring(text.LastIndexOfAny([' ', '\t', '\n', '\r']) + 1);
        if (QuotedLiteral.IsMatch(word))
        {
            return true;
        }

        while (word.Length > 0 && ((word[word.Length - 1] == ')' && word.IndexOf('(') < 0) || (word[word.Length - 1] == ']' && word.IndexOf('[') < 0)
            || word[word.Length - 1] is '"' or '\'' or ',' or ';'))
        {
            word = word.Substring(0, word.Length - 1);
        }

        return word.IndexOfAny(CodeCharacters) >= 0 || VersionLike.IsMatch(word);
    }

    /// <summary>
    /// Whether the element's whole text is one number (<c>&lt;summary&gt;0&lt;/summary&gt;</c> on an enum member: Soulseek.NET's
    /// message codes): a value, not a sentence. StyleCop 1.1.118 never saw these (its SA1629 doesn't check enum members),
    /// 1.2 reports them.
    /// </summary>
    private static bool IsNumberOnly(XmlElementSyntax element) =>
        element.Content.All(c => c is XmlTextSyntax)
        && NumberOnly.IsMatch(string.Concat(element.Content.OfType<XmlTextSyntax>().SelectMany(t => t.TextTokens).Select(t => t.ValueText)).Trim());

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
                    var untrimmed = string.Concat(text.TextTokens.TakeWhile(t => t != last).Append(last).Select(t => t.ValueText)).TrimEnd();
                    var ending = untrimmed.TrimEnd(ClosingPunctuation);
                    return (ending.Length > 0 && ending[ending.Length - 1] is '.' or '?' or '!' or ':' or ',' or ';') || EndsInCode(untrimmed) || IsNumberOnly(element) ? null
                        : last.IsKind(SyntaxKind.XmlEntityLiteralToken) ? last.Span.End
                        : last.SpanStart + last.Text.TrimEnd().Length;

                case XmlElementSyntax child:
                    var name = child.StartTag.Name.LocalName.ValueText;
                    if (BlockElements.Contains(name) || excluded.Contains(name) || EndsWithSentence(child))
                    {
                        return null;
                    }

                    if (ContainerElements.Contains(name))
                    {
                        return GetInsertionPoint(child, excluded);
                    }

                    if (InlineElements.Contains(name))
                    {
                        return child.Span.End;
                    }

                    continue;

                case XmlEmptyElementSyntax empty:
                    var emptyName = empty.Name.LocalName.ValueText;
                    if (BlockElements.Contains(emptyName))
                    {
                        return null;
                    }

                    if (InlineElements.Contains(emptyName))
                    {
                        return empty.Span.End;
                    }

                    continue;

                default:
                    return null;
            }
        }

        return null;
    }
}
