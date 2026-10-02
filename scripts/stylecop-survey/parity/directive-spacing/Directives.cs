# nullable enable
namespace Probe
{
    public class Directives
    {
#  region Members
        public int A;
#	endregion

# if DEBUG
        public int B;
# elif TRACE
        public int D;
# else
        public int E;
# endif

        public void M()
        {
            # pragma warning disable CS0168
            int unused;
            #pragma warning restore CS0168
            // # if in a comment
            var s = "# if";
        }
    }
}
