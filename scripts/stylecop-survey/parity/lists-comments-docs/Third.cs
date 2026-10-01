namespace Probe3
{
    using System;

    /// <summary>Tag.</summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class MarkAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="MarkAttribute"/> class.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        public MarkAttribute(int a, int b)
        {
        }
    }

    /// <summary>Third.</summary>
    public class Third
    {
        /// <summary>The field.</summary>
        private int f;
        /// <summary>Right after a field.</summary>
        public int G { get; set; }

        /// <summary>Empty declaration list.</summary>
        public void Declared(
            )
        {
        }

        /// <summary>Comma first in declaration.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        [Mark(1
            , 2)]
        public void Commas(int a
            , int b)
        {
        }

        /// <summary>Blank in attribute args.</summary>
        [Mark(

            1,

            2)]
        public void AttributeBlank()
        {
        }

        /// <summary>Indexer.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The value.</returns>
        public int this[

            int a,

            int b] => a;


        /// <summary>Two blank lines before the element.</summary>


        public void TwoBlank()
        {
        }
    }
}
