namespace Probe
{
    /// <summary>Words.</summary>
    public class Properties
    {
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

        /// <summary>Gets or sets the internal-set thing.</summary>
        public int InternalSet { get; internal set; }

        /// <summary>Gets or sets the protected-set thing.</summary>
        public int ProtectedSet { get; protected set; }

        /// <summary>Gets or sets the init thing.</summary>
        public int InitThing { get; init; }

        /// <summary>Gets or initializes the set thing.</summary>
        public int SetThing { get; set; }
    }
}