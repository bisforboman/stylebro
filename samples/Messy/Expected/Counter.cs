namespace Messy;

public class Counter
{
    private readonly int _step = 1;
    private int _count;

    public Counter(int count)
    {
        this._count = count;
    }

    public int Next() => _count += _step;
}