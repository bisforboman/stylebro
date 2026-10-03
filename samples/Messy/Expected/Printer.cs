namespace Messy;

/// <summary>Paper sizes.</summary>
public enum Paper
{
    A4,
    A5,
    Letter,
}

/// <summary>Prints labels.</summary>
public class Printer
{
    private int _copies;

    /// <summary>Prints the label.</summary>
    /// <param name="text">The text.</param>
    /// <param name="count">How many.</param>
    public void Print(
        string text,
        int count)
    {
        // Print each copy.
        do
        {
            _copies++;
        }
        while (_copies < count);
        Reset();
    }

    /// <summary>Resets the counter.</summary>
    public void Reset()
    {
        _copies = 0;
    }
}