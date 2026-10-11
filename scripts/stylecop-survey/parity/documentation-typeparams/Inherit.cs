namespace Probe
{
    /// <summary>A top-level inheritdoc turns off SA1620 and SA1621, not SA1613.</summary>
    public class Inherit
    {
        /// <inheritdoc cref="Nested{T, U}(int, int)"/>
        /// <typeparam name="U">The U.</typeparam>
        /// <typeparam>The T.</typeparam>
        /// <param name="a">The a.</param>
        /// <param>The b.</param>
        public void Inherited<T, U>(int a, int b)
        {
        }

        /// <summary>Nested.</summary>
        /// <typeparam name="T">The T.</typeparam>
        /// <typeparam name="U">The U.</typeparam>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        public void Nested<T, U>(int a, int b)
        {
        }
    }
}
