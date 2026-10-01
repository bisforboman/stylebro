using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1610 (StyleCop SA1627: no empty '&lt;remarks&gt;'), BRO1611/BRO1613 (SA1612/SA1620: '&lt;param&gt;'
/// and '&lt;typeparam&gt;' tags match the (type) parameters: none for one that doesn't exist, and in order) and
/// BRO1612/BRO1614 (SA1613/SA1621: every such tag names its (type) parameter).
/// </summary>
internal static class ParameterDocumentation
{
    /// <summary>
    /// The declarations whose '&lt;param&gt;' tags BRO1611 checks: like StyleCop 1.2's SA1612, not constructors or
    /// operators (probed: StyleCop reports neither stale nor out-of-order tags there).
    /// </summary>
    public static readonly SyntaxKind[] MemberKinds =
    [
        SyntaxKind.MethodDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.DelegateDeclaration,
    ];

    /// <summary>Empty '&lt;remarks&gt;' elements ('&lt;remarks&gt;&lt;/remarks&gt;', '&lt;remarks/&gt;' or only whitespace).</summary>
    public static IEnumerable<XmlNodeSyntax> GetEmptyRemarks(SyntaxNode root)
    {
        foreach (var documentation in root.DescendantTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>())
        {
            foreach (var node in documentation.Content)
            {
                if (node is XmlEmptyElementSyntax { Name.LocalName.ValueText: "remarks" }
                    || (node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: "remarks" } element
                        && element.Content.All(c => c is XmlTextSyntax text && text.TextTokens.All(t => t.Text.Trim().Length == 0))))
                {
                    yield return node;
                }
            }
        }
    }

    /// <summary>The declarations whose '&lt;typeparam&gt;' tags BRO1613/BRO1614 check (StyleCop's SA1620/SA1621).</summary>
    public static readonly SyntaxKind[] TypeParameterMemberKinds =
    [
        SyntaxKind.MethodDeclaration,
        SyntaxKind.DelegateDeclaration,
        SyntaxKind.ClassDeclaration,
        SyntaxKind.StructDeclaration,
        SyntaxKind.InterfaceDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
    ];

    /// <summary>'&lt;param&gt;' tags (BRO1611/BRO1612) or '&lt;typeparam&gt;' tags (BRO1613/BRO1614).</summary>
    public enum TagKind
    {
        Parameter,
        TypeParameter,
    }

    /// <summary>One problem: where it's reported, which rule, and the message.</summary>
    public sealed class Problem
    {
        public Problem(TextSpan span, string id, string message)
        {
            Span = span;
            Id = id;
            Message = message;
        }

        public TextSpan Span { get; }

        public string Id { get; }

        public string Message { get; }
    }

    /// <summary>What's wrong with a member's tags of one kind, and the edits that fix all of it.</summary>
    public sealed class Finding
    {
        public Finding(IReadOnlyList<Problem> problems, IReadOnlyList<TextChange> changes)
        {
            Problems = problems;
            Changes = changes;
        }

        public IReadOnlyList<Problem> Problems { get; }

        public IReadOnlyList<TextChange> Changes { get; }
    }

    /// <summary>
    /// The finding for a member's tags of one kind, or null when they match. Problems are found and fixed together, so
    /// the fix of every rule involved gives the same edits. A tag for a (type) parameter that doesn't exist is renamed
    /// when it's the only such tag and exactly one (type) parameter has no tag (it was most likely renamed), and removed
    /// otherwise. A tag without a name gets one when that's unambiguous: it's the only unnamed tag and exactly one
    /// (type) parameter is undocumented, or every tag is unnamed and there's one per (type) parameter (named in order).
    /// Tags out of order are put in order when each has its own '///' lines. Not reported: duplicate tags, and members
    /// with an unnamed tag that can't be named that way (the fix would have to guess).
    /// </summary>
    public static Finding? GetFinding(SyntaxNode member, SourceText text, TagKind kind = TagKind.Parameter)
    {
        var parameters = kind == TagKind.Parameter ? GetParameterNames(member) : GetTypeParameterNames(member);
        var element = kind == TagKind.Parameter ? "param" : "typeparam";
        var noun = kind == TagKind.Parameter ? "parameter" : "type parameter";
        var matchId = kind == TagKind.Parameter ? DiagnosticIds.ParameterTagsMatch : DiagnosticIds.TypeParameterTagsMatch;
        var nameId = kind == TagKind.Parameter ? DiagnosticIds.ParameterTagHasName : DiagnosticIds.TypeParameterTagHasName;
        var tags = member.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>()
            .SelectMany(d => d.Content.OfType<XmlElementSyntax>())
            .Where(e => e.StartTag.Name.LocalName.ValueText == element)
            .ToList();
        if (parameters is null || tags.Count == 0)
        {
            return null;
        }

        var names = tags.Select(GetName).ToList();
        var named = names.Where(n => n is not null).ToList();
        if (named.Distinct().Count() != named.Count)
        {
            return null;
        }

        // Unnamed tags: named only when it's unambiguous.
        var unnamed = tags.Where((t, i) => names[i] is null).ToList();
        var stale = tags.Where((t, i) => names[i] is not null && !parameters.Contains(names[i]!)).ToList();
        var undocumented = parameters.Where(p => !named.Contains(p)).ToList();
        var assigned = new Dictionary<XmlElementSyntax, string>();
        if (unnamed.Count > 0)
        {
            if (named.Count == 0 && unnamed.Count == parameters.Count)
            {
                for (var i = 0; i < unnamed.Count; i++)
                {
                    assigned[unnamed[i]] = parameters[i];
                }
            }
            else if (unnamed.Count == 1 && stale.Count == 0 && undocumented.Count == 1)
            {
                assigned[unnamed[0]] = undocumented[0];
            }
            else
            {
                return null;
            }

            undocumented = undocumented.Where(p => !assigned.ContainsValue(p)).ToList();
        }

        var renamed = stale.Count == 1 && undocumented.Count == 1 ? (stale[0], undocumented[0]) : ((XmlElementSyntax, string)?)null;

        // The tags that stay, with the (type) parameter each documents.
        var kept = tags.Select((t, i) => (Tag: t, Name: assigned.TryGetValue(t, out var a) ? a : renamed is { } r && r.Item1 == t ? r.Item2 : names[i]!))
            .Where(k => parameters.Contains(k.Name))
            .ToList();
        var ordered = kept.OrderBy(k => parameters.IndexOf(k.Name)).ToList();
        var outOfOrder = !kept.Select(k => k.Tag).SequenceEqual(ordered.Select(k => k.Tag));

        var problems = new List<Problem>();
        var changes = new List<TextChange>();
        foreach (var pair in assigned)
        {
            var attribute = pair.Key.StartTag.Attributes.FirstOrDefault(a => a.Name.LocalName.ValueText == "name");
            problems.Add(new Problem(
                attribute?.Span ?? TextSpan.FromBounds(DocumentationTags.GetStart(pair.Key), pair.Key.StartTag.Span.End),
                nameId,
                "The " + noun + " documentation doesn't name the " + noun + "; it's probably '" + pair.Value + "'"));
            changes.Add(attribute is not null
                ? new TextChange(attribute.Span, "name=\"" + pair.Value + "\"")
                : new TextChange(new TextSpan(pair.Key.StartTag.Name.Span.End, 0), " name=\"" + pair.Value + "\""));
        }

        foreach (var tag in stale)
        {
            if (renamed is { } rename && rename.Item1 == tag)
            {
                problems.Add(new Problem(GetNameToken(tag)!.Value.Span, matchId, "The " + noun + " '" + GetName(tag) + "' doesn't exist; it's probably '" + rename.Item2 + "' now"));
                changes.Add(new TextChange(GetNameToken(tag)!.Value.Span, rename.Item2));
            }
            else
            {
                problems.Add(new Problem(GetNameToken(tag)!.Value.Span, matchId, "The " + noun + " '" + GetName(tag) + "' doesn't exist"));
                changes.Add(DocumentationTags.GetRemoval(tag, text));
            }
        }

        if (outOfOrder)
        {
            // A tag that gets a name and moves too would need both edits in its copied lines: skipped.
            if (!kept.All(k => IsOnOwnLines(k.Tag, text)) || assigned.Count > 0)
            {
                return null;
            }

            for (var i = 0; i < kept.Count; i++)
            {
                if (kept[i].Tag != ordered[i].Tag)
                {
                    problems.Add(new Problem(GetNameToken(kept[i].Tag)!.Value.Span, matchId, "The tags aren't in " + noun + " order"));

                    // Slot i gets the lines of the tag that belongs there (with its new name if it was renamed).
                    var source = text.ToString(GetLines(ordered[i].Tag, text));
                    if (renamed is { } rename && rename.Item1 == ordered[i].Tag)
                    {
                        source = source.Replace("\"" + GetName(ordered[i].Tag) + "\"", "\"" + rename.Item2 + "\"");
                    }

                    changes.RemoveAll(c => GetLines(kept[i].Tag, text).Contains(c.Span));
                    changes.Add(new TextChange(GetLines(kept[i].Tag, text), source));
                }
            }
        }

        return problems.Count == 0 ? null : new Finding(problems, changes);
    }

    private static List<string>? GetTypeParameterNames(SyntaxNode member)
    {
        var list = member switch
        {
            MethodDeclarationSyntax method => method.TypeParameterList,
            DelegateDeclarationSyntax @delegate => @delegate.TypeParameterList,
            TypeDeclarationSyntax type => type.TypeParameterList,
            _ => null,
        };

        return list?.Parameters.Select(p => p.Identifier.ValueText).ToList();
    }
    private static List<string>? GetParameterNames(SyntaxNode member)
    {
        var list = member switch
        {
            BaseMethodDeclarationSyntax method => method.ParameterList,
            DelegateDeclarationSyntax @delegate => @delegate.ParameterList,
            IndexerDeclarationSyntax indexer => (BaseParameterListSyntax)indexer.ParameterList,
            _ => null,
        };

        return list?.Parameters.Select(p => p.Identifier.ValueText).ToList();
    }

    /// <summary>The name in the tag, or null for a tag without one (no 'name' attribute, or an empty one).</summary>
    private static string? GetName(XmlElementSyntax tag) => GetNameToken(tag)?.ValueText is { Length: > 0 } name ? name : null;

    /// <summary>The name in the tag's 'name' attribute, where the diagnostic goes (like StyleCop).</summary>
    public static SyntaxToken? GetNameToken(XmlElementSyntax tag) =>
        tag.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier;

    /// <summary>Whether the tag has its '///' lines to itself.</summary>
    private static bool IsOnOwnLines(XmlElementSyntax tag, SourceText text)
    {
        var start = DocumentationTags.GetStart(tag);
        var first = text.Lines.GetLineFromPosition(start);
        var last = text.Lines.GetLineFromPosition(tag.Span.End);
        return text.ToString(TextSpan.FromBounds(first.Start, start)).Trim() == "///"
            && text.ToString(TextSpan.FromBounds(tag.Span.End, last.End)).Trim().Length == 0;
    }

    /// <summary>The tag's lines, without the final line break.</summary>
    private static TextSpan GetLines(XmlElementSyntax tag, SourceText text)
    {
        var first = text.Lines.GetLineFromPosition(DocumentationTags.GetStart(tag));
        var last = text.Lines.GetLineFromPosition(tag.Span.End);
        return TextSpan.FromBounds(first.Start, last.End);
    }
}
