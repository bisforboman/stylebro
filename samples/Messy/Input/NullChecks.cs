namespace Messy.NullChecks
{
    public class NullChecks
    {
        private object _entry;

        public bool IsEmpty => _entry is null;

        public object Get(object fallback)
        {
            if (_entry is not null && fallback is not null)
            {
                return _entry;
            }

            return fallback;
        }
    }
}
