using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace StyleBro.Analyzers.Readability;

/// <summary>
/// Shared logic for BRO1138: no '$' without interpolations, no '@' when nothing needs it, no single-line raw string
/// that a plain string can write without escapes.
/// </summary>
internal static class StringPrefixes
{
    /// <summary>
    /// The edit that writes the literal in the simplest form, and that form's name ("a plain string", "a verbatim string",
    /// "a raw string"), or null when it already is. The whole simplification is one edit (<c>$@"a"</c> becomes
    /// <c>"a"</c>), so one run is enough. Empty strings are left to BRO1106 (<c>string.Empty</c>). An interpolated string
    /// is only reported where it becomes a <see langword="string"/> (or <see langword="object"/>): as a <c>FormattableString</c>,
    /// <c>IFormattable</c> or an interpolated string handler, the plain string would mean something else.
    /// </summary>
    public static (TextChange Change, string Form)? GetChange(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        var replacement = expression switch
        {
            LiteralExpressionSyntax literal => ForLiteral(literal.Token),
            InterpolatedStringExpressionSyntax interpolated when interpolated.Contents.All(c => c is InterpolatedStringTextSyntax)
                && model.GetTypeInfo(interpolated, cancellationToken).ConvertedType?.SpecialType is SpecialType.System_String or SpecialType.System_Object =>
                ForInterpolated(interpolated),
            _ => null,
        };
        return replacement is null ? null : (new TextChange(expression.Span, replacement), Form(replacement));
    }

    private static string? ForLiteral(SyntaxToken token)
    {
        var text = token.Text;
        var suffix = token.IsKind(SyntaxKind.Utf8StringLiteralToken) || token.IsKind(SyntaxKind.Utf8SingleLineRawStringLiteralToken)
            ? text.Substring(text.Length - 2)
            : string.Empty;
        text = text.Substring(0, text.Length - suffix.Length);
        if (text.StartsWith("@\"", System.StringComparison.Ordinal))
        {
            return Plain(text.Substring(2, text.Length - 3), suffix);
        }

        if (token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) || token.IsKind(SyntaxKind.Utf8SingleLineRawStringLiteralToken))
        {
            var quotes = text.TakeWhile(c => c == '"').Count();
            return Plain(text.Substring(quotes, text.Length - (2 * quotes)), suffix);
        }

        return null;
    }

    private static string? ForInterpolated(InterpolatedStringExpressionSyntax interpolated)
    {
        var content = string.Concat(interpolated.Contents.Select(c => ((InterpolatedStringTextSyntax)c).TextToken.Text));
        if (content.Length == 0)
        {
            return null;
        }

        var start = interpolated.StringStartToken;
        if (start.IsKind(SyntaxKind.InterpolatedSingleLineRawStringStartToken) || start.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
        {
            // Raw: braces in the text are literal already (fewer than the '$' count), so only the '$'s go.
            var raw = interpolated.ToString().TrimStart('$');
            return start.IsKind(SyntaxKind.InterpolatedSingleLineRawStringStartToken) ? Plain(content, string.Empty) ?? raw : raw;
        }

        content = content.Replace("{{", "{").Replace("}}", "}");
        return start.Text.IndexOf('@') >= 0 ? Plain(content, string.Empty) ?? "@\"" + content + "\"" : "\"" + content + "\"";
    }

    /// <summary>The text as a plain string, or null when it would need escapes (a backslash, quote or line break).</summary>
    private static string? Plain(string content, string suffix) =>
        content.Length == 0 || content.Any(c => c is '\\' or '"' or '\r' or '\n' or '\u0085' or '\u2028' or '\u2029')
            ? null
            : "\"" + content + "\"" + suffix;

    private static string Form(string replacement) =>
        replacement.StartsWith("\"\"\"", System.StringComparison.Ordinal) ? "a raw string"
        : replacement.StartsWith("@", System.StringComparison.Ordinal) ? "a verbatim string"
        : "a plain string";
}
