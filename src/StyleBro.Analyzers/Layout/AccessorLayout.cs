using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// Shared logic for BRO1510 (StyleCop SA1504: a property's, indexer's or event's accessors are all single-line or all
/// multi-line).
/// <para>
/// Like StyleCop: only accessor lists where every accessor has a block body count (<c>get => x;</c> or <c>get;</c> next
/// to a multi-line <c>set { }</c> is fine), and only multi-line lists (a list on one line is BRO1509's). The fix goes
/// the way StyleCop's does: when every multi-line accessor holds at most one single-line statement, those are put on
/// one line (<c>set { this.x = value; }</c>); otherwise the single-line ones are expanded. Unlike StyleCop's fix, a
/// comment inside a body is never dropped: it rules out collapsing, and a comment in a gap the fix rewrites skips the
/// list.
/// </para>
/// </summary>
internal static class AccessorLayout
{
    /// <summary>The accessor whose position the diagnostic uses (the first), or null when the list is fine or skipped.</summary>
    public static AccessorDeclarationSyntax? GetFinding(AccessorListSyntax list, SourceText text, AnalyzerConfigOptions options)
    {
        return GetChanges(list, text, options) is not null ? list.Accessors[0] : null;
    }

    /// <summary>The edits that make the accessors consistent, or null when they already are or can't be changed safely.</summary>
    public static List<TextChange>? GetChanges(AccessorListSyntax list, SourceText text, AnalyzerConfigOptions options)
    {
        var accessors = list.Accessors;
        if (list.ContainsDiagnostics || list.ContainsDirectives || accessors.Count < 2 || accessors.Any(a => a.Body is null)
            || Line(text, list.OpenBraceToken.SpanStart) == Line(text, list.CloseBraceToken.SpanStart)
            || accessors.Any(a => !StartsLine(a, text)))
        {
            return null;
        }

        var singleLine = accessors.Where(a => IsSingleLine(a, text)).ToList();
        if (singleLine.Count == 0 || singleLine.Count == accessors.Count)
        {
            return null;
        }

        var changes = new List<TextChange>();
        var multiLine = accessors.Where(a => !IsSingleLine(a, text)).ToList();
        var collapse = multiLine.All(a => CanCollapse(a, text));
        foreach (var accessor in collapse ? multiLine : singleLine)
        {
            var body = accessor.Body!;
            var ok = collapse ? Collapse(body, text, changes) : Expand(accessor, body, text, options, changes);
            if (!ok)
            {
                return null;
            }
        }

        // The blank line BRO1505 wants between these accessors now: added here too, so the result doesn't depend on
        // whether 'dotnet format' runs BRO1505's fix before or after this one (identical edits are merged).
        for (var i = 1; i < accessors.Count; i++)
        {
            if (ElementSeparation.NeedsBlankLine(accessors[i - 1], accessors[i], text)
                && !ElementSeparation.HasBlankLineBetween(accessors[i - 1], accessors[i], text)
                && ElementSeparation.GetChange(accessors[i - 1], accessors[i], text) is { } blankLine)
            {
                changes.Add(blankLine);
            }
        }

        return changes;
    }

    /// <summary>At most one statement on a single line, nothing but whitespace around it, and the body's braces on the keyword's line or below.</summary>
    private static bool CanCollapse(AccessorDeclarationSyntax accessor, SourceText text)
    {
        var body = accessor.Body!;
        if (body.Statements.Count > 1 || Line(text, accessor.SpanStart) != Line(text, accessor.Keyword.SpanStart))
        {
            return false;
        }

        if (body.Statements.Count == 1 && Line(text, body.Statements[0].SpanStart) != Line(text, body.Statements[0].Span.End))
        {
            return false;
        }

        return body.DescendantTrivia().All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia))
            && body.OpenBraceToken.LeadingTrivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia))
            && body.OpenBraceToken.GetPreviousToken().TrailingTrivia.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));
    }

    /// <summary><c>set { this.x = value; }</c>: the braces join the keyword's line, single spaces inside.</summary>
    private static bool Collapse(BlockSyntax body, SourceText text, List<TextChange> changes)
    {
        if (!SingleLineBlocks.Gap(body.OpenBraceToken.GetPreviousToken(), body.OpenBraceToken, " ", text, changes))
        {
            return false;
        }

        if (body.Statements.Count == 0)
        {
            return SingleLineBlocks.Gap(body.OpenBraceToken, body.CloseBraceToken, " ", text, changes);
        }

        var statement = body.Statements[0];
        return SingleLineBlocks.Gap(body.OpenBraceToken, statement.GetFirstToken(), " ", text, changes)
            && SingleLineBlocks.Gap(statement.GetLastToken(), body.CloseBraceToken, " ", text, changes);
    }

    /// <summary>The braces on their own lines at the accessor's indentation, each statement one level deeper.</summary>
    private static bool Expand(AccessorDeclarationSyntax accessor, BlockSyntax body, SourceText text, AnalyzerConfigOptions options, List<TextChange> changes)
    {
        var line = text.Lines.GetLineFromPosition(accessor.SpanStart);
        var indent = text.ToString(TextSpan.FromBounds(line.Start, accessor.SpanStart));
        var inner = indent + Indentation.GetUnit(options);
        var lineBreak = SingleLineBlocks.LineBreak(text, accessor.SpanStart);

        var beforeOpen = SingleLineBlocks.NewLineBeforeBrace(body, options) ? lineBreak + indent : " ";
        if (!SingleLineBlocks.Gap(body.OpenBraceToken.GetPreviousToken(), body.OpenBraceToken, beforeOpen, text, changes))
        {
            return false;
        }

        if (body.Statements.Count == 0)
        {
            return SingleLineBlocks.Gap(body.OpenBraceToken, body.CloseBraceToken, lineBreak + indent, text, changes);
        }

        var previous = body.OpenBraceToken;
        foreach (var statement in body.Statements)
        {
            if (!SingleLineBlocks.Gap(previous, statement.GetFirstToken(), lineBreak + inner, text, changes))
            {
                return false;
            }

            previous = statement.GetLastToken();
        }

        return SingleLineBlocks.Gap(previous, body.CloseBraceToken, lineBreak + indent, text, changes);
    }

    private static bool IsSingleLine(AccessorDeclarationSyntax accessor, SourceText text) =>
        Line(text, accessor.SpanStart) == Line(text, accessor.Span.End);

    private static bool StartsLine(AccessorDeclarationSyntax accessor, SourceText text)
    {
        var line = text.Lines.GetLineFromPosition(accessor.SpanStart);
        return text.ToString(TextSpan.FromBounds(line.Start, accessor.SpanStart)).All(c => c is ' ' or '\t');
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
