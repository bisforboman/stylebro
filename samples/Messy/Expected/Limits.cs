namespace Messy;

public static class Limits
{
    public const int MaxItems = 100;
    private static readonly string[] DefaultTags = { "new" };

    public static bool Fits(int count) => count <= MaxItems && DefaultTags.Length > 0;
}