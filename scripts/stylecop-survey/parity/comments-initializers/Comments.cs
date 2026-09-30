namespace Probe
{
    public class Comments
    {
        //no space
        //  two spaces
        //
        ////commented out code
        //---------------------
        //	tab
        // fine
        public void M()
        {
            M(); //trailing
            //TODO: later
            //https://example.com
            ///not a doc comment position
            /**/
        }
#if NEVER
        //disabled
#endif
    }
}
