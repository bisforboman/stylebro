namespace Probe
{
    using System;

    public class Cases
    {
        public Cases(
            int a,
            int b)
        {
        }

        public void Method(int a, int b,
            int c)
        {
        }

        public void Good(
            int a,
            int b)
        {
        }

        public static int Max4(int a, int b, int c, int d) => a;

        public void AllOnOne(int a, int b)
        {
        }

        public int this[
            int a,
            int b] => a;

        [Obsolete(
            "message",
            true)]
        public void Attributed()
        {
        }

        public void Calls()
        {
            Method(
                1,
                2, 3);
            Method(
                1, 2,
                3);
            Method(1, 2, Math.Max(
                1,
                2));
            Func<int, int, int> f = (
                x,
                y) => x + y;
            Max4(1, Max4(
                1, 2, 3, 4), 3, 4);
            Max4(
                Max4(1, 2,
                    3, 4), 2,
                3,
                4);
            Max4(
                1,
                Max4(1, 2, 3,
                    4), 3,
                4);
            Max4(
                1,
                2, Max4(1, 2, 3, 4),
                4);
            Math.Max(1, 2
                );
            var s = string.Join(", ", new[]
            {
                "a",
            });
        }
    }
}