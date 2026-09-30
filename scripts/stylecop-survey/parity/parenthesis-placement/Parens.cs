namespace Probe
{
    using System;

    [AttributeUsage(AttributeTargets.All)]
    public class MarkAttribute : Attribute
    {
        public MarkAttribute(int a = 0, int b = 0)
        {
        }
    }

    public class Parens
    {
        public void Method
            (int a, int b)
        {
        }

        public void Closing(
            int a,
            int b
            )
        {
        }

        public void ClosingComment(
            int a,
            int b // last
            )
        {
        }

        public int this
            [int i] => i;

        public int this[int i,
            string s
            ] => i;

        public Parens
            ()
        {
        }

        public void Empty(
            )
        {
        }

        [Mark(
            1,
            2
            )]
        public void Calls()
        {
            Method
                (1, 2);
            Method(
                1,
                2
                );
            var x = new Parens
                ();
            var y = new Parens(
                );
            Func<int, int> f = (
                v
                ) => v;
            var arr = new int[3];
            var z = arr[
                1
                ];
            Method(1, Math.Max(
                1,
                2
                ));
        }
    }
}
