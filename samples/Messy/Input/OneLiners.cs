namespace Messy;

public class Gauge { private int level; public int Level { get { return level; } } public void Reset() { level = 0; } }

public class Thermostat
{
    private int target;

    public Thermostat() { }

    public void Adjust(int reading)
    {
        if (reading < target) { Heat(); } else if (reading > target) { Cool(); } else { }
        try { Heat(); } catch (System.InvalidOperationException) { Cool(); }
        System.Action log = () => { Cool(); };
        log();
    }

    public void Hold(bool idle) { if (idle) return; Heat(); }

    private void Heat() { target++; }

    private void Cool() { target--; }
}
