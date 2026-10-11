namespace Messy.CallChains
{
    public class CallChains
    {
        public int[] Doubled(int[] items) =>
            items
                .Where(i => i > 0)
                .Select(i => i * 2)
                .OrderBy(i => i)
                .ToArray();

        public int Count(int[] items)
        {
            var limit = 2;

            // BRO1504: a comment between the links needs no blank line above it.
            return items
                .Where(i => i > 0)
                // only the first ones
                .Take(limit)
                .Count();
        }

        public string Describe(string name) =>
            name.Trim()
                .ToUpperInvariant()
                .Replace(" ", "_");
    }
}
