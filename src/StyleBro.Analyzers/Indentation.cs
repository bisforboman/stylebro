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
