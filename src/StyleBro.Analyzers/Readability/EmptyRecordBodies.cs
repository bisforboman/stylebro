using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1140: <c>record R(int X);</c> instead of <c>record R(int X) { }</c>.</summary>
internal static class EmptyRecordBodies
{
    /// <summary>
    /// The edit that replaces the empty body (and a <c>;</c> after it, which BRO1101 would remove) with <c>;</c>, or null
    /// when the record has members, no parameter list, or anything but whitespace in the replaced text.
    /// </summary>
    public static TextChange? GetChange(RecordDeclarationSyntax record)
    {
        if (record.ParameterList is null || !record.OpenBraceToken.IsKind(SyntaxKind.OpenBraceToken) || record.Members.Count > 0
            || record.ContainsDiagnostics)
        {
            return null;
        }

        var end = record.SemicolonToken.IsKind(SyntaxKind.SemicolonToken) ? record.SemicolonToken.Span.End : record.CloseBraceToken.Span.End;
        var span = TextSpan.FromBounds(record.OpenBraceToken.GetPreviousToken().Span.End, end);
        return Trivia.IsBlank(record, span) ? new TextChange(span, ";") : null;
    }
}
