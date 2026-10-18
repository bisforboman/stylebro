using Microsoft.CodeAnalysis.Diagnostics;

namespace StyleBro.Analyzers.Layout;

/// <summary>
/// CSharpier lays the code out (<c>stylebro_layout_by = csharpier</c>, written by <c>bro init</c> when it detects
/// CSharpier): the fixes that rewrite code and the rules that judge layout CSharpier changes afterwards follow CSharpier's
/// line width (<c>stylebro_layout_print_width</c>, CSharpier's printWidth), so the two formatters settle in one round each.
/// </summary>
internal static class CSharpierLayout
{
    /// <summary>Who lays the code out: <c>csharpier</c>, or unset (StyleBro and the SDK's formatter).</summary>
    public const string Key = "stylebro_layout_by";

    /// <summary>CSharpier's printWidth (default 100, CSharpier's).</summary>
    public const string WidthKey = "stylebro_layout_print_width";

    /// <summary>CSharpier's default printWidth.</summary>
    public const int DefaultWidth = 100;

    /// <summary>CSharpier's line width when CSharpier lays the code out, else null.</summary>
    public static int? PrintWidth(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue(Key, out var by) || !string.Equals(by.Trim(), "csharpier", System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return options.TryGetValue(WidthKey, out var value) && int.TryParse(value.Trim(), out var width) && width > 0 ? width : DefaultWidth;
    }
}
