namespace Messy;

public class Steps
{

    private readonly List<string> _log = new();
    // Runs the steps in order.
    public void Run(IEnumerable<string> steps)
    {

        foreach (var step in steps)
        {
            _log.Add(step);
        }
        // Keep the log short.
        if (_log.Count > 100)
        {
            _log.RemoveAt(0);
        }
    }
}