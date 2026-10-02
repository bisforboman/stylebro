namespace P
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class Lambdas
    {
        private Func<int, int> twice = delegate (int x) { return x * 2; };

        public event EventHandler Changed;

        public void Run(Func<int> f)
        {
        }

        public void Run(Expression<Func<int>> f)
        {
        }

        public void M(List<int> items, object sender)
        {
            Action a = delegate { };
            Action b = delegate() { };
            Func<int, int, int> add = delegate (int x, int y) { return x + y; };
            this.Changed += delegate { Console.WriteLine(); };
            var c = (Action)delegate { };
            items.ForEach(delegate (int item) { Console.WriteLine(item); });
            this.Run(delegate { return 1; });
            this.Changed += delegate (object s, EventArgs e)
            {
                Console.WriteLine(s);
            };
        }
    }
}
