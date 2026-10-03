namespace Messy;

/// <summary>Keeps notes.</summary>
public class Notes
{
    private int _count;

    /// <summary>Adds a note.</summary>
    /// <param name="text">The note.</param>
    /// <returns>How many there are.</returns>
    public int Add(string text)
    {
        if (text.Length == 0)
        {
            // nothing to add
            return _count;
        }
        else
        {
            // a real note
            _count++;
        }

        try
        {
            // the count may overflow
            _count = checked(_count + 0);
        }
        catch (System.OverflowException)
        {
            // start over
            _count = 0;
        }

        return _count;
    }
}
