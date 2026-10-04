namespace Messy.Wrapping
{
    public class Wrapping
    {
        private readonly int limit
            = 10;

        public int Limit
            => limit * 2;

        public bool Allowed(bool admin, bool owner, int count) =>
            admin ||
                owner &&
                count < limit;

        public string Describe(int count)
        {
            var label
                = count > limit ?
                    "many" :
                    "few";
            return label +
                " (" + count + ")";
        }
    }
}
