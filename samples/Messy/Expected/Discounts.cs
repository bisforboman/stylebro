namespace Messy;

public class Discounts
{
    public decimal Apply(decimal price, int quantity)
    {
        // Bulk orders get 10% off: right after '{', standalone (trial 27), so its blank line stays.

        if (quantity >= 10)
        {
            return price * 0.9m;
        }

        return price;
    }

    public string Label(int tier)
    {
        switch (tier)
        {
            case 1:
                // The first tier.
                return "bronze";

            default:
                return "none";
        }
    }
}
