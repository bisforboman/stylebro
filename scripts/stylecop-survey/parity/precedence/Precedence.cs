namespace Probe
{
    public class Precedence
    {
        public int M(int a, int b, int c, int d, int e)
        {
            var x = a + b * c;
            var y = a * b % c;
            var z = a << b + c;
            var w = a * b + c * d % e;
            var v = a + b - c;
            var u = a * b / c;
            return x + y + z + w + v + u;
        }

        public bool N(bool a, bool b, bool c, int v)
        {
            var p = a || b && c || v is > 1 and < 5 or 10 && a;
            var q = a && b && c;
            return p && q || a;
        }

        public bool P(int v) => v is > 1 and < 5 or 10;
    }
}
