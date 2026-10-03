namespace Messy;

public class Hygrometer
{
    private int _nReadings;

    public double Average(double dTotal, bool isEmpty)
    {
        var fScale = isEmpty ? 0 : 1.0 / _nReadings;
        return dTotal * fScale;
    }
}
