namespace Messy.Parcels
{
    public class Parcels
    {
        private readonly List<int> _weights = [];

        public void Add(int weight, bool fragile)
        {
            if (weight > 0)
            {
                if (fragile || weight > 30)
                    _weights.Add(-weight);
            }

            if (weight > 0)
            {
                if (!fragile)
                {
                    foreach (var other in _weights.ToArray())
                    {
                        if (other > weight)
                        {
                            if (other < 2 * weight)
                            {
                                _weights.Add(weight);
                            }
                        }
                    }
                }
            }
        }

        public int Heavy() => _weights.Where(w => w > 30).Count();

        public bool AnyFragile() =>
            _weights
                .Where(w => w < 0)
                .Any();

        public int Total() => Sum(0, new[] { 1, 2 }) + Sum(_weights.Count, new int[] { });

        private static int Sum(int first, params int[] rest) => first + rest.Sum();
    }
}
