namespace Messy;

public class Shipment
{
    public int weight { get; set; }

    public bool isHeavy() => weight > 20;
}