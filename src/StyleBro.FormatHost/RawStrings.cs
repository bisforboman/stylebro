using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StyleBro.Host;

/// <summary>
/// The text of the raw string literals a fix must not change. A fix may move a raw string (the whole literal gets another
/// indentation), but never what it says: the literal's lines are compared without the indentation of its closing quotes, an
/// interpolation hole counting as one placeholder. A document whose fix changed one is put back as it was before the fix (trial 66,
/// moq4: IDE0055's formatter added a space to two lines of a <c>$$"""</c> literal in every run, and the format never converged).
/// </summary>
public static class RawStrings
{
    private const string ConvertsStrings = "BRO1138";

    /// <summary>
    /// <paramref name="after"/> with every document the fixer changed put back to its text in <paramref name="before"/> when the
    /// change altered a raw string literal; the files it kept are named in the log. <paramref name="ruleId"/> is the rule whose
    /// fix ran (null: the whitespace formatting); BRO1138 turns plain strings into raw ones and back on purpose, so its fix is not
    /// held to this (the Mediator real-world run, which converts four).
    /// </summary>
    public static async Task<Solution> KeepAsync(Solution before, Solution after, string? ruleId, string id, Action<string> warn, CancellationToken cancellation)
    {
        if (ruleId == ConvertsStrings)
        {
            return after;
        }

        foreach (var project in after.GetChanges(before).GetProjectChanges())
        {
            foreach (var documentId in project.GetChangedDocuments())
            {
                var old = before.GetDocument(documentId);
                var changed = after.GetDocument(documentId);
                if (old is null || changed is null || !old.SupportsSyntaxTree)
                {
                    continue;
                }

                var oldText = await old.GetTextAsync(cancellation).ConfigureAwait(false);
                if (!Contains(oldText))
                {
                    continue;
                }

                var oldRoot = await old.GetSyntaxRootAsync(cancellation).ConfigureAwait(false);
                var newRoot = await changed.GetSyntaxRootAsync(cancellation).ConfigureAwait(false);
                if (oldRoot is null || newRoot is null || Same(Values(oldRoot), Values(newRoot)))
                {
                    continue;
                }

                warn($"Kept {old.FilePath ?? old.Name} as it was: {id} would change the text of a raw string literal.");
                after = after.WithDocumentText(documentId, oldText, PreservationMode.PreserveValue);
            }
        }

        return after;
    }

    /// <summary>Whether two documents' raw string literals say the same (the order of the literals doesn't count).</summary>
    public static bool Same(string before, string after) =>
        Same(Values(CSharpSyntaxTree.ParseText(before).GetRoot()), Values(CSharpSyntaxTree.ParseText(after).GetRoot()));

    private static bool Same(List<string> before, List<string> after) => before.SequenceEqual(after, StringComparer.Ordinal);

    private static bool Contains(Microsoft.CodeAnalysis.Text.SourceText text)
    {
        // Cheap test before any parse: a raw string has three quotes in a row.
        for (var i = 0; i + 2 < text.Length; i++)
        {
            if (text[i] == '"' && text[i + 1] == '"' && text[i + 2] == '"')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The values of the multi-line raw string literals under <paramref name="root"/>, sorted.</summary>
    private static List<string> Values(SyntaxNode root)
    {
        var values = new List<string>();
        foreach (var node in root.DescendantNodes(descendIntoTrivia: false))
        {
            switch (node)
            {
                case LiteralExpressionSyntax { Token: { } token } when token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken):
                    values.Add(Unindented(node.ToString()));
                    break;

                case InterpolatedStringExpressionSyntax { StringStartToken: { } start } interpolated when start.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken):
                    // The literal's source (a text token's own text leaves the margin out), each hole one placeholder.
                    var text = interpolated.ToString();
                    foreach (var hole in interpolated.Contents.OfType<InterpolationSyntax>().Reverse())
                    {
                        text = text.Remove(hole.Span.Start - interpolated.SpanStart, hole.Span.Length).Insert(hole.Span.Start - interpolated.SpanStart, "{}");
                    }

                    values.Add(Unindented(text));
                    break;
            }
        }

        values.Sort(StringComparer.Ordinal);
        return values;
    }

    // The literal's lines without the indentation of the line with the closing quotes (what the compiler strips).
    private static string Unindented(string literal)
    {
        var lines = literal.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var last = lines[^1];
        var indent = last.Substring(0, last.Length - last.TrimStart(' ', '\t').Length);
        return string.Join("\n", lines.Select((line, i) => i > 0 && indent.Length > 0 && line.StartsWith(indent, StringComparison.Ordinal) ? line.Substring(indent.Length) : line));
    }
}
