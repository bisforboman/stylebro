namespace probe.lower
{
    using System;

    public class lowerClass
    {
        public const int lowerConst = 1;
        private const int privateLowerConst = 1;
        public static readonly int lowerStaticReadonly = 1;
        private static readonly int privateLowerStaticReadonly = 1;
        public readonly int publicLowerReadonly = 1;
        protected readonly int protectedLowerReadonly = 1;
        protected internal readonly int protectedInternalLowerReadonly = 1;
        internal readonly int internalLowerReadonly = 1;
        public int publicLower;
        internal int internalLower;
        protected int protectedLower;

        public int lowerProperty { get; set; }

        public event EventHandler lowerEvent;

        public void lowerMethod()
        {
            void lowerLocalFunction()
            {
            }
        }

        private void privateLowerMethod()
        {
        }

        public int this[int i] => i;

        public static lowerClass operator +(lowerClass a, lowerClass b) => a;

        public enum lowerEnum
        {
            lowerMember,
            UpperMember,
        }

        public delegate void lowerDelegate();

        public struct lowerStruct
        {
        }

        public record lowerRecord(int X);

        public interface ILower
        {
            void lowerInterfaceMethod();
        }
    }

    public class Impl : lowerClass.ILower
    {
        public void lowerInterfaceMethod()
        {
        }
    }

    public class _Underscore
    {
        public void _underscoreMethod()
        {
        }
    }
}
