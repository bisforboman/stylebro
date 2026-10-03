namespace Messy.Visibility
{
    class Cache
    {
        int hits;

        public int Hits => hits;

        void Reset()
        {
            hits = 0;
        }
    }

    public partial class Order
    {
    }

    partial class Order
    {
    }
}
