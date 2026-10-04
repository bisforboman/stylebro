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
        if (GetTextStart(GetFirstParagraph(summary) ?? summary, text) is not { } start)
        {
            return null;
        }

        // Like StyleCop, only the beginning counts: 'Initializes a new instance of the <see cref="X"/> class
        // representing ...' is fine.
        var line = text.Lines.GetLineFromPosition(start);
        var rest = text.ToString(TextSpan.FromBounds(start, line.End));
        if (accepted.Any(a => StartsWithWords(Normalize(rest), a.TrimEnd('.'))))
        {
            return null;
        }

        // A near miss of the standard sentence ('Initializes a new instance of the Words class.') is replaced when it
        // ends on this line; any other text stays, after the standard sentence.
        var replaceLength = 0;
        var nearMiss = string.Join(" ", standard.Split(' ').Take(3));
        if (rest.StartsWith(nearMiss, StringComparison.Ordinal))
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

        var remaining = rest.Substring(replaceLength);
        var separator = remaining.Length == 0 || remaining.StartsWith("</", StringComparison.Ordinal) ? string.Empty : " ";
        return new Finding(summary.StartTag.GetLocation(), member is DestructorDeclarationSyntax, new TextSpan(start, replaceLength), standard + separator);
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
