namespace Messy;

internal sealed class Postage
{
    private readonly decimal _rate;

    public Postage(decimal rate)
    {
        this._rate = rate;
    }

    internal decimal Price(decimal weight) => weight * this._rate;

    internal decimal Express(decimal weight) => this.Price(weight) * 2;
}
