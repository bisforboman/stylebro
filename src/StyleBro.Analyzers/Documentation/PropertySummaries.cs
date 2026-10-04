using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1604 (StyleCop SA1623: a property's summary begins with the words its accessors call for) and
/// BRO1605 (SA1624: a property whose setter is less visible says only 'Gets').
/// </summary>
internal static class PropertySummaries
{
    /// <summary>Prefixes a summary may already start with, longest first, so the fix can replace a wrong one.</summary>
    private static readonly string[] KnownPrefixes =
    [
        "Gets or initializes a value indicating whether",
        "Gets or sets a value indicating whether",
        "Initializes a value indicating whether",
        "Gets a value indicating whether",
        "Sets a value indicating whether",
        "Gets or initializes whether",
        "Gets or sets whether",
        "Initializes whether",
        "Gets whether",
        "Sets whether",
        "Indicates whether",
        "Determines whether",
        "Gets or initializes",
        "Gets or sets",
        "Initializes",
        "Gets",
        "Sets",
        "Whether",
    ];

    /// <summary>
    /// The finding for a property, or null when its summary is right or can't be checked. The words follow the
    /// accessors other code can use: 'Gets or sets', 'Gets' (no setter, or a private or internal setter; a protected
    /// one counts, like in StyleCop), 'Sets' (write-only); a bool gets 'a value indicating whether' after them. An
    /// 'init' accessor counts like a setter with 'initializes', and like StyleCop master a property with 'get' and
    /// 'init' may also say just 'Gets'. Not checked: indexers, and summaries that don't start with text.
    /// </summary>
    public static Finding? GetFinding(PropertyDeclarationSyntax property, SourceText text)
    {
        var getter = property.ExpressionBody is not null || property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
        var setter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration));
        var initOnly = setter is not null && setter.IsKind(SyntaxKind.InitAccessorDeclaration);

        // Like StyleCop, a protected setter is usable (by derived types); a private or internal one isn't.
        var restrictedSetter = setter is not null
            && setter.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.InternalKeyword))
            && !setter.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword) && setter.Modifiers.Any(n => n.IsKind(SyntaxKind.InternalKeyword)));
        var visibleSetter = setter is not null && !restrictedSetter;

        var verb = getter && visibleSetter ? (initOnly ? "Gets or initializes" : "Gets or sets")
            : getter ? "Gets"
            : visibleSetter ? (initOnly ? "Initializes" : "Sets")
            : null;
        if (verb is null)
        {
            return null;
        }

        string[] accepted = initOnly && getter && visibleSetter ? [verb, "Gets"] : [verb];

        var isBool = property.Type is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.BoolKeyword }
            || (property.Type is NullableTypeSyntax { ElementType: PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.BoolKeyword } });

        if (GetSummaryTextStart(property) is not { } start)
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(start);
        var rest = text.ToString(TextSpan.FromBounds(start, line.End));

        // The longest known prefix the summary starts with: 'Gets or sets' also starts with 'Gets'.
        var old = KnownPrefixes.FirstOrDefault(p => StartsWithWord(rest, p) && (isBool || !p.EndsWith("whether", StringComparison.OrdinalIgnoreCase))) ?? string.Empty;

        // A bool's summary may use StyleCop's 'a value indicating whether' or just the verb ('Gets the open state'):
        // putting the phrase in front of other text doesn't make a sentence ('a value indicating whether the open
        // state'). Only a summary that already says 'whether' gets the full phrase.
        var longForm = verb + " a value indicating whether";
        if (accepted.Any(v => old == v || (isBool && old == v + " a value indicating whether")))
        {
            return null;
        }

        var prefix = isBool && old.EndsWith("whether", StringComparison.OrdinalIgnoreCase) ? longForm : verb;

        // Replace the wrong prefix, or none, and lower the next word's first letter ('The name' -> 'the name').
        var afterOld = old.Length;
        while (afterOld < rest.Length && rest[afterOld] == ' ')
        {
            afterOld++;
        }

        if (afterOld >= rest.Length)
        {
            return null;
        }

        var next = rest[afterOld];
        var lowered = char.IsUpper(next) && (afterOld + 1 >= rest.Length || !char.IsUpper(rest[afterOld + 1]))
            ? char.ToLowerInvariant(next)
            : next;
        return new Finding(
            property.Identifier,
            prefix,
            restrictedSetter && (old.StartsWith("Gets or sets", StringComparison.Ordinal) || old.StartsWith("Gets or initializes", StringComparison.Ordinal)),
            new TextSpan(start, afterOld + 1),
            prefix + " " + lowered);
    }

    /// <summary>Where the summary's text starts, or null when there's no summary or it starts with an element.</summary>
    private static int? GetSummaryTextStart(SyntaxNode member)
    {
        var summary = member.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .SelectMany(d => d.Content.OfType<XmlElementSyntax>())
            .FirstOrDefault(e => e.StartTag.Name.LocalName.ValueText == "summary");
        if (summary is null)
        {
            return null;
        }

        foreach (var content in summary.Content)
        {
            if (content is not XmlTextSyntax xmlText)
            {
                return null;
            }

            foreach (var token in xmlText.TextTokens)
            {
                var trimmed = token.Text.TrimStart();
                if (token.IsKind(SyntaxKind.XmlTextLiteralToken) && trimmed.Length > 0)
                {
                    return token.SpanStart + (token.Text.Length - trimmed.Length);
                }
            }
        }

        return null;
    }

    private static bool StartsWithWord(string text, string words)
    {
        return text.StartsWith(words, StringComparison.Ordinal)
            && (text.Length == words.Length || !char.IsLetterOrDigit(text[words.Length]));
    }

    /// <summary>A property whose summary doesn't begin with the right words, and what they should be.</summary>
    public sealed class Finding
    {
        public Finding(SyntaxToken identifier, string prefix, bool restrictedSetter, TextSpan replace, string newText)
        {
            Identifier = identifier;
            Prefix = prefix;
            RestrictedSetter = restrictedSetter;
            Replace = replace;
            NewText = newText;
        }

        public SyntaxToken Identifier { get; }

        public string Prefix { get; }

        public bool RestrictedSetter { get; }

        public TextSpan Replace { get; }

        public string NewText { get; }
    }
}
