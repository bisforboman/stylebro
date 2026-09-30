namespace Messy;

public class Throttle
{
    public const int MAX_REQUESTS = 100;
    private int m_count;

    public bool TryEnter() => m_count++ < MAX_REQUESTS;
}