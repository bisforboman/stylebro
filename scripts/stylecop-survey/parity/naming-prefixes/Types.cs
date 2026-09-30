namespace Probe
{
    using System;
    using System.Runtime.InteropServices;

    public interface Shape
    {
        double Area();
    }

    public interface IGood
    {
    }

    public interface iLower
    {
    }

    public interface Item
    {
    }

    internal interface _Hidden
    {
    }

    [ComImport]
    [Guid("00000000-0000-0000-0000-000000000001")]
    public interface ComThing
    {
    }

    public partial interface Split
    {
    }

    public partial interface Split
    {
    }

    public class Circle : Shape
    {
        public double Area() => 1;
    }

    public class Box<Item, TGood, t, T1, _x>
    {
        public Item Value;

        public TOut Map<Result, TOut>(Result r) => default;
    }

    public delegate TResult Handler<Arg, TResult>(Arg a);

    public class Outer
    {
        public interface Nested
        {
        }

        public void M<Value>(Value v)
        {
            void Local<Inner>(Inner i)
            {
            }
        }
    }
}
