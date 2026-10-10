using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1109 (StyleCop SA1110: the opening parenthesis or bracket is on the line of the name it belongs
/// to) and BRO1110 (SA1111: the closing parenthesis or bracket is on the line where the last item ends).
/// </summary>
internal static class ParenthesisPlacement
{
    /// <summary>The .editorconfig key for where BRO1110 wants the closing token of a split list.</summary>
    public const string CloseKey = "stylebro_closing_parenthesis_placement";

    /// <summary>
    /// BRO1109: the opening token when it starts a line after the name before it: 'Method' then '(int a)' on the next
    /// line. Only lists that belong to a name (declarations, calls, 'new', element access, attributes, constructor
    /// initializers), not a lambda's parameters. Null when it's fine or the gap has comments or directives.
    /// </summary>
    public static SyntaxToken? GetMisplacedOpen(SyntaxNode list, SourceText text)
    {
        var (open, _, _) = ParameterLayoutList(list);

        // The token before the list lies in the list's parent: when the parent starts on the list's line, so does that
        // token (most calls), and finding it can be skipped.
        if (open.IsMissing || !BelongsToName(list) || Line(text, list.Parent!.SpanStart) == Line(text, open.SpanStart))
        {
            return null;
        }

        var previous = open.GetPreviousToken();
        return Line(text, previous.Span.End) < Line(text, open.SpanStart) && IsPlainGap(previous, open) ? open : null;
    }

    /// <summary>
    /// The edit for BRO1109: the opening token moves to the end of the name's line. Whatever followed it on its own
    /// line stays on that line, which becomes the list's first line: 'Method' / '(int a, int b)' turns into
    /// 'Method(' / 'int a, int b)'. An empty list is simply joined: 'Parens()'.
    /// </summary>
    public static TextChange GetOpenChange(SyntaxToken open, SourceText text)
    {
        var previous = open.GetPreviousToken();
        var gap = text.ToString(TextSpan.FromBounds(previous.Span.End, open.SpanStart));
        var next = open.GetNextToken();
        var restOfLineIsEmpty = Line(text, next.SpanStart) > Line(text, open.SpanStart);
        var joinOnly = next.IsKind(SyntaxKind.CloseParenToken) || next.IsKind(SyntaxKind.CloseBracketToken) || restOfLineIsEmpty;

        // Replace '<gap>(' (and the spaces after it) with '(' plus the gap, which keeps the next item at its indentation.
        var end = restOfLineIsEmpty || joinOnly ? open.Span.End : next.SpanStart;
        return new TextChange(TextSpan.FromBounds(previous.Span.End, end), open.Text + (joinOnly ? string.Empty : gap));
    }

    /// <summary>
    /// Whether <see cref="CloseKey"/> asks for the closing token of a split list on its own line (<c>own_line</c>)
    /// instead of at the end of the last item (<c>last_item</c>, the default, like StyleCop).
    /// </summary>
    public static bool IsOwnLine(AnalyzerConfigOptions options) =>
        options.TryGetValue(CloseKey, out var value) && value.Trim().Equals("own_line", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// BRO1110: the closing token when it is misplaced, with the edit that moves it. By default (and for a list that
    /// isn't split in <c>own_line</c> mode) it belongs at the end of the last item: see <see cref="GetMisplacedClose"/>.
    /// With <paramref name="ownLine"/>, a split list (see <see cref="GetSplitListLine"/>) has it on
    /// its own line, indented like that line: '    b)' turns into '    b' / ')'. The line of the opening token is the
    /// name's line when BRO1109 (<paramref name="openMoves"/>) moves the token up there, so both fixes agree in any order.
    /// A token already on its own line gets that indentation too. The indentation is the one that line has once the fixes
    /// that move tokens across lines are done (see <see cref="GetIndentation"/>), so nested lists agree in one run.
    /// Skipped in <c>own_line</c> mode: a comment between the last item and the token, directives in the list (each
    /// target framework's copy may have another last item) and a last item ending in single-line braces.
    /// </summary>
    /// <param name="options">The file's options, with <paramref name="isOn"/>: for BRO1105, which moves ': base('.</param>
    public static (SyntaxToken Close, TextChange Change)? GetCloseFix(SyntaxNode list, SourceText text, bool ownLine, bool openMoves, AnalyzerConfigOptions? options = null, Func<string, bool>? isOn = null)
    {
        var split = ownLine ? GetSplitListLine(list, text, openMoves) : default;
        if (split.Skip)
        {
            return null;
        }

        if (split.Line is { } openLine)
        {
            var (_, items, close) = ParameterLayoutList(list);
            var last = items[items.Count - 1].GetLastToken();
            if (list.ContainsDirectives)
            {
                return null;
            }

            var indentation = GetIndentation(list, openLine, text, openMoves, options, isOn);
            if (Line(text, last.Span.End) != Line(text, close.SpanStart))
            {
                // Already on its own line: only its indentation is checked, the same the fix gives a ')' it moves down.
                var closeLine = text.Lines.GetLineFromPosition(close.SpanStart);
                var before = text.ToString(TextSpan.FromBounds(closeLine.Start, close.SpanStart));
                return before == indentation || !string.IsNullOrWhiteSpace(before)
                    ? null
                    : (close, new TextChange(TextSpan.FromBounds(closeLine.Start, close.SpanStart), indentation));
            }

            if (!IsPlainGap(last, close))
            {
                return null;
            }

            var lineBreak = text.ToString(TextSpan.FromBounds(openLine.End, openLine.EndIncludingLineBreak));
            return (close, new TextChange(TextSpan.FromBounds(last.Span.End, close.SpanStart), (lineBreak.Length == 0 ? "\n" : lineBreak) + indentation));
        }

        return GetMisplacedClose(list, text) is { } misplaced ? (misplaced, GetCloseChange(misplaced, text)) : null;
    }

    /// <summary>
    /// Whether BRO1110, as configured for the file, moves the list's closing token, for fixes that join something onto
    /// the token's line (BRO1105/BRO1111 same_line) and take the token along. In own_line mode it is never pulled up
    /// from its own line; when it moves down, joining after it where it is gives the same text.
    /// </summary>
    public static bool MovesCloseToLastItem(SyntaxNode list, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn)
    {
        var ownLine = IsOwnLine(options);

        // A ')' on its own line is only reindented where it is (that edit starts at its line, not after the last item).
        return GetCloseFix(list, text, ownLine, ownLine && isOn(DiagnosticIds.OpenParenthesisOnNameLine), options, isOn) is { } fix
            && fix.Change.Span.Start == fix.Close.GetPreviousToken().Span.End;
    }

    /// <summary>The list's opening token, items and closing token, for the list kinds <see cref="ParameterLayout"/> checks.</summary>
    public static (SyntaxToken Open, IReadOnlyList<SyntaxNode> Items, SyntaxToken Close) ParameterLayoutList(SyntaxNode list)
    {
        return list switch
        {
            BaseParameterListSyntax parameters => (parameters.GetFirstToken(), parameters.Parameters, parameters.GetLastToken()),
            BaseArgumentListSyntax arguments => (arguments.GetFirstToken(), arguments.Arguments, arguments.GetLastToken()),
            AttributeArgumentListSyntax attributeArguments => (attributeArguments.OpenParenToken, attributeArguments.Arguments, attributeArguments.CloseParenToken),
            _ => (default, [], default),
        };
    }

    /// <summary>
    /// BRO1110: the closing token when it isn't on the line where the last item ends. Null for empty lists (spacing,
    /// StyleCop's SA1009), and for gaps with anything but whitespace and one comment at the end of the last item's line
    /// (a comment there is only moved when nothing but whitespace follows the closing token on its line, since the
    /// code after it would otherwise end up behind the comment).
    /// </summary>
    private static SyntaxToken? GetMisplacedClose(SyntaxNode list, SourceText text)
    {
        var (_, items, close) = ParameterLayoutList(list);
        if (items.Count == 0 || close.IsMissing)
        {
            return null;
        }

        var last = items[items.Count - 1].GetLastToken();
        if (Line(text, last.Span.End) == Line(text, close.SpanStart) || GetTrailingComment(last, close, text) is not { } comment)
        {
            return null;
        }

        // 'new Article(' ... ') { Author = user };': with ')' moved up, the SDK formatter (IDE0055, csharp_new_line_before_open_brace
        // = all, its default) puts the initializer's '{' on a line of its own below the call (RealWorld). Left alone.
        if (list.Parent is BaseObjectCreationExpressionSyntax { Initializer.OpenBraceToken: var brace }
            && Line(text, brace.SpanStart) == Line(text, close.SpanStart))
        {
            return null;
        }

        if (comment.Length > 0)
        {
            var closeLine = text.Lines.GetLineFromPosition(close.SpanStart);
            if (!string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(close.Span.End, closeLine.End))))
            {
                return null;
            }
        }

        return close;
    }

    /// <summary>
    /// The edit for BRO1110: the closing token moves right after the last item, and a comment at the end of the last
    /// item's line goes after it: 'int b // last' / ')' turns into 'int b) // last'.
    /// </summary>
    private static TextChange GetCloseChange(SyntaxToken close, SourceText text)
    {
        var last = close.GetPreviousToken();
        var comment = GetTrailingComment(last, close, text) ?? string.Empty;
        return new TextChange(TextSpan.FromBounds(last.Span.End, close.Span.End), close.Text + comment);
    }

    /// <summary>
    /// The line of the opening token (or of the name, when BRO1109 moves the token up there) when the list is split:
    /// its last item ends on a later line, unless every item starts on that line and the last one ends with a
    /// structure whose closing token the list's token hugs, like CSharpier: multi-line braces ('x =>' / '{' / '})'), or
    /// an argument list, tuple or collection expression opened on that line ('x => new T(' / 'a' / '))'). Neither
    /// changes when a fix moves the structure's closing token (an inner list's BRO1110), so the decision is stable; a
    /// parenthesized expression doesn't count (BRO1405 removes parentheses, BRO1406 adds them). Skip is true for a
    /// last item ending in single-line braces ('{ a(); }'), which BRO1508/BRO1509 may expand into a hugged block.
    /// </summary>
    private static (bool Skip, TextLine? Line) GetSplitListLine(SyntaxNode list, SourceText text, bool openMoves)
    {
        var (open, items, close) = ParameterLayoutList(list);
        if (items.Count == 0 || open.IsMissing || close.IsMissing)
        {
            return (false, null);
        }

        var start = openMoves && GetMisplacedOpen(list, text) is not null ? open.GetPreviousToken().SpanStart : open.SpanStart;
        var openLine = text.Lines.GetLineFromPosition(start);
        var lastItem = items[items.Count - 1];
        if (Line(text, lastItem.Span.End) == openLine.LineNumber)
        {
            return (false, null);
        }

        var last = lastItem.GetLastToken();
        if (Line(text, lastItem.SpanStart) != openLine.LineNumber || GetOpening(last) is not { } opening)
        {
            return (false, openLine);
        }

        var openingLine = Line(text, opening.SpanStart);
        if (!last.IsKind(SyntaxKind.CloseBraceToken))
        {
            return (false, openingLine == openLine.LineNumber ? null : openLine);
        }

        return openingLine == Line(text, last.SpanStart) ? (true, openLine) : (false, null);
    }

    /// <summary>
    /// The indentation of the line with the opening token (or the name, when BRO1109 moves the token up there) once the
    /// other fixes that move tokens across lines are done: BRO1105 puts ': base(' on a line of its own, and see
    /// <see cref="GetLineIndentation"/>.
    /// </summary>
    private static string GetIndentation(SyntaxNode list, TextLine openLine, SourceText text, bool openMoves, AnalyzerConfigOptions? options, Func<string, bool>? isOn)
    {
        // BRO1105's same_line mode never joins a split initializer, so only its own_line move counts.
        if (list.Parent is ConstructorInitializerSyntax initializer && options is not null && isOn is not null
            && isOn(DiagnosticIds.ConstructorInitializerLine) && !ConstructorInitializers.IsSameLine(options) && ConstructorInitializers.ShouldMove(initializer, text))
        {
            // Its new line: a line break, the indentation and ': '.
            var moved = ConstructorInitializers.GetChange(initializer, text, Indentation.GetUnit(options)).NewText!;
            return moved.Substring(moved.LastIndexOf('\n') + 1, moved.Length - moved.LastIndexOf('\n') - 3);
        }

        // The opening token, or the name when it moves up there (GetSplitListLine's line).
        var (open, _, _) = ParameterLayoutList(list);
        return GetLineIndentation(Line(text, open.SpanStart) == openLine.LineNumber ? open : open.GetPreviousToken(), text, openMoves, options, isOn);
    }

    /// <summary>
    /// The indentation of the token's line once BRO1110 and BRO1109 are done: a ')' before the token that BRO1110 puts on a
    /// line of its own starts the line, with the indentation the fix gives it; a ')' starting the line that moves up to
    /// the line before (BRO1110's last_item placement for a list that isn't split), or an empty '()' that BRO1109 joins
    /// to the name, takes the line along (BRO1109 keeps the rest of a non-empty list's line where it is).
    /// </summary>
    private static string GetLineIndentation(SyntaxToken token, SourceText text, bool openMoves, AnalyzerConfigOptions? options, Func<string, bool>? isOn)
    {
        var line = text.Lines.GetLineFromPosition(token.SpanStart);
        for (var current = token; current.SpanStart >= line.Start && !current.IsKind(SyntaxKind.None); current = current.GetPreviousToken())
        {
            var startsLine = string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, current.SpanStart)));
            if (current.Parent is { } parent && (current.IsKind(SyntaxKind.CloseParenToken) || current.IsKind(SyntaxKind.CloseBracketToken))
                && ParameterLayoutList(parent).Close == current && GetCloseFix(parent, text, true, openMoves, options, isOn) is { } fix)
            {
                var newText = fix.Change.NewText!;
                if (newText.IndexOf('\n') is var lineBreak and >= 0)
                {
                    return newText.Substring(lineBreak + 1);
                }

                if (fix.Change.Span.Start >= line.Start)
                {
                    return newText; // reindented where it is
                }

                if (startsLine)
                {
                    return GetLineIndentation(current.GetPreviousToken(), text, openMoves, options, isOn);
                }
            }
            else if (startsLine && openMoves && current.Parent is { } list && ParameterLayoutList(list) is var (open, _, close)
                && open == current && current.GetNextToken() == close && GetMisplacedOpen(list, text) is not null)
            {
                return GetLineIndentation(current.GetPreviousToken(), text, openMoves, options, isOn);
            }
        }

        return new string(text.ToString(line.Span).TakeWhile(c => c is ' ' or '\t').ToArray());
    }

    /// <summary>The opening token of the structure a closing token ends, for the closing tokens a list hugs.</summary>
    private static SyntaxToken? GetOpening(SyntaxToken closing)
    {
        var kind = closing.Kind() switch
        {
            SyntaxKind.CloseBraceToken => SyntaxKind.OpenBraceToken,
            SyntaxKind.CloseParenToken when closing.Parent is ArgumentListSyntax or TupleExpressionSyntax => SyntaxKind.OpenParenToken,
            SyntaxKind.CloseBracketToken when closing.Parent is BracketedArgumentListSyntax or CollectionExpressionSyntax => SyntaxKind.OpenBracketToken,
            _ => SyntaxKind.None,
        };
        return kind == SyntaxKind.None ? null : closing.Parent!.ChildTokens().Where(t => t.IsKind(kind)).Cast<SyntaxToken?>().FirstOrDefault();
    }

    private static bool BelongsToName(SyntaxNode list)
    {
        return list.Parent is BaseMethodDeclarationSyntax
            or DelegateDeclarationSyntax
            or LocalFunctionStatementSyntax
            or IndexerDeclarationSyntax
            or TypeDeclarationSyntax
            or InvocationExpressionSyntax
            or BaseObjectCreationExpressionSyntax
            or ElementAccessExpressionSyntax
            or AttributeSyntax
            or ConstructorInitializerSyntax
            or PrimaryConstructorBaseTypeSyntax;
    }

    /// <summary>Only whitespace and line breaks between two tokens.</summary>
    private static bool IsPlainGap(SyntaxToken before, SyntaxToken after)
    {
        return before.TrailingTrivia.Concat(after.LeadingTrivia)
            .All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));
    }

    /// <summary>
    /// The text to keep after the moved closing token: empty when the gap is only whitespace, the comment (with the
    /// spaces before it) when the last item's line ends with a single-line comment, or null when the gap holds
    /// anything else.
    /// </summary>
    private static string? GetTrailingComment(SyntaxToken last, SyntaxToken close, SourceText text)
    {
        var trivia = last.TrailingTrivia.Concat(close.LeadingTrivia).ToList();
        var comments = trivia.Where(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)).ToList();
        if (comments.Count == 0)
        {
            return string.Empty;
        }

        var comment = comments[0];
        if (comments.Count > 1 || !comment.IsKind(SyntaxKind.SingleLineCommentTrivia) || Line(text, comment.SpanStart) != Line(text, last.Span.End))
        {
            return null;
        }

        return text.ToString(TextSpan.FromBounds(last.Span.End, comment.Span.End));
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;
}
