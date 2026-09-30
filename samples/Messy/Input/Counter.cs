namespace Messy;

public class Counter
{
    private int count;
    private readonly int Step = 1;

    public Counter(int count)
    {
        this.count = count;
    }

    public int Next() => count += Step;
}