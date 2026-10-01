using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1005 (StyleCop SA1004: documentation lines begin with a single space) and BRO1120 (SA1120:
/// comments contain text).
/// </summary>
internal static class CommentText
{
    /// <summary>
    /// BRO1005: the whitespace after '///' that isn't exactly one space (where the diagnostic goes, and what the fix
    /// replaces with one space): none at all, or several before a top-level tag. Like StyleCop: not lines with nothing
    /// after '///', and not lines inside &lt;code&gt;.
    /// </summary>
    public static IEnumerable<TextSpan> GetBadDocumentationSpaces(SyntaxNode root, SourceText text)
    {
        foreach (var documentation in root.DescendantTrivia(descendIntoTrivia: true).Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
        {
            // Like StyleCop: a missing space is reported on any line, extra spaces only before a top-level tag
            // ('///   <param ...>'); indented text and nested tags ('///   <para>') are fine.
            var structure = (DocumentationCommentTriviaSyntax)documentation.GetStructure()!;
            var topLevelStarts = new HashSet<int>(structure.Content.Where(n => n is XmlElementSyntax or XmlEmptyElementSyntax).Select(n => n.SpanStart));
            var code = structure.DescendantNodes().OfType<XmlElementSyntax>()
                .Where(e => e.StartTag.Name.LocalName.ValueText == "code")
                .Select(e => TextSpan.FromBounds(e.StartTag.Span.End, e.EndTag.SpanStart))
                .ToList();
            var first = text.Lines.GetLineFromPosition(documentation.SpanStart).LineNumber;
            var last = text.Lines.GetLineFromPosition(documentation.Span.End).LineNumber;
            for (var number = first; number <= last; number++)
            {
                var line = text.Lines[number];
                var content = text.ToString(line.Span);
                var slashes = content.IndexOf("///", System.StringComparison.Ordinal);
                if (slashes < 0 || content.Substring(0, slashes).Trim().Length > 0 || content.Length > slashes + 3 && content[slashes + 3] == '/')
                {
                    continue;
                }

                var start = line.Start + slashes + 3;
                var rest = content.Substring(slashes + 3);
                var spaces = rest.Length - rest.TrimStart().Length;
                if (rest.Trim().Length == 0 || spaces == 1 || code.Any(c => c.Contains(start)) || (spaces > 1 && !topLevelStarts.Contains(start + spaces)))
                {
                    continue;
                }

                yield return new TextSpan(start, spaces);
            }
        }
    }

    /// <summary>
    /// BRO1120: the empty comments StyleCop reports, each with the comments the fix removes. Comments on consecutive
    /// lines form a group, and like StyleCop an empty comment is only reported at the start or end of a group (empty
    /// lines in the middle separate paragraphs). The fix removes all empty comments at that end of the group, so one
    /// pass is enough.
    /// </summary>
    public static IEnumerable<(SyntaxTrivia Reported, IReadOnlyList<SyntaxTrivia> Removed)> GetEmptyComments(SyntaxNode root, SourceText text)
    {
        var comments = root.DescendantTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia))
            .ToList();
        var group = new List<SyntaxTrivia>();
        foreach (var comment in comments)
        {
            // A comment after code starts a new group (like StyleCop: 'x = 1; //' is the first line of what follows).
            var line = text.Lines.GetLineFromPosition(comment.SpanStart);
            var afterCode = text.ToString(TextSpan.FromBounds(line.Start, comment.SpanStart)).Trim().Length > 0;
            if (group.Count > 0 && (afterCode || line.LineNumber != Line(text, group[group.Count - 1].Span.End) + 1))
            {
                foreach (var finding in GetGroupFindings(group))
                {
                    yield return finding;
                }

                group.Clear();
            }

            group.Add(comment);
        }

        foreach (var finding in GetGroupFindings(group))
        {
            yield return finding;
        }
    }

    /// <summary>The edit that removes one comment: its whole line when it's alone there, or the comment and the space before it.</summary>
    public static TextChange GetRemoval(SyntaxTrivia comment, SourceText text)
    {
        var first = text.Lines.GetLineFromPosition(comment.SpanStart);
        var last = text.Lines.GetLineFromPosition(comment.Span.End);
        var before = text.ToString(TextSpan.FromBounds(first.Start, comment.SpanStart));
        var after = text.ToString(TextSpan.FromBounds(comment.Span.End, last.End));
        if (before.Trim().Length == 0 && after.Trim().Length == 0)
        {
            return new TextChange(TextSpan.FromBounds(first.Start, last.EndIncludingLineBreak), string.Empty);
        }

        var start = comment.SpanStart;
        while (start > first.Start && text[start - 1] is ' ' or '\t')
        {
            start--;
        }

        return new TextChange(TextSpan.FromBounds(start, comment.Span.End), string.Empty);
    }

    private static IEnumerable<(SyntaxTrivia, IReadOnlyList<SyntaxTrivia>)> GetGroupFindings(List<SyntaxTrivia> group)
    {
        if (group.Count == 0)
        {
            yield break;
        }

        var leading = group.TakeWhile(IsEmpty).ToList();
        if (leading.Count > 0)
        {
            yield return (group[0], leading);
        }

        if (leading.Count < group.Count)
        {
            var trailing = group.AsEnumerable().Reverse().TakeWhile(IsEmpty).Reverse().ToList();
            if (trailing.Count > 0)
            {
                yield return (group[group.Count - 1], trailing);
            }
        }
    }

    private static bool IsEmpty(SyntaxTrivia comment)
    {
        var text = comment.ToString();
        return comment.IsKind(SyntaxKind.SingleLineCommentTrivia)
            ? text.Length >= 2 && text.Substring(2).Trim().Length == 0
            : text.Length >= 4 && text.Substring(2, text.Length - 4).Trim().Length == 0;
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
