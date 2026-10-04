using System;

namespace Messy.Attributes
{
    public class Legacy
    {
        public void Current()
        {
        }
        [Obsolete("Use Current.")]

        public void Old()
        {
        }
    }
}
