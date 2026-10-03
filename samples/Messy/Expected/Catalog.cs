namespace Messy.Catalog
{
    public static class Shelf
    {
        public static Catalog.Item First() => new Messy.Catalog.Item();
    }

    public class Item
    {
        public string Name { get; set; } = string.Empty;
    }
}
