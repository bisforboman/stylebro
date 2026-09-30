using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>Shared logic for BRO1105, used by both the analyzer and the code fix.</summary>
internal static class ConstructorInitializers
{
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
