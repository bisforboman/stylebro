using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1527: a list of auto-accessors only (<c>get;</c>, <c>private set;</c>, <c>init;</c>) spread over
/// several lines goes on the declaration's line: <c>public int Simple { get; set; }</c>.
/// </summary>
internal static class AutoAccessorLines
{
    /// <summary>
    /// The edits that put the list on one line, or null when it already is or is skipped: an accessor with a body,
    /// attributes or a line break inside it, a comment or directive anywhere from the declaration's name to the '}', an
    /// initializer that doesn't start on the '}' line, or <c>csharp_preserve_single_line_blocks = false</c> (the SDK's
    /// formatter would expand the list again). <paramref name="isOn"/> adds the blank lines that BRO1505/BRO1519 want
    /// next to the multi-line declaration, so the result doesn't depend on which fix 'dotnet format' runs first.
    /// </summary>
    public static List<TextChange>? GetChanges(BasePropertyDeclarationSyntax declaration, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        if (declaration.AccessorList is not { } list || list.Accessors.Count == 0 || declaration.ContainsDiagnostics)
        {
            return null;
        }

        var open = list.OpenBraceToken;
        var close = list.CloseBraceToken;
        var before = open.GetPreviousToken();
        if (Line(text, before.Span.End) == Line(text, close.SpanStart)
            || (options.TryGetValue("csharp_preserve_single_line_blocks", out var preserve) && preserve.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
            || list.Accessors.Any(a => a.Body is not null || a.ExpressionBody is not null || a.AttributeLists.Count > 0 || Line(text, a.SpanStart) != Line(text, a.Span.End))
            || (declaration is PropertyDeclarationSyntax { Initializer: { } initializer } && Line(text, initializer.SpanStart) != Line(text, close.SpanStart)))
        {
            return null;
        }

        // A comment inside an accessor ('private /* x */ set;'); Gap checks the gaps between them.
        if (list.Accessors.Any(a => !Trivia.IsBlank(a, a.Span)))
        {
            return null;
        }

        var gaps = new List<(SyntaxToken Before, SyntaxToken After)> { (before, open), (open, list.Accessors[0].GetFirstToken()) };
        for (var i = 1; i < list.Accessors.Count; i++)
        {
            gaps.Add((list.Accessors[i - 1].GetLastToken(), list.Accessors[i].GetFirstToken()));
        }

        gaps.Add((list.Accessors.Last().GetLastToken(), close));
        var changes = new List<TextChange>();
        if (!gaps.All(gap => SingleLineBlocks.Gap(gap.Before, gap.After, " ", text, changes)))
        {
            return null;
        }

        AddBlankLines(declaration, close, text, isOn, changes);
        return changes;
    }

    /// <summary>
    /// Next to a multi-line declaration BRO1505 wants a blank line (and BRO1519 after its '}' when BRO1505 is off); next
    /// to a one-line property it may not. Added here too: identical edits from both fixes are merged.
    /// </summary>
    private static void AddBlankLines(BasePropertyDeclarationSyntax declaration, SyntaxToken close, SourceText text, Func<string, bool> isOn, List<TextChange> changes)
    {
        if (isOn(DiagnosticIds.ElementsSeparatedByBlankLine) && declaration.Parent is TypeDeclarationSyntax type)
        {
            var index = type.Members.IndexOf(declaration);
            foreach (var (previous, current) in new[] { (index - 1, index), (index, index + 1) })
            {
                if (previous >= 0 && current < type.Members.Count
                    && ElementSeparation.NeedsBlankLine(type.Members[previous], type.Members[current], text)
                    && !ElementSeparation.HasBlankLineBetween(type.Members[previous], type.Members[current], text)
                    && ElementSeparation.GetChange(type.Members[previous], type.Members[current], text) is { } blankLine)
                {
                    changes.Add(blankLine);
                }
            }
        }

        var next = close.GetNextToken(includeZeroWidth: true, includeSkipped: true);
        if (BlankLineRuns.JudgesGapAfter(close, next, text, isOn, gapIsReplaced: false))
        {
            changes.Add(new TextChange(new TextSpan(next.FullSpan.Start, 0), SingleLineBlocks.LineBreak(text, close.SpanStart)));
        }
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
