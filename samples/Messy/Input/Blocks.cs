namespace Messy;

public class Blocks

{
    public int Parse(string text)

    {
        try
        {
            return int.Parse(text);
        }

        catch (FormatException)
        {
            return 0;
        }
    }

    public string Describe(int value)
    {
        if (value < 0)
        {
            return "negative";
        }

        // zero counts as positive here
        else
        {
            return "non-negative";
        }
    }
}