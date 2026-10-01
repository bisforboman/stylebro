namespace Messy;

using System;

[Serializable]
public class Sensor
{
    private int min;
    private int max;
    private double? last;

    [Obsolete]
    public event EventHandler Started;

    [Obsolete]
    public event EventHandler Stopped;

    public event EventHandler Changed
    {
        add { Started += value; }
        remove { Started -= value; }
    }

    public double? Last
    {
        get { return last; }
        set { last = value; }
    }

    public int Range => max - min;
}