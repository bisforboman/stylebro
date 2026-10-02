namespace Messy.Directives
{
    public class Flags
    {
# if DEBUG
        public bool Verbose = true;
# else
        public bool Verbose = false;
# endif
    }
}
