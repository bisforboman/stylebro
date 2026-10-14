namespace Probe
{
    /// <summary>
    /// Inline elements.
    /// </summary>
    public class Inline
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Inline"/> class.
        /// <paramref name="name">The name of the request.</paramref>
        /// </summary>
        /// <param name="name">The name.</param>
        public Inline(string name)
        {
        }

        /// <summary>
        /// Ends with <see cref="Inline">a sentence in a see.</see>
        /// </summary>
        public int A { get; }

        /// <summary>
        /// Ends with <c>code ending with a period.</c>
        /// </summary>
        public int B { get; }

        /// <summary>
        /// Ends with <paramref name="x">a question?</paramref>
        /// </summary>
        /// <param name="x">The x.</param>
        public void C(int x)
        {
        }

        /// <summary>
        /// Ends with <c>no period</c>
        /// </summary>
        public int D { get; }

        /// <summary>
        /// Consumes a token and processes it.0
        /// </summary>
        public int E { get; }

        /// <summary>
        /// Supports HTML 5.0
        /// </summary>
        public int F { get; }
    }
}
