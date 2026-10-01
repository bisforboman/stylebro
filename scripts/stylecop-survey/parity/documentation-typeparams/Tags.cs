namespace Probe
{
    /// <summary>Methods.</summary>
    public class Methods
    {
        /// <summary>Unnamed param.</summary>
        /// <param>The a.</param>
        public void Unnamed(int a)
        {
        }

        /// <summary>Unnamed with others.</summary>
        /// <param name="a">The a.</param>
        /// <param>The b.</param>
        public void UnnamedSecond(int a, int b)
        {
        }

        /// <summary>Two unnamed.</summary>
        /// <param>The a.</param>
        /// <param>The b.</param>
        public void TwoUnnamed(int a, int b)
        {
        }

        /// <summary>Empty name.</summary>
        /// <param name="">The a.</param>
        public void EmptyName(int a)
        {
        }

        /// <summary>Stale type parameter.</summary>
        /// <typeparam name="TOld">The type.</typeparam>
        public void StaleType<T>()
        {
        }

        /// <summary>Swapped type parameters.</summary>
        /// <typeparam name="TValue">The value.</typeparam>
        /// <typeparam name="TKey">The key.</typeparam>
        public void Swapped<TKey, TValue>()
        {
        }

        /// <summary>Unnamed type parameter.</summary>
        /// <typeparam>The type.</typeparam>
        public void UnnamedType<T>()
        {
        }

        /// <summary>Extra type parameter tag.</summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <typeparam name="TGone">Gone.</typeparam>
        public void Extra<T>()
        {
        }

        /// <summary>A delegate.</summary>
        /// <typeparam name="TWrong">The type.</typeparam>
        public delegate void Handler<T>(T value);
    }

    /// <summary>A generic type.</summary>
    /// <typeparam name="TOld">The type.</typeparam>
    public class Box<T>
    {
    }

    /// <summary>Swapped on a type.</summary>
    /// <typeparam name="TB">The b.</typeparam>
    /// <typeparam name="TA">The a.</typeparam>
    public interface IPair<TA, TB>
    {
    }

    /// <summary>Unnamed on a type.</summary>
    /// <typeparam>The type.</typeparam>
    public struct Holder<T>
    {
    }
}