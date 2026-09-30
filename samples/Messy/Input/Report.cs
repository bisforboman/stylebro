namespace Messy;

public class Report<T> where T : class
{
    #region Rendering

    public string Render
        (T item, int width
        )
    {
        #region Body
        var text = item.ToString() ?? string.Empty;
        #endregion
        return text.PadRight(width);
    }

    #endregion
}