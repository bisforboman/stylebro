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
    using static Messy.Helpers.Clock;

    public class Shortcuts
    {
        public event EventHandler? Changed;

        public long Timeout { get; } = 30000L;

        public decimal Precise { get; } = (decimal)1.50;

        public ulong Mask { get; } = 0xFFUL;

        public long Retries { get; } = 5L;

        public List<(string, int)> Counts { get; } = new List<(string, int)>();

        public (string Name, int Count) Top() => ("none", 0);

        public string Describe()
        {
            var top = Top();
            return top.Name + ": " + top.Count + " at " + Ticks;
        }

        public void Wire(List<int> items)
        {
            Changed += (sender, e) => { Console.WriteLine("changed"); };
            items.RemoveAll(item => { return item < 0; });
        }
    }
}
