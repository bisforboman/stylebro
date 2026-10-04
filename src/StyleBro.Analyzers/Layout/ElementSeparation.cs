using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1505 (StyleCop SA1516): which neighbouring elements need a blank line between them, where the
/// diagnostic goes, and the edit that adds the blank line.
/// </summary>
internal static class ElementSeparation
{
    /// <summary>
    /// Pairs of neighbouring elements (previous, current) that need a blank line and don't have one. Elements are the
    /// usings, extern aliases, assembly attributes and members of a file or namespace, the members of a type, and the
    /// accessors of a property, indexer or event. Like StyleCop, these pairs don't need one: two fields (unless the first
    /// spans several lines); two usings, two extern aliases or two attribute lists; and two accessors that are both on a
    /// single line.
    /// </summary>
    public static IEnumerable<(SyntaxNode Previous, SyntaxNode Current)> GetViolations(SyntaxNode root, SourceText text)
    {
        foreach (var node in root.DescendantNodesAndSelf(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax or MemberDeclarationSyntax or AccessorListSyntax))
        {
            var elements = GetElements(node);
            for (var i = 1; i < elements.Count; i++)
            {
                if (NeedsBlankLine(elements[i - 1], elements[i], text) && !HasBlankLineBetween(elements[i - 1], elements[i], text))
                {
                    yield return (elements[i - 1], elements[i]);
                }
            }
        }
    }

    /// <summary>The diagnostic position: the start of the element's first line (its comments, attributes or directives).</summary>
    public static Location GetLocation(SyntaxNode current)
    {
        return Location.Create(current.SyntaxTree, new TextSpan(current.FullSpan.Start, 0));
    }

    /// <summary>
    /// A line break at the start of the element's first line. When the element starts on the previous element's line,
    /// the whitespace between them becomes a blank line plus the line's indentation. Null when a comment sits between
    /// two elements on one line, which the fix would have to move.
    /// </summary>
    public static TextChange? GetChange(SyntaxNode previous, SyntaxNode current, SourceText text)
    {
        var start = GetFirstLineStart(current, text);
        var previousEnd = GetEnd(previous);
        var lineBreak = GetLineBreak(text, text.Lines.GetLineFromPosition(previousEnd));
        if (text.Lines.GetLineFromPosition(start).LineNumber > text.Lines.GetLineFromPosition(previousEnd).LineNumber)
        {
            return new TextChange(new TextSpan(text.Lines.GetLineFromPosition(start).Start, 0), lineBreak);
        }

        var between = TextSpan.FromBounds(previousEnd, current.SpanStart);
        if (text.ToString(between).Trim().Length > 0)
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(previousEnd);
        var indentation = new string(text.ToString(line.Span).TakeWhile(c => c is ' ' or '\t').ToArray());
        return new TextChange(between, lineBreak + lineBreak + indentation);
    }

    /// <summary>Whether two neighbouring elements need a blank line between them (BRO1505; also used by BRO1509's fix).</summary>
    public static bool NeedsBlankLine(SyntaxNode previous, SyntaxNode current, SourceText text)
    {
        if (previous.ContainsDiagnostics || current.ContainsDiagnostics)
        {
            return false;
        }

        return (previous, current) switch
        {
            // Like StyleCop: two fields need one only when the first spans several lines (a multi-line initializer).
            (FieldDeclarationSyntax field, FieldDeclarationSyntax) => IsMultiLineField(field, text),
            (UsingDirectiveSyntax, UsingDirectiveSyntax) => false,
            (ExternAliasDirectiveSyntax, ExternAliasDirectiveSyntax) => false,
            (AttributeListSyntax, AttributeListSyntax) => false,

            // Like StyleCop: only two accessors with block bodies, when either spans several lines. An
            // expression-bodied accessor ('get => x;') never needs a blank line next to it.
            (AccessorDeclarationSyntax { Body: not null }, AccessorDeclarationSyntax { Body: not null }) => IsMultiLine(previous, text) || IsMultiLine(current, text),
            (AccessorDeclarationSyntax, AccessorDeclarationSyntax) => false,
            _ => true,
        };
    }

    /// <summary>Whether there's a blank line anywhere between the two elements' code (BRO1505; also used by BRO1510's fix).</summary>
    public static bool HasBlankLineBetween(SyntaxNode previous, SyntaxNode current, SourceText text)
    {
        // Like StyleCop, a blank line anywhere before the element's code counts, also one between its comment (or
        // '#if') and the code. The fix still adds the line above the comment.
        var from = text.Lines.GetLineFromPosition(GetEnd(previous)).LineNumber + 1;
        var to = text.Lines.GetLineFromPosition(current.SpanStart).LineNumber;
        for (var number = from; number < to; number++)
        {
            if (text.ToString(text.Lines[number].Span).Trim().Length == 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a field spans several lines, like StyleCop's SA1516: from the line where the last attribute list's
    /// trivia ends (attributes on their own lines don't count) or the field's first line, to its last line.
    /// </summary>
    public static bool IsMultiLineField(FieldDeclarationSyntax field, SourceText text)
    {
        var start = field.AttributeLists.Count > 0 ? field.AttributeLists.Last().FullSpan.End : field.SpanStart;
        return text.Lines.GetLineFromPosition(start).LineNumber != text.Lines.GetLineFromPosition(field.Span.End).LineNumber;
    }

    private static List<SyntaxNode> GetElements(SyntaxNode node)
    {
        return node switch
        {
            CompilationUnitSyntax unit => unit.Externs.Cast<SyntaxNode>().Concat(unit.Usings).Concat(unit.AttributeLists)
                .Concat(unit.Members.Where(m => m is not GlobalStatementSyntax)).ToList(),
            FileScopedNamespaceDeclarationSyntax fileScoped => new SyntaxNode[] { fileScoped }.Concat(fileScoped.Externs)
                .Concat(fileScoped.Usings).Concat(fileScoped.Members).ToList(),
            NamespaceDeclarationSyntax ns => ns.Externs.Cast<SyntaxNode>().Concat(ns.Usings).Concat(ns.Members).ToList(),
            TypeDeclarationSyntax type => type.Members.Cast<SyntaxNode>().ToList(),
            AccessorListSyntax accessors => accessors.Accessors.Cast<SyntaxNode>().ToList(),
            _ => new List<SyntaxNode>(),
        };
    }

    /// <summary>
    /// Where an element's code ends. A file-scoped namespace is an element only before the namespace's first element,
    /// and its declaration ends at its ';', not after all its members.
    /// </summary>
    private static int GetEnd(SyntaxNode element)
    {
        return element is FileScopedNamespaceDeclarationSyntax fileScoped ? fileScoped.SemicolonToken.Span.End : element.Span.End;
    }

    /// <summary>Where the element's first line starts: its first comment, directive, attribute or token.</summary>
    private static int GetFirstLineStart(SyntaxNode current, SourceText text)
    {
        foreach (var trivia in current.GetLeadingTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return trivia.SpanStart;
            }
        }

        return current.SpanStart;
    }

    private static bool IsMultiLine(SyntaxNode node, SourceText text)
    {
        return text.Lines.GetLineFromPosition(node.SpanStart).LineNumber != text.Lines.GetLineFromPosition(node.Span.End).LineNumber;
    }

    private static string GetLineBreak(SourceText text, TextLine line)
    {
        var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        return lineBreak.Length > 0 ? lineBreak : "\n";
    }
}
