namespace Probe
{
    using System;

    /// <summary>Calls.</summary>
    public class Calls
    {
        ///<summary>No space after slashes.</summary>
        ///   <param name="a">Three spaces.</param>
        /// <param name="b">One space.</param>
        public void M(int a, int b)
        {
            this.N(
);
            this.N(
                );
            this.M(1
                , 2);
            this.M(1,
                2);
        }

        /// <summary>No arguments.</summary>
        public void N()
        {
        }

        /// <summary>Blank line first.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        public void Blank(

            int a,

            int b)
        {
            //
            // text
            //
            //    
            /* */
            do
            {
                a++;
            }

            while (a < b);
        }

        /// <summary>Comment first.</summary>
        /// <param name="a">The a.</param>
        public void CommentFirst(
            // the a
            int a)
        {
        }

        /// <summary>Doc then blank.</summary>

        public void DocBlank()
        {
        }
        /// <summary>Doc right after member.</summary>
        public void DocNoBlankBefore()
        {
        }

        /// <summary>Indexer.</summary>
        /// <param name="i">The i.</param>
        /// <returns>The value.</returns>
        public int this[int i
            ] => i;
    }

    /// <summary>Values.</summary>
    public enum Values { A, B, C }

    /// <summary>Values on lines.</summary>
    public enum Lines
    {
        /// <summary>A.</summary>
        A, B,

        /// <summary>C.</summary>
        C,
    }
}
