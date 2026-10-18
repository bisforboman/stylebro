using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers;

/// <summary>
/// One level of indentation, read from the standard .editorconfig keys 'indent_style', 'indent_size' and
/// 'tab_width'. Defaults to four spaces, like the SDK's formatter.
/// </summary>
internal static class Indentation
{
    /// <summary>The standard 'max_line_length' key; <see cref="int.MaxValue"/> (no limit) when unset or not a positive number.</summary>
    public static int GetMaxLineLength(AnalyzerConfigOptions options)
    {
        return options.TryGetValue("max_line_length", out var value) && int.TryParse(value.Trim(), out var length) && length > 0 ? length : int.MaxValue;
    }

    /// <summary>
    /// What a fix writes between a call's name (or 'default', 'typeof') and its '(': a space with the SDK's
    /// csharp_space_between_method_call_name_and_opening_parenthesis = true (MimeKit, trial 25), else nothing.
    /// </summary>
    public static string GetCallParenthesis(AnalyzerConfigOptions options) =>
        options.TryGetValue("csharp_space_between_method_call_name_and_opening_parenthesis", out var value)
            && value.Trim().Equals("true", System.StringComparison.OrdinalIgnoreCase) ? " (" : "(";

    /// <summary>
    /// How many columns a tab advances to, as the SDK's formatter counts them: 'tab_width', else a numeric 'indent_size',
    /// else 4. Pinta sets 8, and its formatter-aligned comments line up only with tabs counted that wide.
    /// </summary>
    public static int GetTabWidth(AnalyzerConfigOptions options)
    {
        foreach (var key in new[] { "tab_width", "indent_size" })
        {
            if (options.TryGetValue(key, out var value) && int.TryParse(value.Trim(), out var width) && width is > 0 and <= 16)
            {
                return width;
            }
        }

        return 4;
    }

    /// <summary>The column a position is shown at, counting each tab to the next multiple of <paramref name="tabWidth"/>.</summary>
    public static int GetVisualColumn(Microsoft.CodeAnalysis.Text.SourceText text, int position, int tabWidth)
    {
        var column = 0;
        for (var i = text.Lines.GetLineFromPosition(position).Start; i < position; i++)
        {
            column = text[i] == '\t' ? ((column / tabWidth) + 1) * tabWidth : column + 1;
        }

        return column;
    }

    /// <summary>One level of indentation.</summary>
    public static string GetUnit(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue("indent_style", out var style) && style.Trim().ToLowerInvariant() == "tab")
        {
            return "\t";
        }

        var size = 4;
        if (options.TryGetValue("indent_size", out var sizeText))
        {
            sizeText = sizeText.Trim();
            if (sizeText == "tab" && options.TryGetValue("tab_width", out var tabWidth))
            {
                sizeText = tabWidth.Trim();
            }

            if (int.TryParse(sizeText, out var parsed) && parsed is > 0 and <= 16)
            {
                size = parsed;
            }
        }

        return new string(' ', size);
    }
}
