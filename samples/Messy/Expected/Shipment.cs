namespace Messy;

public class Shipment
{
    public int Weight { get; set; }

    public bool IsHeavy() => Weight > 20;
}