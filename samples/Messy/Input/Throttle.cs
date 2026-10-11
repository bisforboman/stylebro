namespace Messy;

public class Throttle
{
    public const int Max_Requests = 100;
    private int m_count;

    public bool TryEnter() => m_count++ < Max_Requests;
}