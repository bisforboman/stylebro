using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1608 (StyleCop SA1617: no '&lt;returns&gt;' on a method or delegate that returns void) and
/// BRO1609 (SA1651: no '&lt;placeholder&gt;' elements).
/// </summary>
internal static class DocumentationTags
{
    /// <summary>The '&lt;returns&gt;' elements of a void method or delegate.</summary>
    public static IEnumerable<XmlNodeSyntax> GetVoidReturns(SyntaxNode member)
    {
        var returnType = member switch
        {
            MethodDeclarationSyntax method => method.ReturnType,
            DelegateDeclarationSyntax @delegate => @delegate.ReturnType,
            _ => null,
        };

        if (returnType is not PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword })
        {
            return [];
        }

        return GetDocumentation(member).SelectMany(d => d.Content)
            .Where(n => n is XmlElementSyntax { StartTag.Name.LocalName.ValueText: "returns" } or XmlEmptyElementSyntax { Name.LocalName.ValueText: "returns" });
    }

    /// <summary>Every '&lt;placeholder&gt;' element in the tree's documentation comments.</summary>
    public static IEnumerable<XmlElementSyntax> GetPlaceholders(SyntaxNode root)
    {
        return root.DescendantTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .SelectMany(d => d.DescendantNodes().OfType<XmlElementSyntax>())
            .Where(e => e.StartTag.Name.LocalName.ValueText == "placeholder");
    }

    /// <summary>
    /// The edit that removes an element: its whole lines when nothing else shares them (the '///' lines it covers),
    /// otherwise just the element and the spaces before it.
    /// </summary>
    public static TextChange GetRemoval(XmlNodeSyntax element, SourceText text)
    {
        var elementStart = GetStart(element);
        var firstLine = text.Lines.GetLineFromPosition(elementStart);
        var lastLine = text.Lines.GetLineFromPosition(element.Span.End);
        var before = text.ToString(TextSpan.FromBounds(firstLine.Start, elementStart)).Trim();
        var after = text.ToString(TextSpan.FromBounds(element.Span.End, lastLine.End)).Trim();
        return before == "///" && after.Length == 0
            ? new TextChange(TextSpan.FromBounds(firstLine.Start, lastLine.EndIncludingLineBreak), string.Empty)
            : new TextChange(TextSpan.FromBounds(SkipSpacesBack(text, elementStart, firstLine.Start), element.Span.End), string.Empty);
    }

    /// <summary>Where an element starts: its '&lt;', not the '///' before it on a continuation line.</summary>
    public static int GetStart(XmlNodeSyntax element) => element.GetFirstToken().SpanStart;

    /// <summary>The edits that unwrap a placeholder: its tags go, its content stays.</summary>
    public static IEnumerable<TextChange> GetUnwrap(XmlElementSyntax placeholder)
    {
        yield return new TextChange(placeholder.StartTag.Span, string.Empty);
        yield return new TextChange(placeholder.EndTag.Span, string.Empty);
    }

    private static int SkipSpacesBack(SourceText text, int position, int lineStart)
    {
        while (position > lineStart && text[position - 1] == ' ')
        {
            position--;
        }

        return position;
    }

    private static IEnumerable<DocumentationCommentTriviaSyntax> GetDocumentation(SyntaxNode member) =>
        member.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
}
