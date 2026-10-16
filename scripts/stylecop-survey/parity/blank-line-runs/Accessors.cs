namespace Probe
{
    using System.Runtime.CompilerServices;

    public class Accessors
    {
        private int f;

        public int WithAttribute
        {
            get
            {
                var x = this.f;
                return x;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.f = value;
        }
    }
}
