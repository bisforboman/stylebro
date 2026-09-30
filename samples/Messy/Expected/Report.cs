namespace Messy;

public class Report<T>
    where T : class
{
    public string Render(
        T item, int width)
    {
        var text = item.ToString() ?? string.Empty;
        return text.PadRight(width);
    }
}