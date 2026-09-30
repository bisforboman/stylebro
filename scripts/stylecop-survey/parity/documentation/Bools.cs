namespace Probe
{
    /// <summary>Bools.</summary>
    public class Bools
    {
        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }

        /// <summary>Gets or sets the flag.</summary>
        public bool Flag { get; set; }

        /// <summary>The open state.</summary>
        public bool IsOpen { get; }

        /// <summary>Gets whether it is closed.</summary>
        public bool IsClosed { get; }

        /// <summary>Gets a value indicating whether it is ready.</summary>
        public bool IsReady { get; }

        /// <summary>
        /// Gets the summary with a nested remark.
        /// <remarks>Only once.</remarks>
        /// </summary>
        public int Nested { get; }

        /// <summary>Gets the parameter doc.</summary>
        /// <param name="x">The x.
        /// <remarks>Must be positive.</remarks></param>
        /// <returns>The result.</returns>
        public int M(int x) => x;
    }
}
