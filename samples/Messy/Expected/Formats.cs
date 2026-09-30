namespace Messy;

public enum Format
{
    Json,
    Xml,
}

public class Formats
{
    private const string None = "";

    public string Extension(Format format) => format switch
    {
        Format.Json => ".json",
        Format.Xml => ".xml",
        _ => string.Empty,
    };

    public string[] Names(string prefix = "")
    {
        var names = new[]
        {
            prefix + "json",
            prefix + "xml",
        };
        return prefix == string.Empty ? names : new[] { None };
    }
}