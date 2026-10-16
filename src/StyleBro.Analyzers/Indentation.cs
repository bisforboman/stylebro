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
