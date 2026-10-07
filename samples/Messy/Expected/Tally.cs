namespace Messy;

public class Tally
{
    private readonly long _total = 10L;
    private readonly ulong _mask = 0xFFuL;
    private readonly uint _count = 2u;
    private readonly string _label = "";

    public System.TimeSpan Elapsed { get; set; } = default;

    public string Describe() => _label == "" ? "" : _label + _total + _mask + _count + default(int);
}
