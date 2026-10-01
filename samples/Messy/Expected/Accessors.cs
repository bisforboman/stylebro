namespace Messy;

public class Thermometer
{
    private double _celsius;
    private int _readings;

    public double Celsius
    {
        get { return _celsius; }

        set { _celsius = value; }
    }

    public int Readings
    {
        get
        {
            return _readings;
        }

        set
        {
            _readings = value;
            _celsius = 0;
        }
    }
}