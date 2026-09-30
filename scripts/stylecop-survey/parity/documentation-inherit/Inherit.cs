namespace Probe
{
    using System;

    /// <summary>A shape.</summary>
    public interface IShape
    {
        /// <summary>Gets the area.</summary>
        /// <returns>The area.</returns>
        double Area();

        /// <summary>Gets the name.</summary>
        string Name { get; }

        /// <summary>Raised on change.</summary>
        event EventHandler Changed;
    }

    /// <summary>A base.</summary>
    public abstract class Base
    {
        /// <summary>Draws.</summary>
        public abstract void Draw();

        /// <summary>Gets the size.</summary>
        protected virtual int Size => 1;
    }

    /// <summary>A circle.</summary>
    public class Circle : Base, IShape, IDisposable
    {
        public string Name => "circle";

        public event EventHandler Changed;

        protected override int Size => 2;

        public double Area() => 1;

        public override void Draw()
        {
        }

        void IDisposable.Dispose()
        {
        }

        public override string ToString() => Name;

        /// <summary>Documented.</summary>
        public override int GetHashCode() => 1;

        // Just a comment.
        public override bool Equals(object obj) => false;

        public void NotAnOverride()
        {
        }

        private void Helper()
        {
            /// This is a comment written with three slashes.
            var x = 1;
        }
    }

    internal class Hidden : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
