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
    /// <summary>
    /// The .editorconfig key for BRO1604-BRO1607: <c>stylecop</c> (default, StyleCop's SA1623/SA1624/SA1642/SA1643) or
    /// <c>stylesharp</c> (StyleSharp's SST rules at those numbers, which stylebro-migrate writes for a StyleSharp repository).
    /// </summary>
    public const string WordingKey = "stylebro_summary_wording";

    /// <summary>
    /// Words that can't follow other words of a sentence: a condition ('If true, the text is checked', 'True if ...').
    /// BRO1604 doesn't put 'Gets' in front of them (written in lower case: a capitalized word isn't reported anyway), nor
    /// BRO1606 its sentence.
    /// </summary>
    public static readonly string[] Conditions = ["If", "When", "Whenever", "Unless", "While", "Whether", "True", "False"];

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

    /// <summary>Articles: a summary without a known prefix that starts with one is a noun phrase ('The name').</summary>
    private static readonly string[] Articles = ["The", "A", "An"];

    /// <summary>
    /// A summary that starts with a verb of its own: 'Gets' in front of it isn't a sentence ('Gets returns ...'). Only
    /// matters in lower case ('returns ...'): a capitalized word isn't reported anyway.
    /// </summary>
    private static readonly string[] Verbs =
    [
        "Returns", "Specifies", "Indicates", "Determines", "Contains", "Controls", "Defines", "Represents", "Provides",
        "Allows", "Enables", "Describes", "Stores", "Holds", "Is", "Has", "Can",
    ];

    /// <summary>
    /// The finding for a property, or null when its summary is right or can't be checked. The words follow the
    /// accessors other code can use: 'Gets or sets', 'Gets' (no setter, or a private or internal setter; a protected
    /// one counts, like in StyleCop), 'Sets' (write-only); a bool gets 'a value indicating whether' after them. An
    /// 'init' accessor counts like a setter with 'initializes', and like StyleCop master a property with 'get' and
    /// 'init' may also say just 'Gets' (or, unlike StyleCop master, 'Gets or sets'). Not checked: indexers, summaries that don't start with text, and summaries
    /// without a known prefix that don't start like a noun phrase (an article or a lower-case word): 'This is the hash',
    /// 'Device id of the user', 'Returns ...' have no fix that makes a sentence (owner's decision, 2026-10-09); nor
    /// lower-case conditions or verbs ('if true, ...', 'returns ...').
    /// </summary>
    /// <remarks>
    /// With <paramref name="styleSharp"/> (<c>stylebro_summary_wording = stylesharp</c>) the words follow
    /// StyleSharp's SST1623/SST1624: a set or init accessor with any access modifier (protected too) is restricted and asks
    /// for 'Gets', a getter counts whatever its modifier ('{ private get; set; }' says 'Gets or sets'), an 'init' accessor
    /// without one reads as 'Gets', and a summary is right when it starts with the words and a space ('Gets or sets' also
    /// starts with 'Gets '), except 'Gets or sets' for a restricted setter.
    /// </remarks>
    public static Finding? GetFinding(PropertyDeclarationSyntax property, SourceText text, SemanticModel? model, bool styleSharp = false)
    {
        var getter = property.ExpressionBody is not null || property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
        var setter = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration));
        var initOnly = setter is not null && setter.IsKind(SyntaxKind.InitAccessorDeclaration);

        // Like StyleCop, a protected accessor is usable (by derived types); a private or internal one isn't. That goes for the
        // getter too: '{ private get; set; }' says 'Sets' (FakeItEasy's BuildableCallRule, SA1623 accepts it).
        var getAccessor = property.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
        var restrictedSetter = setter is not null && (styleSharp ? setter.Modifiers.Count > 0 : IsRestricted(setter));
        var restrictedGetter = !styleSharp && setter is not null && getAccessor is not null && IsRestricted(getAccessor);

        // Like StyleCop's SA1623, an 'internal' accessor of a property only its assembly sees (an internal class's) is as
        // visible as the property: 'Gets or sets' (trial 27, TheAlgorithms: BRO1605 asked for 'Gets', SA1623 for 'Gets or
        // sets'). A 'private' one stays restricted. Bound only when an accessor is restricted without 'private'.
        if (!styleSharp && ((restrictedSetter && !HasPrivate(setter!)) || (restrictedGetter && !HasPrivate(getAccessor!))))
        {
            if (model is null || model.GetDeclaredSymbol(property) is not { } symbol)
            {
                return null;
            }

            if (DocumentationComments.GetVisibility(symbol) != Accessibility.Public)
            {
                // Only one accessor can have a modifier, and it isn't 'private' here.
                restrictedSetter = false;
                restrictedGetter = false;
            }
        }

        var visibleSetter = setter is not null && !restrictedSetter && !(styleSharp && initOnly);
        var visibleGetter = getter && !restrictedGetter;

        var verb = visibleGetter && visibleSetter ? (initOnly ? "Gets or initializes" : "Gets or sets")
            : visibleGetter ? "Gets"
            : visibleSetter ? (initOnly ? "Initializes" : "Sets")
            : null;
        if (verb is null)
        {
            return null;
        }

        // '{ get; init; }' may also say 'Gets or sets' (unlike StyleCop master): a common wording, also for attribute
        // properties ('[Command(AllowConcurrent = true)]' sets them), and rewriting it to 'Gets or initializes' is churn
        // (trial 22: CommunityToolkit's attributes).
        string[] accepted = initOnly && visibleGetter && visibleSetter ? [verb, "Gets", "Gets or sets"] : [verb];

        // Only 'bool', like StyleCop's SA1623: a 'bool?' summary takes the plain verb ('Gets whether' stays; tfplan2md's
        // became 'Gets a value indicating whether').
        var isBool = property.Type is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.BoolKeyword };

        if (GetSummaryTextStart(property) is not { } start)
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(start);
        var rest = text.ToString(TextSpan.FromBounds(start, line.End));
        if (styleSharp && rest.StartsWith(verb + " ", StringComparison.Ordinal) && !(restrictedSetter && rest.StartsWith("Gets or sets ", StringComparison.Ordinal)))
        {
            return null;
        }

        // The longest known prefix the summary starts with: 'Gets or sets' also starts with 'Gets'.
        // Ignoring case, like StyleCop: 'Gets or Sets key' is right (trial 27: it became 'Gets or sets or Sets key').
        var old = KnownPrefixes.FirstOrDefault(p => StartsWithWord(rest, p) && (isBool || !p.EndsWith("whether", StringComparison.OrdinalIgnoreCase))) ?? string.Empty;

        // A bool's summary may use StyleCop's 'a value indicating whether' or just the verb ('Gets the open state'):
        // putting the phrase in front of other text doesn't make a sentence ('a value indicating whether the open
        // state'). Only a summary that already says 'whether' gets the full phrase.
        var longForm = verb + " a value indicating whether";
        if (accepted.Any(v => old == v || (isBool && old == v + " a value indicating whether")))
        {
            return null;
        }

        if (old.Length == 0 && !char.IsLower(rest[0]) && !StartsWithAnyWord(rest, Articles))
        {
            return null;
        }

        if (old.Length == 0 && (StartsWithAnyWord(rest, Conditions) || StartsWithAnyWord(rest, Verbs)))
        {
            return null;
        }

        // A bool's summary that starts with an article is often a clause ('The tokenizer predicted a token here'): neither
        // 'Gets the tokenizer predicted ...' nor '... a value indicating whether the open state' makes a sentence of every
        // one, so it isn't reported (owner's decision 2026-10-12, issue #132).
        if (old.Length == 0 && isBool && StartsWithAnyWord(rest, Articles))
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
            (restrictedSetter || restrictedGetter) && (old.StartsWith("Gets or sets", StringComparison.Ordinal) || old.StartsWith("Gets or initializes", StringComparison.Ordinal)),
            new TextSpan(start, afterOld + 1),
            prefix + " " + lowered);
    }

    /// <summary>Whether the file's <c>stylebro_summary_wording</c> is <c>stylesharp</c>.</summary>
    public static bool FollowsStyleSharp(Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions options) =>
        options.TryGetValue(WordingKey, out var value) && value.Split(':')[0].Trim().Equals("stylesharp", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="text"/> starts with one of <paramref name="words"/> as a whole word, ignoring case.</summary>
    public static bool StartsWithAnyWord(string text, string[] words)
    {
        return words.Any(w => text.StartsWith(w, StringComparison.OrdinalIgnoreCase)
            && (text.Length == w.Length || !char.IsLetterOrDigit(text[w.Length])));
    }

    /// <summary>Whether other code can't use the accessor: private or internal, not protected internal.</summary>
    private static bool IsRestricted(AccessorDeclarationSyntax accessor) =>
        accessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.InternalKeyword))
        && !accessor.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword) && accessor.Modifiers.Any(n => n.IsKind(SyntaxKind.InternalKeyword)));

    private static bool HasPrivate(AccessorDeclarationSyntax accessor) => accessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));

    /// <summary>Where the summary's text starts, or null when there's no summary or it starts with an element.</summary>
    private static int? GetSummaryTextStart(SyntaxNode member)
    {
        var summary = member.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .Where(d => !DocumentationComments.IsMalformed(d))
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
        return text.StartsWith(words, StringComparison.OrdinalIgnoreCase)
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
