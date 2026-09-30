namespace Messy;

public class Names
{
    public int Sum(int[] Values, int Offset)
    {
        var _total = Offset;
        foreach (var Value in Values)
        {
            _total += Value;
        }

        return _total;
    }

    public int Twice(int Count) => Sum(new[] { Count, Count }, Offset: 0);
}