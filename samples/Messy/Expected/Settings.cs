namespace Messy;

public class Settings
{
    private int _version;

    public void Apply()
    {
        Load();
    }

    [Obsolete("Use Apply")]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void Reset() { }

    private void Load() => _version++;
}