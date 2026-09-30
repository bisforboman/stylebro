namespace Probe
{
    using System;
    using System.Linq;

    public record Person(string Name, int Age);

    public class Primary(int Count)
    {
        public int Get() => Count;
    }

    public interface IThing
    {
        void Do(int Value);
    }

    public abstract class Base
    {
        public abstract void Run(int Speed);

        public virtual void Walk(int Pace)
        {
        }
    }

    public class Derived : Base, IThing
    {
        public override void Run(int Speed)
        {
        }

        public void Do(int Value)
        {
        }

        [System.Runtime.InteropServices.DllImport("x")]
        public static extern void Native(int Handle);

        public void Method(int Good, int _, int __, int _x, int x1, int Été)
        {
            int Local = 1;
            int ___ = 3;
            var (A, b) = (1, 2);
            foreach (var Item in new[] { 1 })
            {
            }

            try
            {
            }
            catch (Exception Ex)
            {
            }

            if (int.TryParse("1", out var Parsed))
            {
            }

            if (Local is int Pattern)
            {
            }

            using var Disposable = (IDisposable)null;
            Func<int, int> f = Arg => Arg;
            Func<int, int, int> g = (_, _) => 0;
            var q = from Row in new[] { 1 } let Doubled = Row * 2 select Doubled;
            const int ConstLocal = 1;
            int LocalFunc(int Param) => Param;
            for (int I = 0; I < 1; I++)
            {
            }

            Action<int> h = delegate(int Anon) { };
            this.Method(Good: 1, _: 2, __: 3, _x: 4, x1: 5, Été: 6);
        }

        public void Underscores()
        {
            int _local = 2;
        }

        public int this[int Index] => Index;

        public int Prop
        {
            set { var Temp = value; }
        }
    }
}
