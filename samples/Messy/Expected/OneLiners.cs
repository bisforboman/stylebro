namespace Messy;

public class Gauge
{
    private int _level;

    public int Level
    {
        get { return _level; }
    }

    public void Reset()
    {
        _level = 0;
    }
}

public class Thermostat
{
    private int _target;

    public Thermostat()
    {
    }

    public void Adjust(int reading)
    {
        if (reading < _target)
        {
            Heat();
        }
        else if (reading > _target)
        {
            Cool();
        }
        else
        {
        }

        try
        {
            Heat();
        }
        catch (System.InvalidOperationException)
        {
            Cool();
        }

        System.Action log = () => { Cool(); };
        log();
    }

    public void Hold(bool idle)
    {
        if (idle)
        {
            return;
        }

        Heat();
    }

    private void Heat()
    {
        _target++;
    }

    private void Cool()
    {
        _target--;
    }
}
