namespace Probe
{
    using System;
    using System.Collections.Generic;

    public record Point(int X,
        int Y);

    public class Base
    {
        public Base(int a, int b)
        {
        }
    }

    public class Derived(int a,
        int b) : Base(a,
        b)
    {
        public delegate void Handler(object sender,
            EventArgs e);

        public static Derived operator +(Derived left,
            Derived right) => left;

        public void Local()
        {
            int Add(int x, int y,
                int z) => x + y + z;
            var d = new Derived(1,
                2);
            var e = new Derived(
                a: 1, b: 2);
            var t = (1,
                2);
            var map = new Dictionary<int,
                string>();
            Span<int> s = stackalloc int[3];
            Action<int, int> act = delegate(int x,
                int y) { };
            var arr = new int[2,
                3];
        }
    }
}