using System;

namespace Probe.Html
{
    using Memory = System.ReadOnlyMemory<System.Char>;
    using Numbers = System.Collections.Generic.List<int>;
    using Texts = System.Collections.Generic.List<String>;
    using Pairs = System.Collections.Generic.Dictionary<System.String, System.Int32>;
    using Mixed = System.Collections.Generic.KeyValuePair<string, System.Int32>;

    public static class Aliases
    {
        public static int Count(Memory memory, Numbers numbers, Texts texts, Pairs pairs, Mixed mixed) => memory.Length + numbers.Count + texts.Count + pairs.Count + mixed.Value;
    }
}
