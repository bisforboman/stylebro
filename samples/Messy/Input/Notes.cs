namespace Messy;

/// <summary>Keeps notes.</summary>
public class Notes
{
    private int count;

    /// <summary>Adds a note.</summary>
    /// <param name="text">The note.</param>
    /// <returns>How many there are.</returns>
    public int Add(string text)
    {
        if (text.Length == 0) // nothing to add
        {
            return count;
        }
        else // a real note
        {
            count++;
        }

        try // the count may overflow
        {
            count = checked(count + 0);
        }
        catch (System.OverflowException)
        // start over
        {
            count = 0;
        }

        return count;
    }
}
