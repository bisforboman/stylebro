using System.Collections.Generic;
using System.Linq;

namespace Messy;

public class Queries
{
    public IEnumerable<int> Large(int[] amounts)
    {
        var large = from a in amounts
            where a > 100
            orderby a
            select a;
        return large;
    }

    public IEnumerable<int> InRange(int[] amounts) =>
        from a in amounts
        where a > 10 &&
            a < 100
        select a;
}
