using System.Collections.Generic;

namespace Messy;

/// <summary>Looks names up in a <see cref="Dictionary{TKey, TValue}"/>.</summary>
public class Reference
{
    private readonly Dictionary<string, int> _ages = new();

    /// <summary>Finds an age.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The age, or <see langword="null"/> when the name is unknown.</returns>
    /// <exception cref="System.ArgumentNullException">When the name is <see langword="null"/>.</exception>
    /// <remarks>
    /// Uses <see cref="List{T}"/> nowhere.
    /// </remarks>
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
