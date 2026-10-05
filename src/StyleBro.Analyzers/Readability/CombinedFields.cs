using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1114 (StyleCop SA1132: one field per declaration; fields and event fields, like StyleCop) and
/// BRO1142 (one local per declaration; not a StyleCop rule).
/// </summary>
internal static class CombinedFields
{
    /// <summary>
    /// The edit that gives every variable its own declaration, or null when the declaration has one variable or can't be
    /// split safely (a comment or directive inside it, or other code on its first line). Every new declaration gets the
    /// attributes, modifiers and type, and the documentation comment if there is one: StyleCop's fix keeps attributes on
    /// the first field only, which takes them away from the others. Event fields, and a field that spans several lines from
    /// the next, are separated by a blank line, which BRO1505 wants between them.
    /// </summary>
    public static TextChange? GetChange(BaseFieldDeclarationSyntax declaration, SourceText text) =>
        GetChange(declaration, declaration.Declaration, declaration.SemicolonToken, text);

    /// <summary>
    /// The same for a local declaration. Not 'using' declarations (each variable's lifetime would stay the same, but
    /// the dispose order is part of the code's meaning); 'for' initializers and 'fixed' aren't local declaration
    /// statements. Locals get no blank lines between them, and a local that shares its line with other code (in a
    /// single-line block, which BRO1508/BRO1509 expand) is split on that line: <c>{ int a, b; }</c> becomes
    /// <c>{ int a; int b; }</c>, so the expansion and the split converge in either order.
    /// </summary>
    public static TextChange? GetChange(LocalDeclarationStatementSyntax declaration, SourceText text) =>
        declaration.UsingKeyword.IsKind(SyntaxKind.None) ? GetChange(declaration, declaration.Declaration, declaration.SemicolonToken, text) : null;

    private static TextChange? GetChange(SyntaxNode declaration, VariableDeclarationSyntax variableDeclaration, SyntaxToken semicolon, SourceText text)
    {
        var variables = variableDeclaration.Variables;
        if (variables.Count < 2 || declaration.ContainsDirectives || declaration.ContainsDiagnostics
            || declaration.DescendantTrivia(declaration.Span).Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
        {
            return null;
        }

        var line = text.Lines.GetLineFromPosition(declaration.SpanStart);
        var indent = text.ToString(TextSpan.FromBounds(line.Start, declaration.SpanStart));
        var sameLine = indent.Trim().Length > 0;
        if (sameLine && declaration is not LocalDeclarationStatementSyntax)
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

        // BRO1505 wants a blank line between event fields, and below a field that spans several lines.
        var parts = variables.Select((v, i) => (i == 0 ? string.Empty
                : sameLine ? " "
                : (declaration is EventFieldDeclarationSyntax || (declaration is FieldDeclarationSyntax && text.ToString(variables[i - 1].Span).IndexOf('\n') >= 0) ? lineBreak + lineBreak : lineBreak)
                    + docText + indent)
            + prefix + text.ToString(v.Span) + semicolon.Text);
        return new TextChange(declaration.Span, string.Concat(parts));
    }
}
