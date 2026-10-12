namespace Messy.CallChains
{
    public class CallChains
    {
        public int[] Doubled(int[] items) =>
            items
                .Where(i => i > 0).Select(i => i * 2)
                .OrderBy(i => i).ToArray();

        public int Count(int[] items)
        {
            var limit = 2;
            // BRO1504: a comment between the links needs no blank line above it.
            return items
                .Where(i => i > 0)
                // only the first ones
                .Take(limit).Count();
        }

        public string Describe(string name) =>
            name.Trim()
                .ToUpperInvariant().Replace(" ", "_");

        // A first line with two or more calls fills its lines: left alone.
        public string Shout(string name) =>
            name.Trim().ToUpperInvariant()
                .Replace(" ", "_").Trim();
    }
}
