namespace Probe
{
    using System.Collections.Generic;

    /// <summary>Keeps a <see cref="List&lt;T&gt;"/> of names.</summary>
    /// <seealso cref="Dictionary&lt;TKey, TValue&gt;"/>
    public class Names
    {
        /// <summary>Calls <see cref="Convert&lt;T&gt;(List&lt;List&lt;T&gt;&gt;)"/>.</summary>
        /// <exception cref="KeyNotFoundException">Never.</exception>
        public void Run()
        {
        }

        /// <summary>Converts <see cref="IEnumerable{T}"/> values.</summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items.</param>
        /// <returns>The first.</returns>
        public T Convert<T>(List<List<T>> items) => items[0][0];

        /// <summary>Compares with <see cref="operator &lt;(Names, Names)"/>.</summary>
        /// <param name="a">The first.</param>
        /// <param name="b">The second.</param>
        /// <returns>Always false.</returns>
        public static bool operator <(Names a, Names b) => false;

        /// <summary>The opposite of <see cref="op_LessThan"/>.</summary>
        /// <param name="a">The first.</param>
        /// <param name="b">The second.</param>
        /// <returns>Always false.</returns>
        public static bool operator >(Names a, Names b) => false;

        /// <summary>Not a type: <c>List&lt;T&gt;</c>, and text List&lt;T&gt;.</summary>
        /// <code>var x = new List&lt;int&gt;();</code>
        public void Text()
        {
        }

        /// <summary>A type argument, not a parameter: <see cref="List&lt;int&gt;"/>.</summary>
        public void Closed()
        {
        }

        /// <inheritdoc cref="Convert&lt;T&gt;(List&lt;List&lt;T&gt;&gt;)"/>
        public void Inherited()
        {
        }
    }
}
