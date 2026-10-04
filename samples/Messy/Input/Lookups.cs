namespace Messy.Lookups
{
    public class Lookups
    {
        public string Find(string key, string fallback)
        {
            if (null == key || key == "")
            {
                return fallback;
            }

            return fallback != null ? fallback + key : key;
        }
    }
}
