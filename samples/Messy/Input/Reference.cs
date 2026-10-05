using System.Collections.Generic;

namespace Messy;

/// <summary>Looks names up in a <see cref="Dictionary&lt;TKey, TValue&gt;"/>.</summary>
public class Reference
{
    private readonly Dictionary<string, int> _ages = new();

    /// <returns>The age, or <c>null</c> when the name is unknown.</returns>
    /// <param name="name">The name.</param>
    /// <summary>Finds an age.</summary>
    /// <remarks>
    /// Uses <see cref="List&lt;T&gt;"/> nowhere.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">When the name is <c>null</c>.</exception>
    public int? Find(string name)
    {
        _ages.TryGetValue(name, out var age);
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (age == 0)
        {
            return null;
        }
        // A plain comment still gets its blank line.
        return age;
    }
}
