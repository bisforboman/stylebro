namespace Messy;

// Trial 27 (PalCalc): removing the parentheses joins two call chains (BRO1405 + BRO1523), and nested pairs go in one run.
public static class Merges
{
    public static IEnumerable<int> Merge(List<int> a, List<int> b)
    {
        return ( from e in a select e )
            .Concat(
            from e in b select e )
            .Distinct();
    }

    public static double Area(double x, double y)
    {
        return Math.Max(
            x * y,
            1.0);
    }
}
