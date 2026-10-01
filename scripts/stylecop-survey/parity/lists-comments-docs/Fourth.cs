namespace Probe4
{
    /// <summary>Four.</summary>
    public class Four
    {
        /// <summary>
        ///   Indented text.
        ///   <para>Indented tag.</para>
        ///<para>No space before a tag.</para>
        ///No space before text.
        ///  <see cref="Four"/> starts with a tag.
        /// </summary>
        public void A()
        {
        }

#pragma warning disable CS1591
        /// <summary>After pragma.</summary>
        public void B()
        {
        }
#pragma warning restore CS1591

#if DEBUG
        /// <summary>After if.</summary>
        public void C()
        {
        }
#else
        /// <summary>After else.</summary>
        public void D()
        {
        }
#endif
        /// <summary>After endif.</summary>
        public void E()
        {
        }

        #region R
        /// <summary>After region.</summary>
        public void F()
        {
        }
        #endregion
        /// <summary>After endregion.</summary>
        public void G()
        {
        }
    }
}
