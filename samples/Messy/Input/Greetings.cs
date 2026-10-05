namespace Messy.Greetings
{
    public class Greetings
    {
        private readonly string _prefix = "Hello";

        public string For(string? name)
        {
            if (name is null)
            {
                return _prefix! + "!";
            }

            return _prefix + ", " + name!.Trim() + "!";
        }

        public string Anyone(string? name) => name!.Trim();

        public string Nobody() => null!;
    }
}
