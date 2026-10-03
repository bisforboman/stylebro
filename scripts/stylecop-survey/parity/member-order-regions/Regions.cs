namespace Parity;

public class Sorted
{
    #region Fields
    private int count;

    public const int Max = 1;
    #endregion

    #region Methods
    private void Reset() => count = 0;

    public void Run() => Reset();
    #endregion
}

public class AcrossRegions
{
    #region Methods
    public void Run()
    {
    }
    #endregion

    #region Fields
    private int count;
    #endregion

    public int Count => count;
}

public class InOrder
{
    #region Fields
    public const int Max = 1;

    private int count;
    #endregion

    public int Count => count + Max;
}
