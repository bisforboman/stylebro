using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Maintainability;

/// <summary>Shared logic for BRO1401, used by both the analyzer and the code fix.</summary>
internal static class TrailingCommas
{
    /// <summary>The .editorconfig key: <c>include</c> (default, StyleCop's SA1413) or <c>omit</c>.</summary>
    public const string OptionKey = "stylebro_trailing_comma";

    /// <summary>Whether the file's setting is <c>omit</c>: no trailing comma after the last item of any list.</summary>
    public static bool Omits(AnalyzerConfigOptions options) =>
        options.TryGetValue(OptionKey, out var value) && value.Split(':')[0].Trim().Equals("omit", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// What BRO1401 reports for a list under the file's setting, or null. Include mode (StyleCop's SA1413): the last item
    /// of a multi-line list without a trailing comma, and the comma's insertion. Omit mode: the trailing comma after the
    /// last item, and its removal; unlike include mode also in single-line lists (<c>new[] { 1, 2, }</c>, like
    /// Roslynator's RCS1260 omit), where a comma never saves a diff line.
    /// <para>
    /// Like StyleCop, the lists are array, object, collection and 'with' initializers, anonymous objects, enums, switch
    /// expressions (not property patterns, like StyleCop 1.2.0-beta.556; only its unreleased master checks them); a
    /// list is multi-line when its braces are on different lines. The comma goes right after the last item's code, so before a trailing comment ('2 // last' becomes
    /// '2, // last'). Lists with a preprocessor directive between their braces are skipped: which item is last can then
    /// depend on the build configuration, and in a multi-targeted project each target framework would want a different
    /// edit to the same file, which 'dotnet format' merges into conflict markers. The options are only read for a list
    /// one of the modes could report (most lists are single-line without a comma).
    /// </para>
    /// </summary>
    public static (Location Location, TextChange Change)? GetFinding(SyntaxNode node, SourceText text, Func<AnalyzerConfigOptions> getOptions)
    {
        if (GetList(node) is not { } l
            || l.Last is null
            || l.OpenBrace.IsMissing
            || l.CloseBrace.IsMissing)
        {
            return null;
        }

        // Include mode reports a multi-line list without the comma, omit mode a list with it.
        var hasComma = l.SeparatorCount == l.Count;
        if ((!hasComma && Line(text, l.OpenBrace.SpanStart) == Line(text, l.CloseBrace.SpanStart))
            || Omits(getOptions()) != hasComma
            || node.ContainsDiagnostics
            || HasDirectiveBetween(node, l.OpenBrace, l.CloseBrace))
        {
            return null;
        }

        if (hasComma)
        {
            var comma = l.Last.GetLastToken().GetNextToken();
            return (comma.GetLocation(), GetRemoval(comma, text));
        }

        return (l.Last.GetLocation(), new TextChange(new TextSpan(l.Last.Span.End, 0), GetInsertion(l.Last, text)));
    }

    /// <summary>
    /// The edit removing a trailing comma. Spaces right before the comma go too ('A ,' at a line end would leave trailing
    /// whitespace); a trailing comment stays where it is ('A, // x' and 'A,// x' become 'A // x').
    /// </summary>
    public static TextChange GetRemoval(SyntaxToken comma, SourceText text)
    {
        var start = comma.SpanStart;
        while (start > 0 && text[start - 1] is ' ' or '\t')
        {
            start--;
        }

        var end = comma.Span.End;
        return new TextChange(TextSpan.FromBounds(start, end), end < text.Length && text[end] == '/' ? " " : string.Empty);
    }

    /// <summary>
    /// The text to insert after the last item: ',' or, when code follows directly ('"x"}'), ', ' so the fix doesn't
    /// leave a comma without the space after it that the formatter and StyleCop's SA1001 expect.
    /// </summary>
    public static string GetInsertion(SyntaxNode lastItem, SourceText text)
    {
        var end = lastItem.Span.End;
        return end < text.Length && text[end] is not (' ' or '\t' or '\r' or '\n') ? ", " : ",";
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

    private static bool HasDirectiveBetween(SyntaxNode node, SyntaxToken open, SyntaxToken close)
    {
        var inside = TextSpan.FromBounds(open.Span.End, close.SpanStart);
        foreach (var trivia in node.DescendantTrivia(inside))
        {
            if (trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            {
                return true;
            }
        }

        return false;
    }

    private static (int Count, int SeparatorCount, SyntaxNode? Last, SyntaxToken OpenBrace, SyntaxToken CloseBrace)? GetList(SyntaxNode node)
    {
        return node switch
        {
            InitializerExpressionSyntax initializer when initializer.Kind() is SyntaxKind.ArrayInitializerExpression
                or SyntaxKind.ObjectInitializerExpression
                or SyntaxKind.CollectionInitializerExpression
                or SyntaxKind.WithInitializerExpression =>
                Describe(initializer.Expressions, initializer.OpenBraceToken, initializer.CloseBraceToken),
            AnonymousObjectCreationExpressionSyntax anonymous =>
                Describe(anonymous.Initializers, anonymous.OpenBraceToken, anonymous.CloseBraceToken),
            EnumDeclarationSyntax enumDeclaration =>
                Describe(enumDeclaration.Members, enumDeclaration.OpenBraceToken, enumDeclaration.CloseBraceToken),
            SwitchExpressionSyntax switchExpression =>
                Describe(switchExpression.Arms, switchExpression.OpenBraceToken, switchExpression.CloseBraceToken),
            _ => null,
        };
    }

    private static (int, int, SyntaxNode?, SyntaxToken, SyntaxToken) Describe<T>(SeparatedSyntaxList<T> items, SyntaxToken open, SyntaxToken close)
        where T : SyntaxNode
    {
        return (items.Count, items.SeparatorCount, items.Count > 0 ? items[items.Count - 1] : null, open, close);
    }
}
