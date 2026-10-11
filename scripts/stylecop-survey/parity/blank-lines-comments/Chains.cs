namespace Probe
{
    using System.Linq;

    public class Chains
    {
        // A comment between the links of a call chain (davidfowl/TodoApi): only StyleCop reports it.
        public int Count(int[] items, string text)
        {
            var count = items
                .Where(i => i > 0)
                // only the first two
                .Take(2)
                .Count();
            return text
                // may be null
                ?.Length ?? count;
        }
    }
}
