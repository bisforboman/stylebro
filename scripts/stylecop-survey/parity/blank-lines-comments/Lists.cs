namespace Probe
{
    using System.IO;

    public class Lists
    {
        // A comment between the items of a list (Chickensoft's GameDemo): only StyleCop reports it.
        public string Join(string directory)
        {
            var items = new[]
            {
                "a",
                // the second
                "b",
            };
            return Path.Combine(
                directory,
                // the file
                items[0]);
        }

        public int Add(
            int a,
            // the second
            int b) => a + b;
    }
}
