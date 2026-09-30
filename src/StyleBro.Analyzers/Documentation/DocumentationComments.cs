using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Documentation;

/// <summary>
/// Shared logic for BRO1601 (StyleCop SA1600 for overrides and implementations: '/// &lt;inheritdoc/&gt;') and BRO1602
/// (SA1626: '///' used for a plain comment). Works whether or not the project generates documentation (without it,
/// the compiler parses '///' as plain comments).
/// </summary>
internal static class DocumentationComments
{
    /// <summary>The member declarations BRO1601 checks.</summary>
    public static readonly SyntaxKind[] MemberKinds =
    [
        SyntaxKind.MethodDeclaration,
        SyntaxKind.PropertyDeclaration,
        SyntaxKind.IndexerDeclaration,
        SyntaxKind.EventDeclaration,
        SyntaxKind.EventFieldDeclaration,
    ];

    /// <summary>Whether the declaration has a documentation comment ('///' or '/**') before it.</summary>
    public static bool HasDocumentation(SyntaxNode member)
    {
        return member.GetLeadingTrivia().Any(IsDocumentationComment);
    }

    /// <summary>
    /// Whether every symbol the declaration declares overrides a member or implements an interface member, so its
    /// documentation can come from there. Members of private types are left out, like StyleCop, which doesn't require
    /// documentation for them.
    /// </summary>
    public static bool InheritsDocumentation(IEnumerable<ISymbol> symbols)
    {
        var list = symbols.ToList();
        return list.Count > 0 && list.All(s =>
            !IsInPrivateType(s) && (s.IsOverride || ImplementsInterfaceMember(s)));
    }

    /// <summary>
    /// The edit for BRO1601: '/// &lt;inheritdoc/&gt;' on its own line right before the member (after a plain comment
    /// or blank line before it, like StyleCop's fix), at the member's indentation.
    /// </summary>
    public static TextChange GetInheritDocChange(SyntaxNode member, SourceText text)
    {
        var line = text.Lines.GetLineFromPosition(member.SpanStart);
        var indentation = text.ToString(TextSpan.FromBounds(line.Start, member.SpanStart));
        var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        return new TextChange(new TextSpan(member.SpanStart, 0), "/// <inheritdoc/>" + lineBreak + indentation);
    }

    /// <summary>
    /// BRO1602: the '///' comments that don't document anything: not directly before a type, member or enum member.
    /// '////' (commented-out code) isn't a documentation comment.
    /// </summary>
    public static IEnumerable<SyntaxTrivia> GetMisplacedDocumentationComments(SyntaxNode root)
    {
        foreach (var trivia in root.DescendantTrivia())
        {
            if (IsTripleSlashComment(trivia) && !DocumentsAMember(trivia))
            {
                yield return trivia;
            }
        }
    }

    /// <summary>Where BRO1602 is reported: the comment's first line, from its '///'.</summary>
    public static TextSpan GetReportSpan(SyntaxTrivia comment, SourceText text)
    {
        var line = text.Lines.GetLineFromPosition(comment.FullSpan.Start);
        return TextSpan.FromBounds(comment.FullSpan.Start, System.Math.Min(line.End, comment.FullSpan.End));
    }

    /// <summary>
    /// The edits for BRO1602: every '///' at the start of a line of the comment becomes '//', plus a space when text
    /// follows directly ('///note' -> '// note'), so the result also satisfies BRO1002.
    /// </summary>
    public static IEnumerable<TextChange> GetSlashChanges(SyntaxTrivia comment, SourceText text)
    {
        var first = text.Lines.GetLineFromPosition(comment.FullSpan.Start).LineNumber;
        var last = text.Lines.GetLineFromPosition(comment.Span.End).LineNumber;
        for (var number = first; number <= last; number++)
        {
            var line = text.Lines[number];
            var lineText = text.ToString(line.Span);
            var start = lineText.Length - lineText.TrimStart().Length;
            if (string.CompareOrdinal(lineText, start, "///", 0, 3) != 0 || line.Start + start < comment.FullSpan.Start)
            {
                continue;
            }

            var next = start + 3 < lineText.Length ? lineText[start + 3] : ' ';
            var replacement = char.IsWhiteSpace(next) ? "//" : "// ";
            yield return new TextChange(new TextSpan(line.Start + start, 3), replacement);
        }
    }

    private static bool IsDocumentationComment(SyntaxTrivia trivia)
    {
        return trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
            || IsTripleSlashComment(trivia);
    }

    private static bool IsTripleSlashComment(SyntaxTrivia trivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
        {
            return true;
        }

        if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
        {
            return false;
        }

        var text = trivia.ToString();
        return text.StartsWith("///", System.StringComparison.Ordinal) && !text.StartsWith("////", System.StringComparison.Ordinal);
    }

    /// <summary>Whether the comment is in the leading trivia of a type, member or enum member declaration.</summary>
    private static bool DocumentsAMember(SyntaxTrivia trivia)
    {
        var token = trivia.Token;
        if (!token.LeadingTrivia.Contains(trivia))
        {
            return false;
        }

        return token.Parent?.AncestorsAndSelf().FirstOrDefault(n => n.SpanStart == token.SpanStart && n is MemberDeclarationSyntax)
            is { } member && member is not BaseNamespaceDeclarationSyntax;
    }

    private static bool IsInPrivateType(ISymbol symbol)
    {
        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.DeclaredAccessibility == Accessibility.Private)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Implicit and explicit implementations alike: FindImplementationForInterfaceMember returns both.</summary>
    private static bool ImplementsInterfaceMember(ISymbol symbol)
    {
        var type = symbol.ContainingType;
        return type is not null && type.AllInterfaces.Any(i => i.GetMembers().Any(m =>
            SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(m), symbol)));
    }
}
