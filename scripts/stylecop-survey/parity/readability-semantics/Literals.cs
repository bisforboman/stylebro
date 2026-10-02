namespace P
{
    public class Literals
    {
        public long A = (long)1;
        public ulong B = (ulong)1;
        public uint C = (uint)1;
        public float D = (float)1;
        public double E = (double)1;
        public decimal F = (decimal)1;
        public long G = (long)-1;
        public long H = (long)(1);
        public long I = (long)0x10;
        public int J = (int)1L;
        public float K = (float)0.5;
        public decimal L = (decimal)0.1234567890123456789;
        public decimal M = (decimal)1.50;
        public long N = (long)+1;
        public int O = (int)1;
        public long Q = (long)(1 + 1);
        public short R = (short)1;

        public long Minus(long a) => a - (long)-1;
    }
}
