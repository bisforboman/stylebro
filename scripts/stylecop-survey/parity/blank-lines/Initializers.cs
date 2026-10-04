namespace Probe3
{
    using System.Collections.Generic;

    public class Initializers
    {
        // A blank line grouping the entries of an initializer (StyleCop #2832): only StyleCop reports these.
        public Dictionary<string, int> Ports { get; } = new Dictionary<string, int>
        {
            { "http", 80 },
            { "https", 443 },

            { "ssh", 22 },
        };

        public int[,] Grid { get; } = new int[,]
        {
            { 1, 2 },

            { 3, 4 },
        };

        // The initializer's own brace is still reported.
        public List<int> Numbers { get; } = new List<int>

        {
            1,
        };
    }
}
