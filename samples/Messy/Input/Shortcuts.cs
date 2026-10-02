namespace Messy.Helpers
{
    public static class Clock
    {
        public static long Ticks => 0;
    }
}

namespace Messy.Shortcuts
{
    using System;
    using System.Collections.Generic;
    using static Helpers.Clock;

    public class Shortcuts
    {
        public event EventHandler? Changed;

        public long Timeout { get; } = (long)30000;

        public decimal Precise { get; } = (decimal)1.50;

        public List<ValueTuple<string, int>> Counts { get; } = new List<ValueTuple<string, int>>();

        public (string Name, int Count) Top() => ValueTuple.Create("none", 0);

        public string Describe()
        {
            var top = Top();
            return top.Item1 + ": " + top.Item2 + " at " + Ticks;
        }

        public void Wire(List<int> items)
        {
            Changed += delegate { Console.WriteLine("changed"); };
            items.RemoveAll(delegate (int item) { return item < 0; });
        }
    }
}
