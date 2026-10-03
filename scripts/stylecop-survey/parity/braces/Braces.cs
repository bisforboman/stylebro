namespace Probe
{
    using System;
    using System.Collections.Generic;

    public class Braces
    {
        private readonly object gate = new object();

        public int Plain(bool a, List<int> items)
        {
            if (a)
                return 1;

            foreach (var item in items)
                a = !a;

            while (a)
                a = false;

            for (var i = 0; i < 3; i++)
                a = true;

            lock (this.gate)
                a = false;

            do
                a = !a;
            while (a);

            return 0;
        }

        public int Chains(int a)
        {
            if (a == 1) return 1;
            else if (a == 2) return 2;
            else return 3;
        }

        public int Inconsistent(int a)
        {
            if (a == 1)
            {
                return 1;
            }
            else
                return 2;
        }

        public int MultiLine(int a, int b)
        {
            if (a > 0)
                if (b > 0)
                    return Add(a,
                        b);

            return 0;
        }

        public void Usings(IDisposable x, IDisposable y)
        {
            using (x)
            using (y)
            {
            }

            using (x)
                Console.WriteLine();
        }

        public void Skipped(bool a)
        {
            if (a) // why
                a = false;

            if (a)
                Console.WriteLine(@"one
two");
        }

        private static int Add(int x, int y) => x + y;
    }
}
