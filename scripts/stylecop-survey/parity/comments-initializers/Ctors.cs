namespace Probe
{
    public class Base
    {
        public Base() { }

        public Base(int x) { }
    }

    public class Ctors : Base
    {
        public Ctors() : base() { }

        public Ctors(int x) : this()
        {
        }

        public Ctors(string s)
            : base(1)
        {
        }

        public Ctors(long l) :
            base(2)
        {
        }

        public Ctors(double d) : base(
            3)
        {
        }

        public Ctors(float f) /* why */ : this() { }

        public Ctors(byte b) : this() => System.Console.WriteLine();
    }

    public class Primary(int x) : Base(x)
    {
    }
}
