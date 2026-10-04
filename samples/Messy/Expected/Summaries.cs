namespace Messy;

/// <summary>Summaries written in two layouts.</summary>
public class Summaries
{
    /// <summary>Gets the title.</summary>
    public string Title { get; } = "x";

    /// <summary>
    /// Counts the words
    /// in the title.
    /// </summary>
    /// <returns>How many there are.</returns>
    public int Count() => Title.Split(' ').Length;

    /// <summary>
    /// <para>Clears the title.</para>
    /// </summary>
    public void Clear()
    {
    }
}
