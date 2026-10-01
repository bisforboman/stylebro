using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1114 (StyleCop SA1132: one field per declaration). Fields and event fields count, locals don't
/// (like StyleCop).
/// </summary>
internal static class CombinedFields
{
    /// <summary>
    /// The edit that gives every variable its own declaration, or null when the declaration has one variable or can't be
    /// split safely (a comment or directive inside it, or other code on its first line). Every new declaration gets the
    /// attributes, modifiers and type, and the documentation comment if there is one: StyleCop's fix keeps attributes on
    /// the first field only, which takes them away from the others. Event fields are separated by a blank line, which
    /// BRO1505 wants between them.
    /// </summary>
    public static TextChange? GetChange(BaseFieldDeclarationSyntax declaration, SourceText text)
    {
        var variables = declaration.Declaration.Variables;
        if (variables.Count < 2 || declaration.ContainsDirectives || declaration.ContainsDiagnostics
            || declaration.DescendantTrivia(declaration.Span).Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(declaration.SpanStart);
        var indent = text.ToString(TextSpan.FromBounds(line.Start, declaration.SpanStart));
        if (indent.Trim().Length > 0)
        {
            return null;
        }

        var lineBreak = text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        if (lineBreak.Length == 0)
        {
            lineBreak = "\n";
        }

        // The documentation comment's lines, each with its indentation, up to the declaration's own line.
        var documentation = declaration.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        var docText = documentation.RawKind == 0
            ? string.Empty
            : text.ToString(TextSpan.FromBounds(text.Lines.GetLineFromPosition(documentation.SpanStart).Start, line.Start));

        // Attributes, modifiers and type. A line break between the type and the first name (Serilog writes
        // 'const string' and then each constant on its own line) becomes a space: every new declaration is one line.
        var prefix = text.ToString(TextSpan.FromBounds(declaration.SpanStart, variables[0].SpanStart)).TrimEnd() + " ";
        var separator = declaration is EventFieldDeclarationSyntax ? lineBreak + lineBreak : lineBreak;
        var parts = variables.Select((v, i) => (i == 0 ? string.Empty : separator + docText + indent) + prefix + text.ToString(v.Span) + declaration.SemicolonToken.Text);
        return new TextChange(declaration.Span, string.Concat(parts));
    }
}
