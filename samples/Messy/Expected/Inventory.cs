namespace Messy;

public class Inventory
{
    private readonly Dictionary<string, int> _stock = new();

    private int _version;

    public int Version => _version;

    public int Count(string sku) => _stock.TryGetValue(sku, out var n) ? n : 0;

    public void Add(string sku)
    {
        _stock[sku] = Count(sku) + 1;
        _version++;
    }
}