namespace Messy.Rates
{
    public class Rates
    {
        public int Total(int price, int count, int fee) => (price * count) + fee;

        public bool CanEdit(bool admin, bool owner, bool locked) => admin || (owner && !locked);
    }
}
