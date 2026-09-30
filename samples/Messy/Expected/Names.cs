namespace Messy;

public class Names
{
    public int Sum(int[] values, int offset)
    {
        var total = offset;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }

    public int Twice(int count) => Sum(new[] { count, count }, offset: 0);
}