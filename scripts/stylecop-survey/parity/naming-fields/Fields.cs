namespace Probe
{
    using System;

    public class Fields
    {
        private int PrivateUpper;
        private int _privateUnderscore;
        private int __privateDouble;
        private int privateGood;
        private readonly int PrivateReadonlyUpper = 1;
        private readonly int _privateReadonlyUnderscore = 1;
        private static int PrivateStaticUpper;
        private static int _privateStaticUnderscore;
        private static readonly int privateStaticReadonlyLower = 1;
        private static readonly int _privateStaticReadonlyUnderscore = 1;
        private const int privateConstLower = 1;
        private const int _privateConstUnderscore = 1;
        protected int ProtectedUpper;
        protected int protectedLower;
        protected int _protectedUnderscore;
        protected readonly int protectedReadonlyLower = 1;
        internal int internalLower;
        internal int _internalUnderscore;
        public int publicLower;
        public int _publicUnderscore;
        public readonly int publicReadonlyLower = 1;
        private int m_hungarian;
        private int s_static;
        private int with_underscore;
        int DefaultAccessUpper;
        private event EventHandler PrivateEvent;
        private event EventHandler _privateEventUnderscore;

        public int Use() => PrivateUpper + _privateUnderscore + __privateDouble + privateGood + PrivateReadonlyUpper
            + _privateReadonlyUnderscore + PrivateStaticUpper + _privateStaticUnderscore + m_hungarian + s_static
            + with_underscore + DefaultAccessUpper;
    }

    public struct S
    {
        private int StructUpper;

        public int Get() => StructUpper;
    }
}
