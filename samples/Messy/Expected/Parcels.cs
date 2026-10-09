namespace Messy.Parcels
{
    public class Parcels
    {
        private readonly List<int> _weights = [];

        public void Add(int weight, bool fragile)
        {
            if (weight > 0 && (fragile || weight > 30))
            {
                _weights.Add(-weight);
            }

            if (weight > 0 && !fragile)
            {
                foreach (var other in _weights.ToArray())
                {
                    if (other > weight && other < 2 * weight)
                    {
                        _weights.Add(weight);
                    }
                }
            }
        }

        public int Heavy() => _weights.Count(w => w > 30);

        public bool AnyFragile() =>
            _weights
                .Any(w => w < 0);

        public int Total() => Sum(0, 1, 2) + Sum(_weights.Count);

        private static int Sum(int first, params int[] rest) => first + rest.Sum();
    }
}
