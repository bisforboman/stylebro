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
    /// formatter would expand the list again). <paramref name="isOn"/> adds the blank line that BRO1519 wants after
    /// the multi-line declaration, so the result doesn't depend on which fix 'dotnet format' runs first.
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

        AddBlankLines(close, text, isOn, changes);
        return changes;
    }

    /// <summary>
    /// Whether the fix leaves the property on one line as BRO1505 measures it (from below its attributes to its end):
    /// BRO1505 and BRO1001's sort judge it like that already while BRO1527 is on.
    /// </summary>
    public static bool BecomesOneLine(PropertyDeclarationSyntax property, SourceText text, AnalyzerConfigOptions options)
    {
        var start = property.AttributeLists.Count > 0 ? property.AttributeLists.Last().FullSpan.End : property.SpanStart;
        return property.AccessorList is { } list
            && Line(text, start) == Line(text, list.OpenBraceToken.GetPreviousToken().Span.End)
            && Line(text, list.CloseBraceToken.SpanStart) == Line(text, property.Span.End)
            && GetChanges(property, text, options, _ => false) is not null;
    }

    /// <summary>
    /// After a multi-line declaration's '}' BRO1519 wants a blank line when BRO1505 is off; after a one-line one it doesn't.
    /// Added here too: identical edits from both fixes are merged. BRO1505 needs nothing here: it judges this declaration as
    /// one-line already.
    /// </summary>
    private static void AddBlankLines(SyntaxToken close, SourceText text, Func<string, bool> isOn, List<TextChange> changes)
    {
        var next = close.GetNextToken(includeZeroWidth: true, includeSkipped: true);
        if (BlankLineRuns.JudgesGapAfter(close, next, text, isOn, gapIsReplaced: false))
        {
            changes.Add(new TextChange(new TextSpan(next.FullSpan.Start, 0), SingleLineBlocks.LineBreak(text, close.SpanStart)));
        }
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
