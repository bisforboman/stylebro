namespace Messy;

public enum Level { None, Low, High }

public class Checks
{
    private const int Limit = 10;

    public bool IsValid(string? name, int count, Level level)
    {
        if (null == name)
        {
            return false;
        }

        return 0 < count && Limit >= count && Level.None != level;
    }

    public Guid NewId(Guid seed = new Guid()) => seed == new Guid() ? Guid.NewGuid() : seed;

    public CancellationToken Token() => new CancellationToken();
}