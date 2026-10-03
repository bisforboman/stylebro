namespace Messy.Spacing
{
    public class Spacing
    {
        public int Count(int[] items)
        {
            var total = 0;

            foreach (var item in items)
            {
                total += item;
            }

            if (total > 10)
            {
                total = 10;
            }

            return total;
        }
    }
}
