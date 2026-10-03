namespace Probe
{
    using System;

    public class Blank
    {
        public int Value { get; set; }

        public int M(bool a, int[] items)
        {
            var x = 1;


            if (a)
            {
                x++;

            }
            foreach (var i in items)
            {
                x += i;
            }
            try
            {
                x++;
            }
            catch (Exception)
            {
            }
            do
            {
                x--;
            }
            while (x > 5);
            var c = new Blank
            {
                Value = 1,
            };
            Action f = () =>
            {
            };
            if (a)
            {
                x = 0;
            }
            // comment after a block
            x++;
            return x + c.Value;
        }
        public void N()
        {
        }
    }
}
