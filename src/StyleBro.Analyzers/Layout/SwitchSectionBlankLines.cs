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
/// BRO1526 (Roslynator RCS0061): a blank line between the sections of a switch statement, or none
/// (<c>stylebro_blank_line_between_switch_sections = include | omit | omit_after_block</c>). A gap after a section whose
/// '}' BRO1519 judges (a multi-line block or if/else at the end of the section) is BRO1519's when that rule is on and the
/// setting wants a blank line there too; where the setting wants none, BRO1519 and BRO1504 leave the gap alone
/// (<see cref="OmitsBlankLineBefore"/>), so the fixes never undo each other.
/// </summary>
internal static class SwitchSectionBlankLines
{
    public const string OptionKey = "stylebro_blank_line_between_switch_sections";

    public enum Mode
    {
        Include,
        Omit,
        OmitAfterBlock,
    }

    public static Mode ReadMode(AnalyzerConfigOptions options) =>
        !options.TryGetValue(OptionKey, out var value)
        ? Mode.Include
        : value.Split(':')[0].Trim().ToLowerInvariant() switch
        {
            "omit" => Mode.Omit,
            "omit_after_block" => Mode.OmitAfterBlock,
            _ => Mode.Include,
        };

    /// <summary>
    /// Whether <paramref name="next"/> starts a switch section (not the first) whose gap above the setting wants without a
    /// blank line: then BRO1519 (after a '}') and BRO1504 (above a comment there) don't ask for one (Stateless: 'omit',
    /// and the two rules added blank lines between 11 sections).
    /// </summary>
    public static bool OmitsBlankLineBefore(SyntaxToken next, Mode mode) =>
        mode != Mode.Include
        && next.Parent is SwitchLabelSyntax { Parent: SwitchSectionSyntax { Parent: SwitchStatementSyntax switchStatement } section } label
        && section.Labels[0] == label
        && label.GetFirstToken() == next
        && switchStatement.Sections.IndexOf(section) is > 0 and var index
        && (mode == Mode.Omit || switchStatement.Sections[index - 1].Statements.LastOrDefault() is BlockSyntax);

    /// <summary>
    /// Each gap between two sections that breaks the setting: the next section's first token, and the edit (a line break
    /// inserted, or the blank lines removed). Gaps with a comment or directive, sections on one line, and switch
    /// statements with syntax errors are skipped. Several blank lines where one is wanted are BRO1517's.
    /// </summary>
    public static IEnumerable<(TextSpan Location, TextChange Change)> GetFindings(SwitchStatementSyntax node, SourceText text, Mode mode, Func<string, bool> isOn)
    {
        if (node.Sections.Count < 2 || node.ContainsDiagnostics)
        {
            yield break;
        }

        for (var i = 1; i < node.Sections.Count; i++)
        {
            var previous = node.Sections[i - 1].GetLastToken();
            var next = node.Sections[i].GetFirstToken();
            if (!previous.TrailingTrivia.LastOrDefault().IsKind(SyntaxKind.EndOfLineTrivia)
                || !next.LeadingTrivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia))
                || (previous.IsKind(SyntaxKind.CloseBraceToken) && BlankLineRuns.JudgesGapAfter(previous, next, text, isOn, gapIsReplaced: true, mode)))
            {
                continue;
            }

            var wanted = mode == Mode.Include || (mode == Mode.OmitAfterBlock && node.Sections[i - 1].Statements.Last() is not BlockSyntax);
            var blank = next.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia);
            if (wanted && !blank)
            {
                yield return (next.Span, new TextChange(new TextSpan(next.FullSpan.Start, 0), SingleLineBlocks.LineBreak(text, next.SpanStart)));
            }
            else if (!wanted && blank)
            {
                yield return (next.Span, new TextChange(TextSpan.FromBounds(next.FullSpan.Start, text.Lines.GetLineFromPosition(next.SpanStart).Start), string.Empty));
            }
        }
    }
}
