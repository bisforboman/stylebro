using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1606 (StyleCop SA1642: a constructor's summary begins with the standard text) and BRO1607
/// (SA1643: a destructor's summary begins with the standard text).
/// </summary>
internal static class ConstructorSummaries
{
    /// <summary>The members BRO1606 and BRO1607 check.</summary>
    public static readonly SyntaxKind[] MemberKinds = [SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration];

    // How a constructor summary of its own starts, besides the standard sentence's 'Initializes' ('Creates a words object'
    // isn't one: it gets the standard sentence in front).
    private static readonly string[] ConstructorVerbs =
    [
        "Initialize", "Initialises", "Initialise", "Instantiates", "Instantiate", "Constructs", "Construct", "Creates a new", "Create a new",
    ];

    // A first sentence that only says it's a constructor (owner's decision, 2026-10-11: "Replace known filler, keep real
    // text"; Stateless: '... the <see cref="Alarm"/> class. Default constructor.'). A deliberately small list: 'Default
    // constructor', 'Constructor', 'Ctor', 'Constructor for X', 'Creates a new instance', 'Creates a new instance of X',
    // 'Creates an instance of X' (also with 'Constructs' or 'Instantiates', 'object' for 'instance', X a name or a
    // '<see cref/>', optionally followed by 'class', 'struct' or 'type').
    private static readonly Regex Filler = new(
        @"^(?:(?:the\s+)?default\s+)?(?:constructor|ctor)(?:\s+(?:for|of)\s+(?:the\s+)?(?:<see\s[^>]*/>|[\w.<>{},]+)(?:\s+(?:class|struct|type))?)?$"
        + @"|^(?:creates|constructs|instantiates)\s+(?:a\s+new|an?)\s+(?:new\s+)?(?:instance|object)(?:\s+of\s+(?:the\s+)?(?:<see\s[^>]*/>|[\w.<>{},]+)(?:\s+(?:class|struct|type))?)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// The finding for a constructor or destructor whose summary doesn't begin with the standard text, or null. Not
    /// reported: members without a summary (missing documentation isn't reported; '&lt;inheritdoc/&gt;' has none), records.
    /// </summary>
    public static Finding? GetFinding(BaseMethodDeclarationSyntax member, SourceText text)
    {
        if (member.Parent is not TypeDeclarationSyntax type || type is RecordDeclarationSyntax || GetSummary(member) is not { } summary)
        {
            return null;
        }

        var cref = type.Identifier.ValueText + (type.TypeParameterList is { } typeParameters
            ? "{" + string.Join(",", typeParameters.Parameters.Select(p => p.Identifier.ValueText)) + "}"
            : string.Empty);
        var tail = "<see cref=\"" + cref + "\"/> " + (type is StructDeclarationSyntax ? "struct" : "class");
        var standard = member is DestructorDeclarationSyntax ? "Finalizes an instance of the " + tail + "."
            : member.Modifiers.Any(SyntaxKind.StaticKeyword) ? "Initializes static members of the " + tail + "."
            : "Initializes a new instance of the " + tail + ".";
        var accepted = member.Modifiers.Any(SyntaxKind.PrivateKeyword) && !member.Modifiers.Any(SyntaxKind.StaticKeyword)
            ? new[] { standard, "Prevents a default instance of the " + tail + " from being created." }
            : new[] { standard };

        // The summary's first line of text, after '<summary>' or after the '///' of the next line. A summary that starts
        // with a paragraph is judged by the paragraph's text (like StyleCop after 1.2.0-beta.556).
        var paragraph = GetFirstParagraph(summary);
        if (GetTextStart(paragraph ?? summary, text) is not { } start)
        {
            return paragraph is null ? GetBlankFinding(member, summary, text, standard) : null;
        }

        // Like StyleCop, only the beginning counts: 'Initializes a new instance of the <see cref="X"/> class
        // representing ...' is fine.
        var line = text.Lines.GetLineFromPosition(start);
        var rest = text.ToString(TextSpan.FromBounds(start, line.End));
        if (accepted.Any(a => StartsWithWords(Normalize(rest), a.TrimEnd('.'))))
        {
            return null;
        }

        // Filler ('Default constructor.') is replaced by the standard sentence, like a near miss of the standard sentence
        // ('Initializes a new instance of the Words class.') when it ends on this line; any other text stays, after the
        // standard sentence.
        var replaceLength = member is ConstructorDeclarationSyntax ? GetFillerLength(rest) : 0;
        var nearMiss = string.Join(" ", standard.Split(' ').Take(3));
        if (rest.StartsWith(nearMiss, StringComparison.Ordinal) && replaceLength == 0)
        {
            var end = rest.IndexOf('.');
            if (end < 0)
            {
                return null;
            }

            // Only a sentence that is just the standard one with the type named differently; anything more
            // ('Initializes a new instance of Foo with the given name.') says something and stays.
            var sentence = rest.Substring(0, end);
            if (!sentence.EndsWith(" class", StringComparison.Ordinal) && !sentence.EndsWith(" struct", StringComparison.Ordinal)
                && !sentence.EndsWith(" type", StringComparison.Ordinal))
            {
                end = -1;
            }

            replaceLength = end + 1;
            while (replaceLength > 0 && replaceLength < rest.Length && rest[replaceLength] == ' ')
            {
                replaceLength++;
            }
        }

        // The kept text becomes the second sentence: one that starts in lower case or with a condition ('construct a
        // bag', 'If true, ...') wouldn't read as one, so it's left alone.
        var remaining = rest.Substring(replaceLength);
        if (remaining.Length > 0 && (char.IsLower(remaining[0]) || PropertySummaries.StartsWithAnyWord(remaining, PropertySummaries.Conditions)))
        {
            return null;
        }

        // A summary that is a constructor sentence of its own ('Initializes the attribute with the specified value.',
        // 'Instantiates a new Hashids encoder ...') would follow the standard one ('... class. Instantiates ...'), and
        // replacing it would lose what it says (Scrutor, Hashids): left alone.
        if (replaceLength == 0
            && (StartsWithWords(remaining, standard.Substring(0, standard.IndexOf(' ')))
                || (member is ConstructorDeclarationSyntax && ConstructorVerbs.Any(v => StartsWithWords(remaining, v)))))
        {
            return null;
        }

        var separator = remaining.Length == 0 || remaining.StartsWith("</", StringComparison.Ordinal) ? string.Empty : " ";
        return new Finding(summary.StartTag.GetLocation(), member is DestructorDeclarationSyntax, new TextSpan(start, replaceLength), standard + separator);
    }

    /// <summary>
    /// The length of a filler first sentence at the start of <paramref name="rest"/> (with its period and the spaces after
    /// it), or 0. The sentence ends at its period, or at the end of the summary or line when it has none.
    /// </summary>
    private static int GetFillerLength(string rest)
    {
        var end = rest.IndexOf('.');
        var close = rest.IndexOf("</", StringComparison.Ordinal);
        var sentence = end >= 0 && (close < 0 || end < close) ? rest.Substring(0, end)
            : close >= 0 ? rest.Substring(0, close)
            : rest;
        if (!Filler.IsMatch(sentence.Trim()))
        {
            return 0;
        }

        var length = sentence.Length + (end >= 0 && end == sentence.Length ? 1 : 0);
        while (length < rest.Length && rest[length] == ' ')
        {
            length++;
        }

        return length;
    }

    /// <summary>
    /// A whitespace-only summary gets the standard sentence, like StyleCop master: on one line it replaces the
    /// whitespace; a multi-line summary gets it on a line of its own. Skipped when the '&lt;/summary&gt;' line holds more
    /// than the '///' (the fix couldn't write the new line's prefix).
    /// </summary>
    private static Finding? GetBlankFinding(BaseMethodDeclarationSyntax member, XmlElementSyntax summary, SourceText text, string standard)
    {
        var inside = TextSpan.FromBounds(summary.StartTag.Span.End, summary.EndTag.SpanStart);
        var startLine = text.Lines.GetLineFromPosition(inside.Start);
        var endLine = text.Lines.GetLineFromPosition(inside.End);
        var isDestructor = member is DestructorDeclarationSyntax;
        if (startLine.LineNumber == endLine.LineNumber)
        {
            return new Finding(summary.StartTag.GetLocation(), isDestructor, inside, standard);
        }

        var prefix = text.ToString(TextSpan.FromBounds(endLine.Start, inside.End));
        if (!Regex.IsMatch(prefix, @"^[ \t]*///[ \t]*$"))
        {
            return null;
        }

        var lineBreak = text.ToString(TextSpan.FromBounds(startLine.End, startLine.EndIncludingLineBreak));
        return new Finding(summary.StartTag.GetLocation(), isDestructor, inside, lineBreak + prefix.TrimEnd() + " " + standard + lineBreak + prefix);
    }

    private static XmlElementSyntax? GetSummary(SyntaxNode member)
    {
        var documentation = member.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
        if (documentation is null)
        {
            return null;
        }

        return documentation.Content.OfType<XmlElementSyntax>().FirstOrDefault(e => e.StartTag.Name.LocalName.ValueText == "summary");
    }

    /// <summary>The '&lt;para&gt;' the summary's content starts with, or null.</summary>
    private static XmlElementSyntax? GetFirstParagraph(XmlElementSyntax summary)
    {
        return summary.Content.FirstOrDefault(n => n is not XmlTextSyntax t || t.TextTokens.Any(token => token.Text.Trim().Length > 0))
            is XmlElementSyntax { StartTag.Name.LocalName.ValueText: "para" } para ? para : null;
    }

    /// <summary>Where the summary's content starts, skipping line breaks, indentation and the '///' of the next line.</summary>
    private static int? GetTextStart(XmlElementSyntax summary, SourceText text)
    {
        var position = summary.StartTag.Span.End;
        var end = summary.EndTag.SpanStart;
        while (position < end)
        {
            var c = text[position];
            if (c is ' ' or '\t' or '\r' or '\n')
            {
                position++;
            }
            else if (c == '/' && position + 2 < end && text[position + 1] == '/' && text[position + 2] == '/')
            {
                position += 3;
            }
            else
            {
                return position;
            }
        }

        return end == summary.StartTag.Span.End ? end : null;
    }

    /// <summary>Whether <paramref name="text"/> begins with <paramref name="prefix"/> as whole words ('class', not 'classes').</summary>
    private static bool StartsWithWords(string text, string prefix)
    {
        return text.StartsWith(prefix, StringComparison.Ordinal) && (text.Length == prefix.Length || !char.IsLetterOrDigit(text[prefix.Length]));
    }

    /// <summary>
    /// Compares documentation text independent of spacing that doesn't change the link: '&lt;see cref="X" /&gt;',
    /// 'X{TKey, TValue}'.
    /// </summary>
    private static string Normalize(string text)
    {
        return Regex.Replace(text.Replace(" />", "/>"), @"\{[^}]*\}", m => Regex.Replace(m.Value, @"\s+", string.Empty));
    }

    /// <summary>A constructor or destructor whose summary doesn't begin with the standard text, and the edit.</summary>
    public sealed class Finding
    {
        public Finding(Location location, bool isDestructor, TextSpan replace, string newText)
        {
            Location = location;
            IsDestructor = isDestructor;
            Replace = replace;
            NewText = newText;
        }

        public Location Location { get; }

        public bool IsDestructor { get; }

        public TextSpan Replace { get; }

        public string NewText { get; }
    }
}
