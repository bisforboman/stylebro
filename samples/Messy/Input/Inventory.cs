namespace Messy;

public class Inventory
{
    public int Count(string sku) => _stock.TryGetValue(sku, out var n) ? n : 0;

    private readonly Dictionary<string, int> _stock = new();
    private int _version;
    public void Add(string sku)
    {
        _stock[sku] = Count(sku) + 1;
        _version++;
    }
    public int Version => _version;
}