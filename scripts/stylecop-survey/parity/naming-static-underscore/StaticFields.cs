namespace Probe
{
    public class StaticFields
    {
        private int count;
        private int other;
        private static int instances;
        private static int _created;
        private static int Upper;
        private static int __twice;
        private static readonly int Limit = 1;
        private const int Max = 2;
        internal static int shared;
        protected static int inherited;

        public int Sum() => count + other + instances + _created + Upper + __twice + Limit + Max + shared + inherited;
    }

    internal static class NativeMethods
    {
        private static int handle;

        public static int Get() => handle;
    }
}
