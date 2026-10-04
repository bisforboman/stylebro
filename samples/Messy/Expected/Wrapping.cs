namespace Messy.Wrapping
{
    public class Wrapping
    {
        private readonly int _limit =
            10;

        public int Limit
            => _limit * 2;

        public bool Allowed(bool admin, bool owner, int count)
            => admin
                || (owner
                && count < _limit);

        public string Describe(int count)
        {
            var label =
                count > _limit
                    ? "many"
                    : "few";
            return label
                + " (" + count + ")";
        }
    }
}
