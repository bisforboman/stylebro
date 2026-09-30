using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1102, used by both the analyzer and the code fix: which attribute lists to split,
/// and the text that replaces them.
/// </summary>
internal static class CombinedAttributes
{
    /// <summary>
    /// A list with more than one attribute. Like StyleCop's SA1133, attributes on parameters and type parameters
    /// are not reported. Lists with comments or directives between their attributes are skipped, since splitting
    /// them would have to move or drop the comments.
    /// </summary>
    public static bool IsSplittable(AttributeListSyntax list)
    {
        return list.Attributes.Count > 1
            && list.Parent is not (ParameterSyntax or TypeParameterSyntax)
            && !list.ContainsDiagnostics
            && !HasCommentsOrDirectivesBetweenAttributes(list);
    }

    /// <summary>
    /// '[A, B]' becomes '[A]' and '[B]', each keeping the target ('[assembly: A]'). A list on its own line
    /// becomes one line per attribute at the same indentation; a list that shares its line becomes '[A] [B]'.
    /// </summary>
    public static TextChange GetChange(AttributeListSyntax list, SourceText text)
    {
        var target = list.Target is null ? string.Empty : list.Target.Identifier.Text + ": ";
        var parts = list.Attributes.Select(a => "[" + target + a.ToString() + "]");

        var line = text.Lines.GetLineFromPosition(list.SpanStart);
        var indentation = text.ToString(TextSpan.FromBounds(line.Start, list.SpanStart));
        var lastLine = text.Lines.GetLineFromPosition(list.Span.End);
        var rest = text.ToString(TextSpan.FromBounds(list.Span.End, lastLine.End)).Trim();
        var onItsOwnLine = indentation.All(c => c == ' ' || c == '\t') && (rest.Length == 0 || rest.StartsWith("//"));
        var separator = onItsOwnLine ? GetLineBreak(text, line) + indentation : " ";

        return new TextChange(list.Span, string.Join(separator, parts));
    }

    private static bool HasCommentsOrDirectivesBetweenAttributes(AttributeListSyntax list)
    {
        var first = list.GetFirstToken();
        var last = list.GetLastToken();
        foreach (var token in list.DescendantTokens())
        {
            // Trivia inside an attribute's own arguments is kept as-is by the fix.
            if (token.Parent?.FirstAncestorOrSelf<AttributeArgumentListSyntax>() is not null)
            {
                continue;
            }

            if ((token != first && IsCommentOrDirective(token.LeadingTrivia))
                || (token != last && IsCommentOrDirective(token.TrailingTrivia)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCommentOrDirective(SyntaxTriviaList trivia) =>
        trivia.Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));

    private static string GetLineBreak(SourceText text, TextLine line)
    {
        var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        return lineBreak.Length > 0 ? lineBreak : "\n";
    }
}
