namespace Messy;

/// <summary>A tariff</summary>
public class Tariff : IComparable<Tariff>
{
    /// <summary>The rate.</summary>
    public decimal Rate { get; set; }

    /// <summary>Gets or sets the currency.</summary>
    public string Currency { get; private set; } = "EUR";

    public int CompareTo(Tariff? other)
    {
        /// Compare by rate.
        return Rate.CompareTo(other?.Rate ?? 0);
    }
}