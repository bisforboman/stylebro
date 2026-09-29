namespace Messy;

/// <summary>A deliberately messy class.</summary>
public class OrderService
{
    public const string Name = "orders";

    private static int s_instances;

    private readonly List<string> _items = new();

    public OrderService()
    {
    }

    public enum Status
    {
        Pending,
        Shipped,
    }

    public int Count => _items.Count;

    /// <summary>Places an order.</summary>
    public void Place(string item)
    {
        _items.Add(item);
    }

    private void Log(string message)
    {
    }
}
