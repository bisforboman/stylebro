namespace Messy.Lookups
{
    public class Lookups
    {
        public string Find(string key, string fallback)
        {
            if (key is null || key == string.Empty)
            {
                return fallback;
            }

            return fallback is not null ? fallback + key : key;
        }
    }
}
