namespace Probe
{
    public class Conditionals
    {
        public int OneLine(bool c, int a, int b) => c ? a : b;

        public int Expanded(bool c, int a, int b)
        {
            var x = c
                ? a
                : b;
            return x;
        }

        public int PartlyWrapped(bool c, int a, int b)
        {
            var x = c ? a :
                b;
            return x;
        }

        public int QuestionShares(bool c, int a, int b)
        {
            var x = c
                ? a : b;
            return x;
        }

        public int ConditionShares(bool c, int a, int b)
        {
            var x = c ? a
                : b;
            return x;
        }

        public int EndOfLine(bool c, int a, int b)
        {
            var x = c ?
                a :
                b;
            return x;
        }

        public int Unaligned(bool c, int a, int b)
        {
            var x = c
                ? a
                    : b;
            return x;
        }

        public int NotIndented(bool c, int a, int b)
        {
            var x = c
            ? a
            : b;
            return x;
        }

        public string AlignedChain(string p)
        {
            var x = p == "Cat" ? "Animals" :
                    p == "Bat" ? "Equipment" :
                    "None";
            return x;
        }

        public string NestedFalse(bool c, bool d)
        {
            var x = c
                ? "a"
                : d
                    ? "b"
                    : "c";
            return x;
        }

        public int MultiLineCondition(bool c, bool d, int a, int b)
        {
            var x = c &&
                    d
                ? a
                : b;
            return x;
        }

        public int LongBranch(bool c, int a, int b)
        {
            var x = c
                ? a +
                    b
                : 0;
            return x;
        }
    }
}
