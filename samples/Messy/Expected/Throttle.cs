namespace Messy;

public class Throttle
{
    public const int MaxRequests = 100;
    private int _count;

    public bool TryEnter() => _count++ < MaxRequests;
}