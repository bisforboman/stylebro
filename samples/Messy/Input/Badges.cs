namespace Messy;

public interface IBadge
{
}

public class GoldBadge : IBadge { }

public record class BadgeHolder(string Name, IBadge Badge);

public class Wallet
{
    public string? Owner
    {
        get;
        set;
    }
    public int Count
    {
        get;
        private set;
    } = 1;

    public decimal Balance { get; init; }
}
