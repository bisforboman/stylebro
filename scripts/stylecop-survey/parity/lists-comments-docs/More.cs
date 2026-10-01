namespace Probe2
{
    using System;

    /// <summary>
    /// Text.
    /// <code>
    /// if (x)
    ///     y();
    /// </code>
    ///<para>No space.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class TagAttribute : Attribute
    {
        /// <summary>Creates it.</summary>
        /// <param name="a">The a.</param>
        public TagAttribute(int a = 0)
        {
        }
    }

    /// <summary>More.</summary>
    public class More
    {
        /// <summary>Attribute empty.</summary>
        [Tag(
)]
        public void Empty()
        {
            var x = new More(
            );
            int[] y = new int[1];
            var z = y[0
                ];
            this.Two(1
                , 2);
            this.Two(

                1, 2);
            // a
            //
            // b
            int u = 0; //
            ////
            //
            _ = u;
        }

        /// <summary>Two.</summary>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        public void Two(int a, int b)
        {
            {
                /// <summary>Not a member.</summary>
            }
        }

        // plain comment
        /// <summary>After a comment.</summary>
        public void AfterComment()
        {
        }

        #region R
        /// <summary>After a region.</summary>
        public void AfterRegion()
        {
        }
        #endregion

        /// <summary>Doc.</summary>

        [Obsolete]
        public void DocBlankAttribute()
        {
        }
    }
}
