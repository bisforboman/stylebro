namespace Messy;

public class Hygrometer
{
    private int _readings;

    public double Average(double total, bool isEmpty)
    {
        var scale = isEmpty ? 0 : 1.0 / _readings;
        return total * scale;
    }
}
