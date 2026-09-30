namespace Probe2
{
    using System.Collections.Generic;

    public record R(int A, int B);

    public class Commas
    {
        public object M(R r)
        {
            var pairs = new Dictionary<int, int>
            {
                {
                    1,
                    2
                },
            };
            var with = r with
            {
                A = 1,
                B = 2
            };
            var tight = new[] { 1,
                2 };
            var nested = new[]
            {
                new[]
                {
                    1,
                    2
                }
            };
            var adjacent = new R(0, 0) with
            {
                A = 1,
                B = 2};
            var conditional = new[]
            {
                1,
#if SOMETHING
                2
#endif
            };
            return (pairs, with, tight, nested, adjacent, conditional);
        }
    }
}