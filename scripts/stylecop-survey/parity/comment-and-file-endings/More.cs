using System;
// after usings, then blank

namespace ProbeMore
{
    public class C
    {
        public void M()
        {
            M(); // trailing, then blank

            M();
            // then blank, then a directive

#if !NEVER
            M();
#endif
            // then blank, then a block comment

            /* block */
            M();
            // last in block, then blank

        }
    }
}
