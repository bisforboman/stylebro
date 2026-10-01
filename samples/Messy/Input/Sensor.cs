namespace Messy;

using System;

[Serializable()]
public class Sensor
{
    private int min, max;
    private Nullable<double> last;

    [Obsolete()]
    public event EventHandler Started, Stopped;

    public Nullable<double> Last
    {
        set { last = value; }
        get { return last; }
    }

    public event EventHandler Changed
    {
        remove { Started -= value; }
        add { Started += value; }
    }

    public int Range => max - min;
}