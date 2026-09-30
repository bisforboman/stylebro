namespace Probe
{
    public enum Color { Red, Green }

    public class Money
    {
        public static bool operator ==(Money a, Money b) => true;
        public static bool operator !=(Money a, Money b) => false;
    }

    public class Conditions
    {
        private const int Max = 5;
        private static readonly string Empty = "";

        public void M(int x, int y, object o, string s, Color c, Money m, int? n, bool b2, System.Guid g, System.DateTime d)
        {
            bool b;
            b = 1 == x;
            b = null != o;
            b = 0 < x;
            b = 0 <= x;
            b = Max > x;
            b = Color.Red == c;
            b = x == y;
            b = 1 == 2;
            b = "a" == s;
            b = string.Empty == s;
            b = Empty == s;
            b = null == m;
            b = 1 + 1 == x;
            b = nameof(x) == s;
            b = default == x;
            b = null == n;
            b = 1 == n;
            b = true == (1 == x);
            b = System.Guid.Empty == g;
            b = System.DateTime.MinValue < d;
            b = 1 ==
                x;
        }
    }
}
