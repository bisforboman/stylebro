namespace Messy.Visibility
{
    public partial class Order
    {
    }

    public partial class Order
    {
    }

    public class Base
    {
        protected readonly int Limit = 1;

        protected int total;
    }

    internal class Cache
    {
        private int _hits;

        public int Hits => _hits;

        private void Reset()
        {
            _hits = 0;
        }
    }
}
