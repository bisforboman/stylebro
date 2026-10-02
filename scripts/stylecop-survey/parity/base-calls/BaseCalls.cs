namespace Probe
{
    public class Base
    {
        public int Count;

        public int this[int i] => i;

        public int Size { get; set; }

        public virtual string Describe() => "base";

        public virtual void Reset()
        {
        }

        public void Hidden()
        {
        }
    }

    public class Derived : Base
    {
        public int Total() => base.Count + base[1] + base.Size;

        public void Clear() => base.Reset();

        public override string Describe() => base.Describe() + "!";

        public new void Hidden() => base.Hidden();
    }

    public sealed class Leaf : Base
    {
        public string Text() => base.Describe();
    }
}
