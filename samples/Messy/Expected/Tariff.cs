namespace Messy;

/// <summary>A tariff.</summary>
public class Tariff : IComparable<Tariff>
{
    /// <summary>Gets or sets the rate.</summary>
    public decimal Rate { get; set; }

    /// <summary>Gets the currency.</summary>
    public string Currency { get; private set; } = "EUR";

    /// <inheritdoc/>
    public int CompareTo(Tariff? other)
    {
        // Compare by rate.
        return Rate.CompareTo(other?.Rate ?? 0);
    }
}