namespace Messy;

/// <summary>Heats water.</summary>
public class Kettle
{
    #region State

    private int _temperature;

    /// <summary>The boiling point.</summary>
    public const int Boiling = 100;

    #endregion

    #region Behavior
    private void Cool() => _temperature--;

    /// <summary>Heats it up.</summary>
    public void Heat()
    {
        _temperature++;
        Cool();
    }
    #endregion
}
