namespace Messy;

///<summary>Prints labels.</summary>
public class Printer
{
    private int copies;
    /// <summary>Prints the label.</summary>

    /// <param name="text">The text.</param>
    /// <param name="count">How many.</param>
    public void Print(

        string text

        , int count)
    {
        //
        // Print each copy.
        //
        do
        {
            copies++;
        }

        while (copies < count);
        Reset(
        );
    }

    /// <summary>Resets the counter.</summary>
    public void Reset()
    {
        copies = 0;
    }
}

/// <summary>Paper sizes.</summary>
public enum Paper
{
    A4, A5,
    Letter,
}