namespace Messy;

public class Settings
{
    public void Apply()
    {
        Load(); ;
        ;
    }

    [Obsolete("Use Apply"), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Reset() { }

    private int _version;

    private void Load() => _version++;
};