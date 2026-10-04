namespace Probe
{
    using System;

    /// <summary>Words.</summary>
    public class Words
    {
        /// <summary>Initializes a new instance of the <see cref="Words"/> class.</summary>
        public Words()
        {
        }

        /// <summary>Creates a words object.</summary>
        /// <param name="x">The x.</param>
        public Words(int x)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Words"/> class representing <paramref name="b"/>.</summary>
        /// <param name="b">The b.</param>
        public Words(byte b)
        {
        }

        /// <summary>Initializes a new instance of Words with the given name.</summary>
        /// <param name="name">The name.</param>
        public Words(string name)
        {
        }

        /// <summary><para>Initializes a new instance of the <see cref="Words"/> class.</para></summary>
        /// <param name="d">The d.</param>
        public Words(double d)
        {
        }

        /// <summary><para>Creates a words object from a float.</para></summary>
        /// <param name="f">The f.</param>
        public Words(float f)
        {
        }

        /// <summary>Initializes the static members.</summary>
        static Words()
        {
        }

        /// <summary>Cleans up.</summary>
        ~Words()
        {
        }

        /// <summary>The name.</summary>
        public string Name { get; set; }

        /// <summary>Gets the size.</summary>
        public int Size { get; set; }

        /// <summary>Gets or sets the count.</summary>
        public int Count { get; }

        /// <summary>Sets the value.</summary>
        public int Value { get; set; }

        /// <summary>Gets or sets the private-set thing.</summary>
        public int PrivateSet { get; private set; }

        /// <summary>Gets a value indicating whether it is open.</summary>
        public bool IsOpen { get; }

        /// <summary>Is it closed.</summary>
        public bool IsClosed { get; set; }

        /// <summary>Sets only.</summary>
        public int WriteOnly { set { } }

        /// <summary>Does nothing.</summary>
        /// <returns>Nothing.</returns>
        public void Nothing()
        {
        }

        /// <summary>Adds.</summary>
        /// <param name="a">The a.</param>
        /// <param name="old">Removed parameter.</param>
        /// <param name="b">The b.</param>
        /// <returns>The sum.</returns>
        public int Add(int a, int b) => a + b;

        /// <summary>Swaps.</summary>
        /// <param name="b">The b.</param>
        /// <param name="a">The a.</param>
        public void Swapped(int a, int b)
        {
        }

        /// <summary>Renamed.</summary>
        /// <param name="value">The value.</param>
        public void Renamed(int input)
        {
        }

        /// <summary>No name.</summary>
        /// <param>The a.</param>
        public void NoName(int a)
        {
        }

        /// <summary>Placeholder.</summary>
        /// <placeholder>TODO</placeholder>
        public void Placeholder()
        {
        }

        /// <summary><placeholder>Inline placeholder.</placeholder></summary>
        public void InlinePlaceholder()
        {
        }
    }

    /// <summary>A struct.</summary>
    public struct Point
    {
        /// <summary>Makes a point.</summary>
        /// <param name="x">The x.</param>
        public Point(int x)
        {
        }
    }

    /// <summary>A generic.</summary>
    /// <typeparam name="T">The type.</typeparam>
    public class Generic<T>
    {
        /// <summary>Makes one.</summary>
        public Generic()
        {
        }
    }
}
