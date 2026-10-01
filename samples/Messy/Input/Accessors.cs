namespace Messy;

public class Thermometer
{
    private double celsius;
    private int readings;

    public double Celsius
    {
        get { return celsius; }
        set
        {
            celsius = value;
        }
    }

    public int Readings
    {
        get { return readings; }
        set
        {
            readings = value;
            celsius = 0;
        }
    }
}