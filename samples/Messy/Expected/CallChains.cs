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

        public string Describe(string name) =>
            name.Trim()
                .ToUpperInvariant()
                .Replace(" ", "_");
    }
}
