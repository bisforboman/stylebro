using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Layout;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1121 (StyleCop SA1136: each enum value on its own line).</summary>
internal static class EnumValueLines
{
    /// <summary>The values that start on the line where the value before them ends (where the diagnostics go).</summary>
    public static IEnumerable<EnumMemberDeclarationSyntax> GetValuesToMove(EnumDeclarationSyntax @enum, SourceText text, AnalyzerConfigOptions options)
    {
        if (@enum.ContainsDiagnostics || @enum.ContainsDirectives || GetChanges(@enum, text, options, true) is null)
        {
            yield break;
        }

        for (var i = 1; i < @enum.Members.Count; i++)
        {
            if (Line(text, @enum.Members[i].SpanStart) == Line(text, @enum.Members[i - 1].Span.End))
            {
                yield return @enum.Members[i];
            }
        }
    }

    /// <summary>
    /// The edits: for an enum on one line, BRO1509's expansion (so both fixes give the same result whichever runs
    /// first); otherwise a line break before each value that shares its line, at the indentation of the first value.
    /// Null when there's nothing to do or a gap holds a comment.
    /// </summary>
    public static List<TextChange>? GetChanges(EnumDeclarationSyntax @enum, SourceText text, AnalyzerConfigOptions options, bool trailingComma)
    {
        var members = @enum.Members;
        if (members.Count < 2)
        {
            return null;
        }

        if (Line(text, @enum.OpenBraceToken.SpanStart) == Line(text, @enum.CloseBraceToken.SpanStart))
        {
            return SingleLineBlocks.GetChanges(@enum, text, options, trailingComma);
        }

        var firstLine = text.Lines.GetLineFromPosition(members[0].SpanStart);
        var indent = text.ToString(TextSpan.FromBounds(firstLine.Start, members[0].SpanStart));
        if (indent.Trim().Length > 0)
        {
            return null;
        }

        var lineBreak = SingleLineBlocks.LineBreak(text, members[0].SpanStart);
        var changes = new List<TextChange>();
        for (var i = 1; i < members.Count; i++)
        {
            if (Line(text, members[i].SpanStart) == Line(text, members[i - 1].Span.End)
                && !SingleLineBlocks.Gap(members[i].GetFirstToken().GetPreviousToken(), members[i].GetFirstToken(), lineBreak + indent, text, changes))
            {
                return null;
            }
        }

        return changes.Count == 0 ? null : changes;
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
