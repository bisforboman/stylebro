using System;
using System.Linq;

namespace Parity;

public class Hungarian : Base, IShape
{
    private const int xMax = 1;
    private static readonly int yMin = 0;
    private int nCount;
    private int _iTotal;
    protected int bFlag;
    public int pValue;
    private int isReady;
    private int dbHandle;

    public int Area(int wWidth) => wWidth;

    public override int Size(int nSize) => nSize;

    public int Run(int[] aItems, object oValue, bool isOn, int strName)
    {
        var iSum = 0;
        foreach (var nItem in aItems)
        {
            iSum += nItem;
        }

        try
        {
            iSum++;
        }
        catch (Exception eError)
        {
            iSum += eError.HResult;
        }

        if (oValue is int vNumber)
        {
            iSum += vNumber;
        }

        int.TryParse("1", out var rResult);
        var qQuery = from xItem in aItems let yItem = xItem select yItem;
        Func<int, int> f = zArg => zArg;
        int LocalFn(int kKey) => kKey;
        return iSum + rResult + qQuery.Count() + f(1) + LocalFn(2) + nCount + _iTotal + bFlag + pValue + isReady + dbHandle
            + xMax + yMin + (isOn ? 1 : 0) + strName;
    }
}

public abstract class Base
{
    public abstract int Size(int nSize);
}

public interface IShape
{
    int Area(int wWidth);
}

internal static class SafeNativeMethods
{
    public static int Call(int lpBuffer)
    {
        var dwSize = lpBuffer;
        return dwSize;
    }
}
