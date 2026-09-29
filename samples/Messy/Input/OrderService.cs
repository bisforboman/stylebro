namespace Messy;

/// <summary>A deliberately messy class.</summary>
public class OrderService
{
    /// <summary>Places an order.</summary>
    public void Place(string item)
    {
        _items.Add(item);
    }

    private readonly List<string> _items = new();

    public OrderService()
    {
    }

    public int Count => _items.Count;

    private static int s_instances;

    public const string Name = "orders";

    private void Log(string message)
    {
    }

    public enum Status
    {
        Pending,
        Shipped,
    }
}
