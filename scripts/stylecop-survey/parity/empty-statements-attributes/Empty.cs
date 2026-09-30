namespace Probe
{
    public class Empty
    {
        public void M(bool b)
        {
            M(b);
            ;
            M(b); ;
            if (b) ;
            while (b) ;
            for (;;) ;
            label: ;
            switch (b)
            {
                case true:
                    ;
                    break;
            }
            ; // trailing comment
        }
    };

}
