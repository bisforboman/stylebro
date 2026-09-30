namespace Messy;

public static class Limits
{
    public const int maxItems = 100;
    private static readonly string[] defaultTags = { "new" };

    public static bool Fits(int count) => count <= maxItems && defaultTags.Length > 0;
}