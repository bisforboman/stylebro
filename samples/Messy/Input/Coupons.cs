namespace Messy;

public class Coupons
{
    public decimal Apply(decimal price,
                         decimal discount, decimal minimum)
    {
        return Clamp(price - discount, minimum,
            price);
    }

    public decimal Stack(decimal price, decimal first, decimal second,
        decimal third)
    {
        return Apply(Apply(price, first,
            0m), second, third);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        value < min ? min : value > max ? max : value;
}
