using System.Collections.Generic;

namespace Parity;

public interface IShape
{
    (int width, int height) Size();
}

public abstract class Base
{
    public abstract (int count, string name) Get();
}

public class Tuples : Base, IShape
{
    private (int total, int Done) progress;

    public (int width, int height) Size() => (1, 2);

    public override (int count, string name) Get() => (count: 1, name: "x");

    public int Use((int first, int Second) pair, List<(string key, int Value)> items)
    {
        (int a, int _) local = (a: 1, 2);
        var inferred = (pair.first, items.Count);
        var literal = (lower: 1, Upper: 2);
        return progress.total + local.a + inferred.first + literal.lower + items.Count;
    }
}
