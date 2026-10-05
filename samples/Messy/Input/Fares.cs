namespace Messy;

public class Fares
{
    public decimal Price(int age, bool student)
    {
        if (age < 6)
        {
            return 0m;
        }
        else
        {
            if (student)
            {
                return 5m;
            }
            else
            {
                var price = age >= 65 ? 6m : 10m;
                return price;
            }
        }
    }

    public string Describe(decimal price)
    {
        if (price == 0m)
            return "free";
        else
            return "paid";
    }

    public decimal Discount(decimal price, bool member)
    {
        if (!member)
            return 0m;
        else
        {
            var rate = price > 100m ? 0.1m : 0.05m;
            return price * rate;
        }
    }
}
