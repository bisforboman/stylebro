namespace Probe
{
    public class Fields
    {
        public const int lowerConst = 1;
        private const int privateLowerConst = 1;
        private const int _underscoreConst = 1;
        public static readonly int lowerStaticReadonly = 1;
        private static readonly int privateLowerStaticReadonly = 1;
        public readonly int publicLowerReadonly = 1;
        protected readonly int protectedLowerReadonly = 1;
        protected internal readonly int protectedInternalLowerReadonly = 1;
        internal readonly int internalLowerReadonly = 1;
        public int publicLower;
        internal int internalLower;
        protected int protectedLower;
        public int PublicUpper;
        public const int UpperConst = 1;

        public enum Kind
        {
            lowerMember,
        }

        public int Use() => lowerConst + privateLowerConst + _underscoreConst + lowerStaticReadonly
            + privateLowerStaticReadonly + publicLowerReadonly + protectedLowerReadonly + protectedInternalLowerReadonly
            + internalLowerReadonly + publicLower + internalLower + protectedLower + PublicUpper + UpperConst;
    }

    // StyleCop's rules skip '*NativeMethods' classes, except SA1311 (static readonly).
    internal static class UnsafeNativeMethods
    {
        internal const int max_path = 260;
        internal static int dwFlags;
        internal static readonly int invalidHandle = -1;
        internal static readonly int readonlyLower = 0;

        internal struct POINT
        {
            public int x;
        }

        internal static int Use(POINT p) => max_path + dwFlags + invalidHandle + readonlyLower + p.x;
    }
}