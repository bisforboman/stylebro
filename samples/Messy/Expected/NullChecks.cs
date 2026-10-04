namespace Messy.NullChecks
{
    public class NullChecks
    {
        private object _entry;

        public bool IsEmpty => _entry == null;

        public object Get(object fallback)
        {
            if (_entry != null && fallback != null)
            {
                return _entry;
            }

            return fallback;
        }
    }
}
