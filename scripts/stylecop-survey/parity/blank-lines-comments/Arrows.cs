namespace Probe
{
    using System;

    public class Arrows
    {
        // A comment right after '=>' (StyleCop #3392, #3550): only StyleCop reports these.
        public int Arm(int x) => x switch
        {
            2 =>
                // quoting from somewhere
                20,
            _ => 0,
        };

        public Func<int> Lambda() => () =>
            // a lambda body
            1;

        public int Body =>
            // an expression body
            1;
    }
}
