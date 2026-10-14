using System.Buffers;

namespace MultiTarget;

// BRO1151 would pass the elements: net10.0 has SearchValues.Create(params ReadOnlySpan<char>), net8.0 only the plain
// ReadOnlySpan<char> overload, where the elements don't bind (CS1501). A params collection is skipped while a target
// framework is older than .NET 9.
public static class Terminators
{
    public static readonly SearchValues<char> Values = SearchValues.Create(['<', '&', '\0']);
}
