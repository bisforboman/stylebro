namespace Messy;

/// <summary>BRO1142 following the 'var' preference, and BRO1603 leaving text that ends in code alone.</summary>
public class Cells
{
    /// <summary>Example: A1:B10</summary>
    /// <returns>The count.</returns>
    public int Count()
    {
        var rowRelative = true;
        var columnRelative = false;
        return rowRelative && columnRelative ? 2 : 1;
    }
}
