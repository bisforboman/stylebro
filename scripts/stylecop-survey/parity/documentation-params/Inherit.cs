namespace Probe
{
    /// <summary>A top-level inheritdoc turns off SA1612 (vs-validation's Requires.cs); a nested one doesn't.</summary>
    public class Inherit
    {
        /// <inheritdoc cref="Nested(int, int)"/>
        /// <param name="b">The b.</param>
        /// <param name="a">The a.</param>
        /// <param name="old">Old.</param>
        public void Inherited(int a, int b)
        {
        }

        /// <summary>Nested.</summary>
        /// <param name="b">The b. <inheritdoc cref="Inherited(int, int)"/></param>
        /// <param name="a">The a.</param>
        public void Nested(int a, int b)
        {
        }
    }
}
