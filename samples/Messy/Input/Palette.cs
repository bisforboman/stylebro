namespace Messy;

public enum Shade { Light, Dark, }

public enum Hue
{
    Red,
    Green,
    Blue, // last
}

public static class Palette
{
    public static readonly string[] Names = new[] { "red", "green", };

    public static readonly int[] Codes = new[]
    {
        1,
        2,
    };
}
