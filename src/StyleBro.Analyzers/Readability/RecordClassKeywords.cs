using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1146: <c>record R</c> instead of <c>record class R</c> (a record struct keeps its keyword).</summary>
internal static class RecordClassKeywords
{
    /// <summary>The edit that removes 'class' and the whitespace before it, or null when there's none or a comment sits there.</summary>
    public static TextChange? GetChange(RecordDeclarationSyntax record)
    {
        var keyword = record.ClassOrStructKeyword;
        if (!keyword.IsKind(SyntaxKind.ClassKeyword) || record.ContainsDiagnostics)
        {
            return null;
        }

        var span = TextSpan.FromBounds(record.Keyword.Span.End, keyword.Span.End);
        return Trivia.IsBlank(record, span) ? new TextChange(span, string.Empty) : null;
    }
}
