namespace Messy;

// Left alone: moving 'Next' above 's_seed' would make it read 0 instead of 42.
public static class IdSource
{
    private static int _seed = 42;
    public static readonly int Next = _seed + 1;
}

// Sorted: the initializers don't depend on each other.
public static class Defaults
{
    public static readonly string Name = nameof(Defaults);
    private static int _retries = 3;
}
