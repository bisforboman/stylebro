namespace Messy.catalog
{
    public static class Shelf
    {
        public static catalog.Item First() => new Messy.catalog.Item();
    }

    public class Item
    {
        public string Name { get; set; } = string.Empty;
    }
}
