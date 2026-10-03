namespace Probe
{
    using System;

    class Outer : IDisposable
    {
        int count, other;

        static readonly int Max = 1;

        event EventHandler Changed;

        Outer()
        {
            this.Changed?.Invoke(this, EventArgs.Empty);
        }

        static Outer()
        {
        }

        int Size { get; set; }

        int this[int i] => i + this.count + this.other + Max;

        void Run()
        {
        }

        void IDisposable.Dispose()
        {
        }

        class Nested
        {
        }

        delegate void Handler();

        partial class Part
        {
        }
    }

    public partial class Widget
    {
    }

    partial class Widget
    {
        partial void OnCreated();
    }

    struct Point
    {
    }

    enum Kind
    {
        A,
    }

    interface IShape
    {
        void Draw();
    }
}
