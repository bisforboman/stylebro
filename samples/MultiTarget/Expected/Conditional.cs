namespace MultiTarget;

public class Conditional
{
    public string Describe(int value)
    {
        return value switch
        {
            0 => string.Empty,
            _ => "some",
        };
    }

    private static readonly string[] Names = new[]
    {
        "a",
        "b",
    };

#if NET10_0_OR_GREATER
    private static readonly int[] Numbers = new[]
    {
        1,
        2,
    };

    public string Latest() => string.Empty;
#endif

    private static readonly string[] More = new[]
    {
        "c",
        "d",
    };
}

#if NET10_0_OR_GREATER
public class OnlyOnNet10
{
    private int count;

    public string Name { get; set; } = string.Empty;

    public void Run()
    {
    }
}
#endif