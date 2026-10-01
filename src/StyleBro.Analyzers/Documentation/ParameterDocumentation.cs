using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1610 (StyleCop SA1627: no empty '&lt;remarks&gt;') and BRO1611 (SA1612: '&lt;param&gt;' tags
/// match the parameters: no tag for a parameter that doesn't exist, and tags in parameter order).
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

    /// <summary>What's wrong with a member's '&lt;param&gt;' tags, and the edits that fix all of it.</summary>
    public sealed class Finding
    {
        public Finding(IReadOnlyList<(XmlElementSyntax Tag, string Message)> problems, IReadOnlyList<TextChange> changes)
        {
            Problems = problems;
            Changes = changes;
        }

        public IReadOnlyList<(XmlElementSyntax Tag, string Message)> Problems { get; }

        public IReadOnlyList<TextChange> Changes { get; }
    }

    /// <summary>
    /// The finding for a member, or null when its tags match. A tag for a parameter that doesn't exist is renamed when
    /// it's the only such tag and exactly one parameter has no tag (the parameter was most likely renamed), and removed
    /// otherwise. Tags out of order are put in parameter order when each has its own '///' lines. Not checked: tags
    /// without a name, duplicate tags, or a member with a tag that can't be fixed this way.
    /// </summary>
    public static Finding? GetFinding(SyntaxNode member, SourceText text)
    {
        var parameters = GetParameterNames(member);
        var tags = member.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>()
            .SelectMany(d => d.Content.OfType<XmlElementSyntax>())
            .Where(e => e.StartTag.Name.LocalName.ValueText == "param")
            .ToList();
        if (parameters is null || tags.Count == 0)
        {
            return null;
        }

        var names = tags.Select(GetName).ToList();
        if (names.Any(n => n is null) || names.Distinct().Count() != names.Count)
        {
            return null;
        }

        var stale = tags.Where((t, i) => !parameters.Contains(names[i]!)).ToList();
        var undocumented = parameters.Where(p => !names.Contains(p)).ToList();
        var renamed = stale.Count == 1 && undocumented.Count == 1 ? (stale[0], undocumented[0]) : ((XmlElementSyntax, string)?)null;

        // The tags that stay, with the parameter each documents.
        var kept = tags.Select((t, i) => (Tag: t, Name: renamed is { } r && r.Item1 == t ? r.Item2 : names[i]!))
            .Where(k => parameters.Contains(k.Name))
            .ToList();
        var ordered = kept.OrderBy(k => parameters.IndexOf(k.Name)).ToList();
        var outOfOrder = !kept.Select(k => k.Tag).SequenceEqual(ordered.Select(k => k.Tag));

        var problems = new List<(XmlElementSyntax, string)>();
        var changes = new List<TextChange>();
        foreach (var tag in stale)
        {
            if (renamed is { } rename && rename.Item1 == tag)
            {
                problems.Add((tag, "The parameter '" + GetName(tag) + "' doesn't exist; it's probably '" + rename.Item2 + "' now"));
                changes.Add(new TextChange(GetNameToken(tag)!.Value.Span, rename.Item2));
            }
            else
            {
                problems.Add((tag, "The parameter '" + GetName(tag) + "' doesn't exist"));
                changes.Add(DocumentationTags.GetRemoval(tag, text));
            }
        }

        if (outOfOrder)
        {
            if (!kept.All(k => IsOnOwnLines(k.Tag, text)))
            {
                return null;
            }

            for (var i = 0; i < kept.Count; i++)
            {
                if (kept[i].Tag != ordered[i].Tag)
                {
                    problems.Add((kept[i].Tag, "The tags aren't in parameter order"));

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

    private static string? GetName(XmlElementSyntax tag) => GetNameToken(tag)?.ValueText;

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
