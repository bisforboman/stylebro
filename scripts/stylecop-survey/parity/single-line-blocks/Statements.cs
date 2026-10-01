namespace Probe
{
    using System;

    public class Statements
    {
        public void If(bool b)
        {
            if (b) { return; }
            if (b) { }
            if (b) { Console.WriteLine(); Console.WriteLine(); }
            if (b) { return; } else { Console.WriteLine(); }
            if (b)
            {
                return;
            }
            else { Console.WriteLine(); }
            while (b) { b = false; }
            for (int i = 0; i < 1; i++) { b = true; }
            foreach (var c in "x") { b = true; }
            do { b = false; } while (b);
            using (var d = (IDisposable)null) { b = true; }
            lock (this) { b = true; }
            try { b = true; } catch { b = false; } finally { b = true; }
            checked { b = true; }
            { b = true; }
            if (b) { if (b) { return; } }
            if (b) { return; } // trailing
            if (b) { /* inner */ return; }
            switch (b) { case true: { b = false; break; } default: break; }
        }

        public void Lambdas()
        {
            Action a = () => { Console.WriteLine(); };
            Func<int> f = () => { return 1; };
            Action d = delegate { Console.WriteLine(); };
            Action e = () => { };
            void Local() { Console.WriteLine(); }
            void EmptyLocal() { }
        }
    }

    public class Empty { }

    public class OneLine { public int X; }

    public struct S { }

    public interface I { void M(); }

    public enum E { A, B }

    public class Members
    {
        private int x;

        public Members() { }

        public Members(int x) { this.x = x; }

        ~Members() { }

        public void M() { }

        public void N() { this.x = 1; }

        public int Auto { get; set; }

        public int AutoInit { get; set; } = 1;

        public int Get { get { return this.x; } }

        public int GetSet { get { return this.x; } set { this.x = value; } }

        public int Accessors
        {
            get { return this.x; }
            set { this.x = value; }
        }

        public int this[int i] { get { return i; } }

        public event EventHandler Ev { add { } remove { } }

        public event EventHandler Ev2
        {
            add { }
            remove { }
        }

        public static Members operator +(Members a, Members b) { return a; }

        public static implicit operator int(Members m) { return 0; }
    }

    namespace Inner { public class C { } }
}
