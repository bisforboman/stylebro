namespace Probe
{
    using System;

    /// <summary>
    /// A holder
    /// </summary>
    public class Kinds
    {
        /// <summary>
        /// A field
        /// </summary>
        public int Field;

        /// <summary>
        /// A constant
        /// </summary>
        public const int Constant = 1;

        /// <summary>
        /// Occurs when a message is received
        /// </summary>
        public event EventHandler Received;

        /// <summary>
        /// Occurs when two things happen
        /// </summary>
        public event EventHandler First, Second;

        /// <summary>
        /// Occurs with accessors
        /// </summary>
        public event EventHandler WithAccessors
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Kinds"/> class
        /// </summary>
        public Kinds()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Kinds"/> class
        /// </summary>
        ~Kinds()
        {
        }

        /// <summary>
        /// A delegate
        /// </summary>
        /// <param name="value">The value</param>
        public delegate void Handler(int value);

        /// <summary>
        /// Gets the value
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Gets an item
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The item</returns>
        public int this[int index] => index;

        /// <summary>
        /// Adds two
        /// </summary>
        /// <param name="a">The first</param>
        /// <param name="b">The second</param>
        /// <returns>The sum</returns>
        public static Kinds operator +(Kinds a, Kinds b) => a;

        /// <summary>
        /// Converts
        /// </summary>
        /// <param name="a">The value</param>
        public static implicit operator int(Kinds a) => 0;

        /// <summary>
        /// Does it
        /// </summary>
        public void Method()
        {
            Received?.Invoke(this, EventArgs.Empty);
            First?.Invoke(this, EventArgs.Empty);
            Second?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// A choice
    /// </summary>
    public enum Choice
    {
        /// <summary>
        /// The first
        /// </summary>
        One,
    }

    /// <summary>
    /// A contract
    /// </summary>
    public interface IContract
    {
        /// <summary>
        /// Occurs in an interface
        /// </summary>
        event EventHandler Changed;
    }

    /// <summary>
    /// A record
    /// </summary>
    /// <param name="Name">The name</param>
    public record Person(string Name);
}
