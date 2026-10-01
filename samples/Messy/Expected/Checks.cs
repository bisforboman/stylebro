namespace Messy;

public enum Level
{
    None,
    Low,
    High,
}

public class Checks
{
    private const int Limit = 10;

    public bool IsValid(string? name, int count, Level level)
    {
        if (name == null)
        {
            return false;
        }

        return count > 0 && count <= Limit && level != Level.None;
    }

    public Guid NewId(Guid seed = default(Guid)) => seed == Guid.Empty ? Guid.NewGuid() : seed;

    public CancellationToken Token() => CancellationToken.None;
}