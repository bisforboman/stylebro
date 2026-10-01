namespace Messy;

/// <summary>A labelled value.</summary>
/// <typeparam name="TKey">The key.</typeparam>
/// <typeparam name="TValue">The value.</typeparam>
public class Label<TKey, TValue>
{
    /// <summary>Formats the label.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>The text.</returns>
    public string Format(TKey key, TValue value) => $"{key}: {value}";

    /// <summary>Converts the value.</summary>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="convert">The conversion.</param>
    /// <returns>The converted value.</returns>
    public TResult Convert<TResult>(System.Func<TValue, TResult> convert) => convert(default!);
}