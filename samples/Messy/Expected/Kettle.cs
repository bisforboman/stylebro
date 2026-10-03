namespace Messy;

/// <summary>Heats water.</summary>
public class Kettle
{
    #region State

    /// <summary>The boiling point.</summary>
    public const int Boiling = 100;

    private int _temperature;

    #endregion

    #region Behavior

    /// <summary>Heats it up.</summary>
    public void Heat()
    {
        _temperature++;
        Cool();
    }

    private void Cool() => _temperature--;
    #endregion
}
