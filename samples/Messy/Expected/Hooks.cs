namespace Messy;

public class Hooks
{
    public Hooks() { }

    public bool Stopped { get; private set; }

    public void OnStart() { }

    public void OnStop()
    {
        Stopped = true;
    }

    public void Run()
    {
        try
        {
            OnStart();
        }
        catch { }
    }
}

public class NoHooks { }
