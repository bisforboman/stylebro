// BRO1008: usings outside the namespace (the repository's .editorconfig); the comment moves along.
using System.Collections.Concurrent;
using System.Text;

namespace Messy.Shelves
{
    public class Shelf
    {
        private readonly ConcurrentBag<string> _items = new();

        public string Describe()
        {
            var builder = new StringBuilder();
            foreach (var item in _items)
            {
                builder.Append(item);
            }

            return builder.ToString();
        }
    }
}
