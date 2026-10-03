namespace Probe
{
    using System;

    public class Parens
    {
        private int field = (1 + 2);

        public int M(int a, int b, string s, int? n, bool f)
        {
            var x = (a);
            x = (a * b);
            Use((a + b), ( b ));
            var len = (a.ToString()).Length;
            var w = -(a);
            Func<int, int> h = (v => v + 1);
            var y = a + (b * a);
            var z = n ?? (n ?? 1);
            var t = (a + b).ToString();
            var c = (object)(-a);
            var d = $"{(f ? 1 : 2)}";
            var e = (s?.Length).ToString();
            var g = (a switch { 1 => "one", _ => "other" }).Length;
            var k = ((a));
            Use(a < b, (b > (a + 1)));
            x = checked((a + b));
            return (x + y + w + h(1) + len + z + t.Length + c.GetHashCode() + d.Length + e.Length + g + k);
        }

        public int N(int a) => (a);

        private static void Use(int p, int q)
        {
        }

        private static void Use(bool p, bool q)
        {
        }
    }
}
