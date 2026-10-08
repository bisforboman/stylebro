namespace Messy;

public class Receipts
{
    public string Format(
        string customer,
        decimal total
    ) =>
        customer + ": " + total;

    public string Print(string customer, decimal total)
    {
        var line = Format(
            customer,
            total
        );
        return string.Join(
            " ",
            line,
            Format(
                customer,
                total
            )
        ).Trim();
    }

    public string Short(decimal total) => Format("anonymous", total);
}
