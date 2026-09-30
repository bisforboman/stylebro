namespace Probe
{
    using System;
    using System.Collections.Immutable;
    using System.Threading;

    public struct Plain { public int X; }

    public struct WithConstructor
    {
        public int X;
        public WithConstructor() { X = 1; }
    }

    public class Creations
    {
        public void M<T>(Guid p = new Guid(), CancellationToken ct = new CancellationToken(), Color col = new Color()) where T : struct
        {
            var a = new int();
            var b = new DateTime();
            var c = new CancellationToken();
            var d = new Guid();
            var e = new Plain();
            var f = new WithConstructor();
            var g = new T();
            Plain h = new();
            var i = new Plain { X = 1 };
            var j = new Plain() { };
            var k = new int?();
            var l = new Color();
            var m = new IntPtr();
            var n = new ImmutableArray<int>();
            var o = new Creations();
            var q = new Plain().X;
            new Plain();
        }
    }
}
