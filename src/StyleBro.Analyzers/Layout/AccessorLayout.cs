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
/// Shared logic for BRO1510 (StyleCop SA1504: a property's, indexer's or event's accessors are all single-line or all
/// multi-line).
/// <para>
/// Like StyleCop 1.3.177-beta: only accessor lists where every accessor has a body count, a block or an expression
/// (<c>get;</c> next to a multi-line <c>set { }</c> is fine), and only multi-line lists (a list on one line is
/// BRO1509's). The fix goes the short way when it can: when every multi-line accessor is a block holding at most one
/// single-line statement, those are put on one line (<c>set { this.x = value; }</c>); otherwise the single-line ones
/// are expanded, an expression-bodied one (<c>get => this.x;</c>) into a block (<c>return this.x;</c>). Unlike
/// StyleCop's fix, a comment inside a body is never dropped: it rules out collapsing, and a comment in a gap the fix
/// rewrites skips the list.
/// </para>
/// </summary>
internal static class AccessorLayout
{
    /// <summary>The SDK's rule for expression-bodied accessors (csharp_style_expression_bodied_accessors).</summary>
    public const string ExpressionBodiedAccessorsId = "IDE0027";

    private const string ExpressionBodiedAccessorsKey = "csharp_style_expression_bodied_accessors";

    /// <summary>The accessor whose position the diagnostic uses (the first), or null when the list is fine or skipped.</summary>
    public static AccessorDeclarationSyntax? GetFinding(AccessorListSyntax list, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        return GetChanges(list, text, options, isOn) is not null ? list.Accessors[0] : null;
    }

    /// <summary>
    /// The edits that make the accessors consistent, or null when they already are or can't be changed safely.
    /// <paramref name="isOn"/>: whether a rule is on for the file (asked about <see cref="ExpressionBodiedAccessorsId"/>).
    /// </summary>
    public static List<TextChange>? GetChanges(AccessorListSyntax list, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        var accessors = list.Accessors;
        if (list.ContainsDiagnostics || list.ContainsDirectives || accessors.Count < 2 || accessors.Any(a => a.Body is null && a.ExpressionBody is null)
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

        // Expanding 'get => x;' into a block while the SDK's IDE0027 wants expression bodies: the two fixes would undo
        // each other, so the list is left alone.
        if (!collapse && singleLine.Any(a => a.Body is null) && PrefersExpressionBodies(options, isOn))
        {
            return null;
        }

        foreach (var accessor in collapse ? multiLine : singleLine)
        {
            var ok = collapse ? Collapse(accessor.Body!, text, changes)
                : accessor.Body is { } body ? Expand(accessor, body, text, options, changes)
                : ExpandExpression(accessor, text, options, changes);
            if (!ok)
            {
                return null;
            }
        }

        // The blank line BRO1505 wants between these accessors now: added here too, so the result doesn't depend on
        // whether 'dotnet format' runs BRO1505's fix before or after this one (identical edits are merged). Also the one
        // it wants once an expression-bodied accessor is a block: next to another block.
        bool BecomesBlock(AccessorDeclarationSyntax accessor) => !collapse && (accessor.Body is not null || IsSingleLine(accessor, text));
        for (var i = 1; i < accessors.Count; i++)
        {
            if ((ElementSeparation.NeedsBlankLine(accessors[i - 1], accessors[i], text) || (BecomesBlock(accessors[i - 1]) && BecomesBlock(accessors[i])))
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
        // An expression body over several lines stays as written: the single-line accessors are expanded instead.
        if (accessor.Body is not { } body)
        {
            return false;
        }

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

    /// <summary>
    /// 'get => x;' becomes a block laid out like <see cref="Expand"/>'s, holding 'return x;' (a getter) or 'x = value;'
    /// (set, init, add, remove); 'get => throw ...;' holds the throw statement.
    /// </summary>
    private static bool ExpandExpression(AccessorDeclarationSyntax accessor, SourceText text, AnalyzerConfigOptions options, List<TextChange> changes)
    {
        var arrow = accessor.ExpressionBody!;
        var expression = arrow.Expression;
        var probe = new List<TextChange>();
        if (!SingleLineBlocks.Gap(accessor.Keyword, arrow.ArrowToken, " ", text, probe)
            || !SingleLineBlocks.Gap(arrow.ArrowToken, expression.GetFirstToken(), " ", text, probe)
            || !SingleLineBlocks.Gap(expression.GetLastToken(), accessor.SemicolonToken, string.Empty, text, probe))
        {
            return false;
        }

        var line = text.Lines.GetLineFromPosition(accessor.SpanStart);
        var indent = text.ToString(TextSpan.FromBounds(line.Start, accessor.SpanStart));
        var lineBreak = SingleLineBlocks.LineBreak(text, accessor.SpanStart);
        var beforeOpen = SingleLineBlocks.NewLineBeforeAccessorBrace(options) ? lineBreak + indent : " ";
        var keyword = accessor.IsKind(SyntaxKind.GetAccessorDeclaration) && !expression.IsKind(SyntaxKind.ThrowExpression) ? "return " : string.Empty;
        changes.Add(new TextChange(
            TextSpan.FromBounds(accessor.Keyword.Span.End, expression.SpanStart),
            beforeOpen + "{" + lineBreak + indent + Indentation.GetUnit(options) + keyword));
        changes.Add(new TextChange(TextSpan.FromBounds(expression.Span.End, accessor.SemicolonToken.Span.End), ";" + lineBreak + indent + "}"));
        return true;
    }

    /// <summary>
    /// Whether the SDK's IDE0027 turns a one-statement block accessor into an expression body: its option says so
    /// ('true' or 'when_on_single_line'), or nothing sets the option (its default is 'true') and the rule is on.
    /// </summary>
    private static bool PrefersExpressionBodies(AnalyzerConfigOptions options, Func<string, bool> isOn) =>
        options.TryGetValue(ExpressionBodiedAccessorsKey, out var value)
            ? value.Split(':')[0].Trim().ToLowerInvariant() is "true" or "when_on_single_line"
            : isOn(ExpressionBodiedAccessorsId);

    /// <summary>
    /// From the first modifier or the keyword to the end: an attribute on its own line above the accessor doesn't make
    /// it multi-line (StyleCop's SA1504 counts it, #3434).
    /// </summary>
    private static bool IsSingleLine(AccessorDeclarationSyntax accessor, SourceText text) =>
        Line(text, (accessor.Modifiers.Count > 0 ? accessor.Modifiers[0] : accessor.Keyword).SpanStart) == Line(text, accessor.Span.End);

    private static bool StartsLine(AccessorDeclarationSyntax accessor, SourceText text)
    {
        var line = text.Lines.GetLineFromPosition(accessor.SpanStart);
        return text.ToString(TextSpan.FromBounds(line.Start, accessor.SpanStart)).All(c => c is ' ' or '\t');
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
