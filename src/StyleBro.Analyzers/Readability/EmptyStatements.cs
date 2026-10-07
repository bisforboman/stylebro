using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1101, used by both the analyzer and the code fix: which semicolons are removable,
/// and the text edits that remove them.
/// </summary>
internal static class EmptyStatements
{
    /// <summary>
    /// An empty statement in a block or switch section. Not reported: labeled empty statements ('label: ;'),
    /// where removing the ';' can leave the label without a statement, and embedded ones ('while (x) ;'), which
    /// the compiler already flags as CS0642 and where a fix would have to guess the intent.
    /// </summary>
    public static bool IsRemovable(EmptyStatementSyntax statement)
    {
        return statement.Parent is BlockSyntax or SwitchSectionSyntax
            && statement.AttributeLists.Count == 0;
    }

    /// <summary>The optional ';' after the closing brace of a type or namespace ('class C { };').</summary>
    public static SyntaxToken GetRemovableTrailingSemicolon(SyntaxNode node)
    {
        var (openBrace, semicolon) = node switch
        {
            BaseTypeDeclarationSyntax type => (type.OpenBraceToken, type.SemicolonToken),
            NamespaceDeclarationSyntax ns => (ns.OpenBraceToken, ns.SemicolonToken),
            _ => (default, default),
        };

        // A record without a body ('record R(int X);') needs its ';'.
        return openBrace.IsKind(SyntaxKind.OpenBraceToken) && !openBrace.IsMissing
            && semicolon.IsKind(SyntaxKind.SemicolonToken) && !semicolon.IsMissing
                ? semicolon
                : default;
    }

    /// <summary>
    /// Deletions that remove the given semicolons. Computed per line, so a single fix and Fix All agree:
    /// a line left with only whitespace is removed completely; otherwise the semicolons go together with the
    /// whitespace that separated them from the code before them, or, at the start of a line, from the code after
    /// them (so '; // note' keeps the comment at the original indentation). Preprocessor directives always
    /// have lines of their own, so these edits never touch one.
    /// </summary>
    public static IEnumerable<TextChange> GetChanges(SourceText text, IEnumerable<TextSpan> semicolons)
    {
        foreach (var group in semicolons.Distinct().GroupBy(span => text.Lines.GetLineFromPosition(span.Start).LineNumber))
        {
            var line = text.Lines[group.Key];
            var lineText = text.ToString(line.Span);
            var remove = new bool[lineText.Length];
            foreach (var span in group)
            {
                for (var i = span.Start - line.Start; i < span.End - line.Start; i++)
                {
                    remove[i] = true;
                }
            }

            if (Enumerable.Range(0, lineText.Length).All(i => remove[i] || IsBlank(lineText[i])))
            {
                yield return new TextChange(line.SpanIncludingLineBreak, string.Empty);
                continue;
            }

            foreach (var (start, end) in GetRuns(lineText, remove))
            {
                var atLineStart = lineText.Take(start).All(IsBlank);
                var from = start;
                var to = end;
                if (atLineStart)
                {
                    while (to < lineText.Length && IsBlank(lineText[to]))
                    {
                        to++;
                    }
                }
                else
                {
                    while (from > 0 && IsBlank(lineText[from - 1]))
                    {
                        from--;
                    }
                }

                yield return new TextChange(new TextSpan(line.Start + from, to - from), string.Empty);
            }
        }
    }

    /// <summary>Stretches of removed characters, including the blanks between them ('; ;' is one run).</summary>
    private static IEnumerable<(int Start, int End)> GetRuns(string lineText, bool[] remove)
    {
        var i = 0;
        while (i < lineText.Length)
        {
            if (!remove[i])
            {
                i++;
                continue;
            }

            var start = i;
            var end = i + 1;
            var j = end;
            while (j < lineText.Length && (remove[j] || IsBlank(lineText[j])))
            {
                if (remove[j])
                {
                    end = j + 1;
                }

                j++;
            }

            yield return (start, end);
            i = end;
        }
    }

    private static bool IsBlank(char c) => c == ' ' || c == '\t';
}
