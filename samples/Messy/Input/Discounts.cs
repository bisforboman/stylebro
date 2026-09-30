namespace Messy;

public class Discounts
{
    public decimal Apply(decimal price, int quantity)
    {
        // Bulk orders get 10% off.

        if (quantity >= 10)
        {
            return price * 0.9m;
        }

        return price;
    }
}


