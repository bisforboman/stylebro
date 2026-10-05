namespace Messy;

public class Fares
{
    public decimal Price(int age, bool student)
    {
        if (age < 6)
        {
            return 0m;
        }

        if (student)
        {
            return 5m;
        }

        var price = age >= 65 ? 6m : 10m;
        return price;
    }

    public string Describe(decimal price)
    {
        if (price == 0m)
        {
            return "free";
        }

        return "paid";
    }
}
