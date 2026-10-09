using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using StyleBro.Analyzers.Maintainability;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// For the same_line joins of BRO1105 and BRO1111 at <c>max_line_length</c>: the line they join onto is measured as the
/// other fixes in the same run leave it, so the decision doesn't depend on which fix runs first.
/// </summary>
internal static class SameLineJoins
{
    /// <summary>
    /// The column <paramref name="position"/> will have once the other rules that are on have changed the declaration's
    /// head: BRO1404/BRO1007 add an access modifier, BRO1109 moves the parameter list's '(' up to the name, BRO1108 puts
    /// every parameter on its own line, BRO1110 moves the ')'. When the join takes the ')' along
    /// (<paramref name="closeMoves"/>) and BRO1110's own_line mode puts it on a line of its own, it starts that line. Not
    /// counted: renames and rewrites inside the line (another name, '@', 'int?'), whose lengths depend on checks this
    /// can't make.
    /// </summary>
    public static int GetColumn(SyntaxNode declaration, int position, bool closeMoves, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn, SemanticModel model, CancellationToken cancellationToken)
    {
        var list = GetParameterList(declaration);
        var close = list is null ? null : GetCloseChange(list, text, options, isOn);
        if (closeMoves && close?.NewText is { } closeText && closeText.LastIndexOf('\n') is var lineBreak and >= 0)
        {
            return closeText.Length - lineBreak - 1;
        }

        var changes = new List<TextChange>();
        if (declaration is MemberDeclarationSyntax member
            && AccessModifiers.GetFinding(member, model, cancellationToken, AccessModifiers.GetPreference(options)) is { } finding
            && isOn(finding.Id))
        {
            changes.Add(new TextChange(new TextSpan(finding.Position, 0), finding.Modifier + " "));
        }

        if (list is not null)
        {
            if (isOn(DiagnosticIds.OpenParenthesisOnNameLine) && ParenthesisPlacement.GetMisplacedOpen(list, text) is { } open)
            {
                changes.Add(ParenthesisPlacement.GetOpenChange(open, text));
            }

            // BRO1107 alone moves only the first item (not counted: it reaches the ')' line only when it spans lines up to it).
            if (isOn(DiagnosticIds.ParametersOnSameOrSeparateLines)
                && ParameterLayout.GetFirstMisplacedItem(list, text, () => isOn(DiagnosticIds.EmptyListOnOneLine)) is not null)
            {
                changes.AddRange(ParameterLayout.GetChanges(list, text, Indentation.GetUnit(options), sameLine: ParameterLayout.IsSameLine(options)));
            }

            // A ')' the join takes along moves after the position: it doesn't change the column.
            if (close is not null)
            {
                changes.Add(close.Value);
            }
        }

        // Earlier first; an edit overlapping one before it is left out (BRO1109 and BRO1108 both rewrite the gap after '(').
        var applied = new List<TextChange>();
        foreach (var change in changes.OrderBy(c => c.Span.Start))
        {
            if (applied.Count == 0 || applied[applied.Count - 1].Span.End <= change.Span.Start)
            {
                applied.Add(change);
            }
        }

        var newText = applied.Count == 0 ? text : text.WithChanges(applied);
        var mapped = position + applied.Where(c => c.Span.End <= position).Sum(c => c.NewText!.Length - c.Span.Length);
        var lineStart = mapped;
        while (lineStart > 0 && newText[lineStart - 1] != '\n')
        {
            lineStart--;
        }

        return mapped - lineStart;
    }

    private static TextChange? GetCloseChange(ParameterListSyntax list, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        var ownLine = ParenthesisPlacement.IsOwnLine(options);
        return isOn(DiagnosticIds.CloseParenthesisOnLastItemLine)
            && ParenthesisPlacement.GetCloseFix(list, text, ownLine, ownLine && isOn(DiagnosticIds.OpenParenthesisOnNameLine), options, isOn) is { } close
            ? close.Change
            : null;
    }

    private static ParameterListSyntax? GetParameterList(SyntaxNode declaration) => declaration switch
    {
        BaseMethodDeclarationSyntax method => method.ParameterList,
        LocalFunctionStatementSyntax local => local.ParameterList,
        DelegateDeclarationSyntax @delegate => @delegate.ParameterList,
        TypeDeclarationSyntax type => type.ParameterList,
        _ => null,
    };
}
