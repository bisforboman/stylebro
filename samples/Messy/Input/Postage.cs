namespace Messy;

internal sealed class Postage
{
    private readonly decimal _rate;

    public Postage(decimal rate)
    {
        this._rate = rate;
    }

    public decimal Price(decimal weight) => weight * this._rate;

    public decimal Express(decimal weight) => this.Price(weight) * 2;
}
