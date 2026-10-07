namespace Messy;

public class Tally
{
    private readonly long _total = 10l;
    private readonly ulong _mask = 0xFFul;
    private readonly uint _count = 2u;
    private readonly string _label = string.Empty;

    public System.TimeSpan Elapsed { get; set; } = new System.TimeSpan();

    public string Describe() => _label == string.Empty ? "" : _label + _total + _mask + _count + new int();
}
