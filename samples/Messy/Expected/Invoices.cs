namespace Messy;

public class Invoices
{
    public Invoices(
        string prefix,
        int start)
    {
        Prefix = prefix;
        Next = start;
    }

    public string Prefix { get; }

    public int Next { get; private set; }

    public string Format(
        string customer,
        decimal amount,
        string currency)
    {
        return string.Join(
            " ",
            Prefix + Next++,
            customer,
            amount + " " + currency);
    }

    public string Sample() => Format(
        "ACME",
        100m,
        "EUR");
}