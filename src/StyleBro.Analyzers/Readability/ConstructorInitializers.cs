using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1105, used by both the analyzer and the code fix.</summary>
internal static class ConstructorInitializers
{
    /// <summary>'own_line' (default, StyleCop's SA1128) or 'same_line' (the initializer joins the parameter list's last line).</summary>
    public const string PlacementKey = "stylebro_constructor_initializer_placement";

    /// <summary>Whether <see cref="PlacementKey"/> is 'same_line'.</summary>
    public static bool IsSameLine(AnalyzerConfigOptions options)
    {
        return options.TryGetValue(PlacementKey, out var value) && value.Trim() == "same_line";
    }

    /// <summary>
    /// For 'same_line': the edit that joins ': base(...)' onto the line of the parameter list's ')', or null when it's
    /// there already or can't be joined: the initializer spans several lines, anything but whitespace sits between ')'
    /// and 'base'/'this', or the joined line (up to the initializer's end; a body after it isn't counted, BRO1509 may move
    /// it) would be longer than 'max_line_length' (no limit when unset). When BRO1110 is on and moves the ')' to the last
    /// parameter's line, the ')' moves in the same edit, so both fixes give the same text in either order. The line is
    /// measured as the other fixes leave it (<see cref="SameLineJoins.GetColumn"/>).
    /// </summary>
    public static TextChange? GetJoin(ConstructorInitializerSyntax initializer, SourceText text, AnalyzerConfigOptions options, Func<string, bool> isOn, SemanticModel model, CancellationToken cancellationToken)
    {
        var keyword = initializer.ThisOrBaseKeyword;
        if (initializer.ColonToken.IsMissing || initializer.ContainsDiagnostics
            || initializer.Parent is not ConstructorDeclarationSyntax { ParameterList.CloseParenToken: { IsMissing: false } close }
            || Line(text, close.Span.End) == Line(text, keyword.SpanStart)
            || Line(text, keyword.SpanStart) != Line(text, initializer.Span.End)
            || !IsPlain(close.TrailingTrivia, initializer.ColonToken.LeadingTrivia, initializer.ColonToken.TrailingTrivia, keyword.LeadingTrivia))
        {
            return null;
        }

        var start = close.Span.End;
        var closeText = string.Empty;
        if (isOn(DiagnosticIds.CloseParenthesisOnLastItemLine) && ParenthesisPlacement.MovesCloseToLastItem(close.Parent!, text, options, isOn))
        {
            var last = close.GetPreviousToken();
            if (!IsPlain(last.TrailingTrivia, close.LeadingTrivia))
            {
                return null;
            }

            start = last.Span.End;
            closeText = close.Text;
        }

        var newText = closeText + " : ";
        var maxLength = Indentation.GetMaxLineLength(options);
        return maxLength != int.MaxValue
            && SameLineJoins.GetColumn(initializer.Parent!, start, closeText.Length > 0, text, options, isOn, model, cancellationToken) + newText.Length + (initializer.Span.End - keyword.SpanStart) > maxLength
            ? null
            : new TextChange(TextSpan.FromBounds(start, keyword.SpanStart), newText);
    }

    /// <summary>
    /// Whether ': base(...)' or ': this(...)' should move to its own line: its colon doesn't start a line. Skipped
    /// when a comment sits between the colon and 'base'/'this', since the fix rewrites exactly that stretch.
    /// </summary>
    public static bool ShouldMove(ConstructorInitializerSyntax initializer, SourceText text)
    {
        var colon = initializer.ColonToken;
        if (colon.IsMissing || initializer.ContainsDiagnostics || StartsLine(colon, text))
        {
            return false;
        }

        return colon.TrailingTrivia.Concat(initializer.ThisOrBaseKeyword.LeadingTrivia)
            .All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia));
    }

    /// <summary>
    /// Replaces the stretch from the colon (including blanks before it) up to 'base'/'this' with a line break and
    /// ': ', one indentation level deeper than the constructor. The arguments and the body stay as they are, and a
    /// comment before the colon stays on the constructor's line, like StyleCop's fix.
    /// </summary>
    public static TextChange GetChange(ConstructorInitializerSyntax initializer, SourceText text, string indentUnit)
    {
        var colon = initializer.ColonToken;
        var colonLine = text.Lines.GetLineFromPosition(colon.SpanStart);
        var start = colon.SpanStart;
        while (start > colonLine.Start && IsBlank(text[start - 1]))
        {
            start--;
        }

        var anchor = initializer.Parent is ConstructorDeclarationSyntax constructor ? constructor.Identifier.SpanStart : colon.SpanStart;
        var anchorLine = text.Lines.GetLineFromPosition(anchor);
        var indentation = new string(text.ToString(anchorLine.Span).TakeWhile(IsBlank).ToArray());
        var lineBreak = text.ToString(TextSpan.FromBounds(colonLine.End, colonLine.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        return new TextChange(
            TextSpan.FromBounds(start, initializer.ThisOrBaseKeyword.SpanStart),
            lineBreak + indentation + indentUnit + ": ");
    }

    private static bool IsPlain(params SyntaxTriviaList[] lists)
    {
        return lists.All(list => list.All(t => t.IsKind(SyntaxKind.WhitespaceTrivia) || t.IsKind(SyntaxKind.EndOfLineTrivia)));
    }

    private static int Line(SourceText text, int position) => text.Lines.GetLineFromPosition(position).LineNumber;

    private static bool StartsLine(SyntaxToken token, SourceText text)
    {
        var line = text.Lines.GetLineFromPosition(token.SpanStart);
        for (var i = line.Start; i < token.SpanStart; i++)
        {
            if (!IsBlank(text[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBlank(char c) => c == ' ' || c == '\t';
}
