namespace P
{
    using System;
    using System.Collections.Generic;

    public class Tuples
    {
        public ValueTuple<int, string> Field;

        public System.ValueTuple<int, ValueTuple<int, int>> Nested(List<ValueTuple<int, int>> items) => default;

        public ValueTuple<int, int>[] Array;

        public ValueTuple<int, int>? Maybe;

        public (int Count, string Name) Get() => (1, "a");

        public void M(int x, (int a, int b)? maybe)
        {
            ValueTuple<long, long> local = default(ValueTuple<long, long>);
            var type = typeof(ValueTuple<int, int>);
            var a = new ValueTuple<int, string>(1, "a");
            var b = ValueTuple.Create(1, 2L);
            var c = ValueTuple.Create(x, 1);
            var d = new ValueTuple<int, int>();
            var t = this.Get();
            var e = t.Item1 + t.Item2.Length;
            var f = maybe?.Item2;
            var g = nameof(t.Item1);
        }
    }
}
